using ReadyM.SDK.Attributes;
using ReadyM.SDK.Core;
using Yooni.Native.Container;

namespace WukongMp.Pvp.Common.Archetypes;

[ArchetypeMixin]
[Replicated]
[Propagates(Propagation.ServerAuthoritative)]
[Extends(typeof(World))]
public readonly partial struct PvpState
{
    // settings
    public partial bool CheatsEnabled { get; set; }
    public partial int LevelId { get; set; }
    public partial int TournamentRounds { get; set; }
    public partial bool GourdAllowed { get; set; }
    public partial bool ConsumablesAllowed { get; set; }
    public partial bool ImmobilizeAllowed { get; set; }
    public partial bool PhantomRushAllowed { get; set; }
    public partial int EnemiesNgPlusLevel { get; set; }
    public partial bool AntiStallEnabled { get; set; }

    // in-game state
    public partial bool InPvP { get; set; }
    public partial bool InTournament { get; set; }

    /// Only one player team is competing, so the tournament is decided by a single round.
    public partial bool IsSingleRoundTournament { get; set; }

    private partial NativeList<int> RoundWinners { get; set; }

    public int CurrentRound => RoundWinnersCount + 1;

    public int DisplayedRound => IsSingleRoundTournament ? 1 : CurrentRound;

    public int DisplayedTournamentRounds => IsSingleRoundTournament ? 1 : TournamentRounds;
}