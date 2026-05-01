# Dungeonizer — Easy-to-Use Dungeon Generator

## Quick Start

Create a new empty GameObject. Add the **Dungeonizer** component to it (`Dungeonizer/Dungeonizer.cs`).

In the inspector, fill out the mandatory parameters:

- **Start Prefab** *(GameObject)*: The prefab that will be instantiated in the middle of the first room of the dungeon. This prefab can contain any objects you want to appear in the starting room, such as a player character, enemies, or items.

- **End Prefab** *(GameObject)*: The prefab that will be instantiated in the middle of the last room of the dungeon. Put your boss, treasure chest, or exit portal here.

- **Floor Prefab** *(GameObject)*: The prefab instantiated as the floor of the dungeon. A simple plane or cube that tiles cleanly.

- **Wall Prefab** *(GameObject)*: The prefab instantiated as room walls.

- **Door Prefab** *(GameObject)*: Placed at each corridor-to-room entrance.

- **Corridor Wall Prefab** *(GameObject)*: The prefab for the walls of the corridors. Can be different from room walls.

- **Corner Prefab** *(GameObject)*: Placed at room corners and at the joints where wide corridors meet rooms.

## Dungeon Size Parameters

- **Maximum Room Count** *(int)*: Target number of rooms in the dungeon.
- **Minimum Room Margin** *(int)*: Minimum gap between rooms. Also affects corridor length.
- **Room Margin** *(int)*: Maximum gap between rooms.
- **Min Room Size / Max Room Size** *(int)*: Controls room dimensions (rooms are forced to odd sizes internally so corridors center cleanly).
- **Tile Scaling** *(float)*: Scales all tiles. Use this to match your prefab sizes.
- **Make It 3D** *(bool)*: If true, the dungeon lies on the XZ plane for 3D. Otherwise XY for 2D.

## Corridor Width *(new in 2.7)*

- **Corridor Width** *(int, odd 1–9)*: Thickness of corridors in tiles. **Default 1** (matches all previous versions).
  - Set to 3, 5, 7 or 9 for wider corridors so bigger characters can fit through.
  - Slider snaps to odd values only so corridors always center on rooms.
  - When wider than 1, ensure **Min Room Size** is at least `corridorWidth + 2` so corridors fit through rooms.
  - Corner walls auto-place at the junctions where corridors meet rooms — no gaps.

## Layout Controls *(new in 2.7)*

These four sliders control the *shape* of the dungeon — how it branches, how deep the branching goes, and whether corridors run straight or zig-zag.

- **Main Path Fraction** *(0.1–0.9, default 0.33)*: Fraction of total rooms that form the main spine from start room to exit room. `0.33` means ~1/3 of rooms are on the main path; the other 2/3 are distributed to branches. **Start-to-exit is guaranteed to be the longest route no matter the seed.**

- **Branch Point Fraction** *(0.1–1.0, default 0.33)*: Of the main-path middle rooms, what fraction become branch anchors. `0.33` = 1/3 of main-path rooms spawn side branches.

- **Max Branch Depth** *(1–4, default 3)*: How many levels of sub-branching.
  - `1` = main path + one level of branches (flat, simple dungeons)
  - `3` = default (fractal-ish trees)
  - `4` = deep nested trees (use with large dungeons, N ≥ 40)

- **Straight Chance** *(0–1, default 0.33)*: Probability that a chain continues in the same direction instead of turning.
  - `0` = always turns (maze-like zig-zag)
  - `0.33` = mixed (default)
  - `1` = always straight when possible (long hallways)

## Other Optional Parameters

- **Seed** *(int)*: Use 0 for a random seed. Set any other value for reproducible dungeons.
- **Generate On Load** *(bool)*: If true, the dungeon regenerates when the scene loads. Otherwise use the **Create Now** button in the inspector.

## Populating the Dungeon

In the **Spawn Options** section of the inspector, add prefabs to the list. Click `+` to add.

- **Min Spawn Count / Max Spawn Count** *(int)*: Range of how many to spawn.
- **Spawn by Wall** *(bool)*: Spawns along walls instead of in room interiors. Good for torches, paintings, cabinets.
- **Spawn in the Middle** *(bool)*: Spawns at the center of rooms. Good for carpets, bosses, hanging lights.
- **Spawn Rotated** *(bool)*: Randomizes Y rotation. Wall objects face the right way automatically.
- **Height Fix** *(float)*: Adjusts Y position of spawned object. Fixes prefabs that sit under the floor or above the ceiling.
- **GameObject** *(GameObject)*: The prefab to spawn.
- **Spawn Room** *(int)*: Which room to spawn in (0 = random). Use for sub-bosses, key items, or storytelling placement.

## Custom Rooms

In the **Custom Rooms** section, define rooms with different art.

- **Room ID** *(int)*: Matches the **Spawn Room** field on spawn options, so you can place specific objects in specific rooms.
- **Floor / Wall / Door / Corner Prefab** *(GameObject)*: Override prefabs for this specific room.

## Multi-Dungeon Support *(improved in 2.7)*

You can now have multiple Dungeonizer components in the same scene without them clobbering each other's state. Each generates independently.

---

### Thank you for using Dungeonizer!

- **Discord Server**: https://discord.com/invite/fWjQWbkfQB
- **Tutorial**: https://www.youtube.com/watch?v=tKOqdpfHdfI
- **More Tutorial**: https://www.youtube.com/watch?v=xEzbP5mO948
- **Support**: support@mahalle.org
