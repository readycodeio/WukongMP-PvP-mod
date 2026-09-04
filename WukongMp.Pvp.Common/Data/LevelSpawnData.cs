using System.Numerics;

namespace WukongMp.Pvp.Common.Data;

public struct LevelSpawnData(int mapId, int mapAreaId, int birthPointId, Vector3 pvpStartingLocation, float pvpRadius = 4000, TeamSpawnPoints? customTeamSpawns = null)
{
    public int MapId { get; private set; } = mapId;
    public int MapAreaId { get; private set; } = mapAreaId;
    public int BirthPointId { get; private set; } = birthPointId;
    public Vector3 PvpStartingLocation { get; private set; } = pvpStartingLocation;
    public float PvpRadius { get; private set; } = pvpRadius;
    public TeamSpawnPoints? CustomTeamSpawns { get; } = customTeamSpawns;
}
