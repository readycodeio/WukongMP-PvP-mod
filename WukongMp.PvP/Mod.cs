using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Gamemode;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using CommandHandlers = WukongMp.PvP.Services.CommandHandlers;
using PvpMode = WukongMp.PvP.Gamemode.PvpMode;

namespace WukongMp.PvP;

// ReSharper disable once UnusedType.Global
public class Mod : ModBase
{
    public override string Name => "WukongMp PvP";

    protected override void Initialize(IDependencyContainer services)
    {
        Logger.LogInformation("Initializing {PluginName}", Name);
        
        services.RegisterSingleton<CheatManager>();
        services.RegisterSingleton<TimerController>();
        services.RegisterSingleton<PvpGameplayConfiguration>();
        services.RegisterSingleton<PvpSaveManager>();
        services.RegisterSingleton<PvpMode>();
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

        WukongApi.Input.RegisterKeyBind(Key.F3, () => { WukongApi.Services.Resolve<CommandHandlers>().TeleportToArena(); });

        WukongApi.Input.RegisterKeyBind(Key.F4, () => { WukongApi.Services.Resolve<CommandHandlers>().TeleportToShrine(); });
    }
}