using System.Numerics;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Core;
using ReadyM.SDK.Server.Entities;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.Pvp.Common.Data;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.PvP.Serverside;

[ServerRpcFor(typeof(PvpRpcContracts))]
public partial class RpcHandlers(IEntities entities, PvpConfig config) : ServerRpcHandlersBase
{
    partial void OnEnableCheats(RpcContext context, bool enabled)
    {
        if (config.CheatsAllowed)
        {
            entities.World.SetCheatsEnabled(enabled);
            SendCheatsEnabledResponse(context.Sender, enabled ? CheatsStatus.Enabled : CheatsStatus.Disabled);
        }
        else
        {
            SendCheatsEnabledResponse(context.Sender, CheatsStatus.Forbidden);
        }
    }

    partial void OnChangeLevel(RpcContext context, int levelId)
    {
        if (!LevelSpawnConfig.IsValidLevel(levelId))
            return;

        var state = entities.World;

        if (state.InTournament)
            return;

        state.InPvP = false;
        state.InTournament = false;
        state.LevelId = levelId;
        state.ClearRoundWinners();

        foreach (var main in entities.Query<MainCharacter>())
        {
            SendChangeLevel(main.PlayerId, levelId);
        }
    }

    /// Calculate placement of each player and send round start RPC.
    public void SendRoundStartToAll()
    {
        var state = entities.World.As<PvpState>();

        var levelId = state.LevelId;
        var round = state.DisplayedRound;
        var totalRounds = state.DisplayedTournamentRounds;

        var levelData = LevelSpawnConfig.GetLevelSpawnData(levelId);
        foreach (var (player, placement) in PlacePlayers(levelData))
        {
            SendStartRound(player, placement, levelData.PvpStartingLocation, round, totalRounds);
        }
    }

    // This code is generalized to support more than 2 teams
    private IEnumerable<(PlayerId, Vector3)> PlacePlayers(LevelSpawnData levelData)
    {
        var center = levelData.PvpStartingLocation;
        var radius = levelData.PvpRadius;
        var customPositions = levelData.CustomTeamSpawns;

        var playerTeams = new Dictionary<PlayerId, int>();
        foreach (var main in entities.Query<Player>())
        {
            playerTeams.Add(main.PlayerId, main.TeamId);
        }

        var teamsIds = playerTeams.Values.Distinct().ToList();
        var teamsCount = teamsIds.Count;
        var teamAngleStep = 2 * MathF.PI / teamsCount;

        const float entityOffsetAngle = 0.15f;
        var teamMemberIndex = new Dictionary<int, int>();
        var teamIndex = new Dictionary<int, int>();
        for (var i = 0; i < teamsIds.Count; i++)
        {
            teamMemberIndex[teamsIds[i]] = 0;
            teamIndex[teamsIds[i]] = i;
        }

        var teamSizes = playerTeams.GroupBy(p => p.Value).ToDictionary(g => g.Key, g => g.Count());

        foreach (var (playerId, team) in playerTeams)
        {
            var memberIndex = teamMemberIndex[team];
            var teamBaseAngle = teamIndex[team] * teamAngleStep;

            Vector3 spawnLocation;
            var teamSize = teamSizes[team];
            var teamAngleOffset = -(teamSize - 1) * entityOffsetAngle / 2f;

            if (customPositions != null && customPositions.TryGetSpawnPosition(team, out var teamSpawn))
            {
                var dir = teamSpawn - center;
                var customTeamAngle = MathF.Atan2(dir.Y, dir.X);

                var angle = customTeamAngle + memberIndex * entityOffsetAngle;
                var x = center.X + radius * MathF.Cos(angle);
                var y = center.Y + radius * MathF.Sin(angle);
                spawnLocation = new Vector3(x, y, center.Z);
            }
            else
            {
                var angle = teamBaseAngle + teamAngleOffset + memberIndex * entityOffsetAngle;
                var x = center.X + radius * MathF.Cos(angle);
                var y = center.Y + radius * MathF.Sin(angle);
                spawnLocation = new Vector3(x, y, center.Z);
            }

            teamMemberIndex[team]++;

            yield return (playerId, spawnLocation);
        }
    }
}