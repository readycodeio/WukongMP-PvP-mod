using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.Data;
using WukongMp.Pvp.Common.ECS;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Resources;
using WukongMp.PvP.UI;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.GameMode;

[ServerRpcFor(typeof(PvpRpcContracts))]
public partial class PvpMode(PvpWidgetManager pvpWidgetManager, TimerController timerController) : ServerRpcClient
{
    private readonly HashSet<ReadyTamer> spawnedDaSheng2 = [];

    private readonly CountdownTimer _countdownTimer = new(1, 5);

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

        if (enabled && main.IsObserver)
        {
            main.TeamId = CommonConstants.SpectatorTeamId;
        }
        else if (!enabled && main.TeamId == CommonConstants.SpectatorTeamId)
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
        mainCharacter.Teleport(spawnPosition, Vector3.Zero);

        // Set IsSpectator if joining during fight.
        if (WukongApi.Services.Resolve<WukongPvpApi>().InPvP)
        {
            WukongApi.Sync.EnableSpectatorMode(mainCharacter, SpectatorReason.Api);
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

    private void RelieveImmobilizedForAll()
    {
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

        main.Get<PvPComponent>().IsReadyForPvP = isReady;
    }

    public void SwitchReadyStateMulti()
    {
        if (WukongApi.Sync.InArea && !WukongApi.Services.Resolve<WukongPvpApi>().InPvpTournament && WukongApi.Sync.AllPlayers.Count > 0)
        {
            if (WukongApi.Sync.LocalMainCharacter is { IsSpectator: false })
            {
                SwitchReadyState();
            }
        }
    }

    private void SwitchReadyState()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        var newIsReady = !main.Get<PvPComponent>().IsReadyForPvP;
        SetReadyState(newIsReady);
        pvpWidgetManager.SwitchReadyState(newIsReady);

        var message = string.Format(newIsReady ? BuiltinTexts.PlayerIsReady : BuiltinTexts.PlayerIsNotReady, main.Nickname);
        WukongApi.Chat.SendServerMessage(message);
    }

