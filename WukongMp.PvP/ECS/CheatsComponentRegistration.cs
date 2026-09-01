using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.ECS;

public class CheatsComponentRegistration : IArchetypeRegistration
{
    public void Register(IArchetypeRegistry registry)
    {
        registry.ModifyArchetype(WukongApi.Archetypes.MainCharacterArchetype, b =>
        {
            b.Add<CheatsComponent>();
        });
    }
}