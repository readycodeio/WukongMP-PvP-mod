using System.Numerics;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.Data;
using WukongMp.Pvp.Common.ECS;

namespace WukongMp.PvP.Serverside;

[ServerRpcFor(typeof(PvpRpcContracts))]
public partial class RpcHandlers(EcsApi ecs) : ServerRpcHandlersBase
{
    partial void OnEnableCheats(RpcContext context, AreaId areaId, bool enabled)
    {
        // TODO: Do it only for this area
        ecs.Query<PvpStateComponent>((ref room) => { room.CheatsAllowed = enabled; });
    }
    
    partial void OnChangeLevel(RpcContext context, int levelId)
    {
        if (!LevelSpawnConfig.IsValidLevel(levelId))
            return;
        
        var inTournament = false;
        ecs.Query<PvpStateComponent>((ref pvp) => { inTournament = pvp.InTournament; });

        if (inTournament)
            return;

        ecs.Query<PvpStateComponent>((ref pvp) =>
        {
            pvp.InPvP = false;
            pvp.InTournament = false;
            pvp.LevelId = levelId;
            pvp.ClearRoundWinners();
        });

        ecs.Query<MainCharacterComponent>((ref player) => { SendChangeLevel(player.PlayerId, levelId); });
    }

    /// Calculate placement of each player and send round start RPC.
    public void SendRoundStartToAll()
    {
        var levelId = 0;
        var round = 1;
        var totalRounds = 1;

        ecs.Query<PvpStateComponent>((ref state) =>
        {
            levelId = state.LevelId;
            round = state.DisplayedRound;
            totalRounds = state.DisplayedTournamentRounds;
        });

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
        ecs.Query<MainCharacterComponent, TeamComponent>((ref main, ref team) => { playerTeams.Add(main.PlayerId, team.TeamId); });

        var teamsIds = playerTeams.Values.Distinct().ToList();
        var teamsCount = teamsIds.Count;
        var teamAngleStep = 2 * MathF.PI / teamsCount;

        var entityOffsetAngle = 0.15f;
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