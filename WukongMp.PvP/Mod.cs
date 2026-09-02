using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Registry;
using WukongMp.PvP.Chat;
using WukongMp.PvP.Command;
using WukongMp.Pvp.Common;
using WukongMp.Pvp.Common.ECS;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.ECS;
using WukongMp.PvP.GameMode;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP;

// ReSharper disable once UnusedType.Global
public class Mod : ModBase
{
    public override string Name => "WukongMp PvP";

    protected override void Initialize(IDependencyContainer services)
    {
        Logger.LogInformation("Initializing {PluginName}", Name);

        services.Resolve<IComponentRegistry>()
            .RegisterComponent<PvPComponent>()
            .RegisterComponent<PvpStateComponent>();
        
        RegisterArchetypes(registry =>
        {
            registry.ModifyArchetype(WukongApi.Archetypes.MainCharacterArchetype, b =>
            {
                b.Add<PvPComponent>();
                b.Add<CheatsComponent>();
            });

            registry.ModifyArchetype(WukongApi.Archetypes.WorldArchetype, b =>
            {
                b.Add<PvpStateComponent>();
            });
        });
        
        services.RegisterSingleton<CheatManager>();
        services.RegisterSingleton<WukongPvpApi>();
        services.RegisterSingleton<TimerController>();
        services.RegisterSingleton<PvpChatter>();
        services.RegisterSingleton<PvpGameplayConfiguration>();
        services.RegisterSingleton<PvpSaveManager>();
        services.RegisterSingleton<PvpWidgetManager>();
        services.RegisterSingleton<PvpMode>();
        services.RegisterSingleton<PvpCommandHandler>();
    }

    public override void LateInit()
    {
        base.LateInit();

        WukongApi.Input.RegisterKeyBind(Key.J, () =>
        {
            Logger.LogDebug("J");
            if (WukongApi.Input.CanApplyInput())
                WukongApi.Services.Resolve<PvpMode>().SwitchReadyStateMulti();
        });

        WukongApi.Input.RegisterKeyBind(Key.L, () =>
        {
            Logger.LogDebug("L");
            if (WukongApi.Input.CanApplyInput())
                WukongApi.Services.Resolve<PvpMode>().SwitchTeam();
        });

        WukongApi.Input.RegisterKeyBind(Key.F3, () => { WukongApi.Services.Resolve<PvpCommandHandler>().TeleportToArena(); });

        WukongApi.Input.RegisterKeyBind(Key.F4, () => { WukongApi.Services.Resolve<PvpCommandHandler>().TeleportToShrine(); });
    }
}