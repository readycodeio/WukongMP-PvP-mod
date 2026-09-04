using System.Collections.Generic;
using System.Numerics;

namespace WukongMp.Pvp.Common.Data;

public class TeamSpawnPoints
{
    private readonly Dictionary<int, Vector3> _spawnPoints = [];

    public TeamSpawnPoints(Vector3 blueTeam, Vector3 redTeam)
    {
        _spawnPoints[CommonConstants.BlueTeamId] = blueTeam;
        _spawnPoints[CommonConstants.RedTeamId] = redTeam;
    }

    public bool TryGetSpawnPosition(int teamId, out Vector3 spawnPosition)
    {
        return _spawnPoints.TryGetValue(teamId, out spawnPosition);
    }
}