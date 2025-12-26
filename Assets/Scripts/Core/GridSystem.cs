using UnityEngine;

namespace StationeersBuildPlanner.Core
{
    /// <summary>
    /// Grid system matching Stationeers measurements.
    /// Main grid: 2.0m (20 units at 0.1m per unit)
    /// Small grid: 0.5m (5 units) for pipes/cables
    /// </summary>
    public class GridSystem : MonoBehaviour
    {
        public static GridSystem Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private float mainGridSize = 2f;      // 2m for frames
        [SerializeField] private float smallGridSize = 0.5f;   // 0.5m for pipes/cables
        [SerializeField] private int gridExtent = 50;          // Grid extends 50 cells in each direction

        [Header("Visualization")]
        [SerializeField] private bool showMainGrid = true;
        [SerializeField] private bool showSmallGrid = false;
        [SerializeField] private Color mainGridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color smallGridColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private Material gridMaterial;

        public float MainGridSize => mainGridSize;
        public float SmallGridSize => smallGridSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Snap a world position to the main grid (for frames/structures)
        /// </summary>
        public Vector3 SnapToMainGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / mainGridSize) * mainGridSize,
                Mathf.Round(position.y / mainGridSize) * mainGridSize,
                Mathf.Round(position.z / mainGridSize) * mainGridSize
            );
        }

        /// <summary>
        /// Snap a world position to the small grid (for pipes/cables)
        /// </summary>
        public Vector3 SnapToSmallGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / smallGridSize) * smallGridSize,
                Mathf.Round(position.y / smallGridSize) * smallGridSize,
                Mathf.Round(position.z / smallGridSize) * smallGridSize
            );
        }

        /// <summary>
        /// Convert a world position to main grid coordinates
        /// </summary>
        public Vector3Int WorldToGridPosition(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x / mainGridSize),
                Mathf.RoundToInt(worldPos.y / mainGridSize),
                Mathf.RoundToInt(worldPos.z / mainGridSize)
            );
        }

        /// <summary>
        /// Convert main grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector3Int gridPos)
        {
            return new Vector3(
                gridPos.x * mainGridSize,
                gridPos.y * mainGridSize,
                gridPos.z * mainGridSize
            );
        }

        private void OnDrawGizmos()
        {
            if (!showMainGrid && !showSmallGrid) return;

            float extent = gridExtent * mainGridSize;

            if (showMainGrid)
            {
                Gizmos.color = mainGridColor;
                DrawGrid(mainGridSize, extent);
            }

            if (showSmallGrid)
            {
                Gizmos.color = smallGridColor;
                DrawGrid(smallGridSize, extent);
            }
        }

        private void DrawGrid(float cellSize, float extent)
        {
            for (float x = -extent; x <= extent; x += cellSize)
            {
                Gizmos.DrawLine(new Vector3(x, 0, -extent), new Vector3(x, 0, extent));
            }
            for (float z = -extent; z <= extent; z += cellSize)
            {
                Gizmos.DrawLine(new Vector3(-extent, 0, z), new Vector3(extent, 0, z));
            }
        }
    }
}
