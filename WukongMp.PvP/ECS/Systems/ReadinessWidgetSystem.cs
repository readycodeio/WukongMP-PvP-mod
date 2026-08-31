using JetBrains.Annotations;
using WukongMp.Pvp.Common;
using WukongMp.PvP.Extensions;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.ECS.Systems;

[UsedImplicitly]
public class ReadinessWidgetSystem(PvpWidgetManager widgetManager) : ModSystemBase
{
    private int lastReadyCount = -1;
    private int lastTotalCount = -1;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.CurrentAreaId.HasValue || WukongApi.Services.Resolve<WukongPvpApi>().InPvpTournament)
            return;

        var players = 0;
        var readyCount = 0;

        foreach (var character in WukongApi.Sync.AreaMainCharacters)
        {
            var pvp = character.Get<PvPComponent>();

            if (character.IsObserver)
                continue;

            players++;
            if (pvp.IsReadyForPvP)
            {
                readyCount++;
            }
        }

        // prevent spamming the widget with updates every frame when nothing has changed
        if (readyCount == lastReadyCount && players == lastTotalCount)
            return;

        widgetManager.UpdateReadyCount(readyCount, players);
        lastReadyCount = readyCount;
        lastTotalCount = players;
    }
}