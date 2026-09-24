using ReadyM.SDK.Attributes;
using ReadyM.SDK.Systems;

namespace WukongMp.PvP.Serverside.Systems;

[System]
public partial class RoomConfigSystem(RoomConfigApplier applier)
{
    private void Update(Tick tick)
    {
        applier.Poll(tick.Time);
    }
}