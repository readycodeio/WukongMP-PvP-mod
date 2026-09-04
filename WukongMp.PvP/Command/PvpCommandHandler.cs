using System.Linq;
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
using WukongMp.PvP.GameMode;
using WukongMp.PvP.Resources;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.Command;

public class PvpCommandHandler(
    IWukongConsoleApi consoleApi,
    IWukongChatApi chatApi,
    WukongPvpApi pvpApi,
    PvpMode pvpMode,
    CheatManager cheatManager,
    IWukongSynchronizationApi syncApi
) : IHostedService
{
    public void OnScopeStart()
    {
        var allmonsterNames = TamerKinds.GetAllValidTamerKinds().Select(x => x.Name);
        consoleApi.AddCommand("spawn", ConsoleCommand.Create(RequestSpawn), allmonsterNames);
        consoleApi.AddCommand("spectator", ConsoleCommand.Create(SetSpectatorStatus));
        consoleApi.AddCommand("instant_cooldown", ConsoleCommand.Create(cheatManager.ToggleNoSkillsCooldown));
        consoleApi.AddCommand("infinite_mana", ConsoleCommand.Create(cheatManager.ToggleInfiniteMana));
        consoleApi.AddCommand("spirit_cooldown", ConsoleCommand.Create(cheatManager.SetSpritCooldownTime));
        consoleApi.AddCommand("infinite_vessel", ConsoleCommand.Create(cheatManager.ToggleInfiniteVessel));
        consoleApi.AddCommand("infinite_transform", ConsoleCommand.Create(cheatManager.ToggleInfiniteTransform));
        consoleApi.AddCommand("arena", ConsoleCommand.Create(TeleportToArena));
        consoleApi.AddCommand("shrine", ConsoleCommand.Create(TeleportToShrine));
        consoleApi.AddCommand("pvp_level", ConsoleCommand.Create(TeleportToPvpLevel));
        consoleApi.AddCommand("cheats", ConsoleCommand.Create(ToggleCheats));
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
                syncApi.EnableSpectatorMode(player, SpectatorReason.Api);
            }
            else
            {
                syncApi.DisableSpectatorMode(player);
            }
        }
    }

    public void TeleportToArena()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Sync.InArea && !mainEntity.IsSpectator && !pvpApi.InPvpTournament)
        {
            var levelData = PvpUtils.GetCurrentLevelSpawnData();
            mainEntity.Location = levelData.PvpStartingLocation;
        }
    }

    public void TeleportToShrine()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Sync.InArea && !mainEntity.IsSpectator && !pvpApi.InPvpTournament)
        {
            var levelData = PvpUtils.GetCurrentLevelSpawnData();
            UBGWFunctionLibraryCS.GetRebirthPointTransform(GameUtils.GetWorld(), levelData.BirthPointId, out var shrineTransform);

            mainEntity.Location = shrineTransform.Translation.ToVector3();
            mainEntity.Rotation = shrineTransform.Rotation.Rotator().ToVector3();
        }
    }

    private void TeleportToPvpLevel(int pvpLevelId)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Sync.InArea && !mainEntity.IsSpectator && !pvpApi.InPvpTournament)
        {
            if (pvpLevelId < 0)
            {
                consoleApi.LogMessage(PvpTexts.InvalidCommand);
                return;
            }

            pvpMode.SendChangeLevel(pvpLevelId);
        }
    }

    private void ToggleCheats()
    {
        var enabledAlready = cheatManager.CheatsEnabled;
        pvpMode.SendEnableCheats(!enabledAlready);
    }
}