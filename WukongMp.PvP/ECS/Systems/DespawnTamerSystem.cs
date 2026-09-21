using System;
using System.Collections.Generic;
using b1;
using JetBrains.Annotations;
using WukongMp.Api;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Entities;
using WukongMp.Sdk.SDK;

namespace WukongMp.PvP.ECS.Systems;

[UsedImplicitly]
public class DespawnTamerSystem : ModSystemBase, IDisposable
{
    private readonly IGameEvents _gameEvents;
    private readonly Queue<BUTamerActor?> _pendingDeleteEvents = [];

    public DespawnTamerSystem(IGameEvents gameEvents)
    {
        _gameEvents = gameEvents;
        _gameEvents.OnMonsterDestroyed += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _gameEvents.OnMonsterDestroyed -= OnEntityDeleteHandler;
    }

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