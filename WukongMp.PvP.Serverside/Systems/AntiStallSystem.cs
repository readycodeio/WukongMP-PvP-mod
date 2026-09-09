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

    // Bots count towards activity, but don't get stall damage
    private readonly List<(Vector3 Position, int TeamId)> _botCombatants = [];
    
    private float _botHpPrevious;
    private float _botHpCurrent;

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

            data.LastPosition = trans.Position;
            data.ForwardDirection = trans.Rotation;
            data.TeamId = team.TeamId;
            data.PrevHp = data.CurrentHp;
            data.CurrentHp = hp.Hp;

            _playerEngagement[main.PlayerId] = data;
        });
        
        _botCombatants.Clear();
        _botHpPrevious = _botHpCurrent;
        _botHpCurrent = 0f;

        ecs.Query<TamerComponent, TransformComponent, HpComponent, TeamComponent>((ref tamer, ref trans, ref hp, ref team) =>
        {
            _botCombatants.Add((trans.Position, team.TeamId));
            _botHpCurrent += hp.Hp;
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
        _botCombatants.Clear();
        _botHpPrevious = 0f;
        _botHpCurrent = 0f;

        ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendHideAntiStall(main.PlayerId); });
    }
}