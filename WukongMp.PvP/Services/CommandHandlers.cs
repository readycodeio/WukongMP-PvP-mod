using System.Linq;
using b1;
using ReadyM.Api.Command;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client;
using ReadyM.SDK.Client.Entities;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;
using WukongMp.Pvp.Common.Archetypes;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Resources;
using WukongMp.PvP.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes.Mixins;
using PvpMode = WukongMp.PvP.Gamemode.PvpMode;

namespace WukongMp.PvP.Services;

[Service]
public sealed partial class CommandHandlers(
    IWukongConsoleApi consoleApi,
    IWukongChatApi chatApi,
    PvpMode pvpMode,
    CheatManager cheatManager,
    IWukongEntityApi entityApi,
    IEntities entities
)
{
    private void Start()
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

    private void RequestSpawn(string unitName, int count = 1)
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } player)
            return;

        var myTeam = player.TeamId;
        var teamId = PvpUtils.GetOppositeTeam(myTeam);
        var playerPawn = player.Pawn;
        if (playerPawn == null)
            return;

        var location = CalculateSpawnLocation(playerPawn.GetActorLocation(), playerPawn.GetActorForwardVector());

        entityApi.SpawnEnemy(new TamerKind(unitName), location.ToVector3(), count, teamId);

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
        if (WukongApi.Entities.LocalMainCharacter is not { } player)
            return;

        if (!entities.World.InTournament)
        {
            if (!player.IsSpectator)
            {
                entityApi.EnableSpectatorMode(player, SpectatorReason.Api);
            }
            else
            {
                entityApi.DisableSpectatorMode(player);
            }
        }
    }

    public void TeleportToArena()
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
            return;

        if (entityApi.InArea && !mainEntity.IsSpectator && !entities.World.InTournament)
        {
            var levelData = PvpUtils.GetCurrentLevelSpawnData();
            mainEntity.Override(Transform.Field.Position, levelData.PvpStartingLocation);
        }
    }

    public void TeleportToShrine()
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Entities.InArea && !mainEntity.IsSpectator && !entities.World.InTournament)
        {
            var levelData = PvpUtils.GetCurrentLevelSpawnData();
            UBGWFunctionLibraryCS.GetRebirthPointTransform(GameUtils.GetWorld(), levelData.BirthPointId, out var shrineTransform);

            mainEntity.Override(Transform.Field.Position, shrineTransform.Translation.ToVector3());
            mainEntity.Override(Transform.Field.Rotation, shrineTransform.Rotation.Rotator().ToVector3());
        }
    }

    private void TeleportToPvpLevel(int pvpLevelId)
    {
        if (WukongApi.Entities.LocalMainCharacter is not { } mainEntity)
            return;

        if (WukongApi.Entities.InArea && !mainEntity.IsSpectator && !entities.World.InTournament)
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