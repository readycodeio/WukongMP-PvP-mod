using b1;
using BtlShare;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Api.Multiplayer.Serialization;
using WukongMp.Api;
using WukongMp.Api.Resources;
using WukongMp.PvP.UI;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP;

public partial class PvpRpc(TimerController timerController) : ClientRpcHandler
{
    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnShowAntiStallWarning(int warningTime)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (mainEntity.IsDead || mainEntity.IsSpectator)
            return;

        WukongApi.Local.ShowInfoMessage(BuiltinTexts.AntiStallWarning);
        timerController.SetTimer(0, warningTime);
        timerController.StartTimer();
        Logging.LogDebug("OnShowAntiStallWarning received");
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnShowAntiStallAction()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (mainEntity.IsDead || mainEntity.IsSpectator)
            return;

        WukongApi.Local.ShowInfoMessage(BuiltinTexts.StallingMessage);
        Logging.LogDebug("OnShowAntiStallAction received");
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnHideAntiStall()
    {
        if (WukongApi.Sync.LocalMainCharacter is null)
            return;

        WukongApi.Local.HideInfoMessage();
        timerController.StopTimer();
        Logging.LogDebug("OnHideAntiStallWarning received");
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnStallDamage(PlayerId damagedPlayer, float value)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (mainEntity.IsDead || mainEntity.IsSpectator)
            return;

        if (damagedPlayer == mainEntity.PlayerId)
        {
            // TODO: Move to player utils
            Logging.LogDebug("Applying stall damage: {Damage}%", value);
            var pawn = mainEntity.Pawn;
            if (pawn == null)
                return;

            var container = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn);
            var maxStamina = container?.GetFloatValue(EBGUAttrFloat.StaminaMax) ?? 1f;

            FSkillDamageConfig skillDamageConfig = new()
            {
                DamageCalcType = EDamageCalcType.HPMaxRatioAbs,
                HPMaxINV10000Damage_Abs = value * 100,
                DamageImmueLevel = 2,
                DmgReason = EDamageReason.FallDmg
            };

            var events = BUS_EventCollectionCS.Get(pawn);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Stamina, -(maxStamina * value / 100 * 3));
            events?.Evt_TriggerNormalDamageEffect.Invoke(null, in skillDamageConfig, default, new FBattleAttrSnapShot(null));
        }
    }
}