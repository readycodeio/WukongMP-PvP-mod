using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.SDK.Server.Entities;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.ECS.Values;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.PvP.Serverside.Systems;

public sealed class RoundEndSystem(IEntities entities, RpcHandlers rpc, ILogger logger) : ModSystemBase
{
    private enum PostRoundPhase
    {
        None,
        EndDelay,
        AwaitRevives,
        ReviveAnimation
    }

    private readonly Stopwatch _phaseTimer = new();
    private PostRoundPhase _phase;
    private int _lastRoundWinner;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (_phase != PostRoundPhase.None)
        {
            TickPostRound();
            return;
        }

        var inPvp = entities.World.InPvP;

        if (!inPvp)
            return;

        if (HasDaShengPhaseOne())
            return;

        // check if all combatants but one are dead
        List<int> aliveTeamIds = [];
        
        foreach (var main in entities.Query<MainCharacter>())
        {
            if (main.IsSpectator && main.SpectatorReason != SpectatorReason.Death)
                continue;

            if (main is { IsDead: true, IsTransformed: false })
                continue;

            aliveTeamIds.Add(main.TeamId);
        }

        List<int> aliveMonsters = [];

        foreach (var tamer in entities.Query<Tamer>())
        {
            if (tamer.IsDead || !CommonConstants.CompetingTeamIds.Contains(tamer.TeamId))
                continue;

            aliveMonsters.Add(tamer.TeamId);
        }

        var alivePlayersTeams = aliveTeamIds.Concat(aliveMonsters).ToList();

        var aliveTeamCount = alivePlayersTeams.Distinct().Count();

        var aliveTeamPlayers = alivePlayersTeams
            .GroupBy(teamId => teamId)
            .Select(group => new { TeamId = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count).ToList();

        if (aliveTeamIds.Count == 0)
        {
            logger.LogInformation("All players are dead, ending round");
            var aliveTeamId = aliveTeamPlayers.Count > 0 ? aliveTeamPlayers[0].TeamId : CommonConstants.DrawTeamId;

            if (alivePlayersTeams.Count == 0)
            {
                SendEndRound(GetOppositeTeam(aliveTeamId));
            }
            else
            {
                SendEndRound(aliveTeamId);
            }

            return;
        }

        if (aliveTeamCount == 1)
        {
            logger.LogInformation("One team with alive players, ending round");
            SendEndRound(aliveTeamIds[0]);
        }
    }

    private void SendEndRound(int winningTeamId)
    {
        // set last round winner

        entities.World.RoundWinners.Add(winningTeamId); // TODO: This does not replicate
        entities.World.SetInPvP(false);

        // send round end RPC to all players
        foreach (var main in entities.Query<MainCharacter>())
        {
            rpc.SendEndRound(main.PlayerId, winningTeamId);
        }

        _lastRoundWinner = winningTeamId;
        EnterPhase(PostRoundPhase.EndDelay);
    }

    private void EnterPhase(PostRoundPhase phase)
    {
        _phase = phase;
        _phaseTimer.Restart();
    }

    /// <summary>
    /// Drives the post-round sequence from the tick. It used to run on a thread pool timer, where
    /// ComponentWriteContext.Current is not the server authoring scope, so writes to player-owned
    /// components silently lost the authoritative API flag and never overrode their owner.
    /// </summary>
    private void TickPostRound()
    {
        var elapsedMs = _phaseTimer.ElapsedMilliseconds;

        switch (_phase)
        {
            case PostRoundPhase.EndDelay when elapsedMs >= CommonConstants.RoundEndDelayMs:
                ResetStatsAndDecide();
                break;

            case PostRoundPhase.AwaitRevives when CountDeadCompetitors() == 0:
                EnterPhase(PostRoundPhase.ReviveAnimation);
                break;

            case PostRoundPhase.AwaitRevives when elapsedMs >= CommonConstants.RoundReviveTimeoutMs:
                logger.LogWarning("Starting the next round with dead players: no revive within {TimeoutMs}ms",
                    CommonConstants.RoundReviveTimeoutMs);
                EnterPhase(PostRoundPhase.ReviveAnimation);
                break;

            // the game reports the players as alive at the start of the revive animation
            case PostRoundPhase.ReviveAnimation when elapsedMs >= CommonConstants.ReviveAnimationDurationMs:
                StartNextRound();
                break;
        }
    }

