using System;
using System.Collections.Generic;
using B1UI;
using B1UI.GSUI;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;
using WukongMp.Api;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Resources;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes.Mixins;
using WukongMp.Sdk.SDK;

namespace WukongMp.PvP.UI;

public class PvpWidgetManager(IEntities entities, IGameEvents gameEvents) : IHostedService
{
    private readonly Lazy<LobbyStatusWidget> _lobbyStatusWidget = new();
    private readonly Lazy<GameMessageWidget> _gameMessageWidget = new();
    private readonly Lazy<CountdownWidget> _countdownWidget = new();

    private static readonly StringComparer NicknameOrder = StringComparer.InvariantCultureIgnoreCase;

    private bool _isAfterLoadingScreen;

    public void OnScopeStart()
    {
        gameEvents.OnJoinedArea += OnAreaChange;
        gameEvents.OnLeftArea += OnAreaChange;
        gameEvents.OnOtherPlayerInsideArea += OnPlayerAreaChange;
        gameEvents.OnOtherPlayerOutsideArea += OnPlayerAreaChange;

        gameEvents.OnLevelLoaded += OnLevelLoaded;
        gameEvents.OnExitLevel += OnExitLevel;
        gameEvents.OnLoadingScreenClose += OnLoadingScreenClose;

        gameEvents.OnPlayerChangedTeam += UpdatePlayerTeam;
        gameEvents.OnLocalPlayerChangedSpectator += OnLocalPlayerChangedSpectator;

        gameEvents.OnDisconnected += OnDisconnected;
    }

    public void Dispose()
    {
        gameEvents.OnJoinedArea -= OnAreaChange;
        gameEvents.OnLeftArea -= OnAreaChange;
        gameEvents.OnOtherPlayerInsideArea -= OnPlayerAreaChange;
        gameEvents.OnOtherPlayerOutsideArea -= OnPlayerAreaChange;

        gameEvents.OnLevelLoaded -= OnLevelLoaded;
        gameEvents.OnExitLevel -= OnExitLevel;
        gameEvents.OnLoadingScreenClose -= OnLoadingScreenClose;

        gameEvents.OnPlayerChangedTeam -= UpdatePlayerTeam;
        gameEvents.OnLocalPlayerChangedSpectator -= OnLocalPlayerChangedSpectator;

        gameEvents.OnDisconnected -= OnDisconnected;
    }

    /// The SDK's disconnect message lands on top of these.
    private void OnDisconnected(PlayerId playerId, DisconnectedReason reason)
    {
        _gameMessageWidget.Value.SetVisibility(false);
        _countdownWidget.Value.SetVisibility(false);
    }

    private void UpdatePlayerTeam(MainCharacter _)
    {
        RefreshPlayerLists();
        RefreshWidgets();
    }

    public void RefreshPlayerLists()
    {
        List<string> redTeamList = [];
        List<string> blueTeamList = [];
        List<string> spectatorsList = [];

        foreach (var areaPlayer in WukongApi.Entities.AreaPlayers)
        {
            if (!entities.TryLookup(areaPlayer, out Player player))
                continue;

            switch (player.TeamId)
            {
                case CommonConstants.RedTeamId:
                    redTeamList.Add(player.Nickname.ToString());
                    break;
                case CommonConstants.BlueTeamId:
                    blueTeamList.Add(player.Nickname.ToString());
                    break;
                case CommonConstants.SpectatorTeamId:
                    spectatorsList.Add(player.Nickname.ToString());
                    break;
            }
        }

        redTeamList.Sort(NicknameOrder);
        blueTeamList.Sort(NicknameOrder);
        spectatorsList.Sort(NicknameOrder);

        _lobbyStatusWidget.Value.SetTeams(redTeamList, blueTeamList, spectatorsList);
    }

    public void SetMainMessage(string message)
    {
        _gameMessageWidget.Value.SetMainText(message);
    }

    public void SetThirdText(string message)
    {
        _gameMessageWidget.Value.SetThirdText(message);
    }

    public void UpdateRoundCountdown(int minutesLeft, int secondsLeft)
    {
        _countdownWidget.Value.SetText(secondsLeft);
    }

    public void ShowCountdown()
    {
        _countdownWidget.Value.SetVisibility(true);
    }

    public void HideCountdown()
    {
        _countdownWidget.Value.SetVisibility(false);
    }

