using WukongMp.Pvp.Common.Archetypes;

namespace WukongMp.PvP.Serverside.Config;

/// Initial values for <see cref="PvpState" />.
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
}