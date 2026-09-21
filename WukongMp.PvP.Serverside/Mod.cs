using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk;
using ReadyM.SDK.Server;
using ReadyM.SDK.Server.Entities;
using WukongMp.Pvp.Common;
using WukongMp.PvP.Serverside.Systems;
using WukongMp.Sdk.Serverside;

namespace WukongMp.PvP.Serverside;

[UsedImplicitly]
public class Mod : ServerMod
{
    protected override void Start()
    {
        RegisterConfig<PvpConfig>();

        Services.RegisterSingleton<RpcHandlers>();

        var logger = Services.Resolve<ILogger>();

        // Watch for settings file change
        Services.RegisterSingleton(new RoomConfigApplier(Services.Resolve<IEntities>(), Services.Resolve<PvpConfig>(), ModDirectory, logger));

        Services.RegisterSystem<RoundStartTimerSystem>();
        Services.RegisterSystem<RoundEndSystem>();
        Services.RegisterSystem<AntiStallSystem>();
        Services.RegisterSystem<RoomConfigSystem>();

        logger.LogInformation("Serverside PvP mod initialized");
    }
}