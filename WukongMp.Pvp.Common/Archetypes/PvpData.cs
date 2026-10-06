using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Pvp.Common.Archetypes;

[ArchetypeMixin]
[Replicated]
[Propagates(Propagation.OwnershipBased)]
[Extends(typeof(MainCharacter))]
public readonly partial struct PvpStateData
{
    public partial bool IsReadyForPvP { get; set; }
}