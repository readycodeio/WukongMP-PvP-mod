using Friflo.Engine.ECS;

namespace WukongMp.PvP.ECS;

public struct CheatsComponent : IComponent
{
    public bool InstantSkillCooldown { get; set; }
    public bool HasInfiniteMana { get; set; }
    public bool HasInfiniteVessel { get; set; }
    public bool HasInfiniteTransform { get; set; }
    public bool SpiritCooldownEnabled { get; set; }
    public float SpiritCooldownTime { get; set; }
    public bool ShouldSetSpiritCooldown { get; set; }
}