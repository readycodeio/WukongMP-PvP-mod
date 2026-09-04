using System.Runtime.InteropServices;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace WukongMp.Pvp.Common.ECS;

/// <summary>
/// Holds the state of the PvP mode, including settings and in-game state.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PvpStateComponent : IServerAuthoritative, INativeInit
{
    // settings
    private bool _cheatsEnabled;
    private int _levelId;
    private int _tournamentRounds;
    private bool _gourdAllowed;
    private bool _consumablesAllowed;
    private bool _immobilizeAllowed;
    private bool _phantomRushAllowed;
    private int _enemiesNgPlusLevel;
    private bool _antiStallEnabled;

    // in-game state
    private bool _inPvP;
    private bool _inTournament;

    /// Only one player team is competing, so the tournament is decided by a single round.
    private bool _isSingleRoundTournament;

    private NativeList<int> _roundWinners;

    public int CurrentRound => RoundWinnersCount + 1;

    public int DisplayedRound => IsSingleRoundTournament ? 1 : CurrentRound;

    public int DisplayedTournamentRounds => IsSingleRoundTournament ? 1 : TournamentRounds;

    public void SetLastRoundWinnerTeam(int teamId)
    {
        AddRoundWinners(teamId);
    }

    public void Init(AllocatorKind allocatorKind)
    {
        _roundWinners = new NativeList<int>(5, allocatorKind);
    }
}