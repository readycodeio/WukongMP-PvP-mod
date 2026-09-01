using b1;
using BtlShare;
using HarmonyLib;
using WukongMp.Api.Configuration;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.ECS;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Api.Implementation;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.Patches;

[HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchAttributeCheats
{
    public static bool Prefix(BUS_AttrComp __instance, EBGUAttrFloat AttrID, float NewValue, BUC_AttrContainer ___AttrContainer)
    {
        if (AttrID == EBGUAttrFloat.Hp)
        {
            return true;
        }
        
        if (!WukongApi.Sync.InArea)
            return true;

        var owner = __instance.GetOwner();
        var cheatsEnabled = WukongApi.Services.Resolve<CheatManager>().CheatsAllowed;

        if (!cheatsEnabled)
            return true;

        if (WukongApi.Sync.LocalMainCharacter is not { } player)
            return true;
        
        if (WukongApi.Sync.LocalMainCharacter?.Pawn != owner) 
            return true;
        
        var cheats = player.Get<CheatsComponent>();
        if (AttrID == EBGUAttrFloat.VigorEnergy && cheats is { SpiritCooldownEnabled: true, ShouldSetSpiritCooldown: false })
        {
            var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.VigorEnergy);
            var max = ___AttrContainer.GetFloatValue(EBGUAttrFloat.VigorEnergyMax);
            if (Equals(max, PvpConstants.FloatComparisonTolerance))
            {
                return true;
            }

            if (NewValue > current)
            {
                return false;
            }
        }

        if (AttrID == EBGUAttrFloat.FabaoEnergy && cheats.HasInfiniteVessel)
        {
            var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.FabaoEnergy);
            if (NewValue < current)
            {
                return false;
            }
        }

        if (AttrID == EBGUAttrFloat.CurEnergy && cheats.HasInfiniteTransform)
        {
            var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.CurEnergy);
            if (NewValue < current)
            {
                return false;
            }
        }

        if (AttrID == EBGUAttrFloat.Mp && cheats.HasInfiniteMana)
        {
            var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.Mp);
            if (NewValue < current)
            {
                return false;
            }
        }

        return true;
    }
}


[HarmonyPatch(typeof(FUStSkillSDesc), "get_CooldownTime")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchSkillCooldownTime
{
    public static void Postfix(ref float __result)
    {
        if (!WukongApi.Sync.InArea)
            return;
        
        var cheats = WukongApi.Services.Resolve<CheatManager>();

        if (cheats.CheatsAllowed && WukongApi.Sync.LocalMainCharacter is {} player)
        {
            __result *= player.Get<CheatsComponent>().InstantSkillCooldown ? 0f : 1f;
        }
    }
}
