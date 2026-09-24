using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using ReadyM.SDK.Core;
using ReadyM.SDK.Server.Entities;
using WukongMp.Pvp.Common.Archetypes;

namespace WukongMp.PvP.Serverside.Config;

/// <summary>
/// Re-reads config.json every few seconds and applies it to the PvP world state.
/// Used for the specific case of ReadyM-hosted servers in Europe / U.S. / Hong Kong,
/// where these settings are set in the Launcher by the player who sets up a room.
/// </summary>
public sealed class RoomConfigWatcher
{
    private const string ConfigFile = "config.json";
    private const float PollIntervalSeconds = 5f;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    private readonly IEntities _entities;
    private readonly PvpConfig _config;
    private readonly ILogger _logger;
    private readonly string _configPath;

    private DateTime _configStamp;
    private float _nextPoll;

    public RoomConfigWatcher(IEntities entities, PvpConfig config, string modDirectory, ILogger logger)
    {
        _entities = entities;
        _config = config;
        _logger = logger;
        _configPath = Path.Combine(modDirectory, ConfigFile);
        _configStamp = Stamp();
    }

    public void Poll(float time)
    {
        if (time < _nextPoll)
        {
            return;
        }

        _nextPoll = time + PollIntervalSeconds;

        var stamp = Stamp();
        if (stamp == _configStamp)
        {
            return;
        }

        // Settings like the arena or the round count cannot change under a running match. Leave the
        // stamp alone so the change is picked up once the match is over.
        if (MatchInProgress())
        {
            _logger.LogInformation("{File} changed, deferring until the match ends", ConfigFile);
            return;
        }

        _configStamp = stamp;

        if (!TryRead(out var updated))
        {
            return;
        }

        CopyInto(_config, updated);
        PushToState();

        _logger.LogInformation(
            "PvP settings applied: level {LevelId}, {Rounds} rounds, NG+{NgPlus}, cheats allowed {Cheats}, " +
            "gourd {Gourd}, consumables {Consumables}, immobilize {Immobilize}, phantom rush {PhantomRush}, " +
            "anti-stall {AntiStall}",
            _config.LevelId, _config.TournamentRounds, _config.EnemiesNgPlusLevel, _config.CheatsAllowed,
            _config.GourdAllowed, _config.ConsumablesAllowed, _config.ImmobilizeAllowed,
            _config.PhantomRushAllowed, _config.AntiStallEnabled);
    }

    private DateTime Stamp()
        => File.Exists(_configPath) ? File.GetLastWriteTimeUtc(_configPath) : default;

    private bool MatchInProgress()
    {
        // TODO: Singleton getter for World
        foreach (var world in _entities.Query<World>())
        {
            return world.InPvP || world.InTournament;
        }

        return false;
    }

    private bool TryRead(out PvpConfig config)
    {
        config = new PvpConfig();

        if (!File.Exists(_configPath))
        {
            return true;
        }

        try
        {
            config = JsonSerializer.Deserialize<PvpConfig>(File.ReadAllText(_configPath), Options) ?? new PvpConfig();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not read {Path}, keeping the settings already in effect", _configPath);
            return false;
        }
    }

    private static void CopyInto(PvpConfig target, PvpConfig source)
    {
        target.LevelId = source.LevelId;
        target.TournamentRounds = source.TournamentRounds;
        target.EnemiesNgPlusLevel = source.EnemiesNgPlusLevel;
        target.CheatsAllowed = source.CheatsAllowed;
        target.GourdAllowed = source.GourdAllowed;
        target.ConsumablesAllowed = source.ConsumablesAllowed;
        target.ImmobilizeAllowed = source.ImmobilizeAllowed;
        target.PhantomRushAllowed = source.PhantomRushAllowed;
        target.AntiStallEnabled = source.AntiStallEnabled;
    }

    private void PushToState()
    {
        // The component owns a NativeList that the host allocated when
        // the entity was created, so assigning the whole struct would drop it.
        foreach (var world in _entities.Query<World>())
        {
            world.SetLevelId(_config.LevelId);
            world.SetTournamentRounds(_config.TournamentRounds);
            world.SetEnemiesNgPlusLevel(_config.EnemiesNgPlusLevel);
            world.SetGourdAllowed(_config.GourdAllowed);
            world.SetConsumablesAllowed(_config.ConsumablesAllowed);
            world.SetImmobilizeAllowed(_config.ImmobilizeAllowed);
            world.SetPhantomRushAllowed(_config.PhantomRushAllowed);
            world.SetAntiStallEnabled(_config.AntiStallEnabled);
        }
    }
}