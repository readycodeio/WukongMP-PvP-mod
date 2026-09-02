using System.Globalization;
using BtlShare;
using UnrealEngine.Runtime;
using WukongMp.Api.Resources;
using WukongMp.PvP.Resources;
using WukongMp.Pvp.Common.ECS;
using WukongMp.PvP.ECS;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;
using BGU_DataUtil = b1.BGU_DataUtil;
using BGUFunctionLibraryCS = b1.BGUFunctionLibraryCS;
using BUC_AttrContainer = b1.BUC_AttrContainer;
using BUS_EventCollectionCS = b1.BUS_EventCollectionCS;
using IBUC_AttrContainer = b1.IBUC_AttrContainer;

namespace WukongMp.PvP;

public sealed class CheatManager
{
    public bool CheatsEnabled => WukongApi.Sync.TryGetGlobalComponent<PvpStateComponent>(out var state) && state.CheatsEnabled;

    public void ToggleInfiniteMana()
    {
        if (!CheatsEnabled)
        {
            WukongApi.Chat.ShowLocalMessage(PvpTexts.CheatsAreDisabled, FLinearColor.Gray);
            return;
        }

        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        ref var cheatsComp = ref mainEntity.Get<CheatsComponent>();
        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(mainEntity.Pawn);
            var maxMana = attrContainer.GetFloatValue(EBGUAttrFloat.MpMax);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.Mp, maxMana);
        }

        cheatsComp.HasInfiniteMana = !cheatsComp.HasInfiniteMana;
        WukongApi.Chat.ShowLocalMessage(string.Format(cheatsComp.HasInfiniteMana ? PvpTexts.InfManaEnabled : PvpTexts.InfManaDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }

    public void SetSpritCooldownTime(float spiritCooldownTime)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!CheatsEnabled)
        {
            WukongApi.Console.LogMessage(PvpTexts.CheatsAreDisabled);
            return;
        }

        if (spiritCooldownTime < 0)
        {
            WukongApi.Console.LogMessage(PvpTexts.InvalidCooldown);
            return;
        }

        ref var cheatsComp = ref mainEntity.Get<CheatsComponent>();

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            cheatsComp.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.VigorEnergyMax));
            cheatsComp.ShouldSetSpiritCooldown = false;
        }

        cheatsComp.SpiritCooldownEnabled = true;
        cheatsComp.SpiritCooldownTime = spiritCooldownTime;

        WukongApi.Chat.ShowLocalMessage(string.Format(PvpTexts.CustomSpiritCooldown, mainEntity.Nickname, spiritCooldownTime.ToString(CultureInfo.InvariantCulture)), FLinearColor.Gray);
    }

    public void ToggleInfiniteVessel()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!CheatsEnabled)
        {
            WukongApi.Console.LogMessage(PvpTexts.CheatsAreDisabled);
            return;
        }

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.FabaoEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.FabaoEnergyMax));
        }

        ref var cheatsComp = ref mainEntity.Get<CheatsComponent>();

        cheatsComp.HasInfiniteVessel = !cheatsComp.HasInfiniteVessel;
        WukongApi.Chat.ShowLocalMessage(string.Format(cheatsComp.HasInfiniteVessel ? PvpTexts.InfVesselEnabled : PvpTexts.InfVesselDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }

    public void ToggleInfiniteTransform()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!CheatsEnabled)
        {
            WukongApi.Console.LogMessage(PvpTexts.CheatsAreDisabled);
            return;
        }

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.CurEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.TransEnergyMax));
        }

        ref var cheatsComp = ref mainEntity.Get<CheatsComponent>();
        cheatsComp.HasInfiniteTransform = !cheatsComp.HasInfiniteTransform;

        WukongApi.Chat.ShowLocalMessage(string.Format(cheatsComp.HasInfiniteTransform ? PvpTexts.InfTransformEnabled : PvpTexts.InfTransformDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }

    public void ToggleNoSkillsCooldown()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!CheatsEnabled)
        {
            WukongApi.Console.LogMessage(PvpTexts.CheatsAreDisabled);
            return;
        }

        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_ResetSkillCD.Invoke();

        ref var cheatsComp = ref mainEntity.Get<CheatsComponent>();
        cheatsComp.InstantSkillCooldown = !cheatsComp.InstantSkillCooldown;

        WukongApi.Chat.ShowLocalMessage(string.Format(cheatsComp.InstantSkillCooldown ? PvpTexts.InstantCooldownEnabled : PvpTexts.InstantCooldownDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }
}