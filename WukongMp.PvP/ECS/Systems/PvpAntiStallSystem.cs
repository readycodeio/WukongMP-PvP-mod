using System;
using System.Collections.Generic;
using System.Numerics;
using b1;
using BtlShare;
using ReadyM.Api.Idents;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.PvP.Configuration;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.ECS.Systems;

public class PvpAntiStallSystem(PvpRpc rpc) : ModSystemBase
{
    private struct PlayerEngagementData
    {
        public FVector LastPosition;
        public FVector ForwardDirection;
        public int TeamId;
        public bool IsAttacking;
        public float CurrentHp;
        public float PrevHp;
    }

    private enum AntiStallState
    {
        Monitoring,
        Warning,
        Active
    }

    private AntiStallState _state = AntiStallState.Monitoring;

    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong _tickCounter;
    private float _elapsedTime;
    private bool _isReset;

    private float _warningTimer;
    private float _activeTimer;

    private float _roomEngagementScore;
    private readonly Dictionary<PlayerId, float> _playerEngagementMultipliers = [];
    private readonly Dictionary<PlayerId, PlayerEngagementData> _playerEngagement = [];
    private readonly Random _rng = new();

    private int _decayRounds;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue || !WukongApi.PvP.OwnsPvpState || !WukongApi.PvP.AntiStallEnabled)
            return;

        if (!WukongApi.PvP.InPvP)
        {
            ResetState();
            return;
        }

        _isReset = false;

        if (_tickCounter++ % TickInterval != 0)
        {
            _elapsedTime += tick.deltaTime;
            return;
        }

        foreach (var playerId in WukongApi.Sync.AreaPlayers)
        {
            if (!WukongApi.Sync.TryGetPlayerInfoById(playerId, out _, out var team))
                continue;

            if (!_playerEngagement.TryGetValue(playerId, out var data))
            {
                data = new PlayerEngagementData();
                _playerEngagement[playerId] = data;
            }

            var main = WukongApi.Sync.GetMainCharacterByPlayerId(playerId);
            if (!main.HasValue || main.Value.IsSpectator)
                continue;

            if (main.Value.Pawn is { } pawn)
            {
                data.LastPosition = pawn.GetActorLocation();
                data.ForwardDirection = pawn.GetActorForwardVector();
                data.TeamId = team.Value;
                data.IsAttacking = BGUFunctionLibraryCS.BGUHasUnitState(pawn, EBGUUnitState.Attacking);
                data.PrevHp = data.CurrentHp;
                data.CurrentHp = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn).GetFloatValue(EBGUAttrFloat.Hp);
            }

            _playerEngagement[playerId] = data;
        }

        UpdatePlayerMultipliers();
        UpdateEngagementScore();
        UpdateState();

        if (_state == AntiStallState.Warning)
        {
            _warningTimer += _elapsedTime;
            if (_warningTimer >= AntiStallConfig.WarningDuration)
            {
                SetActiveState();
            }
        }

        if (_state == AntiStallState.Active)
        {
            _activeTimer += _elapsedTime;
            if (_activeTimer >= AntiStallConfig.ActiveDuration)
            {
                _decayRounds++;
                SetMonitoringState();
            }
        }

        _elapsedTime = 0f;
    }

    private void UpdateEngagementScore()
    {
        foreach (var kvp in _playerEngagement)
        {
            var data = kvp.Value;
            if (data.IsAttacking)
            {
                _roomEngagementScore += _elapsedTime * AntiStallConfig.AttackRoomEngagementScore;
            }

            if (!data.CurrentHp.Equals(data.PrevHp, PvpConstants.FloatComparisonTolerance))
            {
                _roomEngagementScore += AntiStallConfig.DamageRoomEngagementScore;
            }
        }

        _roomEngagementScore = FMath.Min(_roomEngagementScore, AntiStallConfig.MaxRoomEngagementScore);
        _roomEngagementScore -= _elapsedTime * AntiStallConfig.RoomEngagementDecayScore;
        _roomEngagementScore = FMath.Max(_roomEngagementScore, 0f);
    }

    private void UpdatePlayerMultipliers()
    {
        var playerFacingDictionary = CalculatePlayerFacing();
        foreach (var playerId in _playerEngagement.Keys)
        {
            float current = _playerEngagementMultipliers.TryGetValue(playerId, out var val) ? val : 1.0f;

            if (playerFacingDictionary.TryGetValue(playerId, out var isFacing) && isFacing)
            {
                current = MathF.Max(current - AntiStallConfig.PlayerEngagementMultiplierIncrease * _elapsedTime, AntiStallConfig.PlayerEngagementMultiplierMin);
            }
            else
            {
                current = MathF.Min(current + AntiStallConfig.PlayerEngagementMultiplierDecay * _elapsedTime, AntiStallConfig.PlayerEngagementMultiplierMax);
            }

            _playerEngagementMultipliers[playerId] = current;
        }
    }

    private Dictionary<PlayerId, bool> CalculatePlayerFacing()
    {
        var _playerFacingDictionary = new Dictionary<PlayerId, bool>();
        var playerIds = new List<PlayerId>(_playerEngagement.Keys);
        for (int i = 0; i < playerIds.Count; i++)
        {
            var idA = playerIds[i];
            var dataA = _playerEngagement[idA];
            if (_playerFacingDictionary.TryGetValue(idA, out bool isFacingEnemyA) && isFacingEnemyA)
                continue;

            for (int j = i + 1; j < playerIds.Count; j++)
            {
                var idB = playerIds[j];
                var dataB = _playerEngagement[idB];
                if (dataA.TeamId == dataB.TeamId)
                    continue;

                var dirAtoB = Vector3.Normalize(dataB.LastPosition.ToVector3() - dataA.LastPosition.ToVector3());
                var dirBtoA = -dirAtoB;
                float facingA = Vector3.Dot(dataA.ForwardDirection.ToVector3(), dirAtoB);
                float facingB = Vector3.Dot(dataB.ForwardDirection.ToVector3(), dirBtoA);
                if (facingA > AntiStallConfig.PlayersFacingThreshold)
                {
                    _playerFacingDictionary[idA] = true;
                }

                if (facingB > AntiStallConfig.PlayersFacingThreshold)
                {
                    _playerFacingDictionary[idB] = true;
                }
            }

            if (!_playerFacingDictionary.ContainsKey(idA))
                _playerFacingDictionary[idA] = false;
        }

        return _playerFacingDictionary;
    }

    private void UpdateState()
    {
        if (_roomEngagementScore > AntiStallConfig.RoomEngagementThreshold && _state == AntiStallState.Warning)
        {
            SetMonitoringState();
        }

        if (_roomEngagementScore < AntiStallConfig.RoomEngagementThreshold && _state == AntiStallState.Monitoring)
        {
            SetWarningState();
        }
    }

    private void SetMonitoringState()
    {
        _state = AntiStallState.Monitoring;
        rpc.SendHideAntiStall();
    }

    private void SetWarningState()
    {
        _state = AntiStallState.Warning;
        _warningTimer = 0f;
        rpc.SendShowAntiStallWarning(AntiStallConfig.WarningDuration);
    }

    private void SetActiveState()
    {
        _state = AntiStallState.Active;
        _activeTimer = 0f;
        rpc.SendShowAntiStallAction();
        var baseDecayRate = AntiStallConfig.BaseAttributeDecayRate + AntiStallConfig.AttributeDecayMultiplier * _decayRounds;
        foreach (var kvp in _playerEngagementMultipliers)
        {
            var playerId = kvp.Key;
            var multiplier = kvp.Value;
            var randomCoefficient = GetRandomCoefficient();
            var scaledDecay = baseDecayRate * multiplier * AntiStallConfig.ActiveDuration * randomCoefficient;
            Logging.LogDebug("Applying anti-stall decay to player {0}: baseDecayRate={1}, multiplier={2}, random={3}, scaledDecay={4}", playerId, baseDecayRate, multiplier, randomCoefficient, scaledDecay);
            rpc.SendStallDamage(playerId, scaledDecay);
        }
    }

    private float GetRandomCoefficient()
    {
        return AntiStallConfig.RandomCoefficientMin + (float)_rng.NextDouble() * (AntiStallConfig.RandomCoefficientMax - AntiStallConfig.RandomCoefficientMin);
    }

    private void ResetState()
    {
        if (_isReset)
            return;

        _isReset = true;
        _state = AntiStallState.Monitoring;
        _decayRounds = 0;
        _roomEngagementScore = AntiStallConfig.MaxRoomEngagementScore;
        _playerEngagementMultipliers.Clear();
        _playerEngagement.Clear();
        rpc.SendHideAntiStall();
    }
}