using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using WukongMp.Pvp.Common.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.ECS;

public class PvpComponentRegistration : IArchetypeRegistration
{
    public void Register(IArchetypeRegistry registry)
    {
        registry.ModifyArchetype(WukongApi.Archetypes.MainCharacterArchetype, b => { b.Add<PvPComponent>(); });

        registry.RegisterArchetype(new ArchetypeBuilder().Add<PvpStateComponent>());
    }
}