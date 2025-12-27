# Grid System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a 1:1 accurate grid system matching Stationeers' construction mechanics with dual-scale grids, vertical floor layers, and visual overlays.

**Architecture:** Core grid math in static utility class (matching game's Grid3/RocketGrid patterns). Floor manager handles vertical layers. Visual grid renders via Unity line renderer or procedural mesh. UI controls in Build Mode panel.

**Tech Stack:** Unity 6000.0.32f1, C#, UI Toolkit for controls

---

## Reference: Game Grid Constants

From decompiled source - these values are non-negotiable:

```csharp
// RocketGrid.cs
const float GridSize = 2f;           // Main grid
const float SmallGridSize = 0.5f;    // Pipes/cables
const float SmallGridOffset = 0.25f; // Small grid centering

// Grid3.cs
// 1 Grid3 unit = 0.1m world space
// Cardinal directions = 20 Grid3 units = 2m

// Snapping formula (ExtensionMethods.cs)
float num = gridSquareSize * 0.5f;
float num2 = offset + num;
snapped = Round((pos - num2) / gridSquareSize) * gridSquareSize + num2;
```

---

## Task 1: Grid Constants and Math Utilities

**Files:**
- Create: `Assets/Scripts/Grid/GridConstants.cs`
- Create: `Assets/Scripts/Grid/GridMath.cs`
- Create: `Assets/Scripts/Grid.meta`

**Step 1: Create GridConstants.cs**

```csharp
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
```

**Step 2: Create GridMath.cs**

```csharp
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
```

**Step 3: Verify compilation**

Run: Unity recompile (MCP or Editor)
Expected: No errors

**Step 4: Commit**

```bash
git add Assets/Scripts/Grid/
git commit -m "Add grid constants and math utilities matching game values"
```

---

## Task 2: Floor Manager

**Files:**
- Create: `Assets/Scripts/Grid/FloorManager.cs`

**Step 1: Create FloorManager.cs**

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

namespace StationeersBuildPlanner.Grid
{
    /// <summary>
    /// Manages vertical floor layers for Sims-style floor navigation.
    /// </summary>
    public class FloorManager : MonoBehaviour
    {
        public static FloorManager Instance { get; private set; }

        [Header("Floor Settings")]
        [SerializeField] private int minFloor = -5;
        [SerializeField] private int maxFloor = 10;
        [SerializeField] private int currentFloor = 0;

        // Floor visibility state
        private Dictionary<int, bool> floorVisibility = new Dictionary<int, bool>();

        // Events
        public event Action<int> OnFloorChanged;
        public event Action<int, bool> OnFloorVisibilityChanged;

        public int CurrentFloor => currentFloor;
        public int MinFloor => minFloor;
        public int MaxFloor => maxFloor;

        /// <summary>
        /// World Y position of current floor.
        /// </summary>
        public float CurrentFloorY => GridMath.GetFloorY(currentFloor);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize all floors as visible
            for (int i = minFloor; i <= maxFloor; i++)
            {
                floorVisibility[i] = true;
            }
        }

        /// <summary>
        /// Move to a specific floor.
        /// </summary>
        public void SetFloor(int floor)
        {
            floor = Mathf.Clamp(floor, minFloor, maxFloor);
            if (floor != currentFloor)
            {
                currentFloor = floor;
                OnFloorChanged?.Invoke(currentFloor);
                Debug.Log($"[FloorManager] Changed to floor {currentFloor} (Y={CurrentFloorY})");
            }
        }

        /// <summary>
        /// Move up one floor.
        /// </summary>
        public void FloorUp()
        {
            SetFloor(currentFloor + 1);
        }

        /// <summary>
        /// Move down one floor.
        /// </summary>
        public void FloorDown()
        {
            SetFloor(currentFloor - 1);
        }

        /// <summary>
        /// Set visibility for a specific floor.
        /// </summary>
        public void SetFloorVisibility(int floor, bool visible)
        {
            if (floor < minFloor || floor > maxFloor) return;

            if (floorVisibility[floor] != visible)
            {
                floorVisibility[floor] = visible;
                OnFloorVisibilityChanged?.Invoke(floor, visible);
            }
        }

        /// <summary>
        /// Check if a floor is visible.
        /// </summary>
        public bool IsFloorVisible(int floor)
        {
            return floorVisibility.TryGetValue(floor, out bool visible) && visible;
        }

        /// <summary>
        /// Get floor index from world Y position.
        /// </summary>
        public int GetFloorFromY(float y)
        {
            return GridMath.GetFloorIndex(y);
        }
    }
}
```

**Step 2: Verify compilation**

Run: Unity recompile
Expected: No errors

**Step 3: Commit**

```bash
git add Assets/Scripts/Grid/FloorManager.cs
git commit -m "Add floor manager for vertical layer navigation"
```

---

## Task 3: Grid Visualizer (Lines)

**Files:**
- Create: `Assets/Scripts/Grid/GridVisualizer.cs`
- Create: `Assets/Materials/GridLineMaterial.mat`

**Step 1: Create GridVisualizer.cs**

```csharp
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
```

**Step 2: Verify compilation**

Run: Unity recompile
Expected: No errors

**Step 3: Commit**

```bash
git add Assets/Scripts/Grid/GridVisualizer.cs
git commit -m "Add grid visualizer with GL line rendering"
```

---

## Task 4: Snap Indicator

**Files:**
- Create: `Assets/Scripts/Grid/SnapIndicator.cs`

**Step 1: Create SnapIndicator.cs**

```csharp
using UnityEngine;
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
        [SerializeField] private float indicatorSize = 0.2f;
        [SerializeField] private Color indicatorColor = new Color(1f, 0.5f, 0f, 0.8f);

        [Header("References")]
        [SerializeField] private Material indicatorMaterial;

        private Vector3 currentSnapPosition;
        private Camera mainCamera;

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
            mainCamera = Camera.main;

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
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
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
            float size = indicatorSize;

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
```

**Step 2: Verify compilation**

Run: Unity recompile
Expected: No errors

**Step 3: Commit**

```bash
git add Assets/Scripts/Grid/SnapIndicator.cs
git commit -m "Add snap indicator for grid position feedback"
```

---

## Task 5: Build Mode UI Panel

**Files:**
- Create: `Assets/UI/BuildMode/BuildModePanel.uxml`
- Create: `Assets/UI/BuildMode/BuildModeTheme.uss`
- Create: `Assets/UI/BuildMode.meta`

**Step 1: Create BuildModePanel.uxml**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xsi="http://www.w3.org/2001/XMLSchema-instance"
         engine="UnityEngine.UIElements"
         noNamespaceSchemaLocation="../../../../UIElementsSchema/UIElements.xsd"
         editor-extension-mode="False">

    <Style src="BuildModeTheme.uss" />

    <ui:VisualElement name="build-mode-root" class="build-mode-root">

        <!-- Floor Navigation (Top) -->
        <ui:VisualElement name="floor-nav" class="floor-nav">
            <ui:Button name="floor-up" text="▲" class="floor-button" />
            <ui:Label name="floor-label" text="Floor 0" class="floor-label" />
            <ui:Button name="floor-down" text="▼" class="floor-button" />
        </ui:VisualElement>

        <!-- Grid Controls (Side Panel) -->
        <ui:VisualElement name="grid-panel" class="grid-panel">
            <ui:Label text="GRID" class="panel-header" />

            <ui:Toggle name="toggle-main-grid" label="Main Grid (2m)" value="true" class="grid-toggle" />
            <ui:Toggle name="toggle-small-grid" label="Small Grid (0.5m)" class="grid-toggle" />
            <ui:Toggle name="toggle-snap-indicator" label="Snap Indicator" value="true" class="grid-toggle" />

            <ui:Label text="SNAP MODE" class="panel-header" />
            <ui:RadioButtonGroup name="snap-mode" value="0" class="snap-mode-group">
                <ui:RadioButton label="Main Grid" value="0" />
                <ui:RadioButton label="Small Grid" value="1" />
            </ui:RadioButtonGroup>

            <ui:Label text="FLOOR VISIBILITY" class="panel-header" />
            <ui:VisualElement name="floor-visibility-list" class="floor-visibility-list" />
        </ui:VisualElement>

        <!-- Coordinates Display (Bottom Left) -->
        <ui:VisualElement name="coords-display" class="coords-display">
            <ui:Label name="coords-world" text="World: (0, 0, 0)" class="coords-label" />
            <ui:Label name="coords-grid" text="Grid: (0, 0, 0)" class="coords-label" />
            <ui:Label name="coords-floor" text="Floor: 0" class="coords-label" />
        </ui:VisualElement>

    </ui:VisualElement>
</ui:UXML>
```

