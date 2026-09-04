using System.Numerics;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.ECS;

namespace WukongMp.PvP.Serverside.Systems;

public sealed class AntiStallSystem(EcsApi ecs, RpcHandlers rpc, ILogger logger) : ModSystemBase
{
    private struct PlayerEngagementData
    {
        public Vector3 LastPosition;
        public Vector3 ForwardDirection;
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
    private readonly Dictionary<PlayerId, double> _playerEngagementMultipliers = [];
    private readonly Dictionary<PlayerId, PlayerEngagementData> _playerEngagement = [];
    private readonly Random _rng = new();

    private int _decayRounds;

    protected override void OnUpdate(UpdateTick tick)
    {
        var queryState = (AntiStallEnabled: false, InPvP: false);

        ecs.Query(ref queryState, static (ref PvpStateComponent pvp, ref (bool AntiStallEnabled, bool InPvP) state) =>
        {
            state.AntiStallEnabled = pvp.AntiStallEnabled;
            state.InPvP = pvp.InPvP;
        });

        if (!queryState.AntiStallEnabled)
            return;

        if (!queryState.InPvP)
        {
            ResetState();
            return;
        }

        _isReset = false;

        if (_tickCounter++ % TickInterval != 0)
        {
            _elapsedTime += tick.DeltaTime;
            return;
        }

        ecs.Query<MainCharacterComponent, TransformComponent, HpComponent, TeamComponent>((ref main, ref trans, ref hp, ref team) =>
        {
            if (!_playerEngagement.TryGetValue(main.PlayerId, out var data))
            {
                data = new PlayerEngagementData();
                _playerEngagement[main.PlayerId] = data;
            }

            if (main.IsSpectator)
                return;

            data.LastPosition = trans.Position;
            data.ForwardDirection = trans.Rotation; // TODO: Is this actually the forward direction?
            data.TeamId = team.TeamId;
            // TODO: Set this in PvP component or sth
            // data.IsAttacking = BGUFunctionLibraryCS.BGUHasUnitState(pawn, EBGUUnitState.Attacking);
            data.PrevHp = data.CurrentHp;
            data.CurrentHp = hp.Hp;

            _playerEngagement[main.PlayerId] = data;
        });

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

            if (!Equals(data.PrevHp, CommonConstants.FloatComparisonTolerance))
            {
                _roomEngagementScore += AntiStallConfig.DamageRoomEngagementScore;
            }
        }

        _roomEngagementScore = Math.Min(_roomEngagementScore, AntiStallConfig.MaxRoomEngagementScore);
        _roomEngagementScore -= _elapsedTime * AntiStallConfig.RoomEngagementDecayScore;
        _roomEngagementScore = Math.Max(_roomEngagementScore, 0f);
    }

    private void UpdatePlayerMultipliers()
    {
        var playerFacingDictionary = CalculatePlayerFacing();
        foreach (var playerId in _playerEngagement.Keys)
        {
            double current = _playerEngagementMultipliers.TryGetValue(playerId, out var val) ? val : 1.0;

            if (playerFacingDictionary.TryGetValue(playerId, out var isFacing) && isFacing)
            {
                current = Math.Max(current - AntiStallConfig.PlayerEngagementMultiplierIncrease * _elapsedTime, AntiStallConfig.PlayerEngagementMultiplierMin);
            }
            else
            {
                current = Math.Min(current + AntiStallConfig.PlayerEngagementMultiplierDecay * _elapsedTime, AntiStallConfig.PlayerEngagementMultiplierMax);
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

                var dirAtoB = Vector3.Normalize(dataB.LastPosition - dataA.LastPosition);
                var dirBtoA = -dirAtoB;
                float facingA = Vector3.Dot(dataA.ForwardDirection, dirAtoB);
                float facingB = Vector3.Dot(dataB.ForwardDirection, dirBtoA);
                if (facingA > AntiStallConfig.PlayersFacingThreshold)
                {
                    _playerFacingDictionary[idA] = true;
                }

                if (facingB > AntiStallConfig.PlayersFacingThreshold)
                {
                    _playerFacingDictionary[idB] = true;
                }
            }

            _playerFacingDictionary.TryAdd(idA, false);
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
        ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendHideAntiStall(main.PlayerId); });
    }

    private void SetWarningState()
    {
        _state = AntiStallState.Warning;
        _warningTimer = 0f;
        ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendShowAntiStallWarning(main.PlayerId, AntiStallConfig.WarningDuration); });
    }

    private void SetActiveState()
    {
        _state = AntiStallState.Active;
        _activeTimer = 0f;
        ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendShowAntiStallAction(main.PlayerId); });
        var baseDecayRate = AntiStallConfig.BaseAttributeDecayRate + AntiStallConfig.AttributeDecayMultiplier * _decayRounds;
        foreach (var kvp in _playerEngagementMultipliers)
        {
            var playerId = kvp.Key;
            var multiplier = kvp.Value;
            var randomCoefficient = GetRandomCoefficient();
            var scaledDecay = baseDecayRate * multiplier * AntiStallConfig.ActiveDuration * randomCoefficient;
            logger.LogDebug("Applying anti-stall decay to player {0}: baseDecayRate={1}, multiplier={2}, random={3}, scaledDecay={4}", playerId, baseDecayRate, multiplier, randomCoefficient, scaledDecay);
            rpc.SendStallDamage(playerId, (float)scaledDecay);
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

        ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendHideAntiStall(main.PlayerId); });
    }
}