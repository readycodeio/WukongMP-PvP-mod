using System.Linq;
using System.Numerics;
using b1;
using ReadyM.Api.Command;
using ReadyM.Api.DI;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Resources;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.Command;

public class PvpCommandHandler(
    IWukongConsoleApi consoleApi,
    IWukongChatApi chatApi,
    IWukongPvpApi pvpApi,
    IWukongCheatsApi cheatsApi,
    IWukongSynchronizationApi syncApi
) : IHostedService
{
    public void OnScopeStart()
    {
        var allmonsterNames = TamerKinds.GetAllValidTamerKinds().Select(x => x.Name);
        consoleApi.AddCommand("spawn", ConsoleCommand.Create(RequestSpawn, false), allmonsterNames);

        consoleApi.AddCommand("spectator", ConsoleCommand.Create(SetSpectatorStatus, false));
        consoleApi.AddCommand("instant_cooldown", ConsoleCommand.Create(cheatsApi.ToggleNoSkillsCooldown, false));
        consoleApi.AddCommand("infinite_mana", ConsoleCommand.Create(cheatsApi.ToggleInfiniteMana, false));
        consoleApi.AddCommand("spirit_cooldown", ConsoleCommand.Create(cheatsApi.SetSpritCooldownTime, false));
        consoleApi.AddCommand("infinite_vessel", ConsoleCommand.Create(cheatsApi.ToggleInfiniteVessel, false));
        consoleApi.AddCommand("infinite_transform", ConsoleCommand.Create(cheatsApi.ToggleInfiniteTransform, false));
        consoleApi.AddCommand("arena", ConsoleCommand.Create(TeleportToArena, false));
        consoleApi.AddCommand("shrine", ConsoleCommand.Create(TeleportToShrine, false));
        consoleApi.AddCommand("pvp_level", ConsoleCommand.Create(TeleportToPvpLevel, true));
    }

    public void Dispose() { }

    private void RequestSpawn(string unitName, int count = 1)
    {
        if (syncApi.LocalMainCharacter is not { } player)
            return;

        var myTeam = player.TeamId;
        var teamId = PvpUtils.GetOppositeTeam(myTeam);
        var playerPawn = player.Pawn;
        if (playerPawn == null)
            return;

        var location = CalculateSpawnLocation(playerPawn.GetActorLocation(), playerPawn.GetActorForwardVector());

        syncApi.SpawnEnemy(new TamerKind(unitName), location.ToVector3(), count, teamId);

        var message = string.Format(PvpTexts.PlayerSpawned, player.Nickname, count, unitName);
        chatApi.SendServerMessage(message);
    }

    private static FVector CalculateSpawnLocation(FVector playerLocation, FVector playerForwardVector)
    {
        var spawnLoc = playerLocation + playerForwardVector * PvpConstants.MonsterSpawnDistance;

        var startLoc = spawnLoc + FVector.UpVector * PvpConstants.MonsterSpawnTraceHeight / 2;
        var endLoc = spawnLoc - FVector.UpVector * PvpConstants.MonsterSpawnTraceHeight / 2;

        // Trace vertically for spawn height.
        var hitResultSimple = new FHitResultSimple();
        var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), startLoc, endLoc, ref hitResultSimple);
        if (hit)
        {
            spawnLoc = hitResultSimple.HitLocation + FVector.UpVector * PvpConstants.MonsterHalfHeight;
        }

        return spawnLoc;
    }

    private void SetSpectatorStatus()
    {
        if (syncApi.LocalMainCharacter is not { } player)
            return;

        if (!pvpApi.InPvpTournament)
        {
            if (!player.IsSpectator)
            {
                syncApi.EnableSpectatorMode(player, SpectatorReason.Observer);
            }
            else
            {
                syncApi.DisableSpectatorMode(player);
            }
        }
    }

    private void TeleportToArena()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Sync.InArea && !WukongApi.PvP.PvpData(mainEntity).IsSpectator && !WukongApi.PvP.InPvpTournament)
        {
            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            mainEntity.Teleport(levelData.PvpStartingLocation.ToVector3(), Vector3.Zero);
        }
    }

    private void TeleportToShrine()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Sync.InArea && !WukongApi.PvP.PvpData(mainEntity).IsSpectator && !WukongApi.PvP.InPvpTournament)
        {
            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            UBGWFunctionLibraryCS.GetRebirthPointTransform(GameUtils.GetWorld(), levelData.BirthPointID, out var shrineTransform);

            mainEntity.Teleport(shrineTransform.Translation.ToVector3(), shrineTransform.Rotation.Rotator().ToVector3());
        }
    }

    private void TeleportToPvpLevel(int pvpLevelId)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Sync.InArea && !WukongApi.PvP.PvpData(mainEntity).IsSpectator && !WukongApi.PvP.InPvpTournament)
        {
            if (pvpLevelId < 0)
            {
                consoleApi.LogMessage(BuiltinTexts.InvalidCommand);
                return;
            }

            WukongApi.PvP.LevelId = pvpLevelId;
            var levelData = LevelSpawnConfig.GetLevelSpawnData(pvpLevelId);
            BPS_EventCollectionCS.GetLocal(GameUtils.GetWorld()).Evt_BPS_TeleportTo.Invoke(ETeleportTypeV2.RebirthPointTeleportOnly, new TeleportParam_RebirthPoint
            {
                RebirthPointId = levelData.BirthPointID,
            }, EPlayerTeleportReason.RebirthPoint);
        }
    }
}