using ReadyM.Relay.Server.Sdk.Ecs.Systems;

namespace WukongMp.PvP.Serverside.Systems;

/// <summary>
/// Drives <see cref="RoomConfigApplier" />, which does its own rate limiting.
/// </summary>
public sealed class RoomConfigSystem(RoomConfigApplier applier) : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        applier.Poll(tick.Time);
    }
}
