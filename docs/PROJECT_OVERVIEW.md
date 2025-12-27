# Stationeers Build Planner

A Unity-based base planner for [Stationeers](https://store.steampowered.com/app/544550/Stationeers/) that mirrors in-game construction mechanics. Allows players to plan bases outside the game using actual game assets and data.

---

## Tech Stack

| Component | Details |
|-----------|---------|
| **Engine** | Unity 6000.0.32f1 |
| **Render Pipeline** | Built-in Render Pipeline |
| **Input System** | New Input System (com.unity.inputsystem) |
| **UI Framework** | UI Toolkit (UXML/USS) |
| **Language** | C# |
| **Target Platform** | Windows (primary) |

---

## Project Locations

| Path | Description |
|------|-------------|
| `C:\Development\StationeersBuildPlanner` | Unity project root |
| `C:\Development\Stationeers Stuff\resources` | Decompiled game assets (textures, meshes, fonts) |
| `C:\Program Files (x86)\Steam\steamapps\common\Stationeers` | Game installation |

---

## Architecture

### Core Systems

#### GameStateManager
`Assets/Scripts/Core/GameStateManager.cs`

Singleton managing application states (Map Mode vs Build Mode). Fires `OnStateChanged` events that UI components subscribe to for reactivity.

```csharp
public enum GameState { MapMode, BuildMode }
public event Action<GameState> OnStateChanged;
```

#### WorldMapController
`Assets/Scripts/UI/WorldMapController.cs`

Main UI controller for the world map interface. Responsibilities:
- Planet selection and world data management
- Coordinate display and conversion
- Spawn marker rendering
- Ore overlay management
- Side panel controls (toggles, sliders, legend)
- Location selection and tooltip positioning

#### WorldDataLoader
`Assets/Scripts/World/WorldDataLoader.cs`

Static utility class that parses game data:
- Reads world XML files from `StreamingAssets/Worlds/`
- Extracts spawn points, ore regions, named regions
- Loads textures (minimaps, deep mining overlays)
- Provides ore lookup by world coordinate

#### WorldData
`Assets/Scripts/World/WorldData.cs`

Data structures:
- `WorldData` - Complete world information container
- `WorldRegistry` - Static list of available planets (6 total)
- `StartLocation` - Spawn point with position and display name
- `OreRegion` - Ore type with RGB color mapping
- `NamedRegion` - Geographic region with localization key

#### FreeCameraController
`Assets/Scripts/Camera/FreeCameraController.cs`

Jetpack-style free camera for Build Mode:
- Uses new Input System with `BuildPlannerInput.inputactions`
- WASD horizontal movement
- Q/E/Space vertical movement
- Right-click hold to look
- Scroll wheel speed adjustment
- Shift (fast) / Alt (slow) modifiers

---

### UI Structure (UI Toolkit)

Located in `Assets/UI/WorldMap/`:

| File | Purpose |
|------|---------|
| `WorldMap.uxml` | UI layout definition |
| `WorldMapTheme.uss` | Dark theme stylesheet |
| `WorldMapPanelSettings.asset` | Panel configuration |

#### UI Hierarchy

```
world-map-root
├── top-bar
│   └── planet-dropdown
├── map-container
│   ├── map-image (minimap texture)
│   ├── ore-overlay (toggleable)
│   └── spawn-markers (dynamic thumbtacks)
├── side-panel-container (collapsible)
│   ├── side-panel-tab (◀/▶ toggle)
│   └── side-panel
│       ├── Layer toggles (Ore, Spawns, POIs, Labels)
│       ├── Opacity slider
│       ├── Ore legend (dynamic)
│       └── Hover info
├── coordinate-overlay (top-left)
├── info-panel (floating tooltip)
└── confirm-button (bottom-right)
```

#### Styling

Dark theme with NASA/modern aesthetic:
- Primary background: `rgb(18, 18, 18)`
- Accent color: `rgb(255, 140, 50)` (orange)
- Panel backgrounds: `rgba(24, 24, 24, 0.95)`

---

### Game Asset Integration

#### Symlinked Assets
Windows junctions linking to extracted game files:
- `Assets/GameAssets/Meshes` → GLB meshes
- `Assets/GameAssets/Textures` → PNG textures

#### Runtime Data
Loaded directly from game installation at runtime:
- World XMLs: `StreamingAssets/Worlds/{Planet}/{Planet}.xml`
- Minimaps: PNG textures per world
- Deep mining textures: Color-coded ore region maps

---

## Coordinate System

| Property | Value |
|----------|-------|
| World size | 4096 x 4096 units |
| Coordinate range | -2048 to +2048 |
| Origin | Center of map (0, 0) |
| Y-axis | Flipped between screen and game |

### Coordinate Conversion

```csharp
// Game to Screen
float normalizedX = (gamePos.x / WorldSize) + 0.5f;
float normalizedY = 0.5f - (gamePos.y / WorldSize); // Flip Y

// Screen to Game
float gameX = (normalizedX - 0.5f) * WorldSize;
float gameY = (0.5f - normalizedY) * WorldSize;
```

---

## Current Features

### World Map (Phase 1)

- [x] Planet selection (6 worlds: Lunar, Europa, Mars, Mimas, Venus, Vulcan)
- [x] Interactive minimap with proper aspect ratio handling
- [x] Coordinate display (game coordinates)
- [x] Click-to-select location with floating tooltip
- [x] Spawn point markers (thumbtacks with hover labels)
- [x] Ore overlay with adjustable opacity (10%-100%)
- [x] Ore legend with color swatches
- [x] Hover info showing ore type under cursor
- [x] Collapsible side panel with layer toggles
- [x] M key toggles Map Mode / Build Mode
- [x] Game state management with event system

### Camera System

- [x] Free camera controller (jetpack-style)
- [x] New Input System integration
- [x] Speed modifiers and scroll adjustment

---

## Planned Features

### Terrain System (Phase 3 - Next Priority)
- [ ] Parse terrain heightmap data from game files (`Terrain/*.dat`)
- [ ] Generate Unity Terrain or mesh from heightmap chunks
- [ ] Apply macro textures (`macro_diffuse.png`, `macro_normal.png`)
- [ ] 1:1 accuracy with in-game terrain at selected location
- [ ] "Needs mining" warnings when placing below terrain level
- [ ] Terrain visibility toggle in Build Mode
- [ ] Integrate with floor system (terrain hidden on negative floors)

### Build Mode
- [ ] Structure placement system
- [ ] Rotation matching game controls (Ins/Del, Home/End, PgUp/PgDn, C)
- [ ] Frame/slot system for modular construction
- [x] Grid snapping (2m main grid, 0.5m small grid)
- [x] Floor navigation (Sims-style vertical layers)
- [x] Grid visualization with toggles
- [x] Snap indicator

### Additional Layers
- [ ] POI layer (points of interest)
- [ ] Named regions overlay

### Construction
- [ ] Structure assembly from PrefabHierarchyObject meshes
- [ ] Pipe/cable connections
- [ ] Structural integrity validation

### Persistence
- [ ] Save/load base designs
- [ ] Export functionality

---

## Available Worlds

| Display Name | Folder | XML File |
|--------------|--------|----------|
| Lunar | Lunar | Lunar.xml |
| Europa | Europa | Europa.xml |
| Mars | Mars2 | Mars2.xml |
| Mimas | Mimas | MimasHerschel.xml |
| Venus | Venus | Venus.xml |
| Vulcan | Vulcan | Vulcan.xml |

---

## Development Guidelines

1. **Mirror game mechanics** - Research actual game behavior before implementing
2. **Use actual game data** - Extract from decompiled files, never guess
3. **Test against live game** - Validate coordinate systems and placements
4. **Quality over speed** - Get it right through diligence, not luck
5. **No AI attribution in commits** - Standard developer commits only

---

## Key Reference Documents

| Document | Location |
|----------|----------|
| Game data research | `docs/game-data-reference.md` |
| World map UI spec | `docs/plans/2025-12-26-world-map-ui-design.md` |
| Project memory | `CLAUDE.md` (project root) |

---

## Editor Tools

Custom Unity editor menus under `Stationeers >`:
- **Setup Test Scene** - Creates basic test environment
- **Texture Assigner** - Assigns textures to imported meshes
- **Fix Dark Materials** - Corrects material rendering issues

---

## Input Bindings

### Map Mode
| Input | Action |
|-------|--------|
| M | Toggle Map/Build Mode |
| Mouse Move | Update coordinates, hover info |
| Left Click | Select location |

### Build Mode (Camera)
| Input | Action |
|-------|--------|
| WASD | Horizontal movement |
| Q/E/Space | Vertical movement |
| Right Click (hold) | Look around |
| Scroll Wheel | Adjust speed |
| Shift | Fast movement |
| Alt | Slow movement |

---

## Getting Started

1. Open project in Unity 6000.0.32f1
2. Open `Assets/Scenes/SampleScene.unity`
3. Ensure game is installed at default Steam location
4. Enter Play mode
5. Use side panel to toggle layers, M to switch modes

---

## Dependencies

- Unity 6000.0.32f1+
- Input System package
- Stationeers game installation (for runtime data)