**Step 2: Create BuildModeTheme.uss**

```css
/* Build Mode Dark Theme - Matches World Map styling */

:root {
    --color-bg-primary: rgb(18, 18, 18);
    --color-bg-secondary: rgb(28, 28, 28);
    --color-bg-panel: rgba(24, 24, 24, 0.95);
    --color-bg-hover: rgb(38, 38, 38);

    --color-text-primary: rgb(240, 240, 240);
    --color-text-secondary: rgb(180, 180, 180);
    --color-text-muted: rgb(120, 120, 120);

    --color-accent: rgb(255, 140, 50);
    --color-accent-hover: rgb(255, 160, 80);

    --color-border: rgb(60, 60, 60);
}

.build-mode-root {
    flex-grow: 1;
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

/* Floor Navigation */
.floor-nav {
    position: absolute;
    top: 20px;
    left: 50%;
    translate: -50% 0;
    flex-direction: row;
    align-items: center;
    padding: 8px 16px;
    background-color: var(--color-bg-panel);
    border-radius: 6px;
    border-width: 1px;
    border-color: var(--color-border);
}

.floor-button {
    width: 32px;
    height: 32px;
    background-color: var(--color-bg-secondary);
    border-width: 1px;
    border-color: var(--color-border);
    border-radius: 4px;
    color: var(--color-text-primary);
    font-size: 16px;
}

.floor-button:hover {
    background-color: var(--color-bg-hover);
    border-color: var(--color-accent);
}

.floor-label {
    min-width: 80px;
    margin: 0 12px;
    color: var(--color-text-primary);
    font-size: 14px;
    -unity-text-align: middle-center;
    -unity-font-style: bold;
}

/* Grid Panel */
.grid-panel {
    position: absolute;
    right: 0;
    top: 0;
    bottom: 0;
    width: 200px;
    padding: 12px;
    background-color: var(--color-bg-panel);
    border-left-width: 1px;
    border-color: var(--color-border);
}

.panel-header {
    font-size: 11px;
    color: var(--color-text-muted);
    -unity-font-style: bold;
    margin-top: 12px;
    margin-bottom: 8px;
    padding-bottom: 4px;
    border-bottom-width: 1px;
    border-bottom-color: var(--color-border);
}

.panel-header:first-child {
    margin-top: 0;
}

.grid-toggle {
    margin-bottom: 6px;
}

.grid-toggle Label {
    color: var(--color-text-primary);
    font-size: 12px;
}

.snap-mode-group {
    margin-left: 8px;
}

.snap-mode-group RadioButton {
    margin-bottom: 4px;
}

.snap-mode-group RadioButton Label {
    color: var(--color-text-secondary);
    font-size: 11px;
}

.floor-visibility-list {
    max-height: 200px;
    overflow: scroll;
}

/* Coordinates Display */
.coords-display {
    position: absolute;
    left: 16px;
    bottom: 16px;
    padding: 8px 12px;
    background-color: var(--color-bg-panel);
    border-radius: 4px;
    border-width: 1px;
    border-color: var(--color-border);
}

.coords-label {
    font-size: 11px;
    color: var(--color-text-secondary);
    margin-bottom: 2px;
}
```

