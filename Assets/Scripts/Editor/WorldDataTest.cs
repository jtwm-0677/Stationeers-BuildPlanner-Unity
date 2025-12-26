using UnityEngine;
using UnityEditor;
using StationeersBuildPlanner.World;

namespace StationeersBuildPlanner.Editor
{
    /// <summary>
    /// Editor tool to test WorldDataLoader functionality.
    /// </summary>
    public class WorldDataTest : EditorWindow
    {
        private Vector2 scrollPos;
        private WorldData selectedWorld;
        private string[] worldNames;
        private int selectedWorldIndex;

        [MenuItem("Stationeers/World Data Test")]
        public static void ShowWindow()
        {
            GetWindow<WorldDataTest>("World Data Test");
        }

        private void OnEnable()
        {
            worldNames = new string[WorldRegistry.AvailableWorlds.Length];
            for (int i = 0; i < WorldRegistry.AvailableWorlds.Length; i++)
            {
                worldNames[i] = WorldRegistry.AvailableWorlds[i].DisplayName;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("World Data Loader Test", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Initialize Loader", GUILayout.Height(25)))
            {
                if (WorldDataLoader.Initialize())
                {
                    Debug.Log("[WorldDataTest] Loader initialized successfully");
                }
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            selectedWorldIndex = EditorGUILayout.Popup("World:", selectedWorldIndex, worldNames);
            if (GUILayout.Button("Load", GUILayout.Width(60)))
            {
                LoadSelectedWorld();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Load All Worlds"))
            {
                LoadAllWorlds();
            }

            GUILayout.Space(20);

            if (selectedWorld != null)
            {
                DrawWorldInfo();
            }
        }

        private void LoadSelectedWorld()
        {
            if (!WorldDataLoader.Initialize()) return;

            var worldInfo = WorldRegistry.AvailableWorlds[selectedWorldIndex];
            selectedWorld = WorldDataLoader.LoadWorld(worldInfo);

            if (selectedWorld != null)
            {
                Debug.Log($"[WorldDataTest] Loaded {selectedWorld.DisplayName}");
            }
        }

        private void LoadAllWorlds()
        {
            if (!WorldDataLoader.Initialize()) return;

            var worlds = WorldDataLoader.LoadAllWorlds();
            Debug.Log($"[WorldDataTest] Loaded {worlds.Count} worlds");

            foreach (var kvp in worlds)
            {
                var w = kvp.Value;
                Debug.Log($"  {w.DisplayName}: WorldSize={w.WorldSize}, Gravity={w.Gravity}, " +
                          $"Spawns={w.StartLocations.Count}, OreRegions={w.OreRegions.Count}");
            }
        }

        private void DrawWorldInfo()
        {
            GUILayout.Label($"World: {selectedWorld.DisplayName}", EditorStyles.boldLabel);
            GUILayout.Label($"ID: {selectedWorld.Id}");
            GUILayout.Label($"World Size: {selectedWorld.WorldSize} (range {-selectedWorld.WorldSize/2} to {selectedWorld.WorldSize/2})");
            GUILayout.Label($"Gravity: {selectedWorld.Gravity}");

            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Start Locations
            GUILayout.Label($"Start Locations ({selectedWorld.StartLocations.Count}):", EditorStyles.boldLabel);
            foreach (var loc in selectedWorld.StartLocations)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"  {loc.DisplayName}", GUILayout.Width(200));
                GUILayout.Label($"({loc.Position.x}, {loc.Position.y})", GUILayout.Width(150));
                GUILayout.Label($"r={loc.SpawnRadius}");
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Ore Regions
            GUILayout.Label($"Ore Regions ({selectedWorld.OreRegions.Count}):", EditorStyles.boldLabel);
            foreach (var ore in selectedWorld.OreRegions)
            {
                EditorGUILayout.BeginHorizontal();

                // Color preview
                var colorRect = GUILayoutUtility.GetRect(20, 16, GUILayout.Width(20));
                EditorGUI.DrawRect(colorRect, ore.Color);

                GUILayout.Label($"  {ore.OreType}", GUILayout.Width(150));
                GUILayout.Label($"RGB({ore.Color.r}, {ore.Color.g}, {ore.Color.b})");
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Textures
            GUILayout.Label("Textures:", EditorStyles.boldLabel);
            DrawTextureInfo("Minimap", selectedWorld.MinimapTexture, selectedWorld.MinimapPath);
            DrawTextureInfo("Deep Mining", selectedWorld.DeepMiningTexture, selectedWorld.DeepMiningTexturePath);
            DrawTextureInfo("Named Regions", selectedWorld.NamedRegionsTexture, selectedWorld.NamedRegionsTexturePath);

            EditorGUILayout.EndScrollView();
        }

        private void DrawTextureInfo(string label, Texture2D texture, string path)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}:", GUILayout.Width(100));

            if (texture != null)
            {
                GUILayout.Label($"{texture.width}x{texture.height}", GUILayout.Width(80));

                // Small preview
                var rect = GUILayoutUtility.GetRect(50, 50, GUILayout.Width(50), GUILayout.Height(50));
                EditorGUI.DrawPreviewTexture(rect, texture);
            }
            else
            {
                GUILayout.Label("(not loaded)");
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(path))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Path:", path);
                EditorGUI.indentLevel--;
            }
        }
    }
}
