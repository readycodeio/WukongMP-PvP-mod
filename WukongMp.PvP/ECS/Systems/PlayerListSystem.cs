using System;
using System.Collections.Generic;
using System.Diagnostics;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.ECS.Systems;

public class PlayerListSystem(PvpWidgetManager widgetManager) : ModSystemBase
{
    private readonly Stopwatch _timer = Stopwatch.StartNew();

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue)
            return;

        if (_timer.Elapsed < TimeSpan.FromSeconds(1))
            return;

        _timer.Restart();

        List<string> redTeamList = [];
        List<string> blueTeamList = [];
        List<string> spectatorsList = [];

        foreach (var areaPlayer in WukongApi.Sync.AreaPlayers)
        {
            if (WukongApi.Sync.TryGetPlayerInfoById(areaPlayer, out var nickname, out var team))
            {
                switch (team)
                {
                    case PvpConstants.RedTeamId:
                        redTeamList.Add(nickname);
                        break;
                    case PvpConstants.BlueTeamId:
                        blueTeamList.Add(nickname);
                        break;
                    case PvpConstants.SpectatorTeamId:
                        spectatorsList.Add(nickname);
                        break;
                }
            }
        }

        widgetManager.SetTeams(redTeamList, blueTeamList, spectatorsList);
        widgetManager.RefreshWidgets();
    }
}