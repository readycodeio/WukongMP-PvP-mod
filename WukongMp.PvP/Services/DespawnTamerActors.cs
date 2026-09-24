using System.Collections.Generic;
using b1;
using ReadyM.SDK.Attributes;
using WukongMp.Api;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.PvP.Services;

[Service]
public sealed partial class DespawnTamerActors
{
    private readonly Queue<BUTamerActor?> _pendingDeleteEvents = [];

    [DeleteHandler(typeof(Tamer))]
    private void OnTamerDeleted(Tamer tamer)
    {
        if (WukongApi.Entities.LocalMainCharacter == null)
        {
            Logging.LogWarning("Local player ID is null, cannot despawn monster.");
            return;
        }

        tamer.As<Character>().HideMarker();
        _pendingDeleteEvents.Enqueue(tamer.TamerActor);
    }

    private void Update()
    {
        if (!WukongApi.Local.IsGameplayLevel)
            return;

        while (_pendingDeleteEvents.Count > 0)
        {
            var pending = _pendingDeleteEvents.Dequeue();
            pending?.CurrentRef?.DestroyTamer();
        }
    }
}