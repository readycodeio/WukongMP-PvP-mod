using System.Collections.Generic;

namespace WukongMp.PvP.Configuration
{
    internal static class LevelDisabledAreasConfig
    {
        private static readonly Dictionary<int, List<string>> DisabledAreasMap = new()
        {
            { 31, ["1717537845822-hz10-fice-pc-0BBFD4CD494AF632E27134945658AE4B-BuffTriggerArea_C_1", "1695309263809-b70e6a6d4943ee22a64b37bbb5f220d6-BuffTriggerArea_C_1"] }  // Zodiac Village
        };

        public static List<string> GetDisabledAreas(int levelId)
        {
            return DisabledAreasMap.TryGetValue(levelId, out var areas) ? areas : [];
        }
    }
}