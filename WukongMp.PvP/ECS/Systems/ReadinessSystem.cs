using WukongMp.PvP.Configuration;
using WukongMp.PvP.GameMode;
using WukongMp.PvP.Resources;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.ECS.Systems;

public class ReadinessSystem(
    PvpWidgetManager widgetManager,
    PvpMode pvpMode
) : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue || WukongApi.PvP.InPvpTournament)
            return;

        var players = 0;
        var readyCount = 0;
        var blueTeamAnyReady = false;
        var redTeamAnyReady = false;
        var localPlayer = WukongApi.Sync.LocalMainCharacter;

        if (!localPlayer.HasValue)
            return;

        foreach (var character in WukongApi.Sync.AreaMainCharacters)
        {
            if (WukongApi.Sync.TryGetPlayerInfoById(character.PlayerId, out _, out var teamId))
            {
                var info = WukongApi.PvP.PvpData(character);

                if (info.IsObserver)
                    continue;

                players++;
                if (info.IsReadyForPvP)
                {
                    readyCount++;
                    switch (teamId)
                    {
                        case PvpConstants.BlueTeamId:
                            blueTeamAnyReady = true;
                            break;
                        case PvpConstants.RedTeamId:
                            redTeamAnyReady = true;
                            break;
                    }
                }
            }
        }
        
        var updated = widgetManager.UpdateReadyCount(readyCount, players);
        if (!updated)
            return;

        if (!WukongApi.PvP.InPvP)
        {
            var allReady = readyCount == players && players > 0;

            foreach (var tamer in WukongApi.Sync.AreaTamers)
            {
                switch (tamer.TeamId)
                {
                    case PvpConstants.BlueTeamId:
                        blueTeamAnyReady = true;
                        break;
                    case PvpConstants.RedTeamId:
                        redTeamAnyReady = true;
                        break;
                }
            }

            var isSpectator = WukongApi.PvP.PvpData(localPlayer.Value).IsSpectator;
            if (allReady)
            {
                if (blueTeamAnyReady && redTeamAnyReady)
                {
                    pvpMode.StartLobbyCountdown(PvpConstants.CountdownSeconds);
                }
                else if (!isSpectator)
                {
                    // show a message that both teams need at least one ready player
                    widgetManager.SetThirdText(PvpTexts.BothTeamsNeedReadyPlayers);
                }
            }
            else
            {
                pvpMode.CancelLobbyCountdown();
            }
        }
    }
}