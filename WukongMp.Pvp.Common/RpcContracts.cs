using System.Numerics;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;

namespace WukongMp.Pvp.Common;

[ServerRpcContracts]
public static partial class RpcContracts
{
    [ClientToServer, ServerToClient] public static partial void EnableCheats(AreaId areaId, bool enabled);
    [ClientToServer, ServerToClient] public static partial void RoundCountdown(bool start, int seconds);
    [ClientToServer, ServerToClient] public static partial void PlayerReadinessWarning();
    [ClientToServer, ServerToClient] public static partial void StartRound(Vector3 placement, Vector3 lookAt);
    [ClientToServer, ServerToClient] public static partial void EndRound(int winnerTeam);
    [ClientToServer, ServerToClient] public static partial void EndTournament(int winnerTeam);
    [ClientToServer, ServerToClient] public static partial void ResetStats();
    [ClientToServer, ServerToClient] public static partial void HideAntiStall();
    [ClientToServer, ServerToClient] public static partial void ShowAntiStallWarning(int seconds);
    [ClientToServer, ServerToClient] public static partial void ShowAntiStallAction();
    [ClientToServer, ServerToClient] public static partial void StallDamage(float damage);
    [ClientToServer, ServerToClient] public static partial void ChangeLevel(int levelId);
}