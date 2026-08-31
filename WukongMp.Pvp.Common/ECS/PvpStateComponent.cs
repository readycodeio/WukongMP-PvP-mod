using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace WukongMp.Pvp.Common.ECS;

/// <summary>
/// Holds the state of the PvP mode, including settings and in-game state.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PvpStateComponent
{
    // settings
    private int _levelId;
    private int _tournamentRounds;
    private bool _gourdAllowed;
    private bool _consumablesAllowed;
    private bool _immobilizeAllowed;
    private bool _phantomRushAllowed;
    private int _enemiesNgPlusLevel;
    private bool _cheatsAllowed;
    private bool _antiStallEnabled;

    // in-game state
    private bool _inPvP;
    private bool _inTournament;

    private NativeList<int> _roundWinners;

    public int CurrentRound => RoundWinnersCount + 1;

    public void SetLastRoundWinnerTeam(int teamId)
    {
        AddRoundWinners(teamId);
    }
}