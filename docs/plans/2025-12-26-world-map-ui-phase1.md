# World Map UI Phase 1: Core Map Display

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a full-screen 2D world map UI with planet selection dropdown, map texture display, and click-to-select coordinate system.

**Architecture:** UI Toolkit overlay system with GameStateManager controlling map/build mode transitions. MapRenderer handles texture display and coordinate conversion. All world data sourced from existing WorldDataLoader.

**Tech Stack:** Unity 6, UI Toolkit (UXML + USS), C#, existing WorldDataLoader

---

## Prerequisites

- Unity project with WorldDataLoader working (verified)
- WorldData, WorldRegistry classes implemented (verified)
- Game worlds accessible at default Steam path (verified)

---

## Task 1: Create UI Folder Structure

**Files:**
- Create: `Assets/Scripts/UI/` (directory)
- Create: `Assets/UI/WorldMap/` (directory)

**Step 1: Create directories**

In Unity Editor: Right-click Assets > Create > Folder, name "UI"
Then: Right-click Assets/Scripts > Create > Folder, name "UI"
Then: Right-click Assets/UI > Create > Folder, name "WorldMap"

**Step 2: Verify structure**

```
Assets/
├── Scripts/
│   └── UI/                  (new)
└── UI/
    └── WorldMap/            (new)
```

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/.gitkeep Assets/UI/WorldMap/.gitkeep
git commit -m "feat: create UI folder structure for World Map"
```

---

## Task 2: Create GameStateManager Core

**Files:**
- Create: `Assets/Scripts/Core/GameStateManager.cs`

**Step 1: Write GameStateManager class**

```csharp
using UnityEngine;
using System;

namespace StationeersBuildPlanner.Core
{
    public enum GameState
    {
        MapMode,
        BuildMode
    }

    /// <summary>
    /// Manages game state transitions between Map Mode and Build Mode.
    /// Controls which UI elements and systems are active.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField] private GameState initialState = GameState.MapMode;

        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetState(initialState);
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] State changed: {oldState} -> {newState}");
            OnStateChanged?.Invoke(newState);
        }

        public void EnterMapMode()
        {
            SetState(GameState.MapMode);
        }

        public void EnterBuildMode()
        {
            SetState(GameState.BuildMode);
        }

        public void ToggleState()
        {
            SetState(CurrentState == GameState.MapMode ? GameState.BuildMode : GameState.MapMode);
        }
    }
}
```

**Step 2: Verify in Unity Editor**

- Open Unity, let scripts compile
- No compile errors expected

**Step 3: Commit**

```bash
git add Assets/Scripts/Core/GameStateManager.cs
git commit -m "feat: add GameStateManager for map/build mode transitions"
```

---

## Task 3: Create Dark Theme USS Stylesheet

**Files:**
- Create: `Assets/UI/WorldMap/WorldMapTheme.uss`

**Step 1: Write dark theme stylesheet**

```css
/* World Map Dark Theme - NASA/Modern Aesthetic */

:root {
    /* Color palette - Dark grayscale with orange accents */
    --color-bg-primary: rgb(18, 18, 18);
    --color-bg-secondary: rgb(28, 28, 28);
    --color-bg-panel: rgba(24, 24, 24, 0.95);
    --color-bg-hover: rgb(38, 38, 38);

    --color-text-primary: rgb(240, 240, 240);
    --color-text-secondary: rgb(180, 180, 180);
    --color-text-muted: rgb(120, 120, 120);

    --color-accent: rgb(255, 140, 50);
    --color-accent-hover: rgb(255, 160, 80);
    --color-accent-pressed: rgb(200, 110, 40);

    --color-border: rgb(60, 60, 60);
    --color-border-focus: rgb(100, 100, 100);
}

/* Base container */
.world-map-root {
    flex-grow: 1;
    background-color: var(--color-bg-primary);
}

/* Top bar */
.top-bar {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    height: 48px;
    padding: 8px 16px;
    background-color: var(--color-bg-secondary);
    border-bottom-width: 1px;
    border-bottom-color: var(--color-border);
}

/* Planet dropdown */
.planet-dropdown {
    min-width: 180px;
    height: 32px;
    background-color: var(--color-bg-panel);
    border-width: 1px;
    border-color: var(--color-border);
    border-radius: 4px;
    color: var(--color-text-primary);
    padding: 4px 12px;
}

.planet-dropdown:hover {
    border-color: var(--color-border-focus);
}

.planet-dropdown:focus {
    border-color: var(--color-accent);
}

