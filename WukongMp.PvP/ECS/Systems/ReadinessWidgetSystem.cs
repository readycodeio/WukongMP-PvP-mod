using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.UI;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;

namespace WukongMp.PvP.ECS.Systems;

[System]
public partial class ReadinessWidgetSystem(PvpWidgetManager widgetManager, IEntities entities)
{
    private int _lastReadyCount = -1;
    private int _lastTotalCount = -1;

    private void Update()
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
        if (readyCount == _lastReadyCount && players == _lastTotalCount)
            return;

        widgetManager.UpdateReadyCount(readyCount, players);
        _lastReadyCount = readyCount;
        _lastTotalCount = players;
    }
}