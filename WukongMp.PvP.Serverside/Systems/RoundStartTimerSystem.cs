using System.Diagnostics;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.SDK.Server.Entities;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.ECS.Values;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.PvP.Serverside.Systems;

public class RoundStartTimerSystem(IEntities entities, RpcHandlers rpc) : ModSystemBase
{
    private readonly Stopwatch _roundStartStopwatch = new();
    private bool _shownWarning;

    protected override void OnUpdate(UpdateTick tick)
    {
        // exit if we're already in a tournament
        var inTournament = entities.World.InTournament;
        if (inTournament)
            return;

        var (nonObservers, readyCount, blueTeamAnyReady, redTeamAnyReady) = SurveyPlayerReadiness();
        var allReady = readyCount == nonObservers && nonObservers > 0;

        // if the countdown is not running, check if we should start it
        if (!_roundStartStopwatch.IsRunning)
        {
            // does any team container monsters? if so, they are always considered ready
            foreach (var tamer in entities.Query<Tamer>())
            {
                switch (tamer.TeamId)
                {
                    case CommonConstants.BlueTeamId:
                        blueTeamAnyReady = true;
                        break;
                    case CommonConstants.RedTeamId:
                        redTeamAnyReady = true;
                        break;
                }
            }

            // send RPC and begin countdown
            if (allReady)
            {
                if (blueTeamAnyReady && redTeamAnyReady)
                {
                    foreach (var main in entities.Query<MainCharacter>())
                    {
                        rpc.SendRoundCountdown(main.PlayerId, true, CommonConstants.RoundCountdownSeconds);
                    }

                    _roundStartStopwatch.Restart();
                    _shownWarning = false;
                }
                else if (!_shownWarning)
                {
                    // show a message that both teams need at least one ready player
                    foreach (var main in entities.Query<MainCharacter>())
                    {
                        rpc.SendPlayerReadinessWarning(main.PlayerId);
                    }

                    _shownWarning = true;
                }
            }

            return;
        }

        // check if we should cancel the countdown if a competitor is not ready anymore
        if (!allReady)
        {
            foreach (var main in entities.Query<MainCharacter>())
            {
                rpc.SendRoundCountdown(main.PlayerId, false, 0);
            }

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

            var singleRound = CountCompetingPlayerTeams() <= 1;

            var state = entities.World;
            
            // TODO: Generate accessors
            // state.ClearRoundWinners();
            state.IsSingleRoundTournament = singleRound;
            state.InPvP = true;
            state.InTournament = true;

            rpc.SendRoundStartToAll();
        }
    }

    /// Spectators are never ready, so every readiness test has to skip them.
    private static bool IsCompeting(in MainCharacter main)
        => !main.IsSpectator || main.SpectatorReason == SpectatorReason.Death;

    private (int NonObservers, int ReadyCount, bool BlueAnyReady, bool RedAnyReady) SurveyPlayerReadiness()
    {
        var nonObservers = 0;
        var readyCount = 0;
        var blueAnyReady = false;
        var redAnyReady = false;

        foreach (var main in entities.Query<MainCharacter>())
        {
            if (!IsCompeting(main))
                continue;

            nonObservers++;

            if (!main.IsReadyForPvP)
                continue;

            readyCount++;

            switch (main.TeamId)
            {
                case CommonConstants.BlueTeamId:
                    blueAnyReady = true;
                    break;
                case CommonConstants.RedTeamId:
                    redAnyReady = true;
                    break;
            }
        }

        return (nonObservers, readyCount, blueAnyReady, redAnyReady);
    }

    private int CountCompetingPlayerTeams()
    {
        HashSet<int> teams = [];

        foreach (var main in entities.Query<MainCharacter>())
        {
            if (!IsCompeting(main))
                continue;

            teams.Add(main.TeamId);
        }

        return teams.Count;
    }
}