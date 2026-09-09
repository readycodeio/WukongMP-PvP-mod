using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk;
using ReadyM.Relay.Server.Sdk.Ecs;
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
        RegisterConfig<PvpConfig>();
        var initialPvpState = Services.Resolve<PvpConfig>().ToInitialState();

        RegisterArchetypes(registry =>
        {
            registry.ModifyArchetype(WukongArchetypes.MainCharacterArchetype, b => { b.Add<PvPComponent>(); });

            registry.ModifyArchetype(WukongArchetypes.WorldArchetype, b => b.Add(initialPvpState));
        });

        Services.RegisterSingleton<RpcHandlers>();

        var logger = Services.Resolve<ILogger>();

        // Watch for settings file change
        Services.RegisterSingleton(new RoomConfigApplier(Services.Resolve<EcsApi>(), Services.Resolve<PvpConfig>(), ModDirectory, logger));

        Services.RegisterSystem<RoundStartTimerSystem>();
        Services.RegisterSystem<RoundEndSystem>();
        Services.RegisterSystem<AntiStallSystem>();
        Services.RegisterSystem<RoomConfigSystem>();

        logger.LogInformation("Serverside PvP mod initialized");
    }
}