    public void SwitchTeam(bool force = false)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        if (force || WukongApi.Sync.InArea && !main.Get<PvPComponent>().IsReadyForPvP && !WukongApi.Services.Resolve<WukongPvpApi>().InPvpTournament && !main.IsSpectator)
        {
            var teamId = PvpUtils.GetOppositeTeam(main.TeamId);
            main.TeamId = teamId;
        }
    }

    private void EnableHostility()
    {
        Logging.LogInformation("Enabled PvP");
        LogTeams();
        SetTeamHostility(true);
    }

    private void DisableHostility()
    {
        Logging.LogInformation("Disabled PvP");
        LogTeams();
        SetTeamHostility(false);
    }

    /// <summary>
    /// The game's <c>BGC_TeamRelationData.IsEnemyTeam</c> returns true when either team is absent from its
    /// relation table, so a peaceful lobby needs our team ids present with empty hostile lists. Deliberately
    /// independent of the local player: this has to run before the main character exists.
    /// </summary>
    private static void SetTeamHostility(bool hostile)
    {
        foreach (var team1 in CommonConstants.AllTeamIds)
        {
            foreach (var team2 in CommonConstants.AllTeamIds)
            {
                if (hostile)
                {
                    HostilityUtils.RegisterTeamHostility(team1, team2);
                }
                else
                {
                    HostilityUtils.UnregisterTeamHostility(team1, team2);
                }
            }
        }
    }

    private void LogTeams()
    {
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
    }

    private void DisablePlayerImmunity()
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue)
        {
            Logging.LogError("No room joined.");
            return;
        }

        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        main.EnableInteraction(false);
        SetLocalPlayerDamageImmunity(main, false);
    }

    private void EnablePlayerImmunity()
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
    }

    [Obsolete("This does not work since on Area join this.AllPlayers are not populated")]
    private int GetSmallerTeamId()
    {
        Dictionary<int, int> teamsCount = [];
        teamsCount[CommonConstants.RedTeamId] = 0;
        teamsCount[CommonConstants.BlueTeamId] = 0;
        teamsCount[CommonConstants.SpectatorTeamId] = 0; // to avoid KeyNotFoundException

        foreach (var playerEntity in AllPlayers)
        {
            if (playerEntity.PlayerId == WukongApi.Sync.LocalPlayerId)
                continue;

            var assignedTeamId = playerEntity.TeamId;
            Logging.LogDebug("Player {PlayerId} in team {TeamId}", playerEntity.PlayerId, assignedTeamId);
            teamsCount[assignedTeamId]++;
        }

        return teamsCount[CommonConstants.RedTeamId] > teamsCount[CommonConstants.BlueTeamId] ? CommonConstants.BlueTeamId : CommonConstants.RedTeamId;
    }

    private void RefreshReadyCounts()
    {
        var readyForPvp = AllPlayers.Count(c => !c.IsObserver && c.Get<PvPComponent>().IsReadyForPvP);
        var available = AllPlayers.Count(p => !p.IsObserver);
        pvpWidgetManager.UpdateReadyCount(readyForPvp, available);
    }

    private static void DestroyTamersOnArena()
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

    private void SetInitialTeam()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        main.TeamId = GetSmallerTeamId();
        Logging.LogDebug("Assigned team {Id} for player", main.TeamId);
    }

    public void ResetPlayer(ReadyMainCharacter mainCharacter)
    {
        var pawn = mainCharacter.Pawn!;
        BPS_EventCollectionCS.Get(pawn.PlayerState)?.Evt_TriggerPlayerTransEnd.Invoke(EPlayerTransEndType.None, default);
        var events = BUS_EventCollectionCS.Get(pawn);
        events?.Evt_DestroyAllCtrableBullet.Invoke();
        events?.Evt_TriggerTeleportResetPlayer!.Invoke();
    }

    private static void PlayBossDefeatedSound()
    {
        var playUiSound = AccessTools.Method("B1UI.Script.GSUI.Util.GSUIAudioUtil:PlayUISound");
        playUiSound.Invoke(null, ["EVT_ui_kill_jisha_manjingtou"]);
    }

    #region Event Handlers

    private void OnBeginPlayGameplayLevel()
    {
        DestroyTamersOnArena();

        // A fresh world starts with our team ids missing from the relation table, which the game reads
        // as hostile. Without this the lobby is fightable until the first round ends.
        SetTeamHostility(false);
    }

    private void OnJoinedAreaHandler(AreaId areaId)
    {
        Logging.LogInformation("Joined room");

        // Also here, so reconnecting to a new server without reloading the level cannot inherit the
        // hostility of a round that was cut short by the previous server going away.
        DisableHostility();

        RefreshReadyCounts();
    }

    private void OnOtherPlayerInsideAreaHandler(PlayerId playerId, AreaId areaId)
    {
        Logging.LogInformation("Player {PlayerId} entered the room", playerId);
        RefreshReadyCounts();
    }

    private void OnMonsterDead(ReadyTamer victim, ReadyCharacter? attacker)
    {
        if (!WukongApi.Services.Resolve<WukongPvpApi>().InPvP)
            return;

        if (victim.Owner != WukongApi.Sync.LocalPlayerId)
            return;

        var tamerClass = victim.Tamer?.GetClass();
        var character = victim.Pawn;
        if (character != null && tamerClass != null && tamerClass.PathName == UnitPathUtils.GetUnitPathName(TamerKinds.DaSheng))
        {
            var teamId = character.GetTeamIDInCS();
            var location = character.GetActorLocation();

            if (spawnedDaSheng2.Add(victim))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    RunOnGameThread(() => { WukongApi.Sync.SpawnEnemy(TamerKinds.DaSheng2, location.ToVector3(), 1, teamId); });
                });
            }
            else
            {
                Logging.LogDebug("Would spawn DaSheng2, but already spawned for this monster: {Monster}", victim.Guid);
            }
        }
    }

    private void ClearLoobyCountdown()
    {
        _countdownTimer.Reset();
        pvpWidgetManager.HideCountdown();
    }

    #endregion

    #region RPC

    partial void OnCheatsEnabledResponse(CheatsStatus status)
    {
        if (status == CheatsStatus.Enabled)
        {
            WukongApi.Chat.ShowLocalMessage(PvpTexts.CheatsEnabled, FLinearColor.Gray);
        }
        else if (status == CheatsStatus.Disabled)
        {
            WukongApi.Chat.ShowLocalMessage(PvpTexts.CheatsDisabled, FLinearColor.Gray);
        }
        else if (status == CheatsStatus.Forbidden)
        {
            WukongApi.Chat.ShowLocalMessage(PvpTexts.CheatsForbidden, FLinearColor.OrangeRed);
        }
    }

    partial void OnRoundCountdown(bool start, int seconds)
    {
        if (start)
        {
            pvpWidgetManager.SetMainMessage(BuiltinTexts.StartingGame);
            pvpWidgetManager.UpdateRoundCountdown(0, seconds);
            pvpWidgetManager.ShowCountdown();

            _countdownTimer.SetTime(0, seconds);
            _countdownTimer.Start(ClearLoobyCountdown, pvpWidgetManager.UpdateRoundCountdown);
        }
        else
        {
            if (WukongApi.Sync.LocalMainCharacter is not { } main)
                return;

            var isReady = main.Get<PvPComponent>().IsReadyForPvP;

            ClearLoobyCountdown();
            pvpWidgetManager.SetMainMessage(BuiltinTexts.InMultiplayer);
            pvpWidgetManager.SwitchReadyState(isReady);
        }
    }

    partial void OnPlayerReadinessWarning()
    {
        if (!WukongApi.Sync.InArea)
            return;

        pvpWidgetManager.SetThirdText(PvpTexts.BothTeamsNeedReadyPlayers);
    }

    partial void OnStartRound(Vector3 placement, Vector3 lookAt, int round, int totalRounds)
    {
        var mainEntity = WukongApi.Sync.LocalMainCharacter;
        if (!mainEntity.HasValue)
        {
            return;
        }

        PvpUtils.ShowPvpRoundStartMessage(round, totalRounds);
        ResetPlayer(mainEntity.Value);
        ClearLoobyCountdown();
        pvpWidgetManager.HideGameMessageWidget();
        EnableHostility();
        DisablePlayerImmunity();

        // teleport player to starting location and face the center of the arena
        var newPlayerLocation = PvpUtils.AdjustSpawnLocation(mainEntity.Value.Pawn, placement);
        mainEntity.Value.Teleport(newPlayerLocation, UMathLibrary.FindLookAtRotation(newPlayerLocation.ToFVector(), lookAt.ToFVector() - new FVector(0, 0, 500)).ToVector3());
    }

    partial void OnEndRound(int winnerTeam)
    {
        DisableHostility();
        RelieveImmobilizedForAll();

        if (winnerTeam == CommonConstants.DrawTeamId)
        {
            WukongApi.Widgets.ShowTip(BuiltinTexts.RoundDraw, true);
        }
        else
        {
            WukongApi.Widgets.ShowTip(string.Format(BuiltinTexts.RoundEndedWinner, PvpUtils.GetLocalizedTeamName(winnerTeam)), true);
        }

        if (winnerTeam == CommonConstants.DrawTeamId)
            return;

        var playerEntity = WukongApi.Sync.LocalMainCharacter;
        if (playerEntity == null)
            return;

        if (winnerTeam == playerEntity.Value.TeamId)
        {
            PlayBossDefeatedSound();
        }
    }

    partial void OnEndTournament(int winnerTeam)
    {
        if (winnerTeam == CommonConstants.DrawTeamId)
        {
            WukongApi.Widgets.ShowTip(BuiltinTexts.TournamentDraw, true);
        }
        else
        {
            WukongApi.Widgets.ShowTip(string.Format(BuiltinTexts.TournamentEndedWinner, PvpUtils.GetLocalizedTeamName(winnerTeam)), true);
        }

        Task.Run(async () =>
        {
            // Let the winner banner clear before the lobby UI replaces it.
            await Task.Delay(5000);

            RunOnGameThread(() =>
            {
                if (WukongApi.Sync.LocalMainCharacter is { } main)
                    WukongApi.Sync.DisableSpectatorMode(main);
            });

            await Task.Delay(1000);

            Logging.LogInformation("End tournament");

            RunOnGameThread(() =>
            {
                pvpWidgetManager.SetupLobbyUi();
                EnablePlayerImmunity();
            });
        });
    }

    partial void OnResetStats()
    {
        DestroyTamersOnArena();

        if (WukongApi.Sync.LocalMainCharacter is not { } main)
            return;

        if (!main.IsDead)
        {
            ResetPlayer(main);
        }

        foreach (var mainEntity in AllPlayers)
        {
            if (mainEntity.IsDead)
            {
                mainEntity.RebirthInPlace();
            }
        }
    }

    partial void OnHideAntiStall()
    {
        if (WukongApi.Sync.LocalMainCharacter is null)
            return;

        WukongApi.Local.HideInfoMessage();
        timerController.StopTimer();
        Logging.LogDebug("OnHideAntiStallWarning received");
    }

    partial void OnShowAntiStallWarning(int seconds)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (mainEntity.IsDead || mainEntity.IsSpectator)
            return;

        WukongApi.Local.ShowInfoMessage(BuiltinTexts.AntiStallWarning);
        timerController.SetTimer(0, seconds);
        timerController.StartTimer();
        Logging.LogDebug("OnShowAntiStallWarning received");
    }

    partial void OnShowAntiStallAction()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (mainEntity.IsDead || mainEntity.IsSpectator)
            return;

        WukongApi.Local.ShowInfoMessage(BuiltinTexts.StallingMessage);
        Logging.LogDebug("OnShowAntiStallAction received");
    }

    partial void OnStallDamage(float damage)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (mainEntity.IsDead || mainEntity.IsSpectator)
            return;

        Logging.LogDebug("Applying stall damage: {Damage}%", damage);
        var pawn = mainEntity.Pawn;
        if (pawn == null)
            return;

        var container = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn);
        var maxStamina = container?.GetFloatValue(EBGUAttrFloat.StaminaMax) ?? 1f;

        FSkillDamageConfig skillDamageConfig = new()
        {
            DamageCalcType = EDamageCalcType.HPMaxRatioAbs,
            HPMaxINV10000Damage_Abs = damage * 100,
            DamageImmueLevel = 2,
            DmgReason = EDamageReason.FallDmg
        };

        var events = BUS_EventCollectionCS.Get(pawn);
        events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Stamina, -(maxStamina * damage / 100 * 3));
        events?.Evt_TriggerNormalDamageEffect.Invoke(null, in skillDamageConfig, default, new FBattleAttrSnapShot(null));
    }

    // Teleport player to the new level.
    partial void OnChangeLevel(int levelId)
    {
        var levelData = LevelSpawnConfig.GetLevelSpawnData(levelId);
        BPS_EventCollectionCS.GetLocal(GameUtils.GetWorld()).Evt_BPS_TeleportTo.Invoke(ETeleportTypeV2.RebirthPointTeleportOnly, new TeleportParam_RebirthPoint
        {
            RebirthPointId = levelData.BirthPointId,
        }, EPlayerTeleportReason.RebirthPoint);
    }

    #endregion
}