**Step 3: Verify files created**

Run: Check files exist in Unity
Expected: UXML and USS appear in Project window

**Step 4: Commit**

```bash
git add Assets/UI/BuildMode/
git commit -m "Add Build Mode UI panel with grid controls"
```

---

## Task 6: Build Mode Controller

**Files:**
- Create: `Assets/Scripts/UI/BuildModeController.cs`

**Step 1: Create BuildModeController.cs**

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using StationeersBuildPlanner.Core;
using StationeersBuildPlanner.Grid;

namespace StationeersBuildPlanner.UI
{
    /// <summary>
    /// Controls the Build Mode UI panel and grid settings.
    /// </summary>
    public class BuildModeController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Grid References")]
        [SerializeField] private GridVisualizer gridVisualizer;
        [SerializeField] private SnapIndicator snapIndicator;

        // UI Elements
        private VisualElement root;
        private Button floorUpButton;
        private Button floorDownButton;
        private Label floorLabel;
        private Toggle toggleMainGrid;
        private Toggle toggleSmallGrid;
        private Toggle toggleSnapIndicator;
        private RadioButtonGroup snapModeGroup;
        private Label coordsWorld;
        private Label coordsGrid;
        private Label coordsFloor;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
                OnGameStateChanged(GameStateManager.Instance.CurrentState);
            }

            if (FloorManager.Instance != null)
            {
                FloorManager.Instance.OnFloorChanged += OnFloorChanged;
            }
        }

        private void OnEnable()
        {
            InitializeUI();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;

            if (FloorManager.Instance != null)
                FloorManager.Instance.OnFloorChanged -= OnFloorChanged;
        }

        private void Update()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.BuildMode)
                return;

            HandleInput();
            UpdateCoordinatesDisplay();
        }

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;

            // Floor navigation
            floorUpButton = root.Q<Button>("floor-up");
            floorDownButton = root.Q<Button>("floor-down");
            floorLabel = root.Q<Label>("floor-label");

            // Grid toggles
            toggleMainGrid = root.Q<Toggle>("toggle-main-grid");
            toggleSmallGrid = root.Q<Toggle>("toggle-small-grid");
            toggleSnapIndicator = root.Q<Toggle>("toggle-snap-indicator");
            snapModeGroup = root.Q<RadioButtonGroup>("snap-mode");

            // Coordinates
            coordsWorld = root.Q<Label>("coords-world");
            coordsGrid = root.Q<Label>("coords-grid");
            coordsFloor = root.Q<Label>("coords-floor");

            RegisterCallbacks();
            SyncUIState();
        }

        private void RegisterCallbacks()
        {
            if (floorUpButton != null)
                floorUpButton.clicked += OnFloorUpClicked;

            if (floorDownButton != null)
                floorDownButton.clicked += OnFloorDownClicked;

            if (toggleMainGrid != null)
                toggleMainGrid.RegisterValueChangedCallback(OnMainGridToggled);

            if (toggleSmallGrid != null)
                toggleSmallGrid.RegisterValueChangedCallback(OnSmallGridToggled);

            if (toggleSnapIndicator != null)
                toggleSnapIndicator.RegisterValueChangedCallback(OnSnapIndicatorToggled);

            if (snapModeGroup != null)
                snapModeGroup.RegisterValueChangedCallback(OnSnapModeChanged);
        }

        private void UnregisterCallbacks()
        {
            if (floorUpButton != null)
                floorUpButton.clicked -= OnFloorUpClicked;

            if (floorDownButton != null)
                floorDownButton.clicked -= OnFloorDownClicked;

            if (toggleMainGrid != null)
                toggleMainGrid.UnregisterValueChangedCallback(OnMainGridToggled);

            if (toggleSmallGrid != null)
                toggleSmallGrid.UnregisterValueChangedCallback(OnSmallGridToggled);

            if (toggleSnapIndicator != null)
                toggleSnapIndicator.UnregisterValueChangedCallback(OnSnapIndicatorToggled);

            if (snapModeGroup != null)
                snapModeGroup.UnregisterValueChangedCallback(OnSnapModeChanged);
        }

        private void SyncUIState()
        {
            if (gridVisualizer != null)
            {
                if (toggleMainGrid != null)
                    toggleMainGrid.value = gridVisualizer.ShowMainGrid;
                if (toggleSmallGrid != null)
                    toggleSmallGrid.value = gridVisualizer.ShowSmallGrid;
            }

            if (snapIndicator != null && toggleSnapIndicator != null)
                toggleSnapIndicator.value = snapIndicator.ShowIndicator;

            UpdateFloorLabel();
        }

        private void HandleInput()
        {
            // Page Up / Page Down for floor navigation
            if (Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                FloorManager.Instance?.FloorUp();
            }
            else if (Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                FloorManager.Instance?.FloorDown();
            }

            // G toggles main grid
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                if (gridVisualizer != null)
                {
                    gridVisualizer.ShowMainGrid = !gridVisualizer.ShowMainGrid;
                    if (toggleMainGrid != null)
                        toggleMainGrid.value = gridVisualizer.ShowMainGrid;
                }
            }

            // H toggles small grid
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (gridVisualizer != null)
                {
                    gridVisualizer.ShowSmallGrid = !gridVisualizer.ShowSmallGrid;
                    if (toggleSmallGrid != null)
                        toggleSmallGrid.value = gridVisualizer.ShowSmallGrid;
                }
            }
        }

        private void UpdateCoordinatesDisplay()
        {
            if (snapIndicator == null) return;

            Vector3 worldPos = snapIndicator.CurrentSnapPosition;
            Vector3Int gridPos = GridMath.ToGrid3(worldPos);
            int floor = GridMath.GetFloorIndex(worldPos.y);

            if (coordsWorld != null)
                coordsWorld.text = $"World: ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1})";

            if (coordsGrid != null)
                coordsGrid.text = $"Grid3: ({gridPos.x}, {gridPos.y}, {gridPos.z})";

            if (coordsFloor != null)
                coordsFloor.text = $"Floor: {floor}";
        }

        private void UpdateFloorLabel()
        {
            if (floorLabel != null && FloorManager.Instance != null)
            {
                floorLabel.text = $"Floor {FloorManager.Instance.CurrentFloor}";
            }
        }

        // Event Handlers

        private void OnGameStateChanged(GameState state)
        {
            if (root != null)
            {
                root.style.display = (state == GameState.BuildMode)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void OnFloorChanged(int floor)
        {
            UpdateFloorLabel();
        }

        private void OnFloorUpClicked() => FloorManager.Instance?.FloorUp();
        private void OnFloorDownClicked() => FloorManager.Instance?.FloorDown();

        private void OnMainGridToggled(ChangeEvent<bool> evt)
        {
            if (gridVisualizer != null)
                gridVisualizer.ShowMainGrid = evt.newValue;
        }

        private void OnSmallGridToggled(ChangeEvent<bool> evt)
        {
            if (gridVisualizer != null)
                gridVisualizer.ShowSmallGrid = evt.newValue;
        }

        private void OnSnapIndicatorToggled(ChangeEvent<bool> evt)
        {
            if (snapIndicator != null)
                snapIndicator.ShowIndicator = evt.newValue;
        }

        private void OnSnapModeChanged(ChangeEvent<int> evt)
        {
            if (snapIndicator != null)
                snapIndicator.UseSmallGrid = (evt.newValue == 1);
        }
    }
}
```

**Step 2: Verify compilation**

Run: Unity recompile
Expected: No errors

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/BuildModeController.cs
git commit -m "Add Build Mode controller with grid and floor controls"
```

