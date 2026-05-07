using System.Collections.Generic;
using b1;
using ReadyM.Relay.Client.State;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.WukongUtils;

public static class PvpUtils
{
    private const string RedTeamColor = "(R=1,G=0.3,B=0.3)";
    private const string BlueTeamColor = "(R=0.3,G=0.3,B=1)";

    public static void ShowPvPCountDown()
    {
        var current = WukongApi.PvP.CurrentRound;
        var total = WukongApi.PvP.TournamentRounds;
        WukongApi.Widgets.ShowTip(string.Format(BuiltinTexts.RoundCount, current, total), true);
    }

    public static string GetTeamColorString(int teamId)
    {
        if (teamId == PvpConstants.RedTeamId)
            return RedTeamColor;
        if (teamId == PvpConstants.BlueTeamId)
            return BlueTeamColor;
        return "";
    }

    public static string GetLocalizedTeamName(int teamId)
    {
        if (teamId == PvpConstants.RedTeamId)
            return BuiltinTexts.RedTeam;
        if (teamId == PvpConstants.BlueTeamId)
            return BuiltinTexts.BlueTeam;
        return "";
    }

    public static int GetOppositeTeam(int teamId)
    {
        if (teamId == PvpConstants.DrawTeamId)
            return teamId;
        return teamId == PvpConstants.RedTeamId ? PvpConstants.BlueTeamId : PvpConstants.RedTeamId;
    }

    public static FVector GetSpawnPosition(BGUCharacterCS? pawn, int playerId, int maxPlayersCount)
    {
        var angle = playerId / (float)maxPlayersCount * 2f * FMath.PI;
        var x = FMath.Cos(angle) * PvpConstants.PvpStartingRadius;
        var y = FMath.Sin(angle) * PvpConstants.PvpStartingRadius;

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        var baseLocation = levelData.PvpStartingLocation + new FVector(x, y, 0f);

        return AdjustSpawnLocation(pawn, baseLocation);
    }

    public static FVector AdjustSpawnLocation(BGUCharacterCS? pawn, FVector InTargetLocation)
    {
        // For Heart of Birthstone map adjustment resulted in falling - invisible collision. So it is disabled for now.
        if (WukongApi.PvP.LevelId == 0)
        {
            return InTargetLocation;
        }

        var result = InTargetLocation;
        if (pawn == null)
        {
            return result;
        }

        var uCapsuleComponent = pawn.GetRootComponent() as UCapsuleComponent;
        if (uCapsuleComponent == null)
        {
            return result;
        }

        var scaledCapsuleHalfHeight = uCapsuleComponent.GetScaledCapsuleHalfHeight();
        var scaledCapsuleHalfHeight2 = uCapsuleComponent.GetScaledCapsuleHalfHeight();
        var num = 2.4f;
        var start = InTargetLocation + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        var end = InTargetLocation - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        if (UGSE_TraceFuncLib.CharacterCapsuleTraceSingleByProfile(GameUtils.GetWorld(), start, end, scaledCapsuleHalfHeight2, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, pawn, out var OutHitLocation))
        {
            result = OutHitLocation + num + FVector.UpVector * scaledCapsuleHalfHeight;
        }

        return result;
    }

