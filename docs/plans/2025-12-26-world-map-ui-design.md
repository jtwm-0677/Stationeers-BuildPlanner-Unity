# World Map UI Design

**Date:** 2025-12-26
**Status:** Approved

## Overview

Full-screen 2D world map UI for selecting a base location before entering build mode. Users select a planet, view ore regions, pick coordinates, and transition via an animated "planetfall" sequence.

## Key Decisions

| Decision | Choice |
|----------|--------|
| Visual approach | Full-screen 2D map overlay |
| Location selection | Click anywhere + show spawn points as markers |
| Ore display | Toggle overlay (button to show/hide) |
| Planet selection | Dropdown menu |
| Confirm flow | Location summary panel, then smooth planetfall transition |
| UI system | UI Toolkit (UXML + USS) |
| Scene structure | Single scene with overlay |
| Styling | Dark grayscale, NASA/modern aesthetic (no blues/purples) |

## Architecture

### Scene Structure

Single scene with two states:
- **Map Mode** - World Map UI visible, 3D world ready in background
- **Build Mode** - UI hidden, 3D build environment active

`GameStateManager` controls transitions between states.

### File Structure

```
Assets/
├── Scripts/
│   ├── UI/
│   │   ├── WorldMapController.cs   - Main UI logic
│   │   ├── MapRenderer.cs          - Texture display, pan/zoom, coordinates
│   │   ├── SpawnPointMarker.cs     - Spawn location indicators
│   │   ├── LocationInfoPanel.cs    - Selected location details
│   │   ├── PlanetSelector.cs       - Planet dropdown
│   │   └── OreDetector.cs          - Samples ore texture at position
│   └── Core/
│       ├── GameStateManager.cs     - Map/Build state management
│       └── PlanetfallTransition.cs - Camera animation + re-entry effects
└── UI/
    └── WorldMap/
        ├── WorldMap.uxml           - Layout structure
        ├── WorldMap.uss            - Dark grayscale styling
        └── Icons/                  - Spawn markers, pins, etc.
```

### Data Flow

1. `WorldMapController` loads world data via `WorldDataLoader` on startup
2. `PlanetSelector` triggers world changes, updating `MapRenderer`
3. Click on map -> calculate game coordinates -> update `LocationInfoPanel`
4. Confirm button -> `GameStateManager` triggers planetfall -> `PlanetfallTransition` animates

## UI Layout

```
+-------------------------------------------------------------+
|  [Planet: Lunar v]                    [Toggle Ore Overlay]  |  <- Top bar
+-------------------------------------------------------------+
|                                                             |
|                                                             |
|                        MAP AREA                             |
|                     (pannable/zoomable)                     |
|                                                             |
|                          *  <- selected pin                 |
|                       ^ ^    <- spawn markers               |
|                                                             |
+-------------------------------------------------------------+
|  Location Info Panel                                        |
|  +--------------------------------------------------------+ |
|  |  Coordinates: X: -845  Y: 1203                         | |
|  |  Nearest Spawn: Canyon Overlook (342m)                 | |
|  |  Ore Access: Iron, Coal, Gold/Silver                   | |
|  |                                                        | |
|  |                            [Confirm Location]          | |
|  +--------------------------------------------------------+ |
+-------------------------------------------------------------+
```

## Map Interaction

### Coordinate Conversion

Mouse position to game coordinates:
```csharp
float gameX = (mouseX / mapWidth - 0.5f) * worldData.WorldSize;   // -2048 to +2048
float gameY = (mouseY / mapHeight - 0.5f) * worldData.WorldSize;  // -2048 to +2048
```

### Spawn Point Markers

- Small icon at each `StartLocation` position
- Hover tooltip shows spawn name
- Click selects that location directly

### Click-to-Select

- Click anywhere places a "selected location" pin
- Updates coordinate display and `LocationInfoPanel`
- Enables "Confirm Location" button
- Only one location selected at a time

## Planetfall Transition

When "Confirm Location" is clicked:

1. **"Initializing Planetfall..."** text appears
2. Camera begins descent from orthographic top-down view
3. **Re-entry heat effect:**
   - Orange/red gradient vignette at screen edges
   - Subtle screen shake
   - Possible particle trails
4. Heat intensifies mid-descent, fades as "atmosphere clears"
5. Camera tilts from top-down to FreeCameraController's default angle
6. UI fades out, build mode activates

Duration: ~2 seconds with ease-in-out.

### Returning to Map

- "Open Map" button or `M` key reopens World Map UI
- Camera reverses animation (rises, flattens to orthographic)
- UI fades in, selected location persists

## Styling

**Theme:** Dark grayscale with NASA/modern aesthetic

- Matte blacks, charcoal grays
- White/light gray text
- Minimal accent colors (possibly subtle orange for interactive elements)
- Clean, technical, utilitarian
- Semi-transparent panels

## Dependencies

- **Post Processing package** - For re-entry heat vignette effect
- **WorldDataLoader** - Already implemented
- **FreeCameraController** - Already implemented

## Implementation Phases

### Phase 1: Core Map Display
- UI Toolkit setup with dark theme
- Planet dropdown, map texture rendering
- Click-to-select with coordinate display

### Phase 2: Ore & Spawn Points
- Toggle ore overlay
- Spawn point markers with tooltips
- Ore detection at selected position

### Phase 3: Location Panel & Confirmation
- Info panel with coordinates, nearby spawns, ore access
- Confirm button triggers planetfall

### Phase 4: Planetfall Transition
- Camera animation (descent + tilt)
- Re-entry heat effect (post-processing)
- Fade to build mode

### Phase 5: Polish
- Pan/zoom controls for map
- Return-to-map functionality
- Easter eggs and ambient touches

## Future Polish Ideas

- **Ambient sounds** - Space station hum in map mode, wind during planetfall
- **Planet rotation** - Minimap slowly rotates if left idle
- **Ship flyby** - Trader ship prefab occasionally crosses the map
- **Hidden locations** - Clickable easter eggs on certain coordinates
- **Console boot sequence** - Retro terminal startup on launch