    private void ResetStatsAndDecide()
    {
        var state = entities.World.As<PvpState>();
        
        HashSet<int> nonObserverTeams = [];
        foreach (var main in entities.Query<MainCharacter>())
        {
            rpc.SendResetStats(main.PlayerId);

            if (!main.IsSpectator || main.SpectatorReason == SpectatorReason.Death)
            {
                nonObserverTeams.Add(main.TeamId);
            }
        }

        // start new round or end tournament

        Dictionary<int, int> teamWins = [];
        foreach (var w in state.RoundWinners)
        {
            if (w == CommonConstants.DrawTeamId)
                continue;

            if (teamWins.TryGetValue(w, out var value))
            {
                teamWins[w] = value + 1;
            }
            else
            {
                teamWins[w] = 1;
            }
        }

        // check if only one team is present
        if (nonObserverTeams.Count == 1)
        {
            EndTournament(_lastRoundWinner);
            return;
        }

        // check if any team won more than half of the rounds
        var winnerTeam = teamWins.FirstOrDefault(w => w.Value > state.TournamentRounds / 2.0f);
        if (winnerTeam.Key != 0)
        {
            EndTournament(winnerTeam.Key);
            return;
        }

        // otherwise, check if we have a tie
        if (state.CurrentRound > state.TournamentRounds)
        {
            if (teamWins.Count > 0)
            {
                // if any team have won more than others
                int maxWins = teamWins.Values.Max();
                var winningTeams = teamWins.Where(t => t.Value == maxWins).Select(t => t.Key).ToList();
                if (winningTeams.Count == 1)
                {
                    EndTournament(winningTeams[0]);
                }
                else
                {
                    EndTournament(CommonConstants.DrawTeamId);
                }
            }
            else
            {
                // that was the final round
                EndTournament(CommonConstants.DrawTeamId);
            }

            return;
        }

        // The reset above rebirths dead players on their clients. Starting before that
        // replicates back would end the round instantly on the same corpse.
        EnterPhase(PostRoundPhase.AwaitRevives);
    }

    private void EndTournament(int winner)
    {
        entities.World.SetInTournament(false);

        foreach (var main in entities.Query<MainCharacter>())
        {
            main.SetIsReadyForPvP(false);
            rpc.SendEndTournament(main.PlayerId, winner);
        }

        EnterPhase(PostRoundPhase.None);
    }

    private void StartNextRound()
    {
        entities.World.SetInPvP(true);
        rpc.SendRoundStartToAll();

        EnterPhase(PostRoundPhase.None);
    }

    /// Counts the players that <see cref="OnUpdate" /> would treat as dead.
    private int CountDeadCompetitors()
    {
        var dead = 0;

        foreach (var main in entities.Query<MainCharacter>())
        {
            if (main.IsSpectator && main.SpectatorReason != SpectatorReason.Death)
                continue;

            if (main is { IsDead: true, IsTransformed: false })
                dead++;
        }

        return dead;
    }

    /// The owning client spawns his second phase five seconds after he dies, so a dead phase one still
    /// counts as a live combatant until the game clears his actor.
    private bool HasDaShengPhaseOne()
    {
        var present = false;

        foreach (var tamer in entities.Query<Tamer>())
        {
            if (tamer.UnitPath == CommonConstants.DaShengPhaseOneUnitPath)
                present = true;
        }

        return present;
    }

    private static int GetOppositeTeam(int teamId)
    {
        if (teamId == CommonConstants.DrawTeamId)
            return teamId;
        return teamId == CommonConstants.RedTeamId ? CommonConstants.BlueTeamId : CommonConstants.RedTeamId;
    }
}