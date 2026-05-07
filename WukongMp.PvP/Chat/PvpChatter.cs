using ReadyM.Api.DI;
using WukongMp.PvP.Resources;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.Chat;

public class PvpChatter : IHostedService
{
    public void OnScopeStart()
    {
        WukongApi.Events.OnPlayerDead += OnPlayerDead;
    }

    public void Dispose()
    {
        WukongApi.Events.OnPlayerDead -= OnPlayerDead;
    }

    private void OnPlayerDead(ReadyMainCharacter victim, ReadyCharacter? attacker)
    {
        if (!WukongApi.PvP.InPvP || !attacker.HasValue) 
            return;

        if (victim.PlayerId != WukongApi.Sync.LocalPlayerId)
            return;
        
        if (victim.Pawn == attacker.Value.Pawn) 
            return;
        
        if (WukongApi.Sync.GetPlayerEntityByActor(attacker.Value.Pawn) is not { } attackerEntity)
            return;

        var msg = string.Format(PvpTexts.PlayerKilledPlayer, attackerEntity.Nickname, victim.Nickname);
        WukongApi.Chat.SendServerMessage(msg);
    }
}