    private void ShowInGameWidgets()
    {
        _lobbyStatusWidget.Value.SetVisibility(true);
        _lobbyStatusWidget.Value.SetMaxConnectedCount(PvpConstants.MaxPlayers);
    }

    private void OnLevelLoaded()
    {
        Logging.LogDebug("Initializing pvp widgets");
        InitializeWidgets();
    }

    private void OnExitLevel()
    {
        Logging.LogDebug("Deinitializing pvp widgets");
        DeinitializeWidgets();

        _isAfterLoadingScreen = false;
    }

    private void OnLoadingScreenClose()
    {
        var isOnGameplayLevel = WukongApi.Local.IsGameplayLevel;
        WukongApi.Widgets.ShowInGameWidgets(isOnGameplayLevel);

        if (!isOnGameplayLevel)
            return;

        if (WukongApi.Entities.LocalMainCharacter is not { } player)
            return;

        ShowInGameWidgets();
        _isAfterLoadingScreen = true;

        if (!player.IsSpectator)
        {
            SetupLobbyUi();
        }
        else if (entities.World.InTournament)
        {
            SetupSpectatorWaitForEndUi();
        }
    }

    private void OnLocalPlayerChangedSpectator(bool enabled)
    {
        if (enabled && entities.World.InTournament)
        {
            SetupSpectatorWaitForEndUi();
        }
        else if (!entities.World.InPvP)
        {
            SetupLobbyUi();
        }
    }

    private void InitializeWidgets()
    {
        _lobbyStatusWidget.Value.Initialize();
        _gameMessageWidget.Value.Initialize();
        _countdownWidget.Value.Initialize();
    }

    private void DeinitializeWidgets()
    {
        _lobbyStatusWidget.Value.Deinitialize();
        _gameMessageWidget.Value.Deinitialize();
        _countdownWidget.Value.Deinitialize();
    }

    public void RefreshWidgets()
    {
        _lobbyStatusWidget.Value.SetConnectedCount(WukongApi.Entities.AreaPlayers.Count);
    }

    public void HideGameMessageWidget()
    {
        _gameMessageWidget.Value.SetVisibility(false);
        if (GSG.GSPageOP.FindUIPage(12) != null)
            GSB1UIUtil.ExitEquipScene(GameUtils.GetWorld());
    }

    public void SwitchReadyState(bool isReady)
    {
        _gameMessageWidget.Value.SetThirdText(isReady ? PvpTexts.YouAreReady : PvpTexts.PressToSwitchTeam);
        _gameMessageWidget.Value.SetSecondText(TextUtils.GetReadyText(WukongApi.Entities.AllPlayers.Count, isReady));
    }

    public bool UpdateReadyCount(int readyCount, int maxCount)
    {
        return _lobbyStatusWidget.Value.SetReadyCount(readyCount, maxCount);
    }


    public void SetupLobbyUi()
    {
        if (!_isAfterLoadingScreen || !WukongApi.Entities.IsConnected)
            return;

        if (WukongApi.Entities.LocalMainCharacter is not { } player)
            return;

        _gameMessageWidget.Value.SetVisibility(true);
        _gameMessageWidget.Value.SetMainText(PvpTexts.InMultiplayer);
        _gameMessageWidget.Value.SetSecondText(TextUtils.GetReadyText(WukongApi.Entities.AllPlayers.Count, player.IsReadyForPvP));
        _gameMessageWidget.Value.SetThirdText(PvpTexts.PressToSwitchTeam);
        _lobbyStatusWidget.Value.SetVisibility(true);
    }

    private void SetupSpectatorWaitForEndUi()
    {
        if (!_isAfterLoadingScreen || !WukongApi.Entities.IsConnected)
            return;

        _gameMessageWidget.Value.SetVisibility(true);
        _gameMessageWidget.Value.SetMainText(PvpTexts.InMultiplayer);
        _gameMessageWidget.Value.SetSecondText(PvpTexts.WaitForEnd);
        _gameMessageWidget.Value.SetThirdText("");
        _lobbyStatusWidget.Value.SetVisibility(true);
    }

    private void OnPlayerAreaChange(PlayerId playerId, AreaId area)
    {
        RefreshWidgets();
    }

    private void OnAreaChange(AreaId _)
    {
        RefreshWidgets();
    }
}