    public static UnitLockTargetInfo BGUSelectLockTargetInRange(
        ACharacter? Owner,
        float FirstFilterMaxRange,
        EBSelectTargetRangeType RangeType,
        float AngleMax,
        FRotator MyDir,
        float DistScoreRating,
        AActor PreferActor,
        float PreferActorDistTolerance = 0.0f)
    {
        if (USharpExtensions.IsNullOrDestroyed(Owner))
            return new UnitLockTargetInfo();
        var actorLocation = Owner.BGUGetActorLocation();
        var b = MyDir.Vector() with { Z = 0.0f };
        List<ABGUCharacter> OutArray;
        UBGUSelectUtil.SphereOverlapBGUCharacters(Owner, actorLocation, FirstFilterMaxRange, out OutArray);
        ABGUCharacter? TargetActor = null;
        var SkeletonSocketName = "";
        var num1 = -1000f;
        var num2 = FirstFilterMaxRange;
        var flag1 = false;
        var flag2 = true;
        var fvector2D = FVector2D.ZeroVector;
        var playerController = UGSE_EngineFuncLib.GetFirstLocalPlayerController(Owner);
        if (playerController != null)
            fvector2D = UWidgetLayoutLibrary.GetViewportSize(playerController) / UWidgetLayoutLibrary.GetViewportScale(playerController);
        IBGC_CircusControlData readOnlyData1 = BGU_DataUtil.GetReadOnlyData<BGC_CircusControlData>(UGameplayStatics.GetGameState(Owner));
        for (var index = 0; index < OutArray.Count; ++index)
        {
            var bguCharacterCs = OutArray[index] as BGUCharacterCS;
            if (flag1)
                return !flag2 ? new UnitLockTargetInfo(TargetActor, ETargetSourceType.None, SkeletonSocketName: SkeletonSocketName) : new UnitLockTargetInfo(TargetActor, ETargetSourceType.None);
            if (bguCharacterCs != Owner
                && !BGUFunctionLibraryCS.BGUIsUnitDead(bguCharacterCs)
                && !BGUFunctionLibraryCS.BGUHasUnitSimpleState(bguCharacterCs, EBGUSimpleState.PhantomRush) // check for Phantom rush
                && BGUFunctionLibraryCS.BGUIsEnemyTeam(Owner, bguCharacterCs))
            {
                if (RangeType == EBSelectTargetRangeType.CameraLock)
                {
                    if (!BGUFunctionLibraryCS.BGUHasUnitSimpleState(bguCharacterCs, EBGUSimpleState.CantBeLock))
                    {
                        var unitCommDesc = BGW_GameDB.GetUnitCommDesc(bguCharacterCs.GetResID());
                        if (unitCommDesc != null)
                        {
                            num2 = unitCommDesc.CameraLockDist;
                            PreferActorDistTolerance = unitCommDesc.CameraLockDistTolerance;
                        }
                    }
                    else
                        continue;
                }

                var readOnlyData2 = BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(bguCharacterCs);
                if (readOnlyData2 != null)
                {
                    var flag3 = readOnlyData2.CachedLockSkeletonSocket.Count == 1 && readOnlyData2.CachedLockSkeletonSocket[0].Equals("CAMERA_LOCK");
                    foreach (var name in readOnlyData2.CachedLockSkeletonSocket)
                    {
                        // do not lock on Wukong's feet
                        if (name == PvpConstants.FeetCameraLockNode && bguCharacterCs is BGUPlayerCharacterCS)
                            continue;

                        if (!readOnlyData2.DisabledLockSkeletonSocket.Contains(name))
                        {
                            var fvector = bguCharacterCs.Mesh.GetSocketLocation(new FName(name));
                            var flag4 = Owner.GetController() != null && Owner.IsLocallyControlled();
                            var flag5 = true;
                            if (flag4)
                            {
                                FVector2D ScreenPosition;
                                UWidgetLayoutLibrary.ProjectWorldLocationToWidgetPosition(playerController, fvector, out ScreenPosition, false);
                                if (ScreenPosition.X <= 0.0 || ScreenPosition.Y <= 0.0 || ScreenPosition.X >= (double)fvector2D.X || ScreenPosition.Y >= (double)fvector2D.Y)
                                    flag5 = false;
                            }

                            if (flag3 && !flag5)
                                fvector = bguCharacterCs.BGUGetActorLocation();
                            if (flag4)
                            {
                                FVector2D ScreenPosition;
                                UWidgetLayoutLibrary.ProjectWorldLocationToWidgetPosition(playerController, fvector, out ScreenPosition, false);
                                if (ScreenPosition.X <= 0.0 || ScreenPosition.Y <= 0.0 || ScreenPosition.X >= (double)fvector2D.X || ScreenPosition.Y >= (double)fvector2D.Y)
                                    continue;
                            }

                            var num3 = ((fvector - actorLocation) with
                            {
                                Z = 0.0f
                            }).CosineAngle2D(b);
                            var num4 = FVector.Dist(actorLocation, fvector);
                            if (PreferActor != bguCharacterCs)
                            {
                                if (num4 > (double)num2)
                                    continue;
                            }
                            else if (num4 > num2 + (double)PreferActorDistTolerance)
                                continue;

                            if (flag4)
                            {
                                FVector Location;
                                Owner.GetController().GetPlayerViewPoint(out Location, out var _);
                                FHitResultSimple HitResult;
                                UBGUSelectUtil.LineTraceSimple(Owner, Location, fvector, ETraceTypeQuery.TraceTypeQuery1, false, out HitResult, new List<AActor>()
                                {
                                    bguCharacterCs
                                });
                                if (HitResult.IsBlockingHit && (readOnlyData1 == null || !readOnlyData1.IsInSameCircus(bguCharacterCs, HitResult.HitActor)))
                                    continue;
                            }

                            if (!flag1 && PreferActor != null && PreferActor == bguCharacterCs)
                            {
                                flag1 = true;
                                TargetActor = null;
                                num1 = -1000f;
                                SkeletonSocketName = "";
                                flag2 = true;
                            }

                            var num5 = -num4 * DistScoreRating + num3;
                            if (num5 > (double)num1)
                            {
                                num1 = num5;
                                TargetActor = bguCharacterCs;
                                SkeletonSocketName = name;
                                flag2 = flag3;
                            }
                        }
                    }
                }
            }
        }

        return !flag2 ? new UnitLockTargetInfo(TargetActor, ETargetSourceType.None, SkeletonSocketName: SkeletonSocketName) : new UnitLockTargetInfo(TargetActor, ETargetSourceType.None);
    }
}