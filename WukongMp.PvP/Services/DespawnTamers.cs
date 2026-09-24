using System.Collections.Generic;
using b1;
using ReadyM.SDK.Attributes;
using WukongMp.Api;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.SDK;

namespace WukongMp.PvP.Services;

[Service]
public sealed partial class DespawnTamers(IGameEvents gameEvents)
{
    private readonly Queue<BUTamerActor?> _pendingDeleteEvents = [];

    private void Start() => gameEvents.OnMonsterDestroyed += OnEntityDeleteHandler;

    private void Stop() => gameEvents.OnMonsterDestroyed -= OnEntityDeleteHandler;

    // TODO: [DeleteHandler]
    private void OnEntityDeleteHandler(Tamer tamer)
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