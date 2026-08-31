using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Pvp.Common;

namespace WukongMp.PvP.ECS;

public class PvpComponentRegistration(ClientWukongArchetypeRegistration wukongArchetypes) : IArchetypeRegistration
{
    public void Register(IArchetypeRegistry registry)
    {
        registry.ModifyArchetype(wukongArchetypes.MainCharacterArchetype, b =>
        {
            b.Add<PvPComponent>();
        });

        registry.RegisterArchetype(b =>
        {
            b.Add<PvpStateComponent>();
        });
    }
}