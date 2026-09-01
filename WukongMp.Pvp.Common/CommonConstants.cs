namespace WukongMp.Pvp.Common;

public static class CommonConstants
{
    public const float FloatComparisonTolerance = 0.1f;
    public const int DrawTeamId = 9999;
    public const int RedTeamId = -9999;
    public const int BlueTeamId = -9998;
    public const int SpectatorTeamId = -9997;
    public const int RoundEndDelayMs = 5000;
    public const int RoundReviveTimeoutMs = 5000;
    public const int ReviveAnimationDurationMs = 5000;
    public const int RoundCountdownSeconds = 5;
    public const string DaShengPhaseOneUnitPath = "/Game/00Main/Design/Units/MGD/TAMER_mgd_jsds.TAMER_mgd_jsds_C";

    public static readonly int[] CompetingTeamIds = [RedTeamId, BlueTeamId];
    public static readonly int[] AllTeamIds = [RedTeamId, BlueTeamId, SpectatorTeamId];
}