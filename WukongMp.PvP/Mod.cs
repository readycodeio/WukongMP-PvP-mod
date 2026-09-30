using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Gamemode;
using WukongMp.PvP.UI;
using WukongMp.Sdk.Api;
using CommandHandlers = WukongMp.PvP.Services.CommandHandlers;
using PvpMode = WukongMp.PvP.Gamemode.PvpMode;

namespace WukongMp.PvP;

[ModEntry]
public sealed partial class Mod(IDependencyContainer services, ILogger logger)
{
    private void Start()
    {
        logger.LogInformation("Initializing PvP mod");

        services.RegisterSingleton<CheatManager>();
        services.RegisterSingleton<TimerController>();
        services.RegisterSingleton<PvpGameplayConfiguration>();
        services.RegisterSingleton<PvpSaveManager>();
        services.RegisterSingleton<PvpMode>();

        WukongApi.Input.RegisterKeyBind(Key.J, () =>
        {
            logger.LogDebug("J");
            if (WukongApi.Input.CanApplyInput())
                WukongApi.Services.Resolve<PvpMode>().SwitchReadyStateMulti();
        });

        WukongApi.Input.RegisterKeyBind(Key.L, () =>
        {
            logger.LogDebug("L");
            if (WukongApi.Input.CanApplyInput())
                WukongApi.Services.Resolve<PvpMode>().SwitchTeam();
        });

        WukongApi.Input.RegisterKeyBind(Key.F3, () => { WukongApi.Services.Resolve<CommandHandlers>().TeleportToArena(); });

        WukongApi.Input.RegisterKeyBind(Key.F4, () => { WukongApi.Services.Resolve<CommandHandlers>().TeleportToShrine(); });
        
        logger.LogInformation("Initialized PvP mod");
    }
}