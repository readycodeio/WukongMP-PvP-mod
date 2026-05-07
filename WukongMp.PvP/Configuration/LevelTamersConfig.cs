using System.Collections.Generic;

namespace WukongMp.PvP.Configuration
{
    internal static class LevelTamersConfig
    {
        private static readonly Dictionary<int, List<string>> LevelTamers = new()
        {
            { 31, ["UGuid.LYS.Gou.HZSJ", "UGuid.LYS.Shenhou.HZSJ", "UGuid.LYS.Hu_Wind.HZSJ", "UGuid.LYS.RuYiLong.HZSJ", "UGuid.LYS.Hu_Wind_Battle.HZSJ"] }  // Zodiac Village
        };

        public static List<string> GetLevelTamers(int levelId)
        {
            return LevelTamers.TryGetValue(levelId, out var tamers) ? tamers : [];
        }
    }
}