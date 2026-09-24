using ReadyM.SDK.Attributes;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.Serverside.Config;

namespace WukongMp.PvP.Serverside.Services;

[Service]
public sealed partial class RoomConfigSync(RoomConfigWatcher watcher, PvpConfig config)
{
    private void Update()
    {
        watcher.Poll(Time.Elapsed);
    }

    [CreateHandler(typeof(PvpState))]
    private void InitializeFromConfig(PvpState state)
    {
        state.CheatsEnabled = config.CheatsAllowed;
        state.LevelId = config.LevelId;
        state.TournamentRounds = config.TournamentRounds;
        state.GourdAllowed = config.GourdAllowed;
        state.ConsumablesAllowed = config.ConsumablesAllowed;
        state.ImmobilizeAllowed = config.ImmobilizeAllowed;
        state.PhantomRushAllowed = config.PhantomRushAllowed;
        state.AntiStallEnabled = config.AntiStallEnabled;
        state.EnemiesNgPlusLevel = config.EnemiesNgPlusLevel;

        state.InPvP = false;
        state.InTournament = false;
        state.IsSingleRoundTournament = false;

        state.ClearRoundWinners();
    }
}