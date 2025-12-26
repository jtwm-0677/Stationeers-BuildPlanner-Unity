# Stationeers World Data Research

## Overview

All world/planet data is stored in:
`{GameInstall}/rocketstation_Data/StreamingAssets/Worlds/`

## Available Worlds

| World | XML File | Folder |
|-------|----------|--------|
| Lunar (Moon) | Lunar.xml | Lunar/ |
| Europa | Europa.xml | Europa/ |
| Mars | Mars2.xml | Mars2/ |
| Mimas | MimasHerschel.xml | Mimas/ |
| Venus | Venus.xml | Venus/ |
| Vulcan | Vulcan.xml | Vulcan/ |

## Coordinate System

**All worlds use the same coordinate system:**
- WorldSize: 4096
- Coordinate range: -2048 to +2048
- Origin (0,0) is center of map
- X = East/West, Y = North/South

## Folder Structure (per world)

```
{World}/
  {World}.xml       # Main world definition
  Terrain/          # Heightmap chunks (Terrain0.dat, Terrain1.dat, etc.)
  Textures/
    {world}_deep_mining_regions.png   # Ore zones (color-coded)
    {world}_named_regions.png         # Geographic region names
    {world}_poi_regions.png           # Points of interest
    minimap.png                       # Overview image
    macro_diffuse.png                 # Terrain texture
    macro_normal.png                  # Terrain normal map
```

## XML Structure

### Start Locations
```xml
<StartLocation Id="MarsSpawnCanyonOverlook">
    <Name Key="MarsSpawnCanyonOverlookName"/>
    <Description Key="MarsSpawnCanyonOverlookDescription"/>
    <Position x="-612" y="803"/>  <!-- GAME COORDINATES -->
    <SpawnRadius Value="10"/>
</StartLocation>
```

### Deep Mining Regions
```xml
<RegionSet Id="MarsDeepMiningRegions">
    <Texture Path="Worlds/Mars2/Textures/mars_deep_mining_regions.png" Format="RGB24" LoadType="OnRequest"/>
    <Region Id="MarsDeepMiningRegionSilicon" R="182" G="0" B="255"/>
    <Region Id="MarsDeepMiningRegionIron" R="17" G="0" B="255"/>
    <Region Id="MarsDeepMiningRegionGold" R="255" G="234" B="0"/>
    <!-- ... more regions -->
</RegionSet>
```

### Terrain Settings
```xml
<TerrainSettings Path="Worlds/Mars2/Terrain" WorldSize="4096">
    <Curvature Value="0.3"/>
    <MiniMap Path="Worlds/Mars2/Textures/minimap.png" Format="DXT1" LoadType="OnRequest"/>
</TerrainSettings>
```

## Ore Types (consistent across all worlds)

- Iron
- Copper
- Silicon
- Gold
- Silver
- Lead
- Nickel
- Cobalt
- Coal

Plus combination regions:
- IronCopperSilicon
- GoldSilver
- SilverLeadNickel

## Color Mapping

Each world uses DIFFERENT RGB values for the same ore types. The color mapping is defined per-world in the XML. Must parse XML to get correct color→ore mapping.

Example - Iron on different worlds:
- Mars: RGB(17, 0, 255)
- Lunar: RGB(255, 0, 0)
- Vulcan: RGB(65, 144, 23)

## Implementation Notes

1. **Parse XML first** to get color mappings
2. **Load PNG textures** and sample pixel colors
3. **Map coordinates 1:1** with game (no normalization)
4. **Display minimap** as base layer
5. **Overlay ore regions** with transparency

## External Reference

Online ore map (uses normalized 0-1 coords, we use game coords):
https://aproposmath.github.io/stationeers-deepmining-map/
Repository: https://github.com/aproposmath/stationeers-deepmining-map

## Game File Locations

- Game install: `C:\Program Files (x86)\Steam\steamapps\common\Stationeers`
- World data: `{install}/rocketstation_Data/StreamingAssets/Worlds/`
- Shared textures: `{install}/rocketstation_Data/StreamingAssets/Worlds/SharedTextures/`
