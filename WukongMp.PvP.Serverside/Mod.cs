using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Relay.Server.Sdk;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.ECS;
using WukongMp.PvP.Serverside.Systems;
using WukongMp.Sdk.Serverside;

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

        Services.RegisterSingleton<IArchetypeRegistration>(new ArchetypeRegistration(WukongArchetypes.MainCharacterArchetype, WukongArchetypes.WorldArchetype));

        var logger = Services.Resolve<ILogger>();
        logger.LogInformation("Serverside PvP mod initialized");
    }
}