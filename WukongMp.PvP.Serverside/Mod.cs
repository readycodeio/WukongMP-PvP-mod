using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Relay.Server.Sdk;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.PvP.Serverside.Systems;

namespace WukongMp.PvP.Serverside;

[UsedImplicitly]
public class Mod : ServerModBase
{
    protected override void RegisterComponents(IComponentRegistry registry)
    {
        registry.RegisterComponent<PvPComponent>();
        registry.RegisterComponent<PvpStateComponent>();
    }

    protected override void Init()
    {
        Services.RegisterSingleton<RpcHandlers>();

        Services.RegisterSystem<RoundStartTimerSystem>();
        Services.RegisterSystem<RoundEndSystem>();
        Services.RegisterSystem<AntiStallSystem>();

        var registry = Services.Resolve<IArchetypeRegistry>();
        ArchetypeRegistration.RegisterArchetypes(registry);

        var logger = Services.Resolve<ILogger>();
        logger.LogInformation("Serverside PvP mod initialized");
    }
}
