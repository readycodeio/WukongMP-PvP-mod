using System.Collections.Generic;
using System.Numerics;

namespace WukongMp.Pvp.Common.Data;

public static class LevelSpawnConfig
{
    private static readonly Dictionary<int, LevelSpawnData> Configurations = new()
    {
        { 0, new LevelSpawnData(61, 17, 6101, new Vector3(-11146, -3229, 6507), 3000) }, // Heart of Birthstone
        { 1, new LevelSpawnData(98, 5, 9803, new Vector3(78686, -22648, 14646)) }, // Rhino Watch Slope
        { 2, new LevelSpawnData(98, 7, 9802, new Vector3(-48308, -92826, 5658)) }, // Deer Sight Forest
        { 3, new LevelSpawnData(20, 21, 2010, new Vector3(-82034, 26036, -10158), 3000) }, // Windseal Gate
        { 4, new LevelSpawnData(30, 6, 3004, new Vector3(399750, -346464, -17503)) }, // Mirrormere
        { 5, new LevelSpawnData(98, 11, 9801, new Vector3(-128621, -36775, -4407)) }, // Cooling Slope
        { 6, new LevelSpawnData(50, 7, 5009, new Vector3(51132, -5121, 26367), 3000) }, // Fallen Furnance Crater
        { 7, new LevelSpawnData(10, 26, 1008, new Vector3(-73476, 29887, 10001.03f), 3000,
            new TeamSpawnPoints(new Vector3(-70386, 29001, 9993.6f), new Vector3(-77563, 31068, 10049.39f))) }, // Bodhi Peak
        { 8, new LevelSpawnData(70, 7, 7004, new Vector3(107291, -142160, 12900.79f), 2700,
            new TeamSpawnPoints(new Vector3(104444.3f, -140557.5f, 12909.36f), new Vector3(109109.4f, -145044.4f, 12980.76f))) }, // Corridor of Fire and Ice - lava damage
        { 9, new LevelSpawnData(12, 27, 1013, new Vector3(-94705, -22403, -8419.67f), 2700) }, // Loong Claw Grove - no shrine
        { 10, new LevelSpawnData(20, 35, 2016, new Vector3(128532, -21342, 4466.41f), 2600) }, // Bottom of the Well
        { 11, new LevelSpawnData(30, 33, 3020, new Vector3(-153095, -271407, -45556.81f), 2500,
            new TeamSpawnPoints(new Vector3(-151490, -274315, -45556.81f), new Vector3(-154333, -267356, -45556.81f))) }, // Watermelon Field
        { 12, new LevelSpawnData(40, 96, 4013, new Vector3(146478, -66773, -3319.89f), 3000) }, // Bonevault
        { 13, new LevelSpawnData(30, 39, 3026, new Vector3(-216424, -127145, -19491.41f), 3500,
            new TeamSpawnPoints(new Vector3(-213720, -130778, -19491.48f), new Vector3(-218046, -124882, -19492.01f))) }, // Mahavira Hall
        { 14, new LevelSpawnData(80, 12, 8005, new Vector3(12302, 38156, 7803.24f), 3800) }, // Cloudnest Peak
        { 15, new LevelSpawnData(40, 21, 4028, new Vector3(75507, 143275, 51508.68f), 4000,
            new TeamSpawnPoints(new Vector3(72554, 138724, 51497.77f), new Vector3(78732, 149143, 51504.09f))) }, // Court of Illumination
        { 16, new LevelSpawnData(31, 0, 3102, new Vector3(-10046, 91668, -1617.68f), 1700,
            new TeamSpawnPoints(new Vector3(-10797, 90212, -1626.09f), new Vector3(-8972, 94147, -1606.64f))) }, // Zodiac Village
        // { 17, new LevelSpawnData(70, 2, 7002, new Vector3(200524, -45683, 31919.74), 3000) }, // Purge Pit - collider on the arena
    };
    
    public static bool IsValidLevel(int levelId) => Configurations.ContainsKey(levelId);

    public static LevelSpawnData GetLevelSpawnData(int levelId)
    {
        if (!Configurations.TryGetValue(levelId, out var data))
        {
            data = Configurations[0]; // Default to Heart of Birthstone if level ID is not found
        }

        return data;
    }
}