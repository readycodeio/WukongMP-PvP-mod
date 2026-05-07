using System.Linq;
using System.Reflection;
using b1;
using HarmonyLib;
using PreludeLib.Attributes;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace WukongMp.PvP.Patches;

[HarmonyPatch(typeof(BPC_PlayerRoleData), "GetNewGamePlusCount")]
[HarmonyPatchCategory(PatchCategory.Connected)]
public static class PatchGetNewGamePlusCount
{
    public static bool Prefix(ref int __result)
    {
        if (!WukongApi.Sync.InArea)
            return true;
        if (WukongApi.Sync.CurrentAreaId == null)
            return true;

        __result = WukongApi.PvP.EnemiesNgPlusLevel + 1;
        return false;
    }
}

/// <summary>
/// Only reset character Team ID if it was not set by us.
/// This prevents the game from resetting the team ID of monsters assigned to player teams in PvP.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class TamerResetPatch
{
    [HarmonyTargetMethodHint("b1.BUS_TeamIDManageComp", "OnResetTeamID")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_TeamIDManageComp:OnResetTeamID");
    }

    public static bool Prefix(BGUCharacterCS ___OwnerAsCharacterCS)
    {
        if (!WukongApi.Sync.InArea)
            return true;

        var teamId = ___OwnerAsCharacterCS.GetTeamIDInCS();
        return !PvpConstants.CompetingTeamIds.Contains(teamId);
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "OnCameraLockTarget")]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class PlayerCameraLockPatch
{
    public static bool Prefix(UnitLockTargetInfo TargetInfo)
    {
        if (TargetInfo is { LockTargetActor: BGUPlayerCharacterCS, LockTargetSkeletonSocketName: PvpConstants.FeetCameraLockNode })
            return false;

        if (BGUFunctionLibraryCS.BGUHasUnitSimpleState(TargetInfo.LockTargetActor, EBGUSimpleState.PhantomRush))
            return false;

        return true;
    }
}

[HarmonyPatch(typeof(BUS_PlayerCameraCompImpl), "UpdateCameraState_AnyThread")]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class FixTransformCameraLockToOriginPatch
{
    private static MethodInfo? TargetGetter;
    private static MethodInfo? CameraStateGetter;

    public static void Prefix(BUS_PlayerCameraCompImpl __instance)
    {
        if (!WukongApi.Sync.InArea)
            return;

        TargetGetter ??= AccessTools.PropertyGetter(typeof(BUS_PlayerCameraCompImpl), "Target");
        CameraStateGetter ??= AccessTools.PropertyGetter(typeof(BUS_PlayerCameraCompImpl), "CameraState");

        var target = (AActor?)TargetGetter.Invoke(__instance, null);

        if (target == null || target is not BGUCharacterCS targetCharacter)
            return;

        var cameraState = (BUC_CameraState?)CameraStateGetter.Invoke(__instance, null);

        if (cameraState == null)
            return;

        // FIXME: This seems like a hack?
        // we are forced to look at origin
        if (cameraState.TargetSoulFocusPos.Equals(FVector.ZeroVector, PvpConstants.FloatComparisonTolerance))
        {
            var owner = __instance.GetOwner() as BGUCharacterCS;

            if (owner == null)
                return;

            var entity = WukongApi.Sync.GetPlayerEntityByLastTransformation(targetCharacter);
            if (entity.HasValue)
            {
                var events = BUS_EventCollectionCS.Get(owner);
                events?.Evt_Camera_ManualLock?.Invoke(entity.Value.Pawn, PvpConstants.ChestCameraLockNode);
            }
        }
    }
}

[HarmonyPatch(typeof(BGUFuncLibSelectTargetsCS), nameof(BGUFuncLibSelectTargetsCS.BGUSelectLockTargetInRange))]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class PatchBGUSelectLockTargetInRange
{
    public static bool Prefix(
        ref UnitLockTargetInfo __result,
        ACharacter Owner,
        float FirstFilterMaxRange,
        EBSelectTargetRangeType RangeType,
        float AngleMax,
        FRotator MyDir,
        float DistScoreRating,
        AActor PreferActor,
        float PreferActorDistTolerance = 0.0f)
    {
        __result = PvpUtils.BGUSelectLockTargetInRange(Owner, FirstFilterMaxRange, RangeType, AngleMax, MyDir, DistScoreRating, PreferActor, PreferActorDistTolerance);
        return false;
    }
}