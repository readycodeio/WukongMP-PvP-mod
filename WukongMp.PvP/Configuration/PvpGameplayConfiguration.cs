using b1;
using BtlShare;
using ReadyM.Api.DI;
using ReadyM.SDK.Client.Entities;
using WukongMp.Api.WukongUtils;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.Configuration;

internal class PvpGameplayConfiguration(IWukongConfigurationApi configuration, IEntities entities) : IHostedService
{
    public void OnScopeStart()
    {
        configuration.IsSupportMultiLockEnabled = false;
        configuration.IsStrongDamageImmueEnabled = true;
        configuration.EnableCustomCameraArmLength = true;
        configuration.DisableCutscenes = true;
        configuration.SyncTamerTeamFromGameToEcs = false;
        configuration.OverrideLocalPlayerTeamFromGlobalEntity = true;
        configuration.DeleteDestroyedTamersFromEcs = true;

        configuration.SetDisableTamerAttackQuery(ShouldDisableTamerAttack);
        configuration.SetIsSkillEnabledQuery(IsSkillEnabled);

        configuration.SetIsPlayerInBattleQuery(() => entities.World.InPvP);
        configuration.SetIsInteractionAllowedQuery(IsInteractAllowed);
        configuration.SetIsTamerNotSynchronizedQuery(IsTamerNotSynchronized);
        configuration.SetIsAreaOverlapDisabledQuery(IsAreaOverlapDisabled);
    }

    public void Dispose()
    {
        configuration.ClearDisableTamerAttackQuery();
        configuration.ClearIsSkillEnabledQuery();
    }

    private bool ShouldDisableTamerAttack()
    {
        return !entities.World.InPvP;
    }

    private bool IsSkillEnabled(int skillId)
    {
        switch (skillId)
        {
            // Note: Phantom Rush is not a skill in code
            case PvpConstants.ImmobilizeSkillId when !entities.World.ImmobilizeAllowed:
            case PvpConstants.GourdSkillId when !entities.World.GourdAllowed:
            case PvpConstants.ConsumableBuffSkillId when !entities.World.ConsumablesAllowed:
            case PvpConstants.IncenseTrailTalismanSkillId:
            case PvpConstants.RuyiScrollSkillId:
                return false;
            default:
                // more skills here
                return true;
        }
    }

    private static bool IsInteractAllowed(EInteractType interactType)
    {
        return interactType != EInteractType.StandardObj && interactType != EInteractType.TaskNpc;
    }

    private static bool IsTamerNotSynchronized(string guid)
    {
        var currentLevelId = BGUFuncLibMap.GetCurLevelId(GameUtils.GetWorld());
        var levelTamers = LevelTamersConfig.GetLevelTamers(currentLevelId);
        return levelTamers.Contains(guid);
    }

    private static bool IsAreaOverlapDisabled(string guid)
    {
        var currentLevelId = BGUFuncLibMap.GetCurLevelId(GameUtils.GetWorld());
        var disabledAreas = LevelDisabledAreasConfig.GetDisabledAreas(currentLevelId);
        return disabledAreas.Contains(guid);
    }
}