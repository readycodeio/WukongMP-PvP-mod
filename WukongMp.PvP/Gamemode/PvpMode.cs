using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using b1;
using CSharpModBase;
using HarmonyLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.GameEvents;
using WukongMp.PvP.Resources;
using WukongMp.PvP.UI;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.GameMode;

public partial class PvpMode(PvpWidgetManager pvpWidgetManager, IRpcClient rpcClient, IRelaySerializer serializer)
    : RpcClassBase(rpcClient, serializer)
{
    public bool IsRoundEnding { get; private set; }

    public int PendingDaShengSecondPhaseSpawns { get; private set; }
    private readonly HashSet<ReadyTamer> SpawnedDaSheng2 = [];

    private readonly CountdownTimer _countdownTimer = new(1, 5);

    private static bool GetPvPPlayerIds(ReadyMainCharacter main)
    {
        return !WukongApi.PvP.PvpData(main).IsObserver;
    }

    public IEnumerable<ReadyMainCharacter> AllPvPPlayers => WukongApi.Sync.AreaMainCharacters.Where(GetPvPPlayerIds);

    public IEnumerable<ReadyMainCharacter> AllPlayers => WukongApi.Sync.AreaMainCharacters;

    public IEnumerable<ReadyMainCharacter> OtherPlayers => WukongApi.Sync.AreaMainCharacters.Where(p => p.PlayerId != WukongApi.Sync.LocalPlayerId);

    public override void OnScopeStart()
    {
        base.OnScopeStart();

        WukongApi.Events.OnBeginPlayGameplayLevel += OnBeginPlayGameplayLevel;

        WukongApi.Events.OnJoinedArea += OnJoinedAreaHandler;
        WukongApi.Events.OnOtherPlayerInsideArea += OnOtherPlayerInsideAreaHandler;

        WukongApi.Events.OnMonsterDead += OnMonsterDead;
        WukongApi.Events.OnMonsterSpawned += OnMonsterSpawned;
        WukongApi.Events.OnLanguageChanged += OnLanguageChanged;
        WukongApi.Events.OnPlayerChangedTeam += OnPlayerChangedTeam;
        WukongApi.Events.OnLocalPlayerChangedSpectator += OnLocalPlayerChangedSpectator;

        WukongApi.Events.OnPlayerPawnSpawned += OnPlayerPawnSpawned;
        WukongApi.Events.OnMainCharacterEntityInitialized += OnMainCharacterEntityInitialized;
    }

    public override void Dispose()
    {
        base.Dispose();

        WukongApi.Events.OnOtherPlayerInsideArea -= OnOtherPlayerInsideAreaHandler;
        WukongApi.Events.OnJoinedArea -= OnJoinedAreaHandler;

        WukongApi.Events.OnBeginPlayGameplayLevel -= OnBeginPlayGameplayLevel;

        WukongApi.Events.OnMonsterDead -= OnMonsterDead;
        WukongApi.Events.OnMonsterSpawned -= OnMonsterSpawned;
        WukongApi.Events.OnLanguageChanged -= OnLanguageChanged;
        WukongApi.Events.OnPlayerChangedTeam -= OnPlayerChangedTeam;
        WukongApi.Events.OnLocalPlayerChangedSpectator -= OnLocalPlayerChangedSpectator;

        WukongApi.Events.OnPlayerPawnSpawned -= OnPlayerPawnSpawned;
        WukongApi.Events.OnMainCharacterEntityInitialized -= OnMainCharacterEntityInitialized;
    }

    private void OnPlayerChangedTeam(ReadyMainCharacter character)
    {
        if (WukongApi.Sync.TryGetPlayerInfoById(character.PlayerId, out var nickname, out var team))
        {
            Logging.LogDebug("Updating player {Nickname} marker to team {Team}", nickname, team.Value);
            var teamColor = PvpUtils.GetTeamColorString(team.Value);
            character.SetMarkerMessage(nickname, teamColor);
        }
    }

    private void OnLocalPlayerChangedSpectator(bool enabled)
    {
        if (!WukongApi.Local.IsGameplayLevel || WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        if (enabled && WukongApi.PvP.PvpData(main).IsObserver)
        {
            main.TeamId = PvpConstants.SpectatorTeamId;
        }
        else if (!enabled && main.TeamId == PvpConstants.SpectatorTeamId)
        {
            main.TeamId = GetSmallerTeamId();
        }
    }

    private void OnLanguageChanged(CultureInfo culture)
    {
        PvpTexts.Culture = culture;
    }

    private void OnMonsterSpawned(ReadyTamer entity)
    {
        var teamColor = PvpUtils.GetTeamColorString(entity.TeamId);
        entity.SetMarkerMessage(BuiltinTexts.BotName, teamColor);
    }

    private void OnPlayerPawnSpawned(ReadyMainCharacter mainCharacter)
    {
        var teamColor = PvpUtils.GetTeamColorString(mainCharacter.TeamId);
        mainCharacter.SetMarkerMessage(mainCharacter.Nickname, teamColor);
    }

    private void OnMainCharacterEntityInitialized(ReadyMainCharacter mainCharacter)
    {
        var spawnPosition = PvpUtils.GetSpawnPosition(GameUtils.GetControlledPawn(), mainCharacter.PlayerId.RawValue, PvpConstants.MaxPlayers);
        mainCharacter.Teleport(spawnPosition.ToVector3(), Vector3.Zero);

        // Set IsSpectator if joining during fight.
        if (WukongApi.PvP.InPvP)
        {
            WukongApi.Sync.EnableSpectatorMode(mainCharacter, SpectatorReason.Observer);
        }

        SetLocalPlayerDamageImmunity(mainCharacter, true);
        SetInitialTeam();

        if (mainCharacter.Pawn != null)
        {
            OnPlayerPawnSpawned(mainCharacter); // recreate the marker when reconnecting
        }
    }

    private static void SetLocalPlayerDamageImmunity(ReadyMainCharacter mainEntity, bool enabled)
    {
        var pawn = mainEntity.Pawn;
        var events = BUS_EventCollectionCS.Get(pawn);
        if (events != null)
        {
            events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.ImmueDamage, IsRemove: !enabled);
            Logging.LogDebug("Set local player damage immunity to {Enabled}", enabled);
        }
    }

    private void StartPvP()
    {
        if (WukongApi.PvP.OwnsPvpState)
        {
            WukongApi.PvP.RoundWinners = [];
            Task.Run(StartRoundAsync);
        }
    }

    public async Task StartRoundAsync()
    {
        if (!WukongApi.PvP.OwnsPvpState)
        {
            return;
        }

        PlacePlayers();
        await Task.Delay(100);

        SendPvPEvent(new PvpEvent(PvpEventKind.RoundStart));
    }

    private void PlacePlayers()
    {
        if (!WukongApi.PvP.OwnsPvpState)
        {
            return;
        }

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        var center = levelData.PvpStartingLocation;
        var radius = levelData.PvpRadius;
        var customPositions = levelData.CustomTeamSpawns;

        var playerEntities = AllPvPPlayers.ToList();
        var teamsIds = playerEntities.Select(p => p.TeamId).Distinct().ToList();
        var teamsCount = teamsIds.Count;
        var teamAngleStep = 2 * Math.PI / teamsCount;

        var entityOffsetAngle = 0.15f;
        var teamMemberIndex = new Dictionary<int, int>();
        var teamIndex = new Dictionary<int, int>();
        for (var i = 0; i < teamsIds.Count; i++)
        {
            teamMemberIndex[teamsIds[i]] = 0;
            teamIndex[teamsIds[i]] = i;
        }

        foreach (var mainEntity in playerEntities)
        {
            var team = mainEntity.TeamId;
            var memberIndex = teamMemberIndex[team];
            var teamBaseAngle = teamIndex[team] * teamAngleStep;

            FVector spawnLocation;
            var teamAngleOffset = 0.0;

            if (customPositions != null && customPositions.TryGetSpawnPosition(team, out var teamSpawn))
            {
                var dir = teamSpawn - center;
                var customTeamAngle = Math.Atan2(dir.Y, dir.X);
                teamAngleOffset = customTeamAngle - teamBaseAngle;

                var angle = customTeamAngle + memberIndex * entityOffsetAngle;
                var x = center.X + radius * Math.Cos(angle);
                var y = center.Y + radius * Math.Sin(angle);
                spawnLocation = new FVector(x, y, center.Z);
            }
            else
            {
                var angle = teamBaseAngle + teamAngleOffset + memberIndex * entityOffsetAngle;
                var x = center.X + radius * Math.Cos(angle);
                var y = center.Y + radius * Math.Sin(angle);
                spawnLocation = new FVector(x, y, center.Z);
            }

            teamMemberIndex[team]++;
            var newPlayerLocation = PvpUtils.AdjustSpawnLocation(mainEntity.Pawn, spawnLocation);
            mainEntity.Teleport(newPlayerLocation.ToVector3(), UMathLibrary.FindLookAtRotation(newPlayerLocation, center - new FVector(0, 0, 500)).ToVector3());
        }
    }

    public async Task EndRoundAsync(int winner)
    {
        if (IsRoundEnding)
            return;

        if (!WukongApi.PvP.OwnsPvpState)
        {
            return;
        }

        IsRoundEnding = true;

        if (!WukongApi.Sync.CurrentAreaId.HasValue)
        {
            Logging.LogError("Current area is null, cannot end round");
            return;
        }

        // disable pvp until next round
        SendPvPEvent(new PvpEvent(PvpEventKind.RoundEnd, winner));

        // increment round number
        WukongApi.PvP.SetLastRoundWinnerTeam(winner);

        // wait until all players death animations are finished
        await Task.Delay(5000);

        if (!WukongApi.PvP.OwnsPvpState)
        {
            Logging.LogDebug("Master client disconnected before finishing EndRoundAsync");
            return;
        }

        await ResetHpAndRespawnAllPlayers();

        // resolve tournament
        var winnersSoFar = WukongApi.PvP.RoundWinners.ToList();
        var winnersByTeam = winnersSoFar.Where(w => w != PvpConstants.DrawTeamId).GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

        // check if only one team is present
        if (AllPvPPlayers.Select(p => p.TeamId).Distinct().Count() == 1)
        {
            SendPvPEvent(new PvpEvent(PvpEventKind.TournamentEnd, winner));
            IsRoundEnding = false;
            return;
        }

        // check if any team won more than half of the rounds
        var winnerTeam = winnersByTeam.FirstOrDefault(w => w.Value > WukongApi.PvP.TournamentRounds / 2);
        if (winnerTeam.Key != 0)
        {
            SendPvPEvent(new PvpEvent(PvpEventKind.TournamentEnd, winnerTeam.Key));
            IsRoundEnding = false;
            return;
        }

        // otherwise, check if we have a tie
        if (WukongApi.PvP.CurrentRound > WukongApi.PvP.TournamentRounds)
        {
            if (winnersByTeam.Count > 0)
            {
                // if any team have won more than others
                int maxWins = winnersByTeam.Values.Max();
                var winningTeams = winnersByTeam.Where(t => t.Value == maxWins).Select(t => t.Key).ToList();
                if (winningTeams.Count == 1)
                {
                    SendPvPEvent(new PvpEvent(PvpEventKind.TournamentEnd, winningTeams[0]));
                }
                else
                {
                    SendPvPEvent(new PvpEvent(PvpEventKind.TournamentEnd, PvpConstants.DrawTeamId));
                }
            }
            else
            {
                // that was the final round
                SendPvPEvent(new PvpEvent(PvpEventKind.TournamentEnd, PvpConstants.DrawTeamId));
            }
        }
        else
        {
            // start next round
            await StartRoundAsync();
        }

        IsRoundEnding = false;
    }

    private async Task ResetHpAndRespawnAllPlayers()
    {
        if (!WukongApi.PvP.OwnsPvpState)
        {
            return;
        }

        // resurrect dead players and restore health to living ones
        SendPvPEvent(new PvpEvent(PvpEventKind.ResetStats));

        foreach (var mainEntity in AllPlayers)
        {
            if (mainEntity.IsDead)
            {
                mainEntity.RebirthInPlace();
            }
        }

        // wait for that to finish
        await Task.Delay(6500);
    }

    private void StartRound()
    {
        ClearLoobyCountdown();
        pvpWidgetManager.StartRound();

        if (!WukongApi.PvP.OwnsPvpState)
            return;

        WukongApi.PvP.InPvP = true;
    }

    private void EndRound()
    {
        if (WukongApi.PvP.OwnsPvpState)
            WukongApi.PvP.InPvP = false;

        if (WukongApi.Sync.IsMasterClient)
        {
            foreach (var mainEntity in AllPlayers)
            {
                var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
                events?.Evt_RelieveImmobilized.Invoke();
                events?.Evt_RelievePhantomRush.Invoke();
            }
        }
    }

    private void SetReadyState(bool isReady)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        WukongApi.PvP.PvpData(main).IsReadyForPvP = isReady;
    }

    public void SwitchReadyStateMulti()
    {
        if (WukongApi.Sync.InArea && !WukongApi.PvP.InPvpTournament && WukongApi.Sync.AllPlayers.Count > 0)
        {
            if (WukongApi.Sync.LocalMainCharacter is { } main && !WukongApi.PvP.PvpData(main).IsSpectator)
            {
                SwitchReadyState();
            }
        }
    }

    private void SwitchReadyState()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        var pvpData = WukongApi.PvP.PvpData(main);
        var newIsReady = !pvpData.IsReadyForPvP;
        SetReadyState(newIsReady);
        pvpWidgetManager.SwitchReadyState(newIsReady);

        var message = string.Format(newIsReady ? BuiltinTexts.PlayerIsReady : BuiltinTexts.PlayerIsNotReady, main.Nickname);
        WukongApi.Chat.SendServerMessage(message);
    }

    public void SwitchTeam(bool force = false)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        var pvpData = WukongApi.PvP.PvpData(main);

        if (force || WukongApi.Sync.InArea && !pvpData.IsReadyForPvP && !WukongApi.PvP.InPvpTournament && !pvpData.IsSpectator)
        {
            var teamId = PvpUtils.GetOppositeTeam(main.TeamId);
            main.TeamId = teamId;
        }
    }

    private void EnablePvP()
    {
        Logging.LogInformation("Enabled PvP");

        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        var myTeam = main.TeamId;
        var otherTeams = OtherPlayers
            .Where(p => p.TeamId != myTeam)
            .Select(p => p.TeamId)
            .Distinct()
            .ToList();

        Logging.LogDebug("My team: {Team}", myTeam);
        Logging.LogDebug("Other teams: {Teams}", string.Join(", ", otherTeams));

        foreach (var team1 in PvpConstants.AllTeamIds)
        {
            foreach (var team2 in PvpConstants.AllTeamIds)
            {
                HostilityUtils.RegisterTeamHostility(team1, team2);
            }
        }
    }

    private void DisablePvP()
    {
        Logging.LogInformation("Disabled PvP");

        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        var myTeam = main.TeamId;
        var otherTeams = OtherPlayers
            .Where(p => p.TeamId != myTeam)
            .Select(p => p.TeamId)
            .Distinct()
            .ToList();

        Logging.LogDebug("My team: {Team}", myTeam);
        Logging.LogDebug("Other teams: {Teams}", string.Join(", ", otherTeams));


        foreach (var team1 in PvpConstants.AllTeamIds)
        {
            foreach (var team2 in PvpConstants.AllTeamIds)
            {
                HostilityUtils.UnregisterTeamHostility(team1, team2);
            }
        }
    }

    private void StartTournament()
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (WukongApi.PvP.InPvpTournament)
        {
            Logging.LogDebug("Already in tournament.");
            return;
        }

        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        main.EnableInteraction(false);
        SetLocalPlayerDamageImmunity(main, false);

        if (WukongApi.PvP.OwnsPvpState)
        {
            WukongApi.PvP.InPvpTournament = true;
            WukongApi.PvP.InPvP = true;
        }
    }

    private void EndTournament()
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        main.EnableInteraction(true);
        SetLocalPlayerDamageImmunity(main, true);

        if (WukongApi.PvP.OwnsPvpState)
        {
            WukongApi.PvP.InPvpTournament = false;
            WukongApi.PvP.InPvP = false;
        }
    }

    [Obsolete("This does not work since on Area join this.AllPlayers are not populated")]
    private int GetSmallerTeamId()
    {
        Dictionary<int, int> teamsCount = [];
        teamsCount[PvpConstants.RedTeamId] = 0;
        teamsCount[PvpConstants.BlueTeamId] = 0;
        teamsCount[PvpConstants.SpectatorTeamId] = 0; // to avoid KeyNotFoundException

        foreach (var playerEntity in AllPlayers)
        {
            if (playerEntity.PlayerId == WukongApi.Sync.LocalPlayerId)
                continue;

            var assignedTeamId = playerEntity.TeamId;
            Logging.LogDebug("Player {PlayerId} in team {TeamId}", playerEntity.PlayerId, assignedTeamId);
            teamsCount[assignedTeamId]++;
        }

        return teamsCount[PvpConstants.RedTeamId] > teamsCount[PvpConstants.BlueTeamId] ? PvpConstants.BlueTeamId : PvpConstants.RedTeamId;
    }

    private void SetUpRoom()
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (WukongApi.PvP.OwnsPvpState)
        {
            WukongApi.PvP.InPvpTournament = false;
            WukongApi.PvP.InPvP = false;
        }
    }

    public void StartLobbyCountdown(int seconds)
    {
        pvpWidgetManager.SetMainMessage(BuiltinTexts.StartingGame);
        pvpWidgetManager.UpdateRoundCountdown(0, seconds);
        pvpWidgetManager.ShowCountdown();

        _countdownTimer.SetTime(0, seconds);
        _countdownTimer.Start(() =>
        {
            ClearLoobyCountdown();
            StartPvP();
        }, pvpWidgetManager.UpdateRoundCountdown);
    }

    public void CancelLobbyCountdown()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        var isReady = WukongApi.PvP.PvpData(main).IsReadyForPvP;

        ClearLoobyCountdown();
        pvpWidgetManager.SetMainMessage(BuiltinTexts.InMultiplayer);
        pvpWidgetManager.SwitchReadyState(isReady);
    }

    public void ClearLoobyCountdown()
    {
        _countdownTimer.Reset();
        pvpWidgetManager.HideCountdown();
    }

    private void RefreshReadyCounts()
    {
        var readyForPvp = AllPlayers.Count(x => WukongApi.PvP.PvpData(x) is { IsReadyForPvP: true, IsObserver: false });
        var available = AllPlayers.Count(x => !WukongApi.PvP.PvpData(x).IsObserver);
        pvpWidgetManager.UpdateReadyCount(readyForPvp, available);
    }

    private void DestroyTamersOnArena()
    {
        var world = GameUtils.GetWorld();
        var currentLevelId = BGUFuncLibMap.GetCurLevelId(world);
        var levelTamers = LevelTamersConfig.GetLevelTamers(currentLevelId);
        var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(world);
        foreach (var actor in allActorsOfClass)
        {
            var guid = actor.GetFinalGuid();
            if (!levelTamers.Contains(guid))
                actor.CurrentRef.DestroyTamer();
        }
    }

    #region Event Handlers

    private void OnBeginPlayGameplayLevel()
    {
        DestroyTamersOnArena();
    }

    private void OnJoinedAreaHandler(AreaId areaId)
    {
        Logging.LogInformation("Joined room");

        SetUpRoom();
        RefreshReadyCounts();
    }

    private void SetInitialTeam()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        main.TeamId = GetSmallerTeamId();
        Logging.LogDebug("Assigned team {Id} for player", main.TeamId);
    }

    private void OnOtherPlayerInsideAreaHandler(PlayerId playerId, AreaId areaId)
    {
        Logging.LogInformation("Player {PlayerId} entered the room", playerId);
        RefreshReadyCounts();
    }

    private void OnMonsterDead(ReadyTamer victim, ReadyCharacter? attacker)
    {
        if (!WukongApi.PvP.InPvP)
            return;

        if (victim.Owner != WukongApi.Sync.LocalPlayerId)
            return;

        var tamerClass = victim.Tamer?.GetClass();
        var character = victim.Pawn;
        if (character != null && tamerClass != null && tamerClass.PathName == UnitPathUtils.GetUnitPathName(TamerKinds.DaSheng))
        {
            var teamId = character.GetTeamIDInCS();
            var location = character.GetActorLocation();

            if (SpawnedDaSheng2.Add(victim))
            {
                PendingDaShengSecondPhaseSpawns++;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    Utils.TryRunOnGameThread(() =>
                    {
                        WukongApi.Sync.SpawnEnemy(TamerKinds.DaSheng2, location.ToVector3(), 1, teamId);
                        PendingDaShengSecondPhaseSpawns--;
                    });
                });
            }
            else
            {
                Logging.LogDebug("Would spawn DaSheng2, but already spawned for this monster: {Monster}", victim.Guid);
            }
        }
    }

    private static void ResetPlayer(ReadyMainCharacter mainCharacter)
    {
        var pawn = mainCharacter.Pawn!;
        BPS_EventCollectionCS.Get(pawn.PlayerState)?.Evt_TriggerPlayerTransEnd.Invoke(EPlayerTransEndType.None, default);
        var events = BUS_EventCollectionCS.Get(pawn);
        events?.Evt_DestroyAllCtrableBullet.Invoke();
        events?.Evt_TriggerTeleportResetPlayer!.Invoke();
    }

    #endregion

    #region RPC

    public void SendPvPEvent(PvpEvent ev)
    {
        if (!WukongApi.PvP.OwnsPvpState)
        {
            Logging.LogError("Only room owner can send start countdown.");
            return;
        }

        Logging.LogInformation("Sending PvP event: {Event}", ev.Kind);

        SendPvpEvent([(int)ev.Kind, ev.Data]);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    public void OnPvpEvent(int[] data)
    {
        var ev = new PvpEvent((PvpEventKind)data[0], data[1]);
        var winnerTeamId = ev.Data;

        Logging.LogInformation("Received PvP event: {Event}", ev);

        switch (ev.Kind)
        {
            case PvpEventKind.RoundStart:
            {
                Utils.TryRunOnGameThread(PvpUtils.ShowPvPCountDown);

                var mainEntity = WukongApi.Sync.LocalMainCharacter;
                if (mainEntity.HasValue)
                {
                    Utils.TryRunOnGameThread(() => { ResetPlayer(mainEntity.Value); });
                }

                StartRound();
                EnablePvP();
                StartTournament();
                break;
            }
            case PvpEventKind.RoundEnd:
            {
                DisablePvP();
                EndRound();

                if (winnerTeamId == PvpConstants.DrawTeamId)
                {
                    WukongApi.Widgets.ShowTip(BuiltinTexts.RoundDraw, true);
                }
                else
                {
                    WukongApi.Widgets.ShowTip(string.Format(BuiltinTexts.RoundEndedWinner, PvpUtils.GetLocalizedTeamName(winnerTeamId)), true);
                }

                if (winnerTeamId == PvpConstants.DrawTeamId)
                    return;

                var playerEntity = WukongApi.Sync.LocalMainCharacter;
                if (playerEntity == null)
                    return;

                if (winnerTeamId == playerEntity.Value.TeamId)
                {
                    PlayBossDefeatedSound();
                }

                break;
            }
            case PvpEventKind.TournamentEnd:
            {
                if (winnerTeamId == PvpConstants.DrawTeamId)
                {
                    WukongApi.Widgets.ShowTip(BuiltinTexts.TournamentDraw, true);
                }
                else
                {
                    WukongApi.Widgets.ShowTip(string.Format(BuiltinTexts.TournamentEndedWinner, PvpUtils.GetLocalizedTeamName(winnerTeamId)), true);
                }

                // ReSharper disable once AsyncVoidMethod
                Utils.TryRunOnGameThread(async void () =>
                {
                    await Task.Delay(1000);

                    if (WukongApi.Sync.LocalMainCharacter is { } main)
                        WukongApi.Sync.DisableSpectatorMode(main);

                    await Task.Delay(1000);

                    Logging.LogInformation("End tournament");

                    pvpWidgetManager.SetupLobbyUi();
                    EndTournament();
                    SetReadyState(false);
                });

                break;
            }
            case PvpEventKind.ResetStats:
            {
                Utils.TryRunOnGameThread(DestroyTamersOnArena);

                if (WukongApi.Sync.LocalMainCharacter is not { } main)
                    return;

                if (!main.IsDead)
                {
                    Utils.TryRunOnGameThread(() => { ResetPlayer(main); });
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(ev));
        }
    }

    private static void PlayBossDefeatedSound()
    {
        var playUiSound = AccessTools.Method("B1UI.Script.GSUI.Util.GSUIAudioUtil:PlayUISound");
        playUiSound.Invoke(null, ["EVT_ui_kill_jisha_manjingtou"]);
    }

    #endregion
}