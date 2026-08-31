using ReadyM.Api.Multiplayer.ECS.Registry;
using WukongMp.Pvp.Common;

namespace WukongMp.PvP;

public sealed class PvpNetworkedComponentRegistration : INetworkedComponentRegistration
{
    public void Register(INetworkedComponentRegistry registry)
    {
        registry.RegisterComponent<PvPComponent>();
        registry.RegisterComponent<PvpStateComponent>();
    }
}