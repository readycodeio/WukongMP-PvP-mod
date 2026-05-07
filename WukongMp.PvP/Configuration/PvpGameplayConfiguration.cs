using b1;
using BtlShare;
using ReadyM.Api.DI;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.Configuration
{
    internal class PvpGameplayConfiguration(IWukongConfigurationApi configuration) : IHostedService
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

            configuration.SetIsPlayerInBattleQuery(() => WukongApi.PvP.InPvP);
            configuration.SetIsInteractionAllowedQuery(IsInteractAllowed);
            configuration.SetIsTamerNotSynchronizedQuery(IsTamerNotSynchronized);
            configuration.SetIsAreaOverlapDisabledQuery(IsAreaOverlapDisabled);
        }

        public void Dispose()
        {
            configuration.ClearDisableTamerAttackQuery();
            configuration.ClearIsSkillEnabledQuery();
        }

        private static bool ShouldDisableTamerAttack()
        {
            return !WukongApi.PvP.InPvP;
        }

        private static bool IsSkillEnabled(int skillId)
        {
            switch (skillId)
            {
                // Note: Phantom Rush is not a skill in code
                case PvpConstants.ImmobilizeSkillId when !WukongApi.PvP.ImmobilizeAllowed:
                case PvpConstants.GourdSkillId when !WukongApi.PvP.GourdAllowed:
                case PvpConstants.ConsumableBuffSkillId when !WukongApi.PvP.ConsumablesAllowed:
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
}