/* Toggle button */
.toggle-button {
    height: 32px;
    padding: 6px 16px;
    background-color: var(--color-bg-panel);
    border-width: 1px;
    border-color: var(--color-border);
    border-radius: 4px;
    color: var(--color-text-primary);
}

.toggle-button:hover {
    background-color: var(--color-bg-hover);
    border-color: var(--color-border-focus);
}

.toggle-button:active {
    background-color: var(--color-accent-pressed);
}

.toggle-button.active {
    background-color: var(--color-accent);
    border-color: var(--color-accent);
}

/* Map area */
.map-container {
    flex-grow: 1;
    justify-content: center;
    align-items: center;
    background-color: var(--color-bg-primary);
}

.map-image {
    max-width: 100%;
    max-height: 100%;
}

/* Location info panel */
.info-panel {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 140px;
    padding: 16px 24px;
    background-color: var(--color-bg-panel);
    border-top-width: 1px;
    border-top-color: var(--color-border);
}

.info-panel-hidden {
    display: none;
}

.info-title {
    font-size: 14px;
    color: var(--color-text-secondary);
    margin-bottom: 8px;
    -unity-font-style: bold;
}

.info-coordinates {
    font-size: 16px;
    color: var(--color-text-primary);
    margin-bottom: 4px;
}

.info-detail {
    font-size: 13px;
    color: var(--color-text-secondary);
    margin-bottom: 2px;
}

/* Confirm button */
.confirm-button {
    position: absolute;
    right: 24px;
    bottom: 24px;
    width: 160px;
    height: 40px;
    background-color: var(--color-accent);
    border-width: 0;
    border-radius: 4px;
    color: var(--color-bg-primary);
    font-size: 14px;
    -unity-font-style: bold;
}

.confirm-button:hover {
    background-color: var(--color-accent-hover);
}

.confirm-button:active {
    background-color: var(--color-accent-pressed);
}

.confirm-button:disabled {
    background-color: var(--color-bg-hover);
    color: var(--color-text-muted);
}

/* Coordinate display overlay */
.coordinate-overlay {
    position: absolute;
    top: 60px;
    left: 16px;
    padding: 8px 12px;
    background-color: var(--color-bg-panel);
    border-radius: 4px;
    border-width: 1px;
    border-color: var(--color-border);
}

.coordinate-text {
    font-size: 12px;
    color: var(--color-text-secondary);
    -unity-font-style: bold;
}
```

**Step 2: Verify in Unity**

- Open Unity, check no import errors
- File should appear in Project window at Assets/UI/WorldMap/

**Step 3: Commit**

```bash
git add Assets/UI/WorldMap/WorldMapTheme.uss
git commit -m "feat: add dark theme stylesheet for World Map UI"
```

---

## Task 4: Create World Map UXML Layout

**Files:**
- Create: `Assets/UI/WorldMap/WorldMap.uxml`

**Step 1: Write UXML layout**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements"
         xsi="http://www.w3.org/2001/XMLSchema-instance"
         engine="UnityEngine.UIElements" editor="UnityEditor.UIElements"
         noNamespaceSchemaLocation="../../../../UIElementsSchema/UIElements.xsd"
         editor-extension-mode="False">

    <Style src="WorldMapTheme.uss" />

    <ui:VisualElement name="world-map-root" class="world-map-root">

        <!-- Top Bar -->
        <ui:VisualElement name="top-bar" class="top-bar">
            <ui:VisualElement style="flex-direction: row; align-items: center;">
                <ui:Label text="Planet:" style="color: rgb(180, 180, 180); margin-right: 8px;" />
                <ui:DropdownField name="planet-dropdown" class="planet-dropdown" />
            </ui:VisualElement>

            <ui:Button name="ore-toggle" text="Show Ore Overlay" class="toggle-button" />
        </ui:VisualElement>

        <!-- Map Container -->
        <ui:VisualElement name="map-container" class="map-container">
            <ui:VisualElement name="map-image" class="map-image" />
        </ui:VisualElement>

        <!-- Coordinate Overlay (shows mouse position) -->
        <ui:VisualElement name="coordinate-overlay" class="coordinate-overlay">
            <ui:Label name="coordinate-text" text="X: 0  Y: 0" class="coordinate-text" />
        </ui:VisualElement>

        <!-- Location Info Panel (shows when location selected) -->
        <ui:VisualElement name="info-panel" class="info-panel info-panel-hidden">
            <ui:Label text="SELECTED LOCATION" class="info-title" />
            <ui:Label name="info-coordinates" text="Coordinates: X: 0  Y: 0" class="info-coordinates" />
            <ui:Label name="info-nearest-spawn" text="Nearest Spawn: --" class="info-detail" />
            <ui:Label name="info-ore-access" text="Ore Access: --" class="info-detail" />

            <ui:Button name="confirm-button" text="Confirm Location" class="confirm-button" />
        </ui:VisualElement>

    </ui:VisualElement>
</ui:UXML>
```

