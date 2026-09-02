using WukongMp.Pvp.Common.ECS;

namespace WukongMp.Pvp.Common;

/// Initial values for <see cref="PvpStateComponent" />.
public sealed class PvpConfig
{
    public int LevelId { get; set; }
    public int TournamentRounds { get; set; } = 3;
    public bool CheatsAllowed { get; set; }
    public bool GourdAllowed { get; set; } = true;
    public bool ConsumablesAllowed { get; set; } = true;
    public bool ImmobilizeAllowed { get; set; } = true;
    public bool PhantomRushAllowed { get; set; } = true;
    public bool AntiStallEnabled { get; set; } = true;
    public int EnemiesNgPlusLevel { get; set; }

    public PvpStateComponent ToInitialState() => new()
    {
        LevelId = LevelId,
        TournamentRounds = TournamentRounds,
        GourdAllowed = GourdAllowed,
        ConsumablesAllowed = ConsumablesAllowed,
        ImmobilizeAllowed = ImmobilizeAllowed,
        PhantomRushAllowed = PhantomRushAllowed,
        CheatsEnabled = false,
        AntiStallEnabled = AntiStallEnabled,
        EnemiesNgPlusLevel = EnemiesNgPlusLevel,
    };
}