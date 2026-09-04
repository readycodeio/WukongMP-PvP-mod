using b1;
using WukongMp.Api;
using WukongMp.Api.WukongUtils;

namespace WukongMp.PvP.GameMode;

public static class HostilityUtils
{
    public static void RegisterTeamHostility(int team1, int team2)
    {
        if (team1 == team2) return;

        if (GetTeamRelationData() is not { } teamRelationData) return;

        EnsureTeamRelationExists(teamRelationData, team1);
        EnsureTeamRelationExists(teamRelationData, team2);

        var team1RelationInfo = teamRelationData.TeamHostileInfos[team1];
        var team2RelationInfo = teamRelationData.TeamHostileInfos[team2];

        if (!team1RelationInfo.HostileTeamIDs.Contains(team2))
        {
            team1RelationInfo.HostileTeamIDs.Add(team2);
        }

        if (!team2RelationInfo.HostileTeamIDs.Contains(team1))
        {
            team2RelationInfo.HostileTeamIDs.Add(team1);
        }
    }

    public static void UnregisterTeamHostility(int team1, int team2)
    {
        if (GetTeamRelationData() is not { } teamRelationData) return;

        EnsureTeamRelationExists(teamRelationData, team1);
        EnsureTeamRelationExists(teamRelationData, team2);

        var team1RelationInfo = teamRelationData.TeamHostileInfos[team1];
        var team2RelationInfo = teamRelationData.TeamHostileInfos[team2];

        team1RelationInfo.HostileTeamIDs.Remove(team2);
        team2RelationInfo.HostileTeamIDs.Remove(team1);
    }


    /// Null while a level is still coming up, since the game state does not exist yet.
    private static BGC_TeamRelationData? GetTeamRelationData()
    {
        var teamRelationData = BGU_DataUtil.GetGameStateReadonlyData<IBGC_TeamRelationData, BGC_TeamRelationData>(GameUtils.GetWorld()) as BGC_TeamRelationData;

        if (teamRelationData == null)
        {
            Logging.LogWarning("No team relation data available, team hostility unchanged");
        }

        return teamRelationData;
    }

    private static void EnsureTeamRelationExists(BGC_TeamRelationData teamRelationData, int teamId)
    {
        if (!teamRelationData.TeamHostileInfos.ContainsKey(teamId))
        {
            teamRelationData.TeamHostileInfos.Add(teamId, new TeamRelationInfo());
        }
    }
}