**Step 2: Verify in Unity**

- Open Unity, check no import errors
- Double-click UXML to preview in UI Builder

**Step 3: Commit**

```bash
git add Assets/UI/WorldMap/WorldMap.uxml
git commit -m "feat: add World Map UXML layout structure"
```

---

## Task 5: Create WorldMapController Script

**Files:**
- Create: `Assets/Scripts/UI/WorldMapController.cs`

**Step 1: Write controller class**

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using StationeersBuildPlanner.Core;
using StationeersBuildPlanner.World;
using System.Collections.Generic;

namespace StationeersBuildPlanner.UI
{
    /// <summary>
    /// Main controller for the World Map UI.
    /// Handles planet selection, map display, and location selection.
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PanelSettings panelSettings;

        [Header("Configuration")]
        [SerializeField] private int defaultPlanetIndex = 2; // Mars

        // UI Elements
        private VisualElement root;
        private DropdownField planetDropdown;
        private Button oreToggle;
        private VisualElement mapContainer;
        private VisualElement mapImage;
        private Label coordinateText;
        private VisualElement infoPanel;
        private Label infoCoordinates;
        private Label infoNearestSpawn;
        private Label infoOreAccess;
        private Button confirmButton;

        // State
        private Dictionary<string, WorldData> loadedWorlds;
        private WorldData currentWorld;
        private Vector2? selectedLocation;
        private bool oreOverlayActive;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            // Subscribe to state changes
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            InitializeUI();
            LoadWorldData();
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            UnregisterCallbacks();
        }

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;

            // Get element references
            planetDropdown = root.Q<DropdownField>("planet-dropdown");
            oreToggle = root.Q<Button>("ore-toggle");
            mapContainer = root.Q<VisualElement>("map-container");
            mapImage = root.Q<VisualElement>("map-image");
            coordinateText = root.Q<Label>("coordinate-text");
            infoPanel = root.Q<VisualElement>("info-panel");
            infoCoordinates = root.Q<Label>("info-coordinates");
            infoNearestSpawn = root.Q<Label>("info-nearest-spawn");
            infoOreAccess = root.Q<Label>("info-ore-access");
            confirmButton = root.Q<Button>("confirm-button");

            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            if (planetDropdown != null)
            {
                planetDropdown.RegisterValueChangedCallback(OnPlanetChanged);
            }

            if (oreToggle != null)
            {
                oreToggle.clicked += OnOreToggleClicked;
            }

            if (mapContainer != null)
            {
                mapContainer.RegisterCallback<MouseMoveEvent>(OnMapMouseMove);
                mapContainer.RegisterCallback<ClickEvent>(OnMapClicked);
            }

            if (confirmButton != null)
            {
                confirmButton.clicked += OnConfirmClicked;
            }
        }

        private void UnregisterCallbacks()
        {
            if (planetDropdown != null)
            {
                planetDropdown.UnregisterValueChangedCallback(OnPlanetChanged);
            }

            if (oreToggle != null)
            {
                oreToggle.clicked -= OnOreToggleClicked;
            }

            if (mapContainer != null)
            {
                mapContainer.UnregisterCallback<MouseMoveEvent>(OnMapMouseMove);
                mapContainer.UnregisterCallback<ClickEvent>(OnMapClicked);
            }

            if (confirmButton != null)
            {
                confirmButton.clicked -= OnConfirmClicked;
            }
        }

        private void LoadWorldData()
        {
            if (!WorldDataLoader.Initialize())
            {
                Debug.LogError("[WorldMapController] Failed to initialize WorldDataLoader");
                return;
            }

            loadedWorlds = WorldDataLoader.LoadAllWorlds();
            Debug.Log($"[WorldMapController] Loaded {loadedWorlds.Count} worlds");

            // Populate dropdown
            var choices = new List<string>();
            foreach (var world in WorldRegistry.AvailableWorlds)
            {
                choices.Add(world.DisplayName);
            }
            planetDropdown.choices = choices;
            planetDropdown.index = defaultPlanetIndex;

            // Load initial world
            SelectWorld(defaultPlanetIndex);
        }

        private void SelectWorld(int index)
        {
            if (index < 0 || index >= WorldRegistry.AvailableWorlds.Length) return;

            var worldInfo = WorldRegistry.AvailableWorlds[index];
            if (loadedWorlds.TryGetValue(worldInfo.FolderName, out var world))
            {
                currentWorld = world;
                UpdateMapDisplay();
                ClearSelection();
                Debug.Log($"[WorldMapController] Selected world: {currentWorld.DisplayName}");
            }
        }

        private void UpdateMapDisplay()
        {
            if (currentWorld == null || mapImage == null) return;

            // Display minimap texture as background
            if (currentWorld.MinimapTexture != null)
            {
                mapImage.style.backgroundImage = new StyleBackground(currentWorld.MinimapTexture);
                mapImage.style.width = mapContainer.resolvedStyle.width;
                mapImage.style.height = mapContainer.resolvedStyle.height;
            }
        }

        private void ClearSelection()
        {
            selectedLocation = null;
            infoPanel.AddToClassList("info-panel-hidden");
            confirmButton.SetEnabled(false);
        }

        // --- Event Handlers ---

        private void OnPlanetChanged(ChangeEvent<string> evt)
        {
            int index = planetDropdown.index;
            SelectWorld(index);
        }

        private void OnOreToggleClicked()
        {
            oreOverlayActive = !oreOverlayActive;

            if (oreOverlayActive)
            {
                oreToggle.AddToClassList("active");
                oreToggle.text = "Hide Ore Overlay";
                // TODO: Show ore overlay texture
            }
            else
            {
                oreToggle.RemoveFromClassList("active");
                oreToggle.text = "Show Ore Overlay";
                // TODO: Hide ore overlay texture
            }
        }

        private void OnMapMouseMove(MouseMoveEvent evt)
        {
            if (currentWorld == null || mapContainer == null) return;

            Vector2 gameCoords = ScreenToGameCoordinates(evt.localMousePosition);
            coordinateText.text = $"X: {gameCoords.x:F0}  Y: {gameCoords.y:F0}";
        }

        private void OnMapClicked(ClickEvent evt)
        {
            if (currentWorld == null) return;

            selectedLocation = ScreenToGameCoordinates(evt.localPosition);
            UpdateLocationInfo();
        }

        private void OnConfirmClicked()
        {
            if (selectedLocation == null || GameStateManager.Instance == null) return;

            Debug.Log($"[WorldMapController] Confirmed location: {selectedLocation.Value}");
            GameStateManager.Instance.EnterBuildMode();
        }

        private void OnGameStateChanged(GameState newState)
        {
            // Show/hide the entire UI based on game state
            root.style.display = (newState == GameState.MapMode)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        // --- Coordinate Conversion ---

        private Vector2 ScreenToGameCoordinates(Vector2 localPos)
        {
            if (mapContainer == null || currentWorld == null)
                return Vector2.zero;

            float mapWidth = mapContainer.resolvedStyle.width;
            float mapHeight = mapContainer.resolvedStyle.height;

            // Convert screen position (0 to mapWidth/Height) to game coordinates
            float gameX = (localPos.x / mapWidth - 0.5f) * currentWorld.WorldSize;
            float gameY = (localPos.y / mapHeight - 0.5f) * -currentWorld.WorldSize; // Flip Y

            return new Vector2(gameX, gameY);
        }

        private void UpdateLocationInfo()
        {
            if (selectedLocation == null || currentWorld == null) return;

            Vector2 loc = selectedLocation.Value;

            // Show info panel
            infoPanel.RemoveFromClassList("info-panel-hidden");
            confirmButton.SetEnabled(true);

            // Update coordinates
            infoCoordinates.text = $"Coordinates: X: {loc.x:F0}  Y: {loc.y:F0}";

            // Find nearest spawn
            string nearestSpawn = FindNearestSpawn(loc, out float distance);
            infoNearestSpawn.text = $"Nearest Spawn: {nearestSpawn} ({distance:F0}m)";

            // Get ore at location
            var ore = WorldDataLoader.GetOreAtPosition(currentWorld, loc);
            infoOreAccess.text = ore != null
                ? $"Ore Access: {ore.OreType}"
                : "Ore Access: None detected";
        }

        private string FindNearestSpawn(Vector2 location, out float distance)
        {
            distance = float.MaxValue;
            string nearest = "--";

            foreach (var spawn in currentWorld.StartLocations)
            {
                float dist = Vector2.Distance(location, spawn.Position);
                if (dist < distance)
                {
                    distance = dist;
                    nearest = spawn.DisplayName;
                }
            }

            return nearest;
        }
    }
}
```

**Step 2: Verify in Unity**

- Open Unity, check for compile errors
- Script should compile without errors

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/WorldMapController.cs
git commit -m "feat: add WorldMapController for map interaction and state"
```

