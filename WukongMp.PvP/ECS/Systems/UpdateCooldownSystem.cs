using b1;
using BtlShare;
using JetBrains.Annotations;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.PvP.Configuration;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.ECS.Systems;

[UsedImplicitly]
public class UpdateCooldownSystem(CheatManager cheats) : ModSystemBase
{
    private float _vigorRegenAccumulator;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Local.IsGameplayLevel)
            return;

        if (!cheats.CheatsEnabled)
            return;

        if (WukongApi.Sync.LocalMainCharacter is not { } player)
            return;

        ref var cheatsComponent = ref player.Get<CheatsComponent>();
        if (!cheatsComponent.SpiritCooldownEnabled)
            return;

        var localPawn = player.Pawn;
        if (localPawn == null)
            return;

        var magicallyChangeData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_MagicallyChangeData>(localPawn);

        if (magicallyChangeData.DurMagicallyChange)
        {
            _vigorRegenAccumulator = 0f;
            return;
        }

        var currentVigorValue = BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergy);
        if (currentVigorValue.Equals(0, PvpConstants.FloatComparisonTolerance))
        {
            _vigorRegenAccumulator = 0f;
        }

        var events = BUS_EventCollectionCS.Get(localPawn);
        if (cheatsComponent.SpiritCooldownTime.Equals(0, PvpConstants.FloatComparisonTolerance))
        {
            cheatsComponent.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax));
            cheatsComponent.ShouldSetSpiritCooldown = false;
            return;
        }

        if (_vigorRegenAccumulator > cheatsComponent.SpiritCooldownTime)
            return;

        _vigorRegenAccumulator += tick.deltaTime;
        var newVigorValue = FMath.Lerp(0, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax), FMath.Clamp(_vigorRegenAccumulator / cheatsComponent.SpiritCooldownTime, 0f, 1f));
        if (newVigorValue > currentVigorValue)
        {
            cheatsComponent.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, newVigorValue);
            cheatsComponent.ShouldSetSpiritCooldown = false;
        }
    }
}
