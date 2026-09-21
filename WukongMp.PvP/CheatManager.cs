using System.Globalization;
using BtlShare;
using ReadyM.SDK.Client.Entities;
using UnrealEngine.Runtime;
using WukongMp.PvP.Archetypes;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.Resources;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Mixins;
using BGU_DataUtil = b1.BGU_DataUtil;
using BGUFunctionLibraryCS = b1.BGUFunctionLibraryCS;
using BUC_AttrContainer = b1.BUC_AttrContainer;
using BUS_EventCollectionCS = b1.BUS_EventCollectionCS;
using IBUC_AttrContainer = b1.IBUC_AttrContainer;

namespace WukongMp.PvP;

public sealed class CheatManager(IEntities entities)
{
    public bool CheatsEnabled => entities.World.CheatsEnabled;

    public void ToggleInfiniteMana()
    {
        if (!CheatsEnabled)
        {
            WukongApi.Chat.ShowLocalMessage(PvpTexts.CheatsAreDisabled, FLinearColor.Gray);
            return;
        }

        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
            return;

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(mainEntity.Pawn);
            var maxMana = attrContainer.GetFloatValue(EBGUAttrFloat.MpMax);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.Mp, maxMana);
        }

        mainEntity.HasInfiniteMana = !mainEntity.HasInfiniteMana;
        WukongApi.Chat.ShowLocalMessage(string.Format(mainEntity.HasInfiniteMana ? PvpTexts.InfManaEnabled : PvpTexts.InfManaDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }

    public void SetSpritCooldownTime(float spiritCooldownTime)
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
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
        
        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            mainEntity.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.VigorEnergyMax));
            mainEntity.ShouldSetSpiritCooldown = false;
        }

        mainEntity.SpiritCooldownEnabled = true;
        mainEntity.SpiritCooldownTime = spiritCooldownTime;

        WukongApi.Chat.ShowLocalMessage(string.Format(PvpTexts.CustomSpiritCooldown, mainEntity.Nickname, spiritCooldownTime.ToString(CultureInfo.InvariantCulture)), FLinearColor.Gray);
    }

    public void ToggleInfiniteVessel()
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
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
        
        mainEntity.HasInfiniteVessel = !mainEntity.HasInfiniteVessel;
        WukongApi.Chat.ShowLocalMessage(string.Format(mainEntity.HasInfiniteVessel ? PvpTexts.InfVesselEnabled : PvpTexts.InfVesselDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }

    public void ToggleInfiniteTransform()
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
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

        mainEntity.HasInfiniteTransform = !mainEntity.HasInfiniteTransform;

        WukongApi.Chat.ShowLocalMessage(string.Format(mainEntity.HasInfiniteTransform ? PvpTexts.InfTransformEnabled : PvpTexts.InfTransformDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }

    public void ToggleNoSkillsCooldown()
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
            return;

        if (!CheatsEnabled)
        {
            WukongApi.Console.LogMessage(PvpTexts.CheatsAreDisabled);
            return;
        }

        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_ResetSkillCD.Invoke();

        mainEntity.InstantSkillCooldown = !mainEntity.InstantSkillCooldown;

        WukongApi.Chat.ShowLocalMessage(string.Format(mainEntity.InstantSkillCooldown ? PvpTexts.InstantCooldownEnabled : PvpTexts.InstantCooldownDisabled, mainEntity.Nickname), FLinearColor.Gray);
    }
}