---

## Task 6: Create Scene Setup for World Map

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity` (via Unity Editor)

**Step 1: Open scene in Unity Editor**

Open: Assets/Scenes/SampleScene.unity

**Step 2: Create GameStateManager GameObject**

1. In Hierarchy: Right-click > Create Empty
2. Name it "GameStateManager"
3. Add Component: `StationeersBuildPlanner.Core.GameStateManager`
4. Set Initial State to "MapMode"

**Step 3: Create WorldMap UI GameObject**

1. In Hierarchy: Right-click > UI Toolkit > UI Document
2. Name it "WorldMapUI"
3. In UIDocument component:
   - Source Asset: Assign `Assets/UI/WorldMap/WorldMap.uxml`
   - Panel Settings: Create new PanelSettings if needed
4. Add Component: `StationeersBuildPlanner.UI.WorldMapController`
5. In WorldMapController:
   - UI Document: Auto-assigned (same GameObject)
   - Default Planet Index: 2 (Mars)

**Step 4: Create PanelSettings if needed**

1. In Project: Right-click Assets/UI > Create > UI Toolkit > Panel Settings Asset
2. Name it "WorldMapPanelSettings"
3. Configure:
   - Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080

**Step 5: Save scene and commit**

```bash
git add Assets/Scenes/SampleScene.unity Assets/UI/WorldMapPanelSettings.asset
git commit -m "feat: set up World Map UI in scene with GameStateManager"
```

---

## Task 7: Add M Key to Toggle Map Mode

**Files:**
- Modify: `Assets/Scripts/UI/WorldMapController.cs`

**Step 1: Add Update method with M key check**

Add this method to WorldMapController:

```csharp
private void Update()
{
    // M key toggles map mode
    if (Input.GetKeyDown(KeyCode.M))
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ToggleState();
        }
    }
}
```

**Step 2: Verify in Unity**

- Play the scene
- Press M to toggle between Map and Build mode
- Map should show/hide

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/WorldMapController.cs
git commit -m "feat: add M key shortcut to toggle map mode"
```