---

## Task 7: Scene Setup and Integration

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity`

**Step 1: Create scene objects via MCP or manually**

Required GameObjects:
1. **FloorManager** (empty GameObject)
   - Add component: `FloorManager`

2. **GridSystem** (empty GameObject)
   - Add component: `GridVisualizer`
   - Add component: `SnapIndicator`

3. **BuildModeUI** (empty GameObject)
   - Add component: `UIDocument`
   - Add component: `BuildModeController`
   - Set UIDocument source: `Assets/UI/BuildMode/BuildModePanel.uxml`
   - Wire BuildModeController references to GridVisualizer and SnapIndicator

**Step 2: Test in Play mode**

1. Enter Play mode
2. Press M to switch to Build Mode
3. Verify:
   - Grid lines visible on floor
   - Floor navigation works (PgUp/PgDn, buttons)
   - Grid toggles work
   - Snap indicator follows mouse
   - Coordinates update

**Step 3: Commit**

```bash
git add Assets/Scenes/SampleScene.unity
git commit -m "Integrate grid system into scene"
```

---

## Task 8: Documentation Update

**Files:**
- Modify: `docs/PROJECT_OVERVIEW.md`

**Step 1: Add Grid System section**

Add to PROJECT_OVERVIEW.md after Camera System section:

```markdown
### Grid System

