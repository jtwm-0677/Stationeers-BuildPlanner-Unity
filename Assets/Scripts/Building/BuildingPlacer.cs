using UnityEngine;
using StationeersBuildPlanner.Core;

namespace StationeersBuildPlanner.Building
{
    /// <summary>
    /// Handles placing and previewing buildable objects in the scene.
    /// </summary>
    public class BuildingPlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private LayerMask placementLayers;

        [Header("Preview")]
        [SerializeField] private Material previewMaterial;
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("Current Selection")]
        [SerializeField] private GameObject currentPrefab;

        private GameObject previewObject;
        private bool canPlace = false;
        private GridSystem gridSystem;

        private void Start()
        {
            gridSystem = GridSystem.Instance;
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (currentPrefab != null)
            {
                UpdatePreview();
                HandlePlacement();
                HandleRotation();
            }

            HandleCancel();
        }

        public void SetCurrentPrefab(GameObject prefab)
        {
            // Cleanup old preview
            if (previewObject != null)
            {
                Destroy(previewObject);
            }

            currentPrefab = prefab;

            if (prefab != null)
            {
                // Create new preview
                previewObject = Instantiate(prefab);
                previewObject.name = "Preview_" + prefab.name;

                // Remove colliders from preview
                foreach (var col in previewObject.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }

                // Apply preview material
                if (previewMaterial != null)
                {
                    foreach (var renderer in previewObject.GetComponentsInChildren<Renderer>())
                    {
                        Material[] mats = new Material[renderer.materials.Length];
                        for (int i = 0; i < mats.Length; i++)
                        {
                            mats[i] = previewMaterial;
                        }
                        renderer.materials = mats;
                    }
                }
            }
        }

        private void UpdatePreview()
        {
            if (previewObject == null) return;

            // Raycast from mouse position
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placementLayers))
            {
                // Snap to grid
                Vector3 snappedPos = gridSystem != null
                    ? gridSystem.SnapToMainGrid(hit.point)
                    : hit.point;

                previewObject.transform.position = snappedPos;
                previewObject.SetActive(true);

                // TODO: Check for collisions and update canPlace
                canPlace = true;
                UpdatePreviewColor(canPlace);
            }
            else
            {
                // No valid surface - raycast to a plane at y=0
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    Vector3 snappedPos = gridSystem != null
                        ? gridSystem.SnapToMainGrid(point)
                        : point;

                    previewObject.transform.position = snappedPos;
                    previewObject.SetActive(true);
                    canPlace = true;
                    UpdatePreviewColor(canPlace);
                }
                else
                {
                    previewObject.SetActive(false);
                }
            }
        }

        private void UpdatePreviewColor(bool valid)
        {
            if (previewMaterial == null) return;

            Color color = valid ? validColor : invalidColor;
            previewMaterial.color = color;
        }

        private void HandlePlacement()
        {
            if (Input.GetMouseButtonDown(0) && canPlace && previewObject != null)
            {
                // Don't place if over UI
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                // Place the object
                GameObject placed = Instantiate(currentPrefab,
                    previewObject.transform.position,
                    previewObject.transform.rotation);
                placed.name = currentPrefab.name;

                // TODO: Register with building system, network tracking, etc.
            }
        }

        private void HandleRotation()
        {
            if (previewObject == null) return;

            // R key to rotate 90 degrees on Y axis
            if (Input.GetKeyDown(KeyCode.R))
            {
                previewObject.transform.Rotate(0f, 90f, 0f);
            }

            // T key to rotate on X axis
            if (Input.GetKeyDown(KeyCode.T))
            {
                previewObject.transform.Rotate(90f, 0f, 0f);
            }
        }

        private void HandleCancel()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                if (previewObject != null)
                {
                    Destroy(previewObject);
                    previewObject = null;
                    currentPrefab = null;
                }
            }
        }

        private void OnDestroy()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }
        }
    }
}
