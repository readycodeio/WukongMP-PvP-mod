using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.PvP.Serverside;

public static class ArchetypeRegistration
{
    public static void RegisterArchetypes(IArchetypeRegistry registry)
    {
        var archetype = new ArchetypeId(3); // TODO: Un-hardcode this
        registry.ModifyArchetype(archetype, b =>
        {
            b.Add<PvPComponent>();
        });

        var pvpStateArchetype = registry.RegisterArchetype(
            new ArchetypeBuilder().Add(new PvpStateComponent // TODO: Default unused
            {
                ConsumablesAllowed = true,
                ImmobilizeAllowed = true,
                GourdAllowed = true,
                PhantomRushAllowed = true,
                AntiStallEnabled = true,
                EnemiesNgPlusLevel = 0,
                LevelId = 0,
                TournamentRounds = 3
            })
        );
    }
}
