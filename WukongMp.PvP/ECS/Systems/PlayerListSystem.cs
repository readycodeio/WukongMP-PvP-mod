using System;
using System.Diagnostics;
using ReadyM.SDK.Attributes;
using WukongMp.PvP.UI;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.ECS.Systems;

[System]
public partial class PlayerListSystem(PvpWidgetManager widgetManager)
{
    private readonly Stopwatch _timer = Stopwatch.StartNew();

    private void Update()
    {
        if (!WukongApi.Entities.CurrentArea.HasValue)
            return;

        if (_timer.Elapsed < TimeSpan.FromSeconds(1))
            return;

        _timer.Restart();

        widgetManager.RefreshPlayerLists();
        widgetManager.RefreshWidgets();
    }
}