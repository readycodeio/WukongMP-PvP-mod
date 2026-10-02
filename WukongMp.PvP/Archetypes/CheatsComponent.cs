using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.PvP.Archetypes;

[ArchetypeMixin]
[Extends(typeof(MainCharacter))]
public partial struct CheatSettings
{
    public partial bool InstantSkillCooldown { get; set; }
    public partial bool HasInfiniteMana { get; set; }
    public partial bool HasInfiniteVessel { get; set; }
    public partial bool HasInfiniteTransform { get; set; }
    public partial bool SpiritCooldownEnabled { get; set; }
    public partial float SpiritCooldownTime { get; set; }
    public partial bool ShouldSetSpiritCooldown { get; set; }
}