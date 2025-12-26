using UnityEngine;
using System.Collections.Generic;

namespace StationeersBuildPlanner.World
{
    /// <summary>
    /// Data for a single world/planet parsed from game XML files.
    /// Coordinates are 1:1 with game coordinates (4096x4096, range -2048 to +2048).
    /// </summary>
    [System.Serializable]
    public class WorldData
    {
        public string Id;
        public string DisplayName;
        public int WorldSize = 4096;
        public float Gravity;

        public List<StartLocation> StartLocations = new List<StartLocation>();
        public List<OreRegion> OreRegions = new List<OreRegion>();
        public List<NamedRegion> NamedRegions = new List<NamedRegion>();

        // Texture paths (relative to StreamingAssets/Worlds/{WorldFolder}/)
        public string MinimapPath;
        public string DeepMiningTexturePath;
        public string NamedRegionsTexturePath;

        // Loaded textures (populated after loading)
        public Texture2D MinimapTexture;
        public Texture2D DeepMiningTexture;
        public Texture2D NamedRegionsTexture;

        /// <summary>
        /// Get coordinate range for this world.
        /// </summary>
        public Vector2 CoordinateMin => new Vector2(-WorldSize / 2f, -WorldSize / 2f);
        public Vector2 CoordinateMax => new Vector2(WorldSize / 2f, WorldSize / 2f);
    }

    /// <summary>
    /// A named spawn/start location on a world.
    /// </summary>
    [System.Serializable]
    public class StartLocation
    {
        public string Id;
        public string NameKey;           // Localization key
        public string DescriptionKey;    // Localization key
        public Vector2 Position;         // Game coordinates (x, y)
        public float SpawnRadius;

        // Resolved display name (after localization lookup)
        public string DisplayName;
    }

    /// <summary>
    /// An ore/deep mining region defined by RGB color in texture.
    /// </summary>
    [System.Serializable]
    public class OreRegion
    {
        public string Id;
        public Color32 Color;            // RGB color in texture
        public string OreType;           // Derived from Id (e.g., "Iron", "GoldSilver")

        /// <summary>
        /// Parse ore type from region ID.
        /// E.g., "MarsDeepMiningRegionIron" -> "Iron"
        /// E.g., "DeepMinerRegionGoldSilver" -> "GoldSilver"
        /// </summary>
        public static string ParseOreType(string regionId)
        {
            // Common patterns:
            // {World}DeepMiningRegion{OreType}
            // DeepMinerRegion{OreType}

            string[] patterns = { "DeepMiningRegion", "DeepMinerRegion" };
            foreach (var pattern in patterns)
            {
                int idx = regionId.IndexOf(pattern);
                if (idx >= 0)
                {
                    return regionId.Substring(idx + pattern.Length);
                }
            }
            return regionId;
        }
    }

    /// <summary>
    /// A named geographic region (e.g., "Butchers Flat", "Canyon Overlook").
    /// </summary>
    [System.Serializable]
    public class NamedRegion
    {
        public string Id;
        public string NameKey;           // Localization key
        public Color32 Color;            // RGB color in texture

        // Resolved display name
        public string DisplayName;
    }

    /// <summary>
    /// Metadata about all available worlds.
    /// </summary>
    public static class WorldRegistry
    {
        public static readonly WorldInfo[] AvailableWorlds = new WorldInfo[]
        {
            new WorldInfo("Lunar", "Lunar.xml", "Lunar"),
            new WorldInfo("Europa", "Europa.xml", "Europa"),
            new WorldInfo("Mars", "Mars2.xml", "Mars2"),
            new WorldInfo("Mimas", "MimasHerschel.xml", "Mimas"),
            new WorldInfo("Venus", "Venus.xml", "Venus"),
            new WorldInfo("Vulcan", "Vulcan.xml", "Vulcan"),
        };
    }

    /// <summary>
    /// Basic info about a world for loading purposes.
    /// </summary>
    public class WorldInfo
    {
        public string DisplayName;
        public string XmlFileName;
        public string FolderName;

        public WorldInfo(string displayName, string xmlFileName, string folderName)
        {
            DisplayName = displayName;
            XmlFileName = xmlFileName;
            FolderName = folderName;
        }
    }
}
