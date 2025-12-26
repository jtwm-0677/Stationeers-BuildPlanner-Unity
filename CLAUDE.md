# Stationeers Build Planner - Unity Version

## Project Overview

Full-featured 3D base planner for Stationeers using actual game assets. Built in Unity to match the game's rendering pipeline and enable direct asset import.

**Engine:** Unity 6000.3.2f1 (6.3 LTS)
**Render Pipeline:** Built-in (matching Stationeers' Unity 2022.3.7f1)

## Project Structure

```
Assets/
  Scripts/       - C# game logic
  Prefabs/       - Reusable object prefabs
  Materials/     - Custom materials
  Scenes/        - Unity scenes
  GameAssets/    - Imported Stationeers assets
```

## Asset Sources

**Decompiled game assets:** `C:\Development\Stationeers Stuff\resources\Assets`
**Additional asset ripping available if needed**

## Key Features (Planned)

- 3D building placement with actual game models
- Free camera (matches in-game jetpack mobility)
- Network system for pipes/cables with capacity tracking
- Atmosphere/pressure simulation (future)
- Frame build stages (steel: 4 stages, iron: 3 stages)

## Project Location

`C:\Development\StationeersBuildPlanner`

## Related Projects

- **Web Version:** `C:\Development\Stationeers Stuff\BuildPlanner` - Basic 2D grid planner
- **Decompiled Assets:** `C:\Development\Stationeers Stuff\resources\Assets`
- **Decompiled Source:** `C:\Development\Stationeers Stuff\resources\Assemblies\Assembly-CSharp`

## Git Policy

- **No AI attribution in commits** - no "Generated with Claude Code", no "Co-Authored-By"
- All commits appear as standard developer commits

## Development Notes

- Use Built-in Render Pipeline shaders for asset compatibility
- Match Stationeers grid system: 1 unit = 0.1m, main grid = 2.0m (20 units)
