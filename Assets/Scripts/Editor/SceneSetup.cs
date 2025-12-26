using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

namespace StationeersBuildPlanner.Editor
{
    /// <summary>
    /// Quick scene setup utilities for testing assets.
    /// </summary>
    public class SceneSetup
    {
        [MenuItem("Stationeers/Setup Test Scene")]
        public static void SetupTestScene()
        {
            // Add directional light if none exists
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.0f;
                light.color = Color.white;
                lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
                Debug.Log("[SceneSetup] Added directional light");
            }

            // Setup ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);

            // Add ground plane if none exists
            var existingPlane = GameObject.Find("Ground");
            if (existingPlane == null)
            {
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "Ground";
                plane.transform.position = Vector3.zero;
                plane.transform.localScale = new Vector3(10, 1, 10);

                var renderer = plane.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.3f, 0.35f);
                renderer.material = mat;
                Debug.Log("[SceneSetup] Added ground plane");
            }

            // Add camera if none exists
            if (UnityEngine.Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<UnityEngine.Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
                camGo.transform.position = new Vector3(0, 5, -10);
                camGo.transform.LookAt(Vector3.zero);

                // Add free camera controller with input actions
                var cameraController = camGo.AddComponent<StationeersBuildPlanner.Camera.FreeCameraController>();

                // Find and assign the input actions asset
                var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/BuildPlannerInput.inputactions");
                if (inputAsset != null)
                {
                    // Use SerializedObject to set the private serialized field
                    var serializedObj = new SerializedObject(cameraController);
                    var inputActionsProperty = serializedObj.FindProperty("inputActions");
                    inputActionsProperty.objectReferenceValue = inputAsset;
                    serializedObj.ApplyModifiedProperties();
                    Debug.Log("[SceneSetup] Added camera with FreeCameraController and input actions");
                }
                else
                {
                    Debug.LogWarning("[SceneSetup] Could not find BuildPlannerInput.inputactions - camera controls won't work!");
                }
            }

            Debug.Log("[SceneSetup] Test scene setup complete!");
        }

        [MenuItem("Stationeers/Fix Dark Materials")]
        public static void FixDarkMaterials()
        {
            // Find all materials in generated folder
            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/GeneratedMaterials" });
            int fixed_count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null)
                {
                    // Reduce metallic and increase smoothness for better visibility
                    material.SetFloat("_Metallic", 0.0f);
                    material.SetFloat("_Glossiness", 0.2f);
                    EditorUtility.SetDirty(material);
                    fixed_count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneSetup] Fixed {fixed_count} materials (reduced metallic, adjusted glossiness)");
        }
    }
}
