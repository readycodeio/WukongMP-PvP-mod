using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Pvp.Common.Archetypes;

[ArchetypeMixin]
[Extends(typeof(MainCharacter))]
[Replicated]
public readonly partial struct PvpStateData
{
    public partial bool IsReadyForPvP { get; set; }
}