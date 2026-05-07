using System.Collections.Generic;
using UnrealEngine.Runtime;

namespace WukongMp.PvP.Configuration
{
    public class TeamSpawnPoints
    {
        public TeamSpawnPoints(FVector blueTeam, FVector redTeam)
        {
            _spawnPoints[PvpConstants.BlueTeamId] = blueTeam;
            _spawnPoints[PvpConstants.RedTeamId] = redTeam;
        }

        private Dictionary<int, FVector> _spawnPoints = [];

        public bool TryGetSpawnPosition(int teamId, out FVector spawnPosition)
        {
            return _spawnPoints.TryGetValue(teamId, out spawnPosition);
        }
    }
}
