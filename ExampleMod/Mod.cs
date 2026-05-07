using CSharpModBase.Input;
using ReadyM.Api.Command;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Api.Multiplayer.Serialization;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using HarmonyLib;

namespace ExampleMod;

public class Mod : ModBase
{
    public override string Name => "Example Mod"; // TODO: CHANGE ME

    protected override void Initialize(IDependencyContainer services)
    {
        // register and resolve your services here, for example:
        services.RegisterSingleton<ExampleRpc>();
        var rpc = services.Resolve<ExampleRpc>();

        // use the WukongApi class to interact with the SDK, for example:
        WukongApi.Console.AddCommand("example_command", ConsoleCommand.Create(() =>
        {
            WukongApi.Chat.ShowLocalMessage("Example command executed!", FLinearColor.Orange);
            rpc.SendExampleEvent("Hello from the example command!");
        }));

        // register input bindings, for example:
        WukongApi.Input.RegisterKeyBind(Key.F5, () => { WukongApi.Chat.ShowLocalMessage("F5 key pressed!", FLinearColor.Blue); });
    }
}

// define your RPC methods, for example:
public partial class ExampleRpc(IRpcClient client, IRelaySerializer serializer) : RpcClassBase(client, serializer)
{
    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnExampleEvent(PlayerId __sender, string message)
    {
        WukongApi.Chat.ShowLocalMessage($"Received message from {__sender}: {message}", FLinearColor.Green);
    }
}

// use Harmony to patch a game method, for example:
[HarmonyPatch(typeof(UGameplayStatics), nameof(UGameplayStatics.OpenLevel))]
[HarmonyPatchCategory(PatchCategory.Global)]
public static class ExamplePatch
{
    public static void Postfix(FName LevelName)
    {
        Logging.LogDebug("Entering level: {LevelName}", LevelName.ToString());
    }
}