#### GridConstants
`Assets/Scripts/Grid/GridConstants.cs`

Static constants matching Stationeers game values:
- Main grid: 2.0m
- Small grid: 0.5m (pipes, cables)
- Floor height: 2.0m
- Grid3 scale: 10x (0.1m per unit)

#### GridMath
`Assets/Scripts/Grid/GridMath.cs`

Snapping and conversion utilities:
- `SnapToMainGrid(Vector3)` - Snap to 2m grid
- `SnapToSmallGrid(Vector3)` - Snap to 0.5m grid
- `ToGrid3(Vector3)` / `FromGrid3(Vector3Int)` - Grid3 conversion
- `GetFloorIndex(float)` / `GetFloorY(int)` - Floor calculations

#### FloorManager
`Assets/Scripts/Grid/FloorManager.cs`

Singleton managing vertical layers:
- Floor navigation (up/down)
- Per-floor visibility toggles
- Events: `OnFloorChanged`, `OnFloorVisibilityChanged`

#### GridVisualizer
`Assets/Scripts/Grid/GridVisualizer.cs`

Renders grid lines via GL.Lines:
- Toggle-able main/small grid
- Renders at current floor height
- Only visible in Build Mode

#### SnapIndicator
`Assets/Scripts/Grid/SnapIndicator.cs`

