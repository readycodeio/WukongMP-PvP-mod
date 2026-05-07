using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.PvP.Configuration;

namespace WukongMp.PvP.UI;

public class LobbyStatusWidget() : GameWidgetBase(LobbyStatusWidgetPath)
{
    private const string LobbyStatusWidgetPath = "/Game/Mods/WukongMod/WBP_LobbyStatus.WBP_LobbyStatus_C";

    private int currentReadyCount;
    private int maxReadyCount;

    public void SetConnectedCount(int count)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetConnectedCount {count}", true);
    }

    public void SetMaxConnectedCount(int count)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetMaxConnectedCount {count}", true);
    }

    public bool SetReadyCount(int count, int maxCount)
    {
        if (count == currentReadyCount && maxCount == maxReadyCount)
            return false;

        currentReadyCount = count;
        maxReadyCount = maxCount;
        GameWidget?.CallFunctionByNameWithArguments($"SetReadyCount {count}", true);
        GameWidget?.CallFunctionByNameWithArguments($"SetMaxReadyCount {maxCount}", true);
        return true;
    }

    public void UpdatePlayerTeam(string nickName, int teamId)
    {
        RemovePlayerFromTeams(nickName);
        if (teamId == PvpConstants.SpectatorTeamId)
        {
            AddSpectator(nickName);
        }
        else if (teamId == PvpConstants.CompetingTeamIds[0])
        {
            AddToTeam1(nickName);
        }
        else if (teamId == PvpConstants.CompetingTeamIds[1])
        {
            AddToTeam2(nickName);
        }
    }

    private void RemovePlayerFromTeams(string nickName)
    {
        RemoveFromTeam1(nickName);
        RemoveFromTeam2(nickName);
        RemoveSpectator(nickName);
    }

    public unsafe void SetTeams(List<string> redTeamList, List<string> blueTeamList, List<string> spectatorsList)
    {
        if (GameWidget == null || SetText_RedTeamList_PropertyAddress == null || SetText_BlueTeamList_PropertyAddress == null || SetText_SpectatorsList_PropertyAddress == null)
        {
            Logging.LogError("GameWidget or property address is null in WBP_LobbyStatus_C:SetTeams.");
            return;
        }

        if (!SetTeams_IsValid)
        {
            Logging.LogError("Function WBP_LobbyStatus_C:SetTeams is not valid.");
            return;
        }

        byte* ptr = stackalloc byte[(int)(uint)(SetTeams_ParamsSize + 16)];
        int num = (int)((16L - (long)ptr) & 0xF);
        byte* ptr2 = ptr + num;
        Unsafe.InitBlockUnaligned((void*)ptr2, (byte)0, (uint)SetTeams_ParamsSize);
        IntPtr intPtr = new IntPtr(ptr2);

        TArrayCopyMarshaler<string> readTeamArrayCopyMarshaler = new TArrayCopyMarshaler<string>(1, SetText_RedTeamList_PropertyAddress, CachedMarshalingDelegates<string, FStringMarshaler>.FromNative, CachedMarshalingDelegates<string, FStringMarshaler>.ToNative);
        readTeamArrayCopyMarshaler.ToNative(IntPtr.Add(intPtr, SetText_RedTeamList_Offset), redTeamList);
        TArrayCopyMarshaler<string> blueTeamArrayCopyMarshaler = new TArrayCopyMarshaler<string>(1, SetText_BlueTeamList_PropertyAddress, CachedMarshalingDelegates<string, FStringMarshaler>.FromNative, CachedMarshalingDelegates<string, FStringMarshaler>.ToNative);
        blueTeamArrayCopyMarshaler.ToNative(IntPtr.Add(intPtr, SetText_BlueTeamList_Offset), blueTeamList);
        TArrayCopyMarshaler<string> spectatorsArrayCopyMarshaler = new TArrayCopyMarshaler<string>(1, SetText_SpectatorsList_PropertyAddress, CachedMarshalingDelegates<string, FStringMarshaler>.FromNative, CachedMarshalingDelegates<string, FStringMarshaler>.ToNative);
        spectatorsArrayCopyMarshaler.ToNative(IntPtr.Add(intPtr, SetText_SpectatorsList_Offset), spectatorsList);

        NativeReflection.InvokeFunctionOptimized(GameWidget.Address, SetTeams_FunctionAddress, intPtr, SetTeams_ParamsSize);

        NativeReflection.DestroyValue_InContainer(SetText_RedTeamList_PropertyAddress.Address, intPtr);
        NativeReflection.DestroyValue_InContainer(SetText_BlueTeamList_PropertyAddress.Address, intPtr);
        NativeReflection.DestroyValue_InContainer(SetText_SpectatorsList_PropertyAddress.Address, intPtr);
    }

    private void AddToTeam1(string playerName)
    {
        GameWidget?.CallFunctionByNameWithArguments($"AddToTeam1 {playerName}", true);
    }

    private void RemoveFromTeam1(string playerName)
    {
        GameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam1 {playerName}", true);
    }

    private void AddToTeam2(string playerName)
    {
        GameWidget?.CallFunctionByNameWithArguments($"AddToTeam2 {playerName}", true);
    }

    private void RemoveFromTeam2(string playerName)
    {
        GameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam2 {playerName}", true);
    }

    private void AddSpectator(string playerName)
    {
        GameWidget?.CallFunctionByNameWithArguments($"AddSpectator {playerName}", true);
    }

    private void RemoveSpectator(string playerName)
    {
        GameWidget?.CallFunctionByNameWithArguments($"RemoveSpectator {playerName}", true);
    }

    private void SetTeamRedText(string teamRed)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetTeamRedText {teamRed}", true);
    }

    private void SetSpectatorsText(string spectators)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetSpectatorsText {spectators}", true);
    }

    private void SetTeamBlueText(string teamBlue)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetTeamBlueText {teamBlue}", true);
    }

    private void SetMoreText(string more)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetMoreText {more}", true);
    }

    private void SetStatusTexts(string ready, string connected)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetStatusTexts {ready} {connected}", true);
    }

    private void SetStaticTexts(string teamRed, string teamBlue, string spectators, string ready, string connected, string more)
    {
        SetTeamRedText(teamRed);
        SetTeamBlueText(teamBlue);
        SetSpectatorsText(spectators);
        SetStatusTexts(ready, connected);
        SetMoreText(more);
    }

    protected override void PostInitialize()
    {
        SetStaticTexts(BuiltinTexts.RedTeam, BuiltinTexts.BlueTeam, BuiltinTexts.Spectators, BuiltinTexts.Ready, BuiltinTexts.Connected, BuiltinTexts.More);
        InitNativeFunctions();
    }

    static LobbyStatusWidget()
    {
        InitNativeFunctions();
    }

    private static bool SetTeams_IsValid;
    private static IntPtr SetTeams_FunctionAddress;
    private static int SetTeams_ParamsSize;

    private static int SetText_RedTeamList_Offset;
    private static bool SetText_RedTeamList_IsValid;
    private static FFieldAddress? SetText_RedTeamList_PropertyAddress;

    private static int SetText_BlueTeamList_Offset;
    private static bool SetText_BlueTeamList_IsValid;
    private static FFieldAddress? SetText_BlueTeamList_PropertyAddress;

    private static int SetText_SpectatorsList_Offset;
    private static bool SetText_SpectatorsList_IsValid;
    private static FFieldAddress? SetText_SpectatorsList_PropertyAddress;

    public static void InitNativeFunctions()
    {
        IntPtr classPtr = NativeReflection.GetClass(LobbyStatusWidgetPath);
        SetTeams_FunctionAddress = NativeReflectionCached.GetFunction(classPtr, "SetTeams");
        SetTeams_ParamsSize = NativeReflection.GetFunctionParamsSize(SetTeams_FunctionAddress);

        NativeReflectionCached.GetPropertyRef(ref SetText_RedTeamList_PropertyAddress, SetTeams_FunctionAddress, "RedTeamList");
        SetText_RedTeamList_Offset = NativeReflectionCached.GetPropertyOffset(SetTeams_FunctionAddress, "RedTeamList");
        SetText_RedTeamList_IsValid = NativeReflectionCached.ValidatePropertyClass(SetTeams_FunctionAddress, "RedTeamList", Classes.FArrayProperty);

        NativeReflectionCached.GetPropertyRef(ref SetText_BlueTeamList_PropertyAddress, SetTeams_FunctionAddress, "BlueTeamList");
        SetText_BlueTeamList_Offset = NativeReflectionCached.GetPropertyOffset(SetTeams_FunctionAddress, "BlueTeamList");
        SetText_BlueTeamList_IsValid = NativeReflectionCached.ValidatePropertyClass(SetTeams_FunctionAddress, "BlueTeamList", Classes.FArrayProperty);

        NativeReflectionCached.GetPropertyRef(ref SetText_SpectatorsList_PropertyAddress, SetTeams_FunctionAddress, "SpectatorsList");
        SetText_SpectatorsList_Offset = NativeReflectionCached.GetPropertyOffset(SetTeams_FunctionAddress, "SpectatorsList");
        SetText_SpectatorsList_IsValid = NativeReflectionCached.ValidatePropertyClass(SetTeams_FunctionAddress, "SpectatorsList", Classes.FArrayProperty);

        SetTeams_IsValid = SetTeams_FunctionAddress != IntPtr.Zero && SetText_RedTeamList_IsValid && SetText_BlueTeamList_IsValid && SetText_SpectatorsList_IsValid;
        if (!SetTeams_IsValid)
            Logging.LogError("Function WBP_LobbyStatus_C:SetTeams is not valid.");
    }
}