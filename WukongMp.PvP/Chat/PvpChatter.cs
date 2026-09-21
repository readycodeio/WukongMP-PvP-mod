using ReadyM.Api.DI;
using ReadyM.SDK.Client.Entities;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.Resources;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.SDK;

namespace WukongMp.PvP.Chat;

public class PvpChatter(CheatManager cheatManager, IEntities entities, IGameEvents events) : IHostedService
{
    public void OnScopeStart()
    {
        events.OnPlayerDead += OnPlayerDead;
        events.OnLoadingScreenClose += OnLoadingScreenClose;
    }

    public void Dispose()
    {
        events.OnPlayerDead -= OnPlayerDead;
    }

    private void OnPlayerDead(MainCharacter victim, Character? attacker)
    {
        if (!entities.World.InPvP || !attacker.HasValue)
            return;

        if (victim.PlayerId != WukongApi.Sync.LocalPlayerId)
            return;
        
        AActor? pawn = attacker.Value.TryAs<MappedCharacter>(out var attackerMain) ? attackerMain.Pawn : 
            attacker.Value.TryAs<MappedMonster>(out var attackerTamer) ? attackerTamer.Pawn
            : null;

        if (victim.Pawn == pawn)
            return;

        if (WukongApi.Entities.GetPlayerEntityByActor(pawn) is not { } attackerEntity)
            return;

        var msg = string.Format(PvpTexts.PlayerKilledPlayer, attackerEntity.Nickname, victim.Nickname);
        WukongApi.Chat.SendServerMessage(msg);
    }

    private void OnLoadingScreenClose()
    {
        if (WukongApi.Local.IsGameplayLevel && cheatManager.CheatsEnabled)
        {
            WukongApi.Chat.ShowLocalMessage(PvpTexts.CheatsEnabled, FLinearColor.Gray);
        }
    }
}