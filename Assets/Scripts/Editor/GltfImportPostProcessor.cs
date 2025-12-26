using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace StationeersBuildPlanner.Editor
{
    /// <summary>
    /// Post-processor that automatically assigns textures to GLB meshes after import.
    /// Runs whenever assets are imported/reimported.
    /// </summary>
    public class GltfImportPostProcessor : AssetPostprocessor
    {
        private const string MESH_PATH = "Assets/GameAssets/Meshes";
        private const string TEXTURE_PATH = "Assets/GameAssets/Textures";

        // Called after all assets have been imported
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                // Only process GLB files in our mesh folder
                if (!assetPath.StartsWith(MESH_PATH))
                    continue;

                if (!assetPath.EndsWith(".glb") && !assetPath.EndsWith(".gltf"))
                    continue;

                ProcessImportedMesh(assetPath);
            }
        }

        private static void ProcessImportedMesh(string meshPath)
        {
            string meshName = Path.GetFileNameWithoutExtension(meshPath);

            // Load the imported model
            var modelImporter = AssetImporter.GetAtPath(meshPath) as ModelImporter;
            if (modelImporter == null)
                return;

            // Find matching texture
            string texturePath = FindMatchingTexture(meshName);
            if (string.IsNullOrEmpty(texturePath))
            {
                // Debug.Log($"[GltfImport] No texture found for: {meshName}");
                return;
            }

            // Load texture
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                return;

            // Extract materials if not already done
            if (modelImporter.materialImportMode == ModelImporterMaterialImportMode.None)
            {
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                modelImporter.SaveAndReimport();
            }

            // Find materials in the imported asset
            var assets = AssetDatabase.LoadAllAssetsAtPath(meshPath);
            foreach (var asset in assets)
            {
                if (asset is Material material)
                {
                    if (material.mainTexture == null)
                    {
                        material.mainTexture = texture;
                        material.SetFloat("_Metallic", 0.1f);
                        material.SetFloat("_Glossiness", 0.3f);
                        EditorUtility.SetDirty(material);
                        Debug.Log($"[GltfImport] Assigned {Path.GetFileName(texturePath)} to {meshName}");
                    }
                }
            }
        }

        private static string FindMatchingTexture(string meshName)
        {
            // Search for texture with matching name
            var guids = AssetDatabase.FindAssets($"t:Texture2D", new[] { TEXTURE_PATH });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texName = Path.GetFileNameWithoutExtension(path);

                // Exact match
                if (texName.Equals(meshName, System.StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            // Try partial match (mesh might have suffix like _LOD0)
            string baseName = meshName.Split('_')[0];
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texName = Path.GetFileNameWithoutExtension(path);

                if (texName.Equals(baseName, System.StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }
    }
}
