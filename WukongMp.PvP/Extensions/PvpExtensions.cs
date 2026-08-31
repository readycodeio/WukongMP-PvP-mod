using ReadyM.Wukong.Common.ECS.Values;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.Extensions;

public static class PvpExtensions
{
    extension(ReadyMainCharacter character)
    {
        public bool IsObserver
            => character.IsSpectator && character.SpectatorReason != SpectatorReason.Death;
    }
}