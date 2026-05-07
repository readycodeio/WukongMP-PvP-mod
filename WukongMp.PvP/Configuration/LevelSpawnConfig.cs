using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.Configuration
{
    internal static class LevelSpawnConfig
    {
        private static readonly Dictionary<int, LevelSpawnData> Configurations = new()
        {
            { 0, new LevelSpawnData(61, 17, 6101, new FVector(-11146, -3229, 6507), 3000) }, // Heart of Birthstone
            { 1, new LevelSpawnData(98, 5, 9803, new FVector(78686, -22648, 14646)) }, // Rhino Watch Slope
            { 2, new LevelSpawnData(98, 7, 9802, new FVector(-48308, -92826, 5658)) }, // Deer Sight Forest
            { 3, new LevelSpawnData(20, 21, 2010, new FVector(-82034, 26036, -10158), 3000) }, // Windseal Gate
            { 4, new LevelSpawnData(30, 6, 3004, new FVector(399750, -346464, -17503)) }, // Mirrormere
            { 5, new LevelSpawnData(98, 11, 9801, new FVector(-128621, -36775, -4407)) }, // Cooling Slope
            { 6, new LevelSpawnData(50, 7, 5009, new FVector(51132, -5121, 26367), 3000) }, // Fallen Furnance Crater

            { 7, new LevelSpawnData(10, 26, 1008, new FVector(-73476, 29887, 10001.03), 3000,
                new TeamSpawnPoints(new FVector(-70386, 29001, 9993.6), new FVector(-77563, 31068, 10049.39))) }, // Bodhi Peak
            { 8, new LevelSpawnData(70, 7, 7004, new FVector(107291, -142160, 12900.79), 2700,
                new TeamSpawnPoints(new FVector(104444.3, -140557.5, 12909.36), new FVector(109109.4, -145044.4, 12980.76))) }, // Corridor of Fire and Ice - lava damage
            { 9, new LevelSpawnData(12, 27, 1013, new FVector(-94705, -22403, -8419.67), 2700) }, // Loong Claw Grove - no shrine
            { 10, new LevelSpawnData(20, 35, 2016, new FVector(128532, -21342, 4466.41), 2600) }, // Bottom of the Well
            { 11, new LevelSpawnData(30, 33, 3020, new FVector(-153095, -271407, -45556.81), 2500,
                new TeamSpawnPoints(new FVector(-151490, -274315, -45556.81), new FVector(-154333, -267356, -45556.81))) }, // Watermelon Field
            { 12, new LevelSpawnData(40, 96, 4013, new FVector(146478, -66773, -3319.89), 3000) }, // Bonevault
            { 13, new LevelSpawnData(30, 39, 3026, new FVector(-216424, -127145, -19491.41), 3500,
                new TeamSpawnPoints(new FVector(-213720, -130778, -19491.48), new FVector(-218046, -124882, -19492.01))) }, // Mahavira Hall
            { 14, new LevelSpawnData(80, 12, 8005, new FVector(12302, 38156, 7803.24), 3800) }, // Cloudnest Peak
            { 15, new LevelSpawnData(40, 21, 4028, new FVector(75507, 143275, 51508.68), 4000,
                new TeamSpawnPoints(new FVector(72554, 138724, 51497.77), new FVector(78732, 149143, 51504.09))) }, // Court of Illumination
            { 16, new LevelSpawnData(31, 0, 3102, new FVector(-10046, 91668, -1617.68), 1700,
                new TeamSpawnPoints(new FVector(-10797, 90212, -1626.09), new FVector(-8972, 94147, -1606.64))) }, // Zodiac Village
            // { 17, new LevelSpawnData(70, 2, 7002, new FVector(200524, -45683, 31919.74), 3000) }, // Purge Pit - collider on the arena
        };

        public static LevelSpawnData GetLevelSpawnData(int levelId)
        {
            return Configurations[levelId];
        }

        public static LevelSpawnData GetCurrentLevelSpawnData()
        {
            if (WukongApi.Sync.LocalMainCharacter is not { } main)
                return GetLevelSpawnData(0);
            
            var levelId = WukongApi.PvP.LevelId;            
            return GetLevelSpawnData(levelId);
        }
    }
}