using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace WukongMp.Pvp.Common.ECS;

/// <summary>
/// Holds player PvP readiness flag.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PvPComponent : IOwnershipBased
{
    private bool _isReadyForPvP;
}