# Stationeers Game Data Reference

**Last Updated:** 2025-12-26
**Game Version:** Current as of Dec 2024

This document consolidates all game data research for the Build Planner project.

---

## Data Source Locations

### Decompiled Code
`C:\Development\Stationeers Stuff\Assembly.Csharp Decompiled\Assembly-CSharp`

Key files:
- `Assets\Scripts\Objects\IConstructionKit.cs` - Kit interface
- `Assets\Scripts\Objects\Constructor.cs` - Single-variant kit
- `Assets\Scripts\Objects\MultiConstructor.cs` - Multi-variant kit
- `Assets\Scripts\Objects\Structure.cs` - Base structure class
- `Assets\Scripts\Objects\BuildState.cs` - Build stage definitions
- `Objects\Structures\Frame.cs` - Frame structure class

### Decompiled Assets
`C:\Development\Stationeers Stuff\resources`

Contains textures, fonts, models, prefabs, etc.

### Game Installation
`C:\Program Files (x86)\Steam\steamapps\common\Stationeers`

### Game Data Files (Readable XML)
`...\rocketstation_Data\StreamingAssets\`

| File | Contents |
|------|----------|
| `Language\english.xml` | Item/structure names, descriptions |
| `Data\paints.xml` | Paint spray can recipes (color list) |
| `Data\celestialbodies.xml` | Planet orbital data |
| `Worlds\` | Per-planet world settings, terrain, ore regions |

### World Data (Primary Source)
We extract world data directly from game files for 1:1 coordinate accuracy.
See: `docs/world-data-research.md` for detailed world/ore data format.

### Community Map Data (Reference Only)
https://github.com/aproposmath/stationeers-deepmining-map
- Uses normalized 0-1 coordinates (we use game coordinates instead)
- Useful for visual reference but not our data source

---

## Kit/Variant System

### Class Hierarchy

```
Thing (base)
  └── DynamicThing
      └── Item
          └── Stackable (implements IConstructionKit)
              ├── Constructor (single-variant kit)
              ├── MultiConstructor (multi-variant kit)
              └── DynamicThingConstructor
```

### IConstructionKit Interface

```csharp
public interface IConstructionKit
{
    string GetPrefabName();
    string GetFallbackName();
    List<Thing> GetConstructedPrefabs();
}
```

### Constructor (Single-Variant Kit)

```csharp
public class Constructor : Stackable, IConstructionKit
{
    public Structure BuildStructure;  // Single structure to build
    public int QuantityUsed = 1;      // Kits consumed per build
}
```

### MultiConstructor (Multi-Variant Kit)

```csharp
public class MultiConstructor : Stackable, IConstructionKit
{
    public List<Structure> Constructables;  // Multiple variants
    public int LastSelectedIndex;           // Last selected variant

