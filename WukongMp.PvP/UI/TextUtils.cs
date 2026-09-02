using WukongMp.Api.Resources;
using WukongMp.PvP.Resources;

namespace WukongMp.PvP.UI;

public static class TextUtils
{
    public static string GetReadyText(int playersCount, bool isReady)
    {
        if (playersCount == 0)
        {
            return isReady ? PvpTexts.PressToCancelMatch : PvpTexts.PressToPlayWithBots;
        }
        return isReady ? PvpTexts.PressToBeNotReady : PvpTexts.PressToBeReady;
    }
}