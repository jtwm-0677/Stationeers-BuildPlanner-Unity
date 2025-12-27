using UnityEngine;
using UnityEngine.InputSystem;
using StationeersBuildPlanner.Core;

namespace StationeersBuildPlanner.Grid
{
    /// <summary>
    /// Visual indicator showing current snap position.
    /// Renders a small marker at the snapped grid position.
    /// </summary>
    public class SnapIndicator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showIndicator = true;
        [SerializeField] private bool useSmallGrid = false;

        [Header("Appearance")]
        [SerializeField] private Color indicatorColor = new Color(1f, 0.5f, 0f, 0.8f);

        [Header("References")]
        [SerializeField] private Material indicatorMaterial;

        private Vector3 currentSnapPosition;
        private UnityEngine.Camera mainCamera;

        public bool ShowIndicator
        {
            get => showIndicator;
            set => showIndicator = value;
        }

        public bool UseSmallGrid
        {
            get => useSmallGrid;
            set => useSmallGrid = value;
        }

        public Vector3 CurrentSnapPosition => currentSnapPosition;

        private void Start()
        {
            mainCamera = UnityEngine.Camera.main;

            if (indicatorMaterial == null)
            {
                indicatorMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                indicatorMaterial.hideFlags = HideFlags.HideAndDontSave;
                indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                indicatorMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                indicatorMaterial.SetInt("_ZWrite", 0);
            }
        }

        private void Update()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.BuildMode)
                return;

            if (!showIndicator)
                return;

            UpdateSnapPosition();
        }

        private void UpdateSnapPosition()
        {
            // Raycast from mouse to find ground position
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            float floorY = FloorManager.Instance?.CurrentFloorY ?? 0f;

            // Intersect with floor plane
            Plane floorPlane = new Plane(Vector3.up, new Vector3(0, floorY, 0));
            if (floorPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);

                // Snap to appropriate grid
                if (useSmallGrid)
                {
                    currentSnapPosition = GridMath.SnapToSmallGrid(hitPoint);
                }
                else
                {
                    currentSnapPosition = GridMath.SnapToMainGrid(hitPoint);
                }

                // Ensure Y is on current floor
                currentSnapPosition.y = floorY;
            }
        }

        private void OnRenderObject()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.BuildMode)
                return;

            if (!showIndicator)
                return;

            indicatorMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            DrawSnapIndicator();

            GL.PopMatrix();
        }

        private void DrawSnapIndicator()
        {
            Vector3 pos = currentSnapPosition;
            // Scale indicator to match grid size (half the grid cell)
            float gridSize = useSmallGrid ? GridConstants.SmallGridSize : GridConstants.MainGridSize;
            float size = gridSize * 0.5f;

            // Draw a small cross at snap position
            GL.Begin(GL.LINES);
            GL.Color(indicatorColor);

            // X axis line
            GL.Vertex3(pos.x - size, pos.y + 0.01f, pos.z);
            GL.Vertex3(pos.x + size, pos.y + 0.01f, pos.z);

            // Z axis line
            GL.Vertex3(pos.x, pos.y + 0.01f, pos.z - size);
            GL.Vertex3(pos.x, pos.y + 0.01f, pos.z + size);

            GL.End();

            // Draw a small square
            GL.Begin(GL.LINE_STRIP);
            GL.Color(indicatorColor);

            GL.Vertex3(pos.x - size, pos.y + 0.01f, pos.z - size);
            GL.Vertex3(pos.x + size, pos.y + 0.01f, pos.z - size);
            GL.Vertex3(pos.x + size, pos.y + 0.01f, pos.z + size);
            GL.Vertex3(pos.x - size, pos.y + 0.01f, pos.z + size);
            GL.Vertex3(pos.x - size, pos.y + 0.01f, pos.z - size);

            GL.End();
        }
    }
}
