using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using WukongMp.Api;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP;

public sealed class PvpSynchronizer : IHostedService
{
    public void OnScopeStart() 
        => WukongApi.Events.OnJoinedArea += OnJoinedAreaHandler;

    public void Dispose() 
        => WukongApi.Events.OnJoinedArea -= OnJoinedAreaHandler;

    private static void OnJoinedAreaHandler(AreaId areaId)
    {
        var isMasterClient = WukongApi.Sync.IsMasterClient;
        Logging.LogDebug("Joined area {AreaId}, is master client: {IsMasterClient}", areaId, isMasterClient);
        
        if (isMasterClient)
            WukongApi.PvP.InitializeAreaPvpState();
    }
}