    public void Construct(Grid3 localPosition, Quaternion targetRotation,
                         int optionIndex, ...);  // optionIndex selects variant
}
```

### Key Insight
Variants are stored as `List<Structure>`. Player selects by index. Each variant may require different material quantities via `BuildStates[0].Tool.EntryQuantity`.

---

## Frame Variants

### Steel Frames (Kit: ItemSteelFrames)

| Prefab Name | Display Name | Notes |
|-------------|--------------|-------|
| `StructureFrame` | Steel Frame | Basic flat frame |
| `StructureFrameCorner` | Steel Frame (Corner) | L-shaped corner piece |
| `StructureFrameSide` | Steel Frame (Side) | Edge/side piece |
| `StructureFrameCornerCut` | Steel Frame (Corner Cut) | Angled corner cut |

### Iron Frames (Kit: ItemIronFrames)

| Prefab Name | Display Name | Notes |
|-------------|--------------|-------|
| `StructureFrameIron` | Iron Frame | Single variant only |

### Construction Requirements
- Steel frames: `ItemSteelSheets` + `ItemWeldingTorch`
- Iron frames: `ItemIronSheets` + `ItemWeldingTorch`

---

## Paint Colors

12 colors available via spray cans:

| Color | Spray Can Prefab |
|-------|------------------|
| Black | `ItemSprayCanBlack` |
| Blue | `ItemSprayCanBlue` |
| Brown | `ItemSprayCanBrown` |
| Green | `ItemSprayCanGreen` |
| Grey | `ItemSprayCanGrey` |
| Khaki | `ItemSprayCanKhaki` |
| Orange | `ItemSprayCanOrange` |
| Pink | `ItemSprayCanPink` |
| Purple | `ItemSprayCanPurple` |
| Red | `ItemSprayCanRed` |
| White | `ItemSprayCanWhite` |
| Yellow | `ItemSprayCanYellow` |

Source: `Data\paints.xml`

---

## Planets & Coordinate System

### Available Worlds

| World | XML File | Folder |
|-------|----------|--------|
| Lunar (Moon) | Lunar.xml | Lunar/ |
| Europa | Europa.xml | Europa/ |
| Mars | Mars2.xml | Mars2/ |
| Mimas | MimasHerschel.xml | Mimas/ |
| Venus | Venus.xml | Venus/ |
| Vulcan | Vulcan.xml | Vulcan/ |

### Coordinate System
- **WorldSize: 4096** (all planets)
- **Coordinate range:** -2048 to +2048
- **Origin:** Map center (0, 0)
- **1:1 with in-game coordinates** - no conversion needed

### Mars Spawn Points (from XML)

| Location | Coordinates [X, Y] |
|----------|-------------------|
| Canyon Overlook | [-612, 803] |
| Butchers Flat | [347, -281] |
| Finders Canyon | [-1405, 32] |
| Hellas Crags | [-990, -986] |
| Donut Flats | [1359, -315] |

---

## Ore/Mining Data

Mining regions defined per-world via:
- PNG texture file with color-coded regions
- XML mapping of RGB colors to ore types

### Ore Types (all planets)
- Iron
- Copper
- Silicon
- Gold
- Silver
- Lead
- Nickel
- Cobalt
- Coal

Plus combination regions: IronCopperSilicon, GoldSilver, SilverLeadNickel

**Note:** Each planet uses DIFFERENT RGB colors for the same ore types. Must parse XML to get correct mapping.

See: `docs/world-data-research.md` for full details.

---

## Fonts

### Game Installation Fonts
`...\StreamingAssets\Fonts\`

| Font | Style | Use Case |
|------|-------|----------|
| `3270-Regular.ttf` | IBM terminal retro | Headers, titles |
| `RobotoMono-Regular.ttf` | Monospace | Technical data |
| `RobotoMono-Bold.ttf` | Monospace bold | Labels, emphasis |
| `MiSans-Normal.ttf` | Sans-serif | CJK support |

---

## Rotation/Orientation System

### Storage Format
Rotations stored as **Euler angles (X, Y, Z degrees)**, rounded to integers, converted to Quaternion for runtime use.

Source: `Assets\Scripts\StructureSpawnData.cs`
```csharp
Rotation = new Vector3Reference(structure.Rotation.eulerAngles.Round(0))
```

### Rotation Increments
**90-degree increments** for grid-aligned placement (sometimes 180-degree).

Source: `Assets\Scripts\Util\SmartRotate.cs`
```csharp
Quaternion.Euler(90f, 0f, 0f)  // RotX
Quaternion.Euler(0f, 90f, 0f)  // RotY
Quaternion.Euler(0f, 0f, 90f)  // RotZ
Quaternion.Euler(0f, 0f, 180f) // RotZZ
```

### Rotation Axis Enum
Source: `Assets\Scripts\Objects\RotationAxis.cs`

| Value | Name | Meaning |
|-------|------|---------|
| 0 | None | No rotation allowed |
| 1 | X | X-axis only |
| 2 | Y | Y-axis only |
| 4 | Z | Z-axis only |
| 3 | XY | X and Y axes |
| 5 | ZX | Z and X axes |
| 6 | ZY | Z and Y axes |
| 7 | All | All axes |

### Allowed Rotations Enum
Source: `Assets\Scripts\Objects\AllowedRotations.cs`

| Value | Name | Meaning |
|-------|------|---------|
| 0 | None | No placement allowed |
| 1 | Wall | Wall-mounted |
| 2 | Ceiling | Ceiling placement |
| 4 | Floor | Floor placement |
| 6 | Vertical | Ceiling + Floor |
| 7 | All | Wall + Ceiling + Floor |

### Placement Snap Types
Source: `Assets\Scripts\Objects\PlacementSnap.cs`

| Type | Description |
|------|-------------|
| Grid | Snap to grid - 90-degree increments |
| Face | Snap to face - variable rotation |
| FaceMount | Face-mounted placement |

### Keybind Mapping
| Keys | Axis | Increment |
|------|------|-----------|
| Ins / Del | X-axis | 90° |
| Home / End | Y-axis | 90° |
| PgUp / PgDn | Z-axis | 90° |
| C | Cycle | Next valid orientation |

### SmartRotate System
`SmartRotate.cs` handles intelligent rotation cycling that:
- Respects `PlacementSnap` type
- Honors `RotationAxis` constraints
- Validates against `AllowedRotations`
- Maintains grid alignment and connection validity

---

## Placement Rules & Slot System

### Cell Storage Model
Source: `Assets\Scripts\GridSystem\Cell.cs`

Each grid cell contains:
```csharp
Dictionary<Grid3, Structure> Structural;  // One structure per grid position
List<Structure> AllStructures;            // All coexisting structures
```

### Collision Types
Source: `Assets\Scripts\Objects\CollisionType.cs`

| Type | Value | Behavior |
|------|-------|----------|
| BlockGrid | 0 | Prevents ANY coexistence - exclusive cell ownership |
| BlockFace | 1 | Only blocks at exact same position (face-mounted) |
| BlockCustom | 2 | Allows coexistence (used by frames, stairs) |

**Frames use `BlockCustom`** - they CAN coexist with pipes, cables, devices.

### SmallCell Slots (for items mounted on frames)
Source: `Assets\Scripts\GridSystem\SmallCell.cs`

```csharp
public Chute Chute;           // Atmospheric chutes
public Pipe Pipe;             // Fluid pipes
public Device Device;         // Devices
public Cable Cable;           // Power cables
public SmallGrid Other;       // Other small grid items
public IRoboticArmRail Rail;  // Robotic arm rails
```

Each slot holds ONE item of that type per small cell position.

### Frame Placement Properties

| Property | Value | Meaning |
|----------|-------|---------|
| PlacementType | `PlacementSnap.Grid` | Snaps to 2m large grid |
| CollisionType | `BlockCustom` | Allows coexistence |
| AllowMounting | `true` | Pipes/cables/devices can mount on it |
| AllowedRotations | `All` (default) | Wall, ceiling, floor valid |
| Grid Spacing | 2.0f | Large grid cells |

### Placement Validation Flow
Source: `Assets\Scripts\Objects\Structure.cs` (lines 1167-1185)

1. Get all grid cells the structure would occupy (`GridBounds`)
2. For each cell, call `CanConstructCell()`
3. Check existing structures' `CollisionType`:
   - If any is `BlockGrid` → invalid
   - If `BlockFace` and same position → invalid
   - If `BlockCustom` → allowed
4. Return valid if all cells pass

### Frame Variant Differences
Frame variants (corner, side, corner cut) use the **same Frame class** - differences are in prefab configuration:
- Different mesh/model
- Potentially different `GridBounds`
- Same placement logic

### Multi-Object Cell Example
A single grid cell can contain:
```
Large Grid Layer:
  └── Frame (BlockCustom, AllowMounting=true)

Small Grid Layer (mounted on frame):
  ├── Cable slot → one cable
  ├── Pipe slot → one pipe
  ├── Chute slot → one chute
  └── Device slot → one device
```

---

## TODO: Research Needed

### Pipe/Cable Connection Rules
- [ ] How do pipes validate corner connections?
- [ ] What defines valid junction configurations?
- [ ] How are networks tracked?
- [ ] Source files: likely in `Assets\Scripts\Objects\Pipes\`

### Wall Variants
- [ ] Extract full list of wall kit variants
- [ ] Window types
- [ ] Door types

### Device Variants
- [ ] Atmospherics kit variants
- [ ] Storage kit variants
- [ ] Other kit mappings

---

## Testing Configuration

**Preferred test location:** Mars - Donut Flats [1359, -315]

---

## Version Notes

This data extracted from game version current as of December 2024. May need updates when game patches release new content.
