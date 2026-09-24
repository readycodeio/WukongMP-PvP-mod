using b1;
using BtlShare;
using ReadyM.SDK.Attributes;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.PvP.Archetypes;
using WukongMp.PvP.Configuration;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Mixins;

namespace WukongMp.PvP.Services;

[Service]
public sealed partial class CheatCooldownsRefresh(CheatManager cheats)
{
    private float _vigorRegenAccumulator;

    private void Update()
    {
        if (!WukongApi.Local.IsGameplayLevel)
            return;

        if (!cheats.CheatsEnabled)
            return;

        if (WukongApi.Entities.LocalMainCharacter is not { } player)
            return;

        if (!player.SpiritCooldownEnabled)
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
        if (player.SpiritCooldownTime.Equals(0, PvpConstants.FloatComparisonTolerance))
        {
            player.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax));
            player.ShouldSetSpiritCooldown = false;
            return;
        }

        if (_vigorRegenAccumulator > player.SpiritCooldownTime)
            return;

        _vigorRegenAccumulator += Time.DeltaTime;
        var newVigorValue = FMath.Lerp(0, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax), FMath.Clamp(_vigorRegenAccumulator / player.SpiritCooldownTime, 0f, 1f));
        if (newVigorValue > currentVigorValue)
        {
            player.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, newVigorValue);
            player.ShouldSetSpiritCooldown = false;
        }
    }
}