using System.Numerics;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Core;
using ReadyM.SDK.Server.Entities;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.Pvp.Common.Data;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.PvP.Serverside.Services;

[Service]
public sealed partial class AntiStallSystem(IEntities entities, RpcHandlers rpc, ILogger logger)
{
    private struct PlayerEngagementData
    {
        public Vector3 LastPosition;
        public Vector3 ForwardDirection;
        public int TeamId;
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
    private float _elapsedTime;
    private bool _isReset;

    private float _warningTimer;
    private float _activeTimer;

    private float _roomEngagementScore;
    private readonly Dictionary<PlayerId, double> _playerEngagementMultipliers = [];
    private readonly Dictionary<PlayerId, PlayerEngagementData> _playerEngagement = [];
    private readonly Random _rng = new();

    // Bots count towards activity, but don't get stall damage
    private readonly List<(Vector3 Position, int TeamId)> _botCombatants = [];

    private float _botHpPrevious;
    private float _botHpCurrent;

    private int _decayRounds;

    private void Update()
    {
        if (!entities.World.AntiStallEnabled)
            return;

        if (!entities.World.InPvP)
        {
            ResetState();
            return;
        }

        _isReset = false;

        if (Time.Ticks % TickInterval != 0)
        {
            _elapsedTime += Time.DeltaTime;
            return;
        }

        foreach (var main in entities.Query<MainCharacter>())
        {
            if (main.IsSpectator)
            {
                _playerEngagement.Remove(main.PlayerId);
                _playerEngagementMultipliers.Remove(main.PlayerId);
                return;
            }

            if (!_playerEngagement.TryGetValue(main.PlayerId, out var data))
            {
                data = new PlayerEngagementData();
            }
            
            if (!entities.TryLookup(main.PlayerId, out Player player))
            {
                logger.LogWarning("Player {Player} not found in entities", main.PlayerId);
                continue;
            }

            data.LastPosition = main.Position;
            data.ForwardDirection = main.Rotation;
            data.TeamId = player.TeamId;
            data.PrevHp = data.CurrentHp;
            data.CurrentHp = main.Hp;

            _playerEngagement[main.PlayerId] = data;
        }

        _botCombatants.Clear();
        _botHpPrevious = _botHpCurrent;
        _botHpCurrent = 0f;

        foreach (var tamer in entities.Query<Tamer>())
        {
            _botCombatants.Add((tamer.Position, tamer.TeamId));
            _botHpCurrent += tamer.Hp;
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

            if (Math.Abs(data.PrevHp - data.CurrentHp) > CommonConstants.FloatComparisonTolerance)
            {
                _roomEngagementScore += AntiStallConfig.DamageRoomEngagementScore;
            }
        }

        if (_botCombatants.Count > 0 && Math.Abs(_botHpPrevious - _botHpCurrent) > CommonConstants.FloatComparisonTolerance)
        {
            _roomEngagementScore += AntiStallConfig.DamageRoomEngagementScore;
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
            double current = _playerEngagementMultipliers.GetValueOrDefault(playerId, 1.0);

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
        var facing = new Dictionary<PlayerId, bool>();

        foreach (var player in _playerEngagement)
        {
            facing[player.Key] = IsFacingAnEnemy(player.Value);
        }

        return facing;
    }

    private bool IsFacingAnEnemy(PlayerEngagementData player)
    {
        foreach (var other in _playerEngagement.Values)
        {
            if (other.TeamId != player.TeamId && IsFacing(player, other.LastPosition))
            {
                return true;
            }
        }

        foreach (var bot in _botCombatants)
        {
            if (bot.TeamId != player.TeamId && IsFacing(player, bot.Position))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFacing(PlayerEngagementData from, Vector3 target)
    {
        var toTarget = target - from.LastPosition;

        if (toTarget.LengthSquared() <= float.Epsilon)
        {
            return false;
        }

        return Vector3.Dot(from.ForwardDirection, Vector3.Normalize(toTarget)) > AntiStallConfig.PlayersFacingThreshold;
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
        foreach (var main in entities.Query<MainCharacter>())
        {
            rpc.SendHideAntiStall(main.PlayerId);
        }
    }

    private void SetWarningState()
    {
        _state = AntiStallState.Warning;
        _warningTimer = 0f;
        foreach (var main in entities.Query<MainCharacter>())
        {
            rpc.SendShowAntiStallWarning(main.PlayerId, AntiStallConfig.WarningDuration);
        }
    }

    private void SetActiveState()
    {
        _state = AntiStallState.Active;
        _activeTimer = 0f;

        foreach (var main in entities.Query<MainCharacter>())
        {
            rpc.SendShowAntiStallAction(main.PlayerId);
        }

        var baseDecayRate = AntiStallConfig.BaseAttributeDecayRate + AntiStallConfig.AttributeDecayMultiplier * _decayRounds;
        foreach (var (playerId, multiplier) in _playerEngagementMultipliers)
        {
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
        _botCombatants.Clear();
        _botHpPrevious = 0f;
        _botHpCurrent = 0f;

        foreach (var main in entities.Query<MainCharacter>())
        {
            rpc.SendHideAntiStall(main.PlayerId);
        }
    }
}