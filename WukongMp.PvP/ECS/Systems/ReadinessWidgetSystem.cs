using JetBrains.Annotations;
using ReadyM.SDK.Client.Entities;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;

namespace WukongMp.PvP.ECS.Systems;

[UsedImplicitly]
public class ReadinessWidgetSystem(PvpWidgetManager widgetManager, IEntities entities) : ModSystemBase
{
    private int lastReadyCount = -1;
    private int lastTotalCount = -1;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Entities.CurrentArea.HasValue || entities.World.InTournament)
            return;

        var players = 0;
        var readyCount = 0;

        foreach (var character in WukongApi.Entities.AreaMainCharacters)
        {
            if (character.IsObserver)
                continue;

            players++;
            if (character.IsReadyForPvP)
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