using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace StationeersBuildPlanner.Editor
{
    /// <summary>
    /// Editor utility to auto-assign textures to imported GLB meshes.
    /// GLB files from Stationeers are geometry-only; textures are in a separate folder
    /// and matched by naming convention.
    /// </summary>
    public class TextureAssigner : EditorWindow
    {
        private const string MESH_PATH = "Assets/GameAssets/Meshes";
        private const string TEXTURE_PATH = "Assets/GameAssets/Textures";
        private const string GENERATED_MATERIALS_PATH = "Assets/GeneratedMaterials";

        private Vector2 scrollPos;
        private string searchFilter = "";
        private List<string> processLog = new List<string>();

        [MenuItem("Stationeers/Texture Assigner")]
        public static void ShowWindow()
        {
            GetWindow<TextureAssigner>("Texture Assigner");
        }

        private void OnGUI()
        {
            GUILayout.Label("Stationeers Texture Assigner", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool assigns textures to imported GLB meshes by matching filenames.\n" +
                "Example: FrameIron.glb will use FrameIron.png as its albedo texture.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Assign Textures to All Meshes", GUILayout.Height(30)))
            {
                AssignAllTextures();
            }

            GUILayout.Space(5);

            searchFilter = EditorGUILayout.TextField("Filter (mesh name):", searchFilter);

            if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("Assign to Filtered Meshes"))
            {
                AssignFilteredTextures(searchFilter);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Assign to Selected Objects"))
            {
                AssignToSelection();
            }

            GUILayout.Space(20);
            GUILayout.Label("Process Log:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var log in processLog.TakeLast(100))
            {
                EditorGUILayout.LabelField(log);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log"))
            {
                processLog.Clear();
            }
        }

        private string GetFullPath(string assetPath)
        {
            // Convert "Assets/GameAssets/Meshes" to full filesystem path
            // Application.dataPath = "C:/Development/StationeersBuildPlanner/Assets"
            return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        }

        private void AssignAllTextures()
        {
            processLog.Clear();
            EnsureGeneratedMaterialsFolder();

            // Find all GLB/GLTF files directly instead of relying on asset type
            string meshFullPath = GetFullPath(MESH_PATH);
            processLog.Add($"Searching in: {meshFullPath}");

            if (!System.IO.Directory.Exists(meshFullPath))
            {
                processLog.Add($"ERROR: Directory not found: {meshFullPath}");
                return;
            }

            var meshFiles = System.IO.Directory.GetFiles(
                meshFullPath,
                "*.glb",
                System.IO.SearchOption.AllDirectories);

            int processed = 0;
            int assigned = 0;

            foreach (var fullPath in meshFiles)
            {
                // Convert to Unity asset path
                var meshPath = "Assets" + fullPath.Replace(Application.dataPath, "").Replace("\\", "/");

                if (ProcessMesh(meshPath))
                    assigned++;
                processed++;

                if (processed % 100 == 0)
                {
                    EditorUtility.DisplayProgressBar("Assigning Textures",
                        $"Processing {processed}/{meshFiles.Length}...",
                        (float)processed / meshFiles.Length);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            processLog.Add($"--- Complete: {assigned}/{processed} meshes assigned textures ---");
            Log($"Processed {processed} meshes, assigned textures to {assigned}");
        }

        private void AssignFilteredTextures(string filter)
        {
            processLog.Clear();
            EnsureGeneratedMaterialsFolder();

            // Find all GLB files directly
            string meshFullPath = GetFullPath(MESH_PATH);
            processLog.Add($"Searching in: {meshFullPath}");

            if (!System.IO.Directory.Exists(meshFullPath))
            {
                processLog.Add($"ERROR: Directory not found: {meshFullPath}");
                return;
            }

            var meshFiles = System.IO.Directory.GetFiles(
                meshFullPath,
                "*.glb",
                System.IO.SearchOption.AllDirectories);

            int processed = 0;
            int assigned = 0;

            foreach (var fullPath in meshFiles)
            {
                var meshName = Path.GetFileNameWithoutExtension(fullPath);

                if (meshName.ToLower().Contains(filter.ToLower()))
                {
                    var meshPath = "Assets" + fullPath.Replace(Application.dataPath, "").Replace("\\", "/");
                    if (ProcessMesh(meshPath))
                        assigned++;
                    processed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            processLog.Add($"--- Complete: {assigned}/{processed} filtered meshes assigned ---");
        }

        private void AssignToSelection()
        {
            processLog.Clear();
            EnsureGeneratedMaterialsFolder();

            int assigned = 0;
            foreach (var obj in Selection.gameObjects)
            {
                var renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    // Try to find source mesh name from the hierarchy
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        string meshName = meshFilter.sharedMesh.name;
                        if (AssignTextureToRenderer(renderer, meshName))
                            assigned++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            processLog.Add($"--- Complete: Assigned textures to {assigned} renderers ---");
        }

        private bool ProcessMesh(string meshPath)
        {
            string meshName = Path.GetFileNameWithoutExtension(meshPath);

            // Find matching texture
            string texturePath = FindTexture(meshName);
            if (string.IsNullOrEmpty(texturePath))
            {
                processLog.Add($"[SKIP] {meshName}: No texture found");
                return false;
            }

            // Load or create material
            string materialPath = $"{GENERATED_MATERIALS_PATH}/{meshName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            // Assign texture
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                material.mainTexture = texture;

                // Set reasonable defaults for Stationeers assets
                material.SetFloat("_Metallic", 0.1f);
                material.SetFloat("_Glossiness", 0.3f);

                EditorUtility.SetDirty(material);
                processLog.Add($"[OK] {meshName} -> {Path.GetFileName(texturePath)}");
                return true;
            }

            return false;
        }

        private bool AssignTextureToRenderer(Renderer renderer, string meshName)
        {
            string texturePath = FindTexture(meshName);
            if (string.IsNullOrEmpty(texturePath))
            {
                // Try parent object name
                texturePath = FindTexture(renderer.gameObject.name);
            }

            if (string.IsNullOrEmpty(texturePath))
            {
                processLog.Add($"[SKIP] {meshName}: No texture found");
                return false;
            }

            string materialPath = $"{GENERATED_MATERIALS_PATH}/{meshName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                material.SetFloat("_Metallic", 0.1f);
                material.SetFloat("_Glossiness", 0.3f);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                material.mainTexture = texture;
                EditorUtility.SetDirty(material);

                // Assign to renderer
                renderer.sharedMaterial = material;
                processLog.Add($"[OK] {meshName} -> {Path.GetFileName(texturePath)}");
                return true;
            }

            return false;
        }

        private string FindTexture(string meshName)
        {
            // First, check our texture mappings for the correct texture name
            string mappedTextureName = TextureMappings.GetTextureForMesh(meshName);

            // Try the mapped name first, then fall back to original mesh name
            string[] texturesToTry = mappedTextureName != meshName
                ? new[] { mappedTextureName, meshName }
                : new[] { meshName };

            string[] extensions = { ".png", ".jpg", ".tga" };
            string textureFullPath = GetFullPath(TEXTURE_PATH);

            foreach (var texName in texturesToTry)
            {
                // Try direct file path
                foreach (var ext in extensions)
                {
                    string fullPath = Path.Combine(textureFullPath, texName + ext);
                    if (File.Exists(fullPath))
                    {
                        return $"{TEXTURE_PATH}/{texName}{ext}";
                    }
                }

                // Try searching in AssetDatabase
                var guids = AssetDatabase.FindAssets($"{texName} t:Texture2D", new[] { TEXTURE_PATH });
                if (guids.Length > 0)
                {
                    // Prefer exact name match
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var foundTexName = Path.GetFileNameWithoutExtension(path);
                        if (foundTexName.Equals(texName, System.StringComparison.OrdinalIgnoreCase))
                            return path;
                    }
                }
            }

            // Last resort: return any partial match
            var lastGuids = AssetDatabase.FindAssets($"{meshName} t:Texture2D", new[] { TEXTURE_PATH });
            if (lastGuids.Length > 0)
                return AssetDatabase.GUIDToAssetPath(lastGuids[0]);

            return null;
        }

        private void EnsureGeneratedMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder(GENERATED_MATERIALS_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "GeneratedMaterials");
            }
        }

        private void Log(string message)
        {
            Debug.Log($"[TextureAssigner] {message}");
        }
    }
}
