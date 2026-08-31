using System.Diagnostics;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.ECS.Values;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.ECS;

namespace WukongMp.PvP.Serverside.Systems;

public class RoundStartTimerSystem(EcsApi ecs, RpcHandlers rpc) : ModSystemBase
{
    private readonly Stopwatch _roundStartStopwatch = new();
    private bool _shownWarning;

    protected override void OnUpdate(UpdateTick tick)
    {
        // exit if we're already in a tournament
        var inTournament = false;
        ecs.Query<PvpStateComponent>((ref state) => { inTournament = state.InTournament; });

        if (inTournament)
            return;

        // if the countdown is not running, check if we should start it
        if (!_roundStartStopwatch.IsRunning)
        {
            // check if we should start the countdown
            var nonObservers = 0;
            var readyCount = 0;
            var blueTeamAnyReady = false;
            var redTeamAnyReady = false;

            ecs.Query<MainCharacterComponent, PvPComponent, TeamComponent>((ref main, ref pvp, ref team) =>
            {
                // observers do not count
                if (main.IsSpectator && main.SpectatorReason != SpectatorReason.Death)
                    return;

                nonObservers++;
                if (pvp.IsReadyForPvP)
                {
                    readyCount++;
                    switch (team.TeamId)
                    {
                        case CommonConstants.BlueTeamId:
                            blueTeamAnyReady = true;
                            break;
                        case CommonConstants.RedTeamId:
                            redTeamAnyReady = true;
                            break;
                    }
                }
            });

            var allReady = readyCount == nonObservers && nonObservers > 0;

            // does any team container monsters? if so, they are always considered ready
            ecs.Query<TamerComponent, TeamComponent>((ref _, ref team) =>
            {
                switch (team.TeamId)
                {
                    case CommonConstants.BlueTeamId:
                        blueTeamAnyReady = true;
                        break;
                    case CommonConstants.RedTeamId:
                        redTeamAnyReady = true;
                        break;
                }
            });

            // send RPC and begin countdown
            if (allReady)
            {
                if (blueTeamAnyReady && redTeamAnyReady)
                {
                    ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendRoundCountdown(main.PlayerId, true, CommonConstants.RoundCountdownSeconds); });
                    _roundStartStopwatch.Restart();
                    _shownWarning = false;
                }
                else if (!_shownWarning)
                {
                    // show a message that both teams need at least one ready player
                    ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendPlayerReadinessWarning(main.PlayerId); });
                    _shownWarning = true;
                }
            }

            return;
        }

        // check if we should cancel the countdown if someone is not ready anymore
        var shouldCancel = false;
        ecs.Query<PvPComponent>((ref pvp) =>
        {
            if (!pvp.IsReadyForPvP) shouldCancel = true;
        });

        if (shouldCancel)
        {
            ecs.Query<MainCharacterComponent>((ref main) => { rpc.SendRoundCountdown(main.PlayerId, false, 0); });
            _roundStartStopwatch.Reset();
            _shownWarning = false;
            return;
        }

        // countdown was running and everyone is still ready, check if we should start the round
        if (_roundStartStopwatch.Elapsed >= TimeSpan.FromSeconds(5))
        {
            // start round
            _roundStartStopwatch.Reset();
            _shownWarning = false;

            ecs.Query<PvpStateComponent>((ref state) =>
            {
                state.InPvP = true;
                state.InTournament = true;
            });

            rpc.SendRoundStartToAll();
        }
    }
}