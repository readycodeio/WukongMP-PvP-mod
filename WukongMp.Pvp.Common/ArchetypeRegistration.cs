using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using WukongMp.Pvp.Common.ECS;

namespace WukongMp.Pvp.Common;

public class ArchetypeRegistration(ArchetypeId mainCharacterArchetype, ArchetypeId worldArchetype) : IArchetypeRegistration
{
    public void Register(IArchetypeRegistry registry)
    {
        registry.ModifyArchetype(mainCharacterArchetype, b => { b.Add<PvPComponent>(); });

        registry.ModifyArchetype(worldArchetype,
            b => b.Add(new PvpStateComponent
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