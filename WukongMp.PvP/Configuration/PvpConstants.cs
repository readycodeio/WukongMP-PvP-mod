namespace WukongMp.PvP.Configuration;

public static class PvpConstants
{
    public const float PvpStartingRadius = 500;
    public const int CharacterArchiveId = 10;
    public const int WorldArchiveId = 0;
    public const int NewCharacterArchiveId = 1;
    public const float FloatComparisonTolerance = 0.1f;
    public const int MaxPlayers = 10;

    public const string ChestCameraLockNode = "CAMERA_LOCK";
    public const string FeetCameraLockNode = "CAMERA_LOCK_Root";
    public const string PlayerMarkerPath = "/Game/Mods/WukongMod/BP_PlayerMarker.BP_PlayerMarker_C";

    public const float MonsterSpawnDistance = 2000f;
    public const float MonsterSpawnTraceHeight = 2000f;
    public const float MonsterHalfHeight = 200f;

    public const int GourdSkillId = 10530;
    public const int ImmobilizeSkillId = 10518;
    public const int IncenseTrailTalismanSkillId = 10909;
    public const int RuyiScrollSkillId = 10912;
    public const int ConsumableBuffSkillId = 10913;
    public const int IronBodySkillId = 10505;

    public const int DrawTeamId = 9999;

    public const int CountdownSeconds = 5;
    public const int RedTeamId = -9999;
    public const int BlueTeamId = -9998;
    public const int SpectatorTeamId = -9997;
    public static readonly int[] CompetingTeamIds = [RedTeamId, BlueTeamId];
    public static readonly int[] AllTeamIds = [RedTeamId, BlueTeamId, SpectatorTeamId];
}