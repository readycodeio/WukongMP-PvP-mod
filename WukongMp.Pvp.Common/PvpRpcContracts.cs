using System.Numerics;
using ReadyM.Api.Multiplayer;

namespace WukongMp.Pvp.Common;

[ServerRpcContracts]
public static partial class PvpRpcContracts
{
    [ClientToServer] public static partial void EnableCheats(bool enabled);
    [ServerToClient] public static partial void CheatsEnabledResponse(CheatsStatus status);
    [ServerToClient] public static partial void RoundCountdown(bool start, int seconds);
    [ServerToClient] public static partial void PlayerReadinessWarning();
    [ServerToClient] public static partial void StartRound(Vector3 placement, Vector3 lookAt, int round, int totalRounds);
    [ServerToClient] public static partial void EndRound(int winnerTeam);
    [ServerToClient] public static partial void EndTournament(int winnerTeam);
    [ServerToClient] public static partial void ResetStats();
    [ServerToClient] public static partial void HideAntiStall();
    [ServerToClient] public static partial void ShowAntiStallWarning(int seconds);
    [ServerToClient] public static partial void ShowAntiStallAction();
    [ServerToClient] public static partial void StallDamage(float damage);
    [ClientToServer, ServerToClient] public static partial void ChangeLevel(int levelId);
}