---

## Task 8: Integration Test - Full Flow

**Manual Testing Steps:**

1. **Open Unity and play scene**
   - World Map UI should appear fullscreen
   - Planet dropdown should show all 6 planets
   - Map texture (minimap) should display

2. **Test planet selection**
   - Change dropdown to each planet
   - Map should update to show that planet's minimap
   - Console should log planet changes

3. **Test coordinate display**
   - Move mouse over map
   - Coordinate overlay should update in real-time
   - Coordinates should be in game format (-2048 to +2048)

4. **Test location selection**
   - Click anywhere on map
   - Info panel should appear at bottom
   - Coordinates, nearest spawn, ore access should display

5. **Test ore toggle**
   - Click "Show Ore Overlay" button
   - Button should toggle state (visual feedback)
   - (Ore overlay display is Phase 2)

6. **Test mode toggle**
   - Click "Confirm Location"
   - UI should hide (entering Build mode)
   - Press M key
   - UI should reappear (Map mode)

7. **Test state persistence**
   - Select a location
   - Press M to enter Build mode
   - Press M to return to Map mode
   - Selected location should still be visible

**Expected Results:**
- All tests pass
- No console errors
- Smooth transitions

**Step: Commit if tests pass**

```bash
git add .
git commit -m "feat: complete World Map UI Phase 1 - core map display"
```

---

## Summary

Phase 1 deliverables:
- [x] GameStateManager for map/build mode transitions
- [x] Dark theme USS stylesheet (NASA aesthetic)
- [x] World Map UXML layout structure
- [x] WorldMapController with:
  - Planet dropdown population
  - Map texture display
  - Real-time coordinate tracking
  - Click-to-select location
  - Info panel with coordinates, nearest spawn, ore access
  - Confirm button to enter build mode
- [x] M key shortcut to toggle map mode
- [x] Integration testing

**Next Phase (Phase 2):** Ore overlay toggle, spawn point markers with tooltips, ore detection at selected position.
