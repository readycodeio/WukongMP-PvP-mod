using WukongMp.Api.Resources;

namespace WukongMp.PvP.UI;

public static class TextUtils
{
    public static string GetReadyText(int playersCount, bool isReady)
    {
        if (playersCount == 0)
        {
            return isReady ? BuiltinTexts.PressToCancelMatch : BuiltinTexts.PressToPlayWithBots;
        }
        return isReady ? BuiltinTexts.PressToBeNotReady : BuiltinTexts.PressToBeReady;
    }
}