using UnityEngine;

namespace StationeersBuildPlanner.Grid
{
    /// <summary>
    /// Grid math utilities matching Stationeers snapping behavior exactly.
    /// Source: ExtensionMethods.cs GridCenter() from decompiled game.
    /// </summary>
    public static class GridMath
    {
        /// <summary>
        /// Snap a position to main grid (2m).
        /// </summary>
        public static Vector3 SnapToMainGrid(Vector3 worldPosition)
        {
            return SnapToGrid(worldPosition, GridConstants.MainGridSize, 0f);
        }

        /// <summary>
        /// Snap a position to small grid (0.5m).
        /// </summary>
        public static Vector3 SnapToSmallGrid(Vector3 worldPosition)
        {
            return SnapToGrid(worldPosition, GridConstants.SmallGridSize, GridConstants.SmallGridOffset);
        }

        /// <summary>
        /// Core grid snapping - matches game's GridCenter() exactly.
        /// </summary>
        public static Vector3 SnapToGrid(Vector3 worldPosition, float gridSize, float offset)
        {
            float halfGrid = gridSize * 0.5f;
            float centerOffset = offset + halfGrid;

            worldPosition.x = Mathf.Round((worldPosition.x - centerOffset) / gridSize) * gridSize + centerOffset;
            worldPosition.y = Mathf.Round((worldPosition.y - centerOffset) / gridSize) * gridSize + centerOffset;
            worldPosition.z = Mathf.Round((worldPosition.z - centerOffset) / gridSize) * gridSize + centerOffset;

            return worldPosition;
        }

        /// <summary>
        /// Convert world position to Grid3 (10x integer scale).
        /// </summary>
        public static Vector3Int ToGrid3(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPosition.x * GridConstants.Grid3InverseScale),
                Mathf.RoundToInt(worldPosition.y * GridConstants.Grid3InverseScale),
                Mathf.RoundToInt(worldPosition.z * GridConstants.Grid3InverseScale)
            );
        }

        /// <summary>
        /// Convert Grid3 to world position.
        /// </summary>
        public static Vector3 FromGrid3(Vector3Int grid3)
        {
            return new Vector3(
                grid3.x * GridConstants.Grid3Scale,
                grid3.y * GridConstants.Grid3Scale,
                grid3.z * GridConstants.Grid3Scale
            );
        }

        /// <summary>
        /// Get floor index from Y position.
        /// Floor 0 is at Y=0 to Y=2, Floor 1 is Y=2 to Y=4, etc.
        /// </summary>
        public static int GetFloorIndex(float yPosition)
        {
            return Mathf.FloorToInt(yPosition / GridConstants.FloorHeight);
        }

        /// <summary>
        /// Get Y position for floor index.
        /// </summary>
        public static float GetFloorY(int floorIndex)
        {
            return floorIndex * GridConstants.FloorHeight;
        }

        /// <summary>
        /// Snap Y to floor level.
        /// </summary>
        public static float SnapToFloor(float yPosition)
        {
            int floor = GetFloorIndex(yPosition);
            return GetFloorY(floor);
        }
    }
}
