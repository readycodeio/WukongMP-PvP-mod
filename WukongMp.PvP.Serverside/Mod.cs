using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;
using WukongMp.PvP.Serverside.Config;

namespace WukongMp.PvP.Serverside;

[ModEntry]
public sealed partial class Mod(
    IDependencyContainer services,
    IEntities entities,
    PvpConfig config,
    ILogger logger
)
{
    private void Start()
    {
        services.RegisterSingleton<RpcHandlers>();

        // Watch for settings file change
        services.RegisterSingleton(new RoomConfigWatcher(entities, config, ModDirectory, logger));

        logger.LogInformation("Serverside PvP mod initialized");
    }
}