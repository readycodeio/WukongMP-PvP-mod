using System.Collections.Generic;
using System.IO;
using System.Reflection;
using b1;
using b1.BGW;
using b1.UI.Comm;
using B1UI.GSUI;
using CSharpModBase;
using GSE.GSUI;
using HarmonyLib;
using LiteNetLib;
using PreludeLib.Attributes;
using ResB1;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Resources;
using WukongMp.Sdk.Api;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace WukongMp.PvP.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Global)]
public static class PatchStartGameUiPvp
{
    [HarmonyTargetMethodHint("B1UI.GSUI.UIStartGame", "OnUIPageConstructImpl")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("B1UI.GSUI.UIStartGame:OnUIPageConstructImpl");
    }

    public static void Postfix(GSUIView __instance, ref List<VIButtonBaseV2> ___StartGameBtnList, ref UTextBlock ___TxtMainName, ref UTextBlock ___TxtSubName, DSStartGame ___DataStore, ref UCanvasPanel ___RegionNameCon)
    {
        var playerMarkerActorClass = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UClass>(PvpConstants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);
        var hasPak = playerMarkerActorClass != null;
        var isConnected = WukongApi.Sync.IsConnected;
        if (!hasPak)
        {
            WukongApi.Local.ShowTip(BuiltinTexts.MissingPak, false);
            Logging.LogError("WukongMP.pak is not loaded. Could not continue game.");
        }
        else if (!isConnected)
        {
            WukongApi.Sync.GetDisconnectReasonAndInvoke(reason => { Utils.TryRunOnGameThread(() => { WukongApi.Local.ShowInfoMessage(reason == DisconnectReason.ConnectionRejected ? BuiltinTexts.ConnectionRejectedByServer : BuiltinTexts.Disconnected); }); });
            Logging.LogError(" PvP Disconnected. Could not continue game.");
        }

        for (int j = ___DataStore.BtnDataList.Count - 1; j >= 0; j--)
        {
            DSButtonBase BtnBase2 = ___DataStore.BtnDataList[j];
            var buttonName = BtnBase2.Name.Value.ToString();

            Logging.LogDebug("Button name: {Name}, id: {Id}", buttonName, BtnBase2.Id.Value);

            if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME).ToString())
            {
                Logging.LogDebug("Continue UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME));

                var slot = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, PvpConstants.CharacterArchiveId);
                var savePath = FPaths.Combine(WukongApi.Files.GetModDirectory<Mod>(), $"{slot}.sav");

                if (!hasPak || !isConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);
                }
                else if (File.Exists(savePath))
                {
                    ___StartGameBtnList[j].SetTxtName(FText.FromString(PvpTexts.QuickJoin));
                }
                else
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);
                }

                // Clear OnGSButtonUnFocused event form the continue game button.
                var type = ___StartGameBtnList[j].GetBUIButton().GetType();
                var field = type.GetField(nameof(BUI_Button.OnGSButtonUnFocused), BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(___StartGameBtnList[j].GetBUIButton(), null);
            }
            else if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME).ToString())
            {
                Logging.LogDebug("New game UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME));
                if (!hasPak || !isConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);
                }
                else
                {
                    ___StartGameBtnList[j].SetTxtName(FText.FromString(PvpTexts.NewCharacter));
                }
            }
            else if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.LOAD_GAME).ToString())
            {
                Logging.LogDebug("Load game UI name desc : {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.LOAD_GAME));
                if (!hasPak || !isConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);
                }
                else
                {
                    ___StartGameBtnList[j].SetTxtName(FText.FromString(PvpTexts.SelectCharacter));
                }
            }
            else if (buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME).ToString() && buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.START_GAME_SETTING).ToString())
            {
                Logging.LogDebug("UI name desc to hide: {Description}", buttonName);
                ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                ___StartGameBtnList.RemoveAt(j);
            }
        }

        __instance.GSAnimKeyToState("GSAKBContinueBtn", "CBtnFocus");

        ___TxtMainName.SetText(FText.FromString(""));
        ___TxtSubName.SetText(FText.FromString("Wukong Multiplayer Mod"));
        ___TxtSubName.SetRenderScale(new FVector2D(1.2, 1.2));
        ___RegionNameCon.SetVisibility(ESlateVisibility.SelfHitTestInvisible);
    }
}

[HarmonyPatch(typeof(UIBattleMainCon), "OnClickOpenMapUI")]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchOnClickOpenMapUI
{
    public static bool Prefix()
    {
        if (!WukongApi.Sync.InArea)
            return true;

        return false;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchShrineRegisterFunc
{
    [HarmonyTargetMethodHint(typeof(FMenuHelper<EShrineMenuTag>), "RegisterFunc")]
    public static MethodBase TargetMethod()
    {
        var specializedType = typeof(FMenuHelper<EShrineMenuTag>);
        return specializedType.GetMethod("RegisterFunc")!;
    }

    public static bool Prefix(int FuncId)
    {
        if (!WukongApi.Sync.InArea)
            return true;

        InteractionFuncDesc interactionFuncDesc = GameDBRuntime.GetInteractionFuncDesc(FuncId);
        return interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.Teleport
               && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossIterations
               && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossRechallenge;
    }
}

[HarmonyPatch(typeof(GSEUtil), "GetCanTeleportGroupMapList")]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchGetCanTeleportGroupMapList
{
    public static bool Prefix(ref List<int> __result)
    {
        if (!WukongApi.Sync.InArea)
            return true;

        __result = [];
        return false;
    }
}

[HarmonyPatch(typeof(UISaveTips), "OnChangeSaveTipsStat")]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchOnChangeSaveTipsStat
{
    public static bool Prefix(UWidget ___RootCon)
    {
        ___RootCon.SetVisibility(ESlateVisibility.Collapsed);
        return false;
    }
}

[HarmonyPatch(typeof(BUI_BattleInfoCS), "SetDamageNumCanEnabled")]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchSetDamageNumCanEnabled
{
    public static void Prefix(ref bool InIsDamageNumCanEnabled)
    {
        InIsDamageNumCanEnabled = true;
    }
}

[HarmonyPatch(typeof(UBGWFunctionLibraryCS), "IsShowSettingUiOnly")]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class PatchIsShowSettingUiOnly
{
    public static bool Prefix(ref bool __result)
    {
        if (!WukongApi.Sync.InArea)
            return true;

        if (WukongApi.PvP.InPvpTournament)
        {
            __result = true;
            return false;
        }

        return true;
    }
}

// TODO: Maybe there's a way to fix free floating camera after exiting menu without prohibiting it altogether
[HarmonyPatch(typeof(UIBattleMainCon), "OnClickOpenEquipUI")]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class PatchOnClickOpenEquipUI
{
    public static bool Prefix()
    {
        return WukongApi.Sync.LocalMainCharacter?.IsSpectator is not true;
    }
}

[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.IsArchiveNewGameplusReady))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchNewGamePlusArchiveCheck
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.IsNewGameplusReady))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchNewGamePlusCheck
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.LatestArchiveNewGameplus))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchNewGamePlusLatestCheck
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}