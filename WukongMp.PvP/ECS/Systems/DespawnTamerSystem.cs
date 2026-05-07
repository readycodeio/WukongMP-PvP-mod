using System;
using System.Collections.Generic;
using b1;
using WukongMp.Api;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.ECS.Systems;

public class DespawnTamerSystem : ModSystemBase, IDisposable
{
    private readonly Queue<BUTamerActor?> _pendingDeleteEvents = [];

    public DespawnTamerSystem()
    {
        WukongApi.Events.OnMonsterDestroyed += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        WukongApi.Events.OnMonsterDestroyed -= OnEntityDeleteHandler;
    }

    private void OnEntityDeleteHandler(ReadyTamer tamer)
    {
        if (WukongApi.Sync.LocalPlayerId == null)
        {
            Logging.LogWarning("Local player ID is null, cannot despawn monster.");
            return;
        }

        tamer.HideMarker();
        _pendingDeleteEvents.Enqueue(tamer.Tamer);
    }

    protected override void OnUpdate(UpdateTick _)
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