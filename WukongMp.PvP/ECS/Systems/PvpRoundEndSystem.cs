using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WukongMp.Api;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.GameMode;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.ECS.Systems;

public class PvpRoundEndSystem(PvpMode pvpMode) : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue || !WukongApi.PvP.OwnsPvpState || !WukongApi.PvP.InPvP)
            return;

        if (pvpMode.IsRoundEnding || pvpMode.PendingDaShengSecondPhaseSpawns > 0)
            return;

        // check if all players but one are dead
        var playerEntities = pvpMode.AllPvPPlayers.ToList();
        var aliveTeamIds = playerEntities.Where(p =>
            {
                var pvp = WukongApi.PvP.PvpData(p);
                return !pvp.IsObserver && (!p.IsDead || p.IsTransformed);
            })
            .Select(x => x.TeamId)
            .ToList();

        var aliveMonsters = new List<int>();

        foreach (var tamer in WukongApi.Sync.AreaTamers)
        {
            if (tamer.IsDead || !PvpConstants.CompetingTeamIds.Contains(tamer.TeamId))
                break;

            aliveMonsters.Add(tamer.TeamId);
        }

        var alivePlayersTeams = aliveTeamIds.Concat(aliveMonsters).ToList();

        var aliveTeamCount = alivePlayersTeams.Distinct().Count();

        var aliveTeamPlayers = alivePlayersTeams
            .GroupBy(teamId => teamId)
            .Select(group => new { TeamId = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count).ToList();

        if (aliveTeamIds.Count == 0)
        {
            Logging.LogInformation("All players are dead, ending round");
            var aliveTeamId = aliveTeamPlayers.Count > 0 ? aliveTeamPlayers[0].TeamId : PvpConstants.DrawTeamId;
            if (alivePlayersTeams.Count == 0)
            {
                Task.Run(async () => await pvpMode.EndRoundAsync(PvpUtils.GetOppositeTeam(aliveTeamId)));
            }
            else
            {
                Task.Run(async () => await pvpMode.EndRoundAsync(aliveTeamId));
            }

            return;
        }

        if (aliveTeamCount == 1)
        {
            Logging.LogInformation("One team with alive players, ending round");
            var winner = playerEntities.First(p => !p.IsDead);
            Task.Run(async () => await pvpMode.EndRoundAsync(winner.TeamId));
        }
    }
}