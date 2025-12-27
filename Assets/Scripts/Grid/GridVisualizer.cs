using UnityEngine;
using StationeersBuildPlanner.Core;

namespace StationeersBuildPlanner.Grid
{
    /// <summary>
    /// Renders visual grid lines for main and small grids.
    /// Uses GL.Lines for efficient rendering.
    /// </summary>
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private bool showMainGrid = true;
        [SerializeField] private bool showSmallGrid = false;
        [SerializeField] private float gridExtent = 50f; // How far grid extends from center

        [Header("Colors")]
        [SerializeField] private Color mainGridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color smallGridColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private Color floorHighlightColor = new Color(1f, 0.5f, 0f, 0.3f);

        [Header("References")]
        [SerializeField] private Material gridMaterial;

        private Camera mainCamera;

        public bool ShowMainGrid
        {
            get => showMainGrid;
            set => showMainGrid = value;
        }

        public bool ShowSmallGrid
        {
            get => showSmallGrid;
            set => showSmallGrid = value;
        }

        private void Start()
        {
            mainCamera = Camera.main;

            // Create grid material if not assigned
            if (gridMaterial == null)
            {
                gridMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                gridMaterial.hideFlags = HideFlags.HideAndDontSave;
                gridMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                gridMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                gridMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                gridMaterial.SetInt("_ZWrite", 0);
            }

            // Subscribe to game state changes
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            // Only show grid in Build Mode
            enabled = (state == GameState.BuildMode);
        }

        private void OnRenderObject()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.BuildMode)
                return;

            if (!showMainGrid && !showSmallGrid)
                return;

            float floorY = FloorManager.Instance?.CurrentFloorY ?? 0f;

            gridMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);

            if (showSmallGrid)
            {
                DrawGrid(GridConstants.SmallGridSize, smallGridColor, floorY);
            }

            if (showMainGrid)
            {
                DrawGrid(GridConstants.MainGridSize, mainGridColor, floorY);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void DrawGrid(float gridSize, Color color, float yPosition)
        {
            GL.Color(color);

            // Calculate grid bounds (snap to grid)
            float halfExtent = gridExtent;
            float startX = Mathf.Floor(-halfExtent / gridSize) * gridSize;
            float endX = Mathf.Ceil(halfExtent / gridSize) * gridSize;
            float startZ = Mathf.Floor(-halfExtent / gridSize) * gridSize;
            float endZ = Mathf.Ceil(halfExtent / gridSize) * gridSize;

            // Draw lines parallel to Z axis
            for (float x = startX; x <= endX; x += gridSize)
            {
                GL.Vertex3(x, yPosition, startZ);
                GL.Vertex3(x, yPosition, endZ);
            }

            // Draw lines parallel to X axis
            for (float z = startZ; z <= endZ; z += gridSize)
            {
                GL.Vertex3(startX, yPosition, z);
                GL.Vertex3(endX, yPosition, z);
            }
        }
    }
}
