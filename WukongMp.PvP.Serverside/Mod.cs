using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.SDK.Server;
using ReadyM.SDK.Server.Entities;
using WukongMp.PvP.Serverside.Config;

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
        Services.RegisterSingleton(new RoomConfigWatcher(Services.Resolve<IEntities>(), Services.Resolve<PvpConfig>(), ModDirectory, logger));

        logger.LogInformation("Serverside PvP mod initialized");
    }
}