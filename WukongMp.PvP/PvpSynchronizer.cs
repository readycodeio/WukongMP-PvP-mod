using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using WukongMp.Api;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP;

public class PvpSynchronizer : IHostedService
{
    public void OnScopeStart()
    {
        WukongApi.Events.OnJoinedArea += OnJoinedAreaHandler;
    }

    public void Dispose()
    {
        WukongApi.Events.OnJoinedArea -= OnJoinedAreaHandler;
    }

    private void OnJoinedAreaHandler(AreaId areaId)
    {
        var isFirst = WukongApi.Sync.IsMasterClient;

        Logging.LogDebug("Joined area {AreaId}, is master client: {IsMasterClient}", areaId, isFirst);

        if (isFirst)
        {
            WukongApi.PvP.InitializeAreaPvpState();
        }
    }
}