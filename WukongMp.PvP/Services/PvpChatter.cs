using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.Resources;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.SDK;

namespace WukongMp.PvP.Services;

[Service]
public sealed partial class PvpChatter(CheatManager cheatManager, IEntities entities, IGameEvents events)
{
    private void Start()
    {
        events.OnPlayerDead += OnPlayerDead;
        events.OnLoadingScreenClose += OnLoadingScreenClose;
    }

    private void Stop()
    {
        events.OnLoadingScreenClose -= OnLoadingScreenClose;
        events.OnPlayerDead -= OnPlayerDead;
    }

    private void OnPlayerDead(MainCharacter victim, Character? attacker)
    {
        if (!entities.World.InPvP || !attacker.HasValue)
            return;

        if (victim.PlayerId != WukongApi.Entities.LocalPlayer?.PlayerId)
            return;
        
        AActor? pawn = attacker.Value.TryAs<MappedCharacter>(out var attackerMain) ? attackerMain.Pawn : 
            attacker.Value.TryAs<MappedTamer>(out var attackerTamer) ? attackerTamer.Pawn
            : null;

        if (victim.Pawn == pawn)
            return;

        if (!WukongApi.Entities.TryGetByActor(pawn, out MainCharacter attackerEntity))
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