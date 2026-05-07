using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using WukongMp.PvP.Chat;
using WukongMp.PvP.Command;
using WukongMp.PvP.Configuration;
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
        
        services.RegisterSingleton<PvpRpc>();
        services.RegisterSingleton<TimerController>();
        services.RegisterSingleton<PvpChatter>();
        services.RegisterSingleton<PvpGameplayConfiguration>();
        services.RegisterSingleton<PvpSaveManager>();
        services.RegisterSingleton<PvpWidgetManager>();
        services.RegisterSingleton<PvpMode>();
        services.RegisterSingleton<PvpCommandHandler>();
        services.RegisterSingleton<PvpSynchronizer>();
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
    }
}