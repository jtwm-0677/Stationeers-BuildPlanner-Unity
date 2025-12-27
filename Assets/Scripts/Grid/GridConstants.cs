namespace StationeersBuildPlanner.Grid
{
    /// <summary>
    /// Grid constants matching Stationeers game values exactly.
    /// Source: RocketGrid.cs, Grid3.cs from decompiled game.
    /// </summary>
    public static class GridConstants
    {
        // Main grid (structures, frames)
        public const float MainGridSize = 2f;

        // Small grid (pipes, cables, small devices)
        public const float SmallGridSize = 0.5f;
        public const float SmallGridOffset = 0.25f;

        // Floor height (same as main grid)
        public const float FloorHeight = 2f;

        // Grid3 scale factor (1 Grid3 unit = 0.1m)
        public const float Grid3Scale = 0.1f;
        public const float Grid3InverseScale = 10f;

        // World extents (±8192m)
        public const float WorldExtent = 8192f;

        // Small grids per main grid (2m / 0.5m = 4)
        public const int SmallGridsPerMain = 4;
    }
}
