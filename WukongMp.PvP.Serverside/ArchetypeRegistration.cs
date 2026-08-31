using ReadyM.Api.ECS.Worlds;
using WukongMp.Pvp.Common.ECS;
using WukongMp.Sdk.Serverside;

namespace WukongMp.PvP.Serverside;

public static class ArchetypeRegistration
{
    public static void RegisterArchetypes(IArchetypeRegistry registry)
    {
        registry.ModifyArchetype(WukongArchetypes.MainCharacterArchetype, b =>
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
