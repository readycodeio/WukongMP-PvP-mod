using System.IO;
using ArchiveB1;
using b1;
using B1UI.GSSvc;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.PvP.Configuration;

namespace WukongMp.PvP.GameMode;

public class PvpSaveManager
{
    private bool _redirectSaveFiles;
    private bool _shouldCacheSave;

    public bool ShouldRedirectSaveFiles => _redirectSaveFiles;

    public void OnSavedGameLoad()
    {
        _shouldCacheSave = true;
    }

    public void OnNewGameLoad(UObject worldContext)
    {
        _redirectSaveFiles = true;
        GSGMSvc.ClearAllAutoRunTag();
        if (BGW_GameLifeTimeMgr.Get(worldContext).IsInFSMState(SGI_Global.MainMenu))
        {
            BGW_EventCollection.Get(worldContext).Evt_ResetGameInstanceData(EGameInstanceResetType.StartNewGame);
        }

        BGW_EventCollection.Get(worldContext).Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.LoadArchive, new FSMInputData_GI_Global_SubG_GI_Loading_TravelLevel
        {
            ArchiveId = PvpConstants.NewCharacterArchiveId
        });
    }

    public void OnLoadArchive(BGW_GameArchiveMgr __instance, ref ReadArchiveResult __result, int ArchiveId, ref FUStBEDArchivesData OutArchiveData)
    {
        if (!_redirectSaveFiles)
        {
            if (_shouldCacheSave)
            {
                _shouldCacheSave = false;
                var characterArchiveSlotName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, ArchiveId);
                var characterArchiveFullName = GSWindowsPlatformSaveGame.GetFileFullName(characterArchiveSlotName, __instance.ArchiveWorker.UserId);

                _redirectSaveFiles = true;
                var newCharacterArchiveSlotName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, PvpConstants.CharacterArchiveId);
                var newCharacterArchiveFullName = GSWindowsPlatformSaveGame.GetFileFullName(newCharacterArchiveSlotName, __instance.ArchiveWorker.UserId);
                File.Copy(characterArchiveFullName, newCharacterArchiveFullName, true);
            }
            else
            {
                _redirectSaveFiles = true;
                var characterReadArchiveResult = __instance.ReadArchiveData(PvpConstants.CharacterArchiveId, out var characterGameArchiveData, out _);
                if (characterReadArchiveResult == ReadArchiveResult.Success)
                {
                    OutArchiveData = characterGameArchiveData.GameArchiveData;
                }
            }
        }

        // Read archive with our world state.
        var readArchiveResult = __instance.ReadArchiveData(PvpConstants.WorldArchiveId, out var gameArchiveData, out _);
        if (readArchiveResult != 0)
        {
            Logging.LogError("ReadArchiveData Failed, Result: {Result}", readArchiveResult);
            return;
        }

        // Keep only RoleData with player state
        OutArchiveData.LevelArchiveData = gameArchiveData.GameArchiveData.LevelArchiveData;
        OutArchiveData.PersistentECSData = gameArchiveData.GameArchiveData.PersistentECSData;
        OutArchiveData.StateMachineArchiveData = gameArchiveData.GameArchiveData.StateMachineArchiveData;
        OutArchiveData.TaskArchiveData = gameArchiveData.GameArchiveData.TaskArchiveData;

        var levelConfig = LevelSpawnConfig.GetCurrentLevelSpawnData();
        OutArchiveData.PersistentECSData.BPCData.BPCPlayerRoleData.MapId = levelConfig.MapId;
        OutArchiveData.PersistentECSData.BPCData.BPCPlayerRoleData.MapAreaId = levelConfig.MapAreaId;
        OutArchiveData.PersistentECSData.BPCData.BPCRebirthPointData.CurrentBirthPoint.PointID = levelConfig.BirthPointID;

        _redirectSaveFiles = false;
    }
}
