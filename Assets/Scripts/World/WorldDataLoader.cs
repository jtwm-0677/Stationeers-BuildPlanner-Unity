using UnityEngine;
using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace StationeersBuildPlanner.World
{
    /// <summary>
    /// Loads world data from Stationeers game files.
    /// Parses XML configuration and loads textures for ore regions, minimaps, etc.
    /// </summary>
    public static class WorldDataLoader
    {
        // Default Steam installation path
        private const string DEFAULT_GAME_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Stationeers";
        private const string WORLDS_SUBPATH = @"rocketstation_Data\StreamingAssets\Worlds";

        private static string _gamePath;
        private static string _worldsPath;

        /// <summary>
        /// Initialize the loader with game path. Call once at startup.
        /// </summary>
        public static bool Initialize(string gamePath = null)
        {
            _gamePath = gamePath ?? DEFAULT_GAME_PATH;
            _worldsPath = Path.Combine(_gamePath, WORLDS_SUBPATH);

            if (!Directory.Exists(_worldsPath))
            {
                Debug.LogError($"[WorldDataLoader] Worlds path not found: {_worldsPath}");
                return false;
            }

            Debug.Log($"[WorldDataLoader] Initialized with worlds path: {_worldsPath}");
            return true;
        }

        /// <summary>
        /// Load data for a specific world.
        /// </summary>
        public static WorldData LoadWorld(WorldInfo worldInfo)
        {
            if (string.IsNullOrEmpty(_worldsPath))
            {
                Debug.LogError("[WorldDataLoader] Not initialized. Call Initialize() first.");
                return null;
            }

            string worldFolder = Path.Combine(_worldsPath, worldInfo.FolderName);
            string xmlPath = Path.Combine(worldFolder, worldInfo.XmlFileName);

            if (!File.Exists(xmlPath))
            {
                Debug.LogError($"[WorldDataLoader] World XML not found: {xmlPath}");
                return null;
            }

            try
            {
                WorldData world = ParseWorldXml(xmlPath, worldInfo);
                if (world != null)
                {
                    LoadWorldTextures(world, worldFolder);
                }
                return world;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDataLoader] Failed to load {worldInfo.DisplayName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load all available worlds.
        /// </summary>
        public static Dictionary<string, WorldData> LoadAllWorlds()
        {
            var worlds = new Dictionary<string, WorldData>();

            foreach (var worldInfo in WorldRegistry.AvailableWorlds)
            {
                var world = LoadWorld(worldInfo);
                if (world != null)
                {
                    // Key by FolderName for consistent lookup in WorldMapController
                    worlds[worldInfo.FolderName] = world;
                    Debug.Log($"[WorldDataLoader] Loaded {worldInfo.FolderName}: {world.StartLocations.Count} spawn points, {world.OreRegions.Count} ore regions");
                }
            }

            return worlds;
        }

        private static WorldData ParseWorldXml(string xmlPath, WorldInfo worldInfo)
        {
            var world = new WorldData
            {
                Id = worldInfo.FolderName,
                DisplayName = worldInfo.DisplayName
            };

            var doc = new XmlDocument();
            doc.Load(xmlPath);

            // Find the World element
            var worldNode = doc.SelectSingleNode("//World");
            if (worldNode == null)
            {
                Debug.LogError($"[WorldDataLoader] No <World> element found in {xmlPath}");
                return null;
            }

            // Parse basic attributes
            if (worldNode.Attributes["Id"] != null)
                world.Id = worldNode.Attributes["Id"].Value;

            // Parse gravity
            var gravityNode = worldNode.SelectSingleNode("Gravity");
            if (gravityNode != null && float.TryParse(gravityNode.InnerText, out float gravity))
                world.Gravity = gravity;

            // Parse terrain settings for world size
            var terrainNode = worldNode.SelectSingleNode("TerrainSettings");
            if (terrainNode?.Attributes["WorldSize"] != null)
            {
                if (int.TryParse(terrainNode.Attributes["WorldSize"].Value, out int worldSize))
                    world.WorldSize = worldSize;

                // Get minimap path
                var minimapNode = terrainNode.SelectSingleNode("MiniMap");
                if (minimapNode?.Attributes["Path"] != null)
                    world.MinimapPath = minimapNode.Attributes["Path"].Value;
            }

            // Parse start locations
            ParseStartLocations(worldNode, world);

            // Parse deep mining regions
            ParseDeepMiningRegions(worldNode, world);

            // Parse named regions
            ParseNamedRegions(worldNode, world);

            return world;
        }

        private static void ParseStartLocations(XmlNode worldNode, WorldData world)
        {
            var startNodes = worldNode.SelectNodes("StartLocation");
            if (startNodes == null) return;

            foreach (XmlNode node in startNodes)
            {
                var location = new StartLocation();

                if (node.Attributes["Id"] != null)
                    location.Id = node.Attributes["Id"].Value;

                var nameNode = node.SelectSingleNode("Name");
                if (nameNode?.Attributes["Key"] != null)
                    location.NameKey = nameNode.Attributes["Key"].Value;

                var descNode = node.SelectSingleNode("Description");
                if (descNode?.Attributes["Key"] != null)
                    location.DescriptionKey = descNode.Attributes["Key"].Value;

                var posNode = node.SelectSingleNode("Position");
                if (posNode != null)
                {
                    float x = 0, y = 0;
                    if (posNode.Attributes["x"] != null)
                        float.TryParse(posNode.Attributes["x"].Value, out x);
                    if (posNode.Attributes["y"] != null)
                        float.TryParse(posNode.Attributes["y"].Value, out y);
                    location.Position = new Vector2(x, y);
                }

                var radiusNode = node.SelectSingleNode("SpawnRadius");
                if (radiusNode?.Attributes["Value"] != null)
                    float.TryParse(radiusNode.Attributes["Value"].Value, out location.SpawnRadius);

                // Generate display name from ID if no localization available
                location.DisplayName = GenerateDisplayName(location.Id);

                world.StartLocations.Add(location);
            }
        }

        private static void ParseDeepMiningRegions(XmlNode worldNode, WorldData world)
        {
            // Find DeepMiningRegions RegionSet
            var regionSets = worldNode.SelectNodes("RegionSet");
            if (regionSets == null) return;

            foreach (XmlNode regionSet in regionSets)
            {
                var idAttr = regionSet.Attributes["Id"];
                if (idAttr == null) continue;

                // Look for deep mining region sets
                if (!idAttr.Value.Contains("DeepMining")) continue;

                // Get texture path
                var textureNode = regionSet.SelectSingleNode("Texture");
                if (textureNode?.Attributes["Path"] != null)
                    world.DeepMiningTexturePath = textureNode.Attributes["Path"].Value;

                // Parse region definitions
                var regions = regionSet.SelectNodes("Region");
                if (regions == null) continue;

                foreach (XmlNode regionNode in regions)
                {
                    var region = new OreRegion();

                    if (regionNode.Attributes["Id"] != null)
                        region.Id = regionNode.Attributes["Id"].Value;

                    // Parse RGB color
                    byte r = 0, g = 0, b = 0;
                    if (regionNode.Attributes["R"] != null)
                        byte.TryParse(regionNode.Attributes["R"].Value, out r);
                    if (regionNode.Attributes["G"] != null)
                        byte.TryParse(regionNode.Attributes["G"].Value, out g);
                    if (regionNode.Attributes["B"] != null)
                        byte.TryParse(regionNode.Attributes["B"].Value, out b);

                    region.Color = new Color32(r, g, b, 255);
                    region.OreType = OreRegion.ParseOreType(region.Id);

                    // Only add if we have a color definition (skip reference-only entries)
                    if (r != 0 || g != 0 || b != 0)
                    {
                        world.OreRegions.Add(region);
                    }
                }
            }
        }

        private static void ParseNamedRegions(XmlNode worldNode, WorldData world)
        {
            var regionSets = worldNode.SelectNodes("RegionSet");
            if (regionSets == null) return;

            foreach (XmlNode regionSet in regionSets)
            {
                var idAttr = regionSet.Attributes["Id"];
                if (idAttr == null) continue;

                // Look for named regions (not deep mining, not POI, not playable bounds)
                if (!idAttr.Value.Contains("NamedRegions")) continue;

                // Get texture path
                var textureNode = regionSet.SelectSingleNode("Texture");
                if (textureNode?.Attributes["Path"] != null)
                    world.NamedRegionsTexturePath = textureNode.Attributes["Path"].Value;

                // Parse region definitions
                var regions = regionSet.SelectNodes("Region");
                if (regions == null) continue;

                foreach (XmlNode regionNode in regions)
                {
                    var region = new NamedRegion();

                    if (regionNode.Attributes["Id"] != null)
                        region.Id = regionNode.Attributes["Id"].Value;

                    var nameNode = regionNode.SelectSingleNode("Name");
                    if (nameNode?.Attributes["Key"] != null)
                        region.NameKey = nameNode.Attributes["Key"].Value;

                    // Parse RGB color
                    byte r = 0, g = 0, b = 0;
                    if (regionNode.Attributes["R"] != null)
                        byte.TryParse(regionNode.Attributes["R"].Value, out r);
                    if (regionNode.Attributes["G"] != null)
                        byte.TryParse(regionNode.Attributes["G"].Value, out g);
                    if (regionNode.Attributes["B"] != null)
                        byte.TryParse(regionNode.Attributes["B"].Value, out b);

                    region.Color = new Color32(r, g, b, 255);
                    region.DisplayName = GenerateDisplayName(region.Id);

                    if (r != 0 || g != 0 || b != 0)
                    {
                        world.NamedRegions.Add(region);
                    }
                }
            }
        }

        private static void LoadWorldTextures(WorldData world, string worldFolder)
        {
            // Load minimap
            if (!string.IsNullOrEmpty(world.MinimapPath))
            {
                string minimapFullPath = Path.Combine(_gamePath, "rocketstation_Data", "StreamingAssets", world.MinimapPath);
                world.MinimapTexture = LoadTexture(minimapFullPath);
            }

            // Load deep mining texture
            if (!string.IsNullOrEmpty(world.DeepMiningTexturePath))
            {
                string miningFullPath = Path.Combine(_gamePath, "rocketstation_Data", "StreamingAssets", world.DeepMiningTexturePath);
                world.DeepMiningTexture = LoadTexture(miningFullPath);
            }

            // Load named regions texture
            if (!string.IsNullOrEmpty(world.NamedRegionsTexturePath))
            {
                string namedFullPath = Path.Combine(_gamePath, "rocketstation_Data", "StreamingAssets", world.NamedRegionsTexturePath);
                world.NamedRegionsTexture = LoadTexture(namedFullPath);
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[WorldDataLoader] Texture not found: {path}");
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point; // No interpolation for color-coded textures

                if (texture.LoadImage(data))
                {
                    texture.name = Path.GetFileNameWithoutExtension(path);
                    return texture;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDataLoader] Failed to load texture {path}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Generate a readable display name from an ID string.
        /// E.g., "MarsSpawnCanyonOverlook" -> "Canyon Overlook"
        /// </summary>
        private static string GenerateDisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;

            // Remove common prefixes
            string[] prefixes = { "MarsSpawn", "LunarSpawn", "EuropaSpawn", "VulcanSpawn", "VenusSpawn", "MimasSpawn",
                                  "MarsNamedRegion", "LunarNamedRegion", "GeoRegion" };

            string name = id;
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix))
                {
                    name = name.Substring(prefix.Length);
                    break;
                }
            }

            // Insert spaces before capital letters
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(name[i]);
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Get ore color at a specific world coordinate.
        /// Returns null if no ore region at that location.
        /// </summary>
        public static OreRegion GetOreAtPosition(WorldData world, Vector2 worldPos)
        {
            if (world.DeepMiningTexture == null || world.OreRegions.Count == 0)
                return null;

            // Convert world coordinates to texture UV
            float u = (worldPos.x + world.WorldSize / 2f) / world.WorldSize;
            float v = (worldPos.y + world.WorldSize / 2f) / world.WorldSize;

            // Clamp to valid range
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            // Sample texture
            int x = Mathf.RoundToInt(u * (world.DeepMiningTexture.width - 1));
            int y = Mathf.RoundToInt(v * (world.DeepMiningTexture.height - 1));

            Color32 pixelColor = world.DeepMiningTexture.GetPixel(x, y);

            // Find matching ore region
            foreach (var region in world.OreRegions)
            {
                if (region.Color.r == pixelColor.r &&
                    region.Color.g == pixelColor.g &&
                    region.Color.b == pixelColor.b)
                {
                    return region;
                }
            }

            return null;
        }
    }
}
