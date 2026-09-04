using System;
using System.Diagnostics;
using JetBrains.Annotations;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.ECS.Systems;

[UsedImplicitly]
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

        widgetManager.RefreshPlayerLists();
        widgetManager.RefreshWidgets();
    }
}