Visual feedback for snap position:
- Cross + square at snapped position
- Switches between main/small grid snapping
```

**Step 2: Update Input Bindings table**

Add to Build Mode inputs:

```markdown
| PgUp | Floor up |
| PgDn | Floor down |
| G | Toggle main grid |
| H | Toggle small grid |
```

**Step 3: Commit**

```bash
git add docs/PROJECT_OVERVIEW.md
git commit -m "Document grid system in project overview"
```

---

## Summary

**Files Created:**
- `Assets/Scripts/Grid/GridConstants.cs`
- `Assets/Scripts/Grid/GridMath.cs`
- `Assets/Scripts/Grid/FloorManager.cs`
- `Assets/Scripts/Grid/GridVisualizer.cs`
- `Assets/Scripts/Grid/SnapIndicator.cs`
- `Assets/UI/BuildMode/BuildModePanel.uxml`
- `Assets/UI/BuildMode/BuildModeTheme.uss`
- `Assets/Scripts/UI/BuildModeController.cs`

**Key Hotkeys:**
- M: Toggle Map/Build Mode
- PgUp/PgDn: Floor navigation
- G: Toggle main grid (2m)
- H: Toggle small grid (0.5m)

**Grid Values (from game source):**
- Main: 2.0m
- Small: 0.5m
- Floor: 2.0m
- Origin: (0,0,0) with 1m center offset
