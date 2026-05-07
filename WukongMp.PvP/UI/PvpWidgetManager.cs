using System;
using System.Collections.Generic;
using B1UI;
using B1UI.GSUI;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using WukongMp.Api;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.UI;

public class PvpWidgetManager : IHostedService
{
    private readonly Lazy<LobbyStatusWidget> _lobbyStatusWidget = new();
    private readonly Lazy<GameMessageWidget> _gameMessageWidget = new();
    private readonly Lazy<CountdownWidget> _countdownWidget = new();

    private bool _isAfterLoadingScreen;

    public void OnScopeStart()
    {
        WukongApi.Events.OnJoinedArea += OnAreaChange;
        WukongApi.Events.OnLeftArea += OnAreaChange;
        WukongApi.Events.OnOtherPlayerInsideArea += OnPlayerAreaChange;
        WukongApi.Events.OnOtherPlayerOutsideArea += OnPlayerAreaChange;

        WukongApi.Events.OnLevelLoaded += OnLevelLoaded;
        WukongApi.Events.OnExitLevel += OnExitLevel;
        WukongApi.Events.OnLoadingScreenClose += OnLoadingScreenClose;

        WukongApi.Events.OnPlayerChangedTeam += UpdatePlayerTeam;
        WukongApi.Events.OnLocalPlayerChangedSpectator += OnLocalPlayerChangedSpectator;
    }

    public void Dispose()
    {
        WukongApi.Events.OnJoinedArea -= OnAreaChange;
        WukongApi.Events.OnLeftArea -= OnAreaChange;
        WukongApi.Events.OnOtherPlayerInsideArea -= OnPlayerAreaChange;
        WukongApi.Events.OnOtherPlayerOutsideArea -= OnPlayerAreaChange;

        WukongApi.Events.OnLevelLoaded -= OnLevelLoaded;
        WukongApi.Events.OnExitLevel -= OnExitLevel;
        WukongApi.Events.OnLoadingScreenClose -= OnLoadingScreenClose;

        WukongApi.Events.OnPlayerChangedTeam -= UpdatePlayerTeam;
        WukongApi.Events.OnLocalPlayerChangedSpectator -= OnLocalPlayerChangedSpectator;
    }

    private void UpdatePlayerTeam(ReadyMainCharacter character)
    {
        if (WukongApi.Sync.TryGetPlayerInfoById(character.PlayerId, out var nickname, out var team))
        {
            _lobbyStatusWidget.Value.UpdatePlayerTeam(nickname, team.Value);
        }

        RefreshWidgets();
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

        if (WukongApi.Sync.LocalMainCharacter is not { } player)
            return;

        ShowInGameWidgets();
        _isAfterLoadingScreen = true;

        if (!WukongApi.PvP.PvpData(player).IsSpectator)
        {
            SetupLobbyUi();
        }
        else
        {
            SetupSpectatorUi();
        }
    }

    private void OnLocalPlayerChangedSpectator(bool enabled)
    {
        if (enabled)
        {
            SetupSpectatorUi();
        }
        else if (!WukongApi.PvP.InPvP)
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
        _lobbyStatusWidget.Value.SetConnectedCount(WukongApi.Sync.AreaPlayers.Count);
    }

    public void StartRound()
    {
        _gameMessageWidget.Value.SetVisibility(false);
        if (GSG.GSPageOP.FindUIPage(12) != null)
            GSB1UIUtil.ExitEquipScene(GameUtils.GetWorld());
    }

    public void SwitchReadyState(bool isReady)
    {
        _gameMessageWidget.Value.SetThirdText(isReady ? BuiltinTexts.YouAreReady : BuiltinTexts.PressToSwitchTeam);
        _gameMessageWidget.Value.SetSecondText(TextUtils.GetReadyText(WukongApi.Sync.AllPlayers.Count, isReady));
    }

    public bool UpdateReadyCount(int readyCount, int maxCount)
    {
        return _lobbyStatusWidget.Value.SetReadyCount(readyCount, maxCount);
    }

    public void SetTeams(List<string> redTeamList, List<string> blueTeamList, List<string> spectatorsList) => _lobbyStatusWidget.Value.SetTeams(redTeamList, blueTeamList, spectatorsList);

    public void SetupLobbyUi()
    {
        if (!_isAfterLoadingScreen)
            return;

        if (WukongApi.Sync.LocalMainCharacter is not { } player)
            return;

        _gameMessageWidget.Value.SetVisibility(true);
        _gameMessageWidget.Value.SetMainText(BuiltinTexts.InMultiplayer);
        _gameMessageWidget.Value.SetSecondText(TextUtils.GetReadyText(WukongApi.Sync.AllPlayers.Count, WukongApi.PvP.PvpData(player).IsReadyForPvP));
        _gameMessageWidget.Value.SetThirdText(BuiltinTexts.PressToSwitchTeam);
        _lobbyStatusWidget.Value.SetVisibility(true);
    }

    private void SetupSpectatorUi()
    {
        if (!_isAfterLoadingScreen)
            return;

        _gameMessageWidget.Value.SetVisibility(true);
        _gameMessageWidget.Value.SetMainText(BuiltinTexts.InMultiplayer);
        _gameMessageWidget.Value.SetSecondText(BuiltinTexts.WaitForEnd);
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