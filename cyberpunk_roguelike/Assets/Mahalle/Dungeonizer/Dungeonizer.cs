/*
Thank you for buying Dungeonizer. I hope you will enjoy it.

My aim was to create a dungeon generator that can be used in 2D and 3D games. 
And i never used too much complicated algorithms to make this one easy to modify and understand.

So feel free to hack it and make it better.

And join our discord channel for support and discussions.
https://discord.com/invite/fWjQWbkfQB

Have fun!
Mert
*/



using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Dungeonizer {
	// Runtime attribute; the slider drawer lives in Editor/DungeonizerEditor.cs.
	// Behaves like [Range] but snaps to odd integers only.
	public class OddIntRangeAttribute : PropertyAttribute {
		public readonly int min;
		public readonly int max;
		public OddIntRangeAttribute(int min, int max) { this.min = min; this.max = max; }
	}

	public class Room {
		public int x = 0;
		public int y = 0;
		public int w = 0;
		public int h = 0;
		public Room connectedTo = null;
		public bool dead_end = false;
		public int room_id = 0;

		public int room_height = 0;
	}

	public class SpawnList {
		public int x;
		public int y;
		public bool byWall;
		public string wallLocation;
		public bool inTheMiddle;
		public bool byCorridor;

		public int asDoor = 0;
		public Room room = null;
		public bool spawnedObject;
	}

	[System.Serializable]
	public class SpawnOption {
		public int minSpawnCount;
		public int maxSpawnCount;
		public bool spawnByWall;
		public bool spawmInTheMiddle;
		public bool spawnRotated;
		//public bool byCorridor;
		[Tooltip("This is for make spawned object will be higher than ground.")]
		public float heightFix = 0;

		public GameObject gameObject;
		[Tooltip("Use 0 for random room, make sure spawn room isnt bigger than your room count")]
		public int spawnRoom = 0;
	}

	[System.Serializable]
	public class CustomRoom {
		[Tooltip("make sure room id isnt bigger than your room count")]
		public int roomId = 1;
		public GameObject floorPrefab;
		public GameObject wallPrefab;
		public GameObject doorPrefab;
		public GameObject cornerPrefab;
	}

	public class MapTile {
		public int x = 0;
		public int y = 0;
		public int z = 0;

		public int type = 0; //Default = 0 , Room Floor = 1, Wall = 2, Corridor Floor 3, Room Corners = 4, 5, 6 , 7
		public int orientation = 0;
		public Room room = null;   
		public int tile_height = 0;

		public bool isCorner = false;
		public bool isEdge = false;
		public bool isDoor = false;
		public int doorDirection = 0;
		public bool byCorridor = false;
		public string edgeLocation = "";

		public bool isWall(){
			if(this.type >= 8 && this.type <= 11) return true;
			return false;
		}
		
	}


	public class Dungeonizer : MonoBehaviour {
		[Tooltip("Select a seed. use 0 for random seed.")]
        public int seed = 0; // Use 0 for a random seed

        [Tooltip("This prefab will be instantiate on dungeons entrance. You can put your character, or character spawner here.")]
		public GameObject startPrefab;

		[Tooltip("This will be end of level. ")]
		public GameObject exitPrefab;

		public List<SpawnList> spawnedObjectLocations = new List<SpawnList>();

		[Header("LockDown System")]
		public GameObject roomManagerPrefab;

		[Header("Boss")]
		public GameObject bossPrefab;

		[Header("Base Tiles")]
		public GameObject floorPrefab;       // 기본 바닥 (리스트가 비었을 때나 CustomRoom이 없을 때 사용)
		public GameObject wallPrefab;        // 기본 벽

		[Header("Random Variations")]
		public List<GameObject> floorPrefabs; // 여러 종류의 랜덤 바닥 프리팹들
		public List<GameObject> wallPrefabs;  // 여러 종류의 랜덤 벽 프리팹들
		public GameObject doorPrefab;
		//public GameObject doorCorners;
		public GameObject corridorFloorPrefab;
		public GameObject corridorWallPrefab;

		public GameObject cornerPrefab;
		public bool cornerRotation = false;

		public int maximumRoomCount = 10;

		[Tooltip("Min gap between rooms. Also affects corridor lengths ")]
		public int minimumRoomMargin = 0;

		[Tooltip("Maximum gap between rooms. Also affects corridor lengths ")]
		public int roomMargin = 3;

		[Tooltip("Corridor thickness in tiles. Odd values only (1, 3, 5, 7, 9). " +
		         "Default 1 matches previous versions exactly. For widths > 1 ensure minRoomSize >= corridorWidth + 2.")]
		[OddIntRange(1, 9)]
		public int corridorWidth = 1;

		[Header("Layout")]
		[Tooltip("Fraction of total rooms used for the main path from start to exit. " +
		         "0.33 = main path is ~1/3 of total rooms; the other 2/3 are distributed to branches.")]
		[Range(0.1f, 0.9f)]
		public float mainPathFraction = 0.33f;

		[Tooltip("Fraction of middle main-path rooms that become branch anchors. " +
		         "0.33 = ~1/3 of middle rooms spawn branches.")]
		[Range(0.1f, 1.0f)]
		public float branchPointFraction = 0.33f;

		[Tooltip("Maximum recursion depth for sub-branching. " +
		         "1 = main path + first-level branches only. 3 = branches can sub-branch twice (fractal-ish). " +
		         "4 = deep nested trees; use with large dungeons (N >= 40).")]
		[Range(1, 4)]
		public int maxBranchDepth = 3;

		[Tooltip("Chance that a chain continues in the same direction as the previous step, instead of turning. " +
		         "0 = always turns (zig-zag). 0.33 = mixed (default). 1 = always straight when possible (hallway-like).")]
		[Range(0f, 1f)]
		public float straightChance = 0.33f;
		[Tooltip("If Checked: makes dungeon reset on every time level loads.")]	
		public bool generate_on_load = true;
		public int minRoomSize = 5;
		public int maxRoomSize = 10;
		
		[Tooltip("How big are your tiles? (Affects corridor and room sizes)")]
		public float tileScaling = 1f;
		public List<SpawnOption> spawnOptions = new List<SpawnOption>();
		public List<CustomRoom> customRooms = new List<CustomRoom> ();
		public bool makeIt3d = false;

		public static int TargetRoomId = -1;
		private Dungeon _dungeon;

		//public NavMeshSurface surface;

		public class Dungeon {
			public int seed;
			public int map_size;


			// Spatial hash: one bucket per grid cell. Multiple tiles can coexist at the same (x,y)
			// (e.g., stacked room wall + corridor wall at a door flank), so each bucket is a list.
			public Dictionary<Vector2Int, List<MapTile>> map;

			public List<Room> rooms = new List<Room>();

			public void AddTile(MapTile t)
			{
				var key = new Vector2Int(t.x, t.y);
				if (!map.TryGetValue(key, out var bucket))
				{
					bucket = new List<MapTile>();
					map[key] = bucket;
				}
				bucket.Add(t);
			}

			public MapTile FindAt(int x, int y, System.Predicate<MapTile> pred)
			{
				if (!map.TryGetValue(new Vector2Int(x, y), out var bucket)) return null;
				for (int i = 0; i < bucket.Count; i++)
				{
					if (pred(bucket[i])) return bucket[i];
				}
				return null;
			}

			public int RemoveAllAt(int x, int y, System.Predicate<MapTile> pred)
			{
				if (!map.TryGetValue(new Vector2Int(x, y), out var bucket)) return 0;
				return bucket.RemoveAll(pred);
			}

			public IEnumerable<MapTile> AllTiles()
			{
				foreach (var kv in map)
				{
					var bucket = kv.Value;
					for (int i = 0; i < bucket.Count; i++) yield return bucket[i];
				}
			}

			public Room goalRoom;
			public Room startRoom;

			public int min_size;
			public int max_size;

			public int maximumRoomCount;
			public int minimumRoomMargin;
			public int roomMargin;
			public int roomMarginTemp;
			public int corridorWidth;

			public float mainPathFraction;
			public float branchPointFraction;
			public int maxBranchDepth;
			public float straightChance;

			//tile types for ease
			public static readonly List<int> corners = new List<int> {4,5,6,7};
			public static readonly List<int> walls = new List<int> {8,9,10,11};
			public static readonly List<int> corridor_walls = new List<int> {108,109,1010,1011};
			private static readonly List<string> directions = new List<string> {"x","y","-y","-x"}; //,"-y"};
			
			public MapTile createTile(int type, int x, int y, Room room = null){
				MapTile newRoomTile = new MapTile();
				newRoomTile.type = type;
				newRoomTile.room = room;
				newRoomTile.x = x;
				newRoomTile.y = y;
				if(room != null) {
					newRoomTile.z = room.room_height;
					newRoomTile.tile_height = room.room_height;
				}
				return newRoomTile;
			}

			// For corridorWidth > 1: carves (width - 1) parallel floor tiles perpendicular to the spine.
			// The spine tile retains sole ownership of isDoor / doorDirection — parallel tiles never carry them.
			private void WidenCorridorStep(int spineX, int spineY, bool horizontal, int radius)
			{
				if (radius <= 0) return;

				for (int off = -radius; off <= radius; off++)
				{
					if (off == 0) continue; // spine already placed by the caller

					int fx = horizontal ? spineX : spineX + off;
					int fy = horizontal ? spineY + off : spineY;

					// Remove any wall the widened strip punches through (same policy as the spine).
					RemoveAllAt(fx, fy, item => item.isWall());

					// If a room floor or existing corridor floor sits here, don't overwrite — just mark it byCorridor.
					var existing = FindAt(fx, fy, item => item.type == 1 || item.type == 3);
					if (existing != null)
					{
						existing.byCorridor = true;
						continue;
					}

					// Skip room corners (types 4-7) — leave their geometry alone.
					if (FindAt(fx, fy, item => corners.Contains(item.type)) != null) continue;

					MapTile extra = new MapTile();
					extra.type = 3;
					extra.x = fx;
					extra.y = fy;
					AddTile(extra);
				}
			}

			private static string OppositeDirection(string dir)
			{
				switch (dir)
				{
					case "y":  return "-y";
					case "-y": return "y";
					case "x":  return "-x";
					case "-x": return "x";
					default:   return null;
				}
			}

			// For corridorWidth > 1 only. At the door row where the spine punches through a room wall,
			// the flanking positions currently have a room wall (running perpendicular to the corridor).
			// The corridor's own side wall stops one tile short of the room and "turns" into the room wall,
			// leaving an exposed corner gap in many wall prefab designs. This stacks a corridor side wall
			// at the flank position — perpendicular to the room wall, both present — so both directions are
			// sealed. The room wall stays intact (keeps the room's perimeter closed).
			private void AddDoorFlankCorners(int spineX, int spineY, bool horizontal, int wallOffset)
			{
				if (wallOffset <= 1) return; // width==1 has no visible gap

				int negFx, negFy, posFx, posFy;
				int negType, posType;

				if (!horizontal)
				{
					// Vertical corridor — flanks sit east (+) and west (-) of spine on the door row.
					negFx = spineX - wallOffset; negFy = spineY;
					posFx = spineX + wallOffset; posFy = spineY;
					negType = 1011; // corridor west wall (face points east, into corridor)
					posType = 109;  // corridor east wall (face points west)
				}
				else
				{
					// Horizontal corridor — flanks sit north (+) and south (-) of spine on the door row.
					negFx = spineX; negFy = spineY - wallOffset;
					posFx = spineX; posFy = spineY + wallOffset;
					negType = 1010; // corridor south wall
					posType = 108;  // corridor north wall
				}

				// Do NOT remove the existing room walls — we need both walls present to seal both directions.
				AddTile(createTile(negType, negFx, negFy));
				AddTile(createTile(posType, posFx, posFy));
			}

			// Builds a Room adjacent to `anchor` in the given direction ("y", "-y", "x", "-x").
			// Does NOT add it to rooms or check collisions — caller decides.
			private Room BuildRoomInDirection(Room anchor, string dir)
			{
				Room room = new Room();
				this.roomMarginTemp = Random.Range(0, this.roomMargin - 1);
				room.w = Random.Range(min_size, max_size);
				if (room.w % 2 == 0) room.w += 1;
				room.h = Random.Range(min_size, max_size);
				if (room.h % 2 == 0) room.h += 1;

				if (dir == "y") {
					room.x = anchor.x + anchor.w + this.roomMarginTemp + this.minimumRoomMargin + 2;
					room.y = anchor.y;
				} else if (dir == "-y") {
					room.x = anchor.x - room.w - this.roomMarginTemp - this.minimumRoomMargin - 2;
					room.y = anchor.y;
				} else if (dir == "x") {
					room.y = anchor.y + anchor.h + this.roomMarginTemp + this.minimumRoomMargin + 2;
					room.x = anchor.x;
				} else if (dir == "-x") {
					room.y = anchor.y - room.h - this.roomMarginTemp - this.minimumRoomMargin - 2;
					room.x = anchor.x;
				}
				room.room_height = roomMarginTemp;
				return room;
			}

			// Determines the direction from `from` to `to` based on their positions.
			private static string DirectionBetween(Room from, Room to)
			{
				if (from == null || to == null) return null;
				if (to.x > from.x) return "y";
				if (to.x < from.x) return "-y";
				if (to.y > from.y) return "x";
				if (to.y < from.y) return "-x";
				return null;
			}

			// Extends a chain of up to `targetLength` rooms starting from `startAnchor`. Each placed room is
			// added to `rooms`. Direction is chosen avoiding pre-excluded ones (direction back toward parent,
			// or another direction caller wants to avoid). Straight-run bias lets the chain continue in the
			// same direction (vs turning) based on `straightChance`. Returns the list of newly placed rooms.
			private List<Room> BuildChain(Room startAnchor, int targetLength, string biasAwayFromDir, string extraExcludedDir)
			{
				List<Room> placed = new List<Room>();
				Room currentAnchor = startAnchor;
				string lastDir = null;

				System.Collections.Generic.HashSet<string> tried = new System.Collections.Generic.HashSet<string>();
				if (biasAwayFromDir != null) {
					string opp = OppositeDirection(biasAwayFromDir);
					if (opp != null) tried.Add(opp);
				}
				if (extraExcludedDir != null) tried.Add(extraExcludedDir);

				while (placed.Count < targetLength) {
					string chosen = null;

					// Straight-run bias: continue in the last successful direction if allowed.
					if (lastDir != null && !tried.Contains(lastDir) && Random.value < straightChance) {
						chosen = lastDir;
					} else {
						List<string> available = new List<string>();
						for (int d = 0; d < directions.Count; d++) {
							if (!tried.Contains(directions[d])) available.Add(directions[d]);
						}
						if (available.Count == 0) break; // all directions exhausted
						chosen = available[Random.Range(0, available.Count)];
					}

					Room room = BuildRoomInDirection(currentAnchor, chosen);

					if (DoesCollide(room, 0)) {
						tried.Add(chosen);
						continue;
					}

					room.room_id = rooms.Count;
					room.connectedTo = currentAnchor;
					rooms.Add(room);
					placed.Add(room);

					// Chain continues from the newly placed room. Reset tried, pre-exclude opposite.
					currentAnchor = room;
					lastDir = chosen;
					tried.Clear();
					string oppNew = OppositeDirection(chosen);
					if (oppNew != null) tried.Add(oppNew);
				}

				return placed;
			}

			// Recursively distributes `budget` rooms as branches off `parentChain`. Depth 1 = direct branches
			// off the main path; depth N+1 = branches off those branches. Stops when depth > maxBranchDepth
			// or when budget runs out.
			private void BuildBranches(List<Room> parentChain, int budget, int depth)
			{
				if (depth > maxBranchDepth) return;
				if (budget <= 0) return;
				if (parentChain.Count < 3) return; // no middle rooms to branch from

				// Middle rooms (exclude first and last — the last is the chain's exit/tip).
				List<Room> middle = new List<Room>();
				for (int i = 1; i < parentChain.Count - 1; i++) middle.Add(parentChain[i]);

				int branchPointCount = Mathf.Max(1, Mathf.CeilToInt(middle.Count * branchPointFraction));
				if (branchPointCount > middle.Count) branchPointCount = middle.Count;

				// Fisher-Yates shuffle of middle so branch anchors are random.
				for (int i = 0; i < middle.Count; i++) {
					int j = Random.Range(i, middle.Count);
					Room tmp = middle[i]; middle[i] = middle[j]; middle[j] = tmp;
				}

				int perBranch = budget / branchPointCount;
				if (perBranch < 1) return;

				for (int b = 0; b < branchPointCount; b++) {
					Room anchor = middle[b];

					// Pre-exclude directions already used by the parent chain at this anchor:
					// - direction back toward parent (incoming)
					// - direction forward to next chain room (outgoing)
					string parentDir = DirectionBetween(anchor.connectedTo, anchor);
					string childDir = null;
					int idx = parentChain.IndexOf(anchor);
					if (idx >= 0 && idx + 1 < parentChain.Count) {
						childDir = DirectionBetween(anchor, parentChain[idx + 1]);
					}

					// At max depth, branches don't sub-branch — use full budget as linear chain.
					int subMainLen = (depth >= maxBranchDepth)
						? perBranch
						: Mathf.Max(1, Mathf.CeilToInt(perBranch * mainPathFraction));

					List<Room> subChain = new List<Room> { anchor };
					List<Room> placed = BuildChain(anchor, subMainLen, parentDir, childDir);
					subChain.AddRange(placed);

					int remaining = perBranch - placed.Count;
					if (remaining > 0 && depth + 1 <= maxBranchDepth) {
						BuildBranches(subChain, remaining, depth + 1);
					}
				}
			}

			public void Generate() {
                if (corridorWidth < 1) corridorWidth = 1;
                if (corridorWidth % 2 == 0)
                {
                    Debug.LogWarning($"Dungeonizer: corridorWidth={corridorWidth} is even; using {corridorWidth + 1}. " +
                                     "Even widths don't center on force-odd rooms.");
                    corridorWidth += 1;
                }

                if (seed == 0)
                {
                    seed = Random.Range(int.MinValue, int.MaxValue);
                }
                Random.InitState(seed);

                int room_count = this.maximumRoomCount;
				int min_size = this.min_size;
				int max_size = this.max_size;
				map = new Dictionary<Vector2Int, List<MapTile>>();
				rooms = new List<Room> ();

				// Phase A: place the first room (start), centered on the map.
				Room firstRoom = new Room();
				firstRoom.x = (int)Mathf.Floor(map_size / 2f);
				firstRoom.y = (int)Mathf.Floor(map_size / 2f);
				firstRoom.w = Random.Range(min_size, max_size);
				if (firstRoom.w % 2 == 0) firstRoom.w += 1;
				firstRoom.h = Random.Range(min_size, max_size);
				if (firstRoom.h % 2 == 0) firstRoom.h += 1;
				firstRoom.room_id = 0;
				rooms.Add(firstRoom);

				// Phase A: build the main path off the first room.
				int mainPathTarget = Mathf.Max(2, Mathf.CeilToInt(room_count * mainPathFraction));
				List<Room> mainPath = new List<Room>();
				mainPath.Add(firstRoom);
				List<Room> mainExtensions = BuildChain(firstRoom, mainPathTarget - 1, null, null);
				mainPath.AddRange(mainExtensions);

				// Phases B + C: distribute remaining rooms as a tree of branches off the main path.
				int branchBudget = room_count - rooms.Count;
				if (branchBudget > 0) {
					BuildBranches(mainPath, branchBudget, 1);
				}

				// Start = first room of main path. Goal = last room of main path (so start→exit is the spine).
				Room mainStart = mainPath[0];
				Room mainGoal = mainPath[mainPath.Count - 1];

				//room making
				for (int i = 0; i < rooms.Count; i++) {
					Room room = rooms [i];
					for (int x = room.x; x < room.x + room.w; x++) {       
						for (int y = room.y; y < room.y + room.h; y++) {
							MapTile newRoomTile = new MapTile();
							newRoomTile.type = 1;
							newRoomTile.room = room;
							newRoomTile.x = x;
							newRoomTile.y = y;
							newRoomTile.z = room.room_height;
							newRoomTile.tile_height = room.room_height;

							//mark edges:
							if(y == room.y + room.h - 1) { newRoomTile.isEdge = true; newRoomTile.edgeLocation = "n"; }
							if(y == room.y) { newRoomTile.isEdge = true; newRoomTile.edgeLocation = "s"; }
							if(x == room.x) { newRoomTile.isEdge = true; newRoomTile.edgeLocation = "w"; }
							if(x == room.x + room.w - 1) { newRoomTile.isEdge = true; newRoomTile.edgeLocation = "e"; }

							//mark corners:
							if(x == room.x && y == room.y){ AddTile(this.createTile(4, room.x - 1, room.y - 1, room)); }
							if(x == room.x && y == room.y + room.h - 1){ AddTile(this.createTile(5, room.x - 1, room.y + room.h, room)); }
							if(x == room.x + room.w - 1 && y == room.y){ AddTile(this.createTile(7, room.x + room.w, room.y - 1 , room)); }
							if(x == room.x + room.w - 1 && y ==  room.y + room.h - 1){ AddTile(this.createTile(6, room.x + room.w , room.y + room.h, room)); }

							AddTile(newRoomTile);
						}
					}

					/* these 4 loops creates room walls */
					for(int j = 0; j < room.h; j++ ){
						AddTile(createTile(11, room.x -1, room.y + j, room));
					}

					for(int j = 0; j < room.w; j++ ){
						AddTile(createTile(10, room.x + j, room.y - 1, room));
					}	

					for(int j = 0; j < room.h; j++ ){
						AddTile(createTile(9, room.x + room.w, room.y + j, room));
					}	

					for(int j = 0; j < room.w; j++ ){
						AddTile(createTile(8, room.x + j, room.y + room.h, room));
					}				
				}

				
				// Start = first placed (center of map). Goal = last of the main path (tip of the spine).
				goalRoom = mainGoal;
				startRoom = mainStart;

				
				//corridor making
				for (int i = 0; i < rooms.Count; i++) {
					Room roomA = rooms [i];
					Room roomB = rooms [i].connectedTo;

					if (roomB != null) {
						var pointA = new Room (); //start
						var pointB = new Room ();
						bool horizontalCorridor = false;
						bool nextTileBlocksDoor = false;

						
						// Created door count for this corridor
						int doorCount = 0;
						int doorDirection = 0;
						
						pointA.x = roomA.x + (int)Mathf.Floor (roomA.w / 2); // First Room Center X
						pointB.x = roomB.x + (int)Mathf.Floor (roomB.w / 2); // Second Room Center X

						pointA.y = roomA.y + (int)Mathf.Floor (roomA.h / 2); // First Room Center Y
						pointB.y = roomB.y + (int)Mathf.Floor (roomB.h / 2); // Second Room Center Y

						if (Mathf.Abs (pointA.x - pointB.x) > Mathf.Abs (pointA.y - pointB.y)) {
							//horizontal
							horizontalCorridor = true;
							if (roomA.h > roomB.h) {
								pointA.y = pointB.y;
							} else {
								pointB.y = pointA.y;
							}						
						} else {
							//vertical
							if (roomA.w > roomB.w) {
								pointA.x = pointB.x;
							} else {
								pointB.x = pointA.x;
							}					
						}

						// For corridorWidth=1 this is 1, keeping original wall placement unchanged.
						// For width=3 it becomes 2, width=5 → 3, etc.
						int wallOffset = (corridorWidth / 2) + 1;
						int widenRadius = (corridorWidth - 1) / 2;

						MapTile currentTile = null;
						while ((pointB.x != pointA.x) || (pointB.y != pointA.y)) {						
							// So dungeonizer starts from one room's center and goes to other room's center tile by tile.
							// And it creates a corridor.
							// This currentDirection means which direction we are going to create a corridor tile.
							// When its created it doesnt matter if its created left to right or right to left.
							// But we need to know which direction we are going to create next tile and how to rotate doors etc.
							
							int currentDirection = 0;
							if (horizontalCorridor) { 
								if (pointB.x > pointA.x) {
									pointB.x--;
									currentDirection = 4;
								} else {
									pointB.x++;
									currentDirection = 2;
								}
							} else {
								if (pointB.y > pointA.y) {
									currentDirection = 1;
									pointB.y--;
								} else {
									currentDirection = 3;
									pointB.y++;
								}
							}

							//This code checks if corridor hits a wall. Also saves it for later to create door if needed.
							//That means Dungeonizer will try not to spawn anything blocks corridors and doors.					
							bool isWall = FindAt(pointB.x, pointB.y, item => item.isWall()) != null;
							RemoveAllAt(pointB.x, pointB.y, item => item.isWall());

							if(isWall && currentTile != null){
								currentTile.byCorridor = true; //this is actually previous tile
							}

							//dont spawn anything if there is a floor already
							currentTile = FindAt(pointB.x, pointB.y, item => item.type == 1);
							if(currentTile != null && nextTileBlocksDoor)
							{
								currentTile.byCorridor = true;
								nextTileBlocksDoor = false;
								continue;
							}
							else if(currentTile != null) {
								continue;
							}


							
							MapTile newCorridorTile = new MapTile();
							newCorridorTile.type = 3;
							//newCorridorTile.room = room;
							newCorridorTile.x = pointB.x;
							newCorridorTile.y = pointB.y;
							if(isWall){							
								// if this is the first door:
								doorCount++;
								if(doorCount == 1){
									doorDirection = currentDirection;
								}
								else {
									doorDirection = currentDirection + 2;
								}
								nextTileBlocksDoor = true; //noting this because we want some items to spawn and block corridor entrances.			
								newCorridorTile.isDoor = true; //this tile could be a door?
								newCorridorTile.doorDirection = doorDirection;

							}
							AddTile(newCorridorTile);

							if (corridorWidth > 1) {
								WidenCorridorStep(pointB.x, pointB.y, horizontalCorridor, widenRadius);
								if (isWall) {
									AddDoorFlankCorners(pointB.x, pointB.y, horizontalCorridor, wallOffset);
								}
							}

							//Corridor wall locations (offset scales with corridorWidth; for width=1, wallOffset=1 so this is unchanged)
							if(horizontalCorridor){
								currentTile = FindAt(pointB.x, pointB.y + wallOffset, item => Dungeon.walls.Contains(item.type));
								if(currentTile == null) {
									AddTile(createTile(108, pointB.x, pointB.y + wallOffset));
								}

								currentTile = FindAt(pointB.x, pointB.y - wallOffset, item => Dungeon.walls.Contains(item.type));
								if(currentTile == null)
								{
									AddTile(createTile(1010, pointB.x, pointB.y - wallOffset));
								}
							}
							else {
								currentTile = FindAt(pointB.x + wallOffset, pointB.y, item => Dungeon.walls.Contains(item.type));
								if(currentTile == null) {
									AddTile(createTile(109, pointB.x + wallOffset, pointB.y));
								}
								currentTile = FindAt(pointB.x - wallOffset, pointB.y, item => Dungeon.walls.Contains(item.type));
								if(currentTile == null) {
									AddTile(createTile(1011, pointB.x - wallOffset, pointB.y));
								}
							}
							


						} 

					}
				}
				
			}

			private bool DoesCollide (Room room, int ignore) {
				int random_blankliness = 0;

				for (int i = 0; i < rooms.Count; i++) {
					//if (i == ignore) continue;
					var check = rooms[i];
					if ( 
						!((room.x + room.w + random_blankliness < check.x) ||
						(room.x > check.x + check.w + random_blankliness) || 
						(room.y + room.h + random_blankliness < check.y) || 
						(room.y > check.y + check.h + random_blankliness)))
						return true;
				}
				
				return false;
			}
	

			private float lineDistance( Room point1, Room point2 )
			{
				var xs = 0;
				var ys = 0;
				
				xs = point2.x - point1.x;
				xs = xs * xs;
				
				ys = point2.y - point1.y;
				ys = ys * ys;
				
				return Mathf.Sqrt( xs + ys );
			}



		}

		public void ClearOldDungeon(bool immediate = false)
		{
			int childs = transform.childCount;
			for (var i = childs - 1; i >= 0; i--)
			{
				if(immediate){
					DestroyImmediate(transform.GetChild(i).gameObject);
				}
				else {
					Destroy(transform.GetChild(i).gameObject);
				}
			}
		}


        public void Generate()
        {
            SetupDungeon();
            InstantiateTiles();
            InstantiateStartAndEndPoints();
			SetupRoomManagersAndAreas();
            List<SpawnList> spawnedObjectLocations = FindSpawnLocations();
            ShuffleSpawnLocations(spawnedObjectLocations);
            SpawnObjects(spawnedObjectLocations);
            InstantiateDoors(spawnedObjectLocations);
			SetupDoorsToManagers();
        }

        // Sets up the dungeon parameters and generates the dungeon layout
        private void SetupDungeon()
        {
            Dungeon dungeon = new Dungeon();

            dungeon.seed = this.seed;
            dungeon.min_size = minRoomSize;
            dungeon.max_size = maxRoomSize;
            dungeon.maximumRoomCount = maximumRoomCount;
            dungeon.roomMargin = roomMargin;
            dungeon.minimumRoomMargin = minimumRoomMargin;
            dungeon.corridorWidth = corridorWidth;

            dungeon.mainPathFraction = mainPathFraction;
            dungeon.branchPointFraction = branchPointFraction;
            dungeon.maxBranchDepth = maxBranchDepth;
            dungeon.straightChance = straightChance;

            dungeon.Generate(); // Calculates all object locations (walls, corridors, doors, corners etc.)

            _dungeon = dungeon;

			TargetRoomId = _dungeon.goalRoom.room_id;
        }

        // Instantiates the tiles (floors, walls, corridors, corners) based on the dungeon map
        private void InstantiateTiles()
        {
            foreach (MapTile mapTile in _dungeon.AllTiles())
            {
                int tile = mapTile.type;
                int tile_height = mapTile.tile_height;

                int orientation = mapTile.orientation;
                GameObject created_tile = null;
                Vector3 tile_location;

                if (!makeIt3d)
                {
                    tile_location = new Vector3(mapTile.x * tileScaling, mapTile.y * tileScaling, 0);
                }
                else
                {
                    tile_location = new Vector3(mapTile.x * tileScaling, 0, mapTile.y * tileScaling);
                }

                created_tile = null;
                if (tile == 1)
                {
                    GameObject floorPrefabToUse = GetFloorPrefab(mapTile);

					if (floorPrefabToUse == floorPrefab)
					{
						floorPrefabToUse = GetRandomPrefabFromList(floorPrefabs, floorPrefab);
					}
                    created_tile = Instantiate(floorPrefabToUse, tile_location, Quaternion.identity) as GameObject;
                }

                if (Dungeon.walls.Contains(tile) || (Dungeon.corridor_walls.Contains(tile) && !corridorWallPrefab))
                {
                    GameObject wallPrefabToUse = GetWallPrefab(mapTile);
					if(wallPrefabToUse == wallPrefab)
					{
						wallPrefabToUse = GetRandomPrefabFromList(wallPrefabs, wallPrefab);
					}
                    created_tile = Instantiate(wallPrefabToUse, tile_location, Quaternion.identity) as GameObject;

                    RotateWall(created_tile, tile);
                }
                else if (Dungeon.corridor_walls.Contains(tile))
                {
                    GameObject wallPrefabToUse = GetCorridorWallPrefab(mapTile);
                    created_tile = Instantiate(wallPrefabToUse, tile_location, Quaternion.identity) as GameObject;

                    RotateWall(created_tile, tile);
                }

                if (tile == 3)
                {
                    GameObject floorPrefabToUse = corridorFloorPrefab ? corridorFloorPrefab : floorPrefab;
                    created_tile = Instantiate(floorPrefabToUse, tile_location, Quaternion.identity) as GameObject;

                    if (orientation == 1 && makeIt3d)
                    {
                        created_tile.transform.Rotate(Vector3.up * (-90));
                    }
                }

                if (Dungeon.corners.Contains(tile))
                {
                    GameObject cornerPrefabToUse = GetCornerPrefab(mapTile);

                    if (cornerPrefabToUse)
                    {
                        created_tile = Instantiate(cornerPrefabToUse, tile_location, Quaternion.identity) as GameObject;
                        RotateCorner(created_tile, tile);
                    }
                    else
                    {
                        created_tile = Instantiate(wallPrefab, tile_location, Quaternion.identity) as GameObject;
                    }
                }

                if (created_tile)
                {
                    created_tile.transform.parent = transform;
                }
            }
        }

        // Instantiates the start and end points of the dungeon
        private void InstantiateStartAndEndPoints()
        {
            GameObject end_point;
            GameObject start_point;

            if (!makeIt3d)
            {
                end_point = Instantiate(exitPrefab, new Vector3(_dungeon.goalRoom.x * tileScaling, _dungeon.goalRoom.y * tileScaling, 0), Quaternion.identity) as GameObject;
                start_point = Instantiate(startPrefab, new Vector3(_dungeon.startRoom.x * tileScaling, _dungeon.startRoom.y * tileScaling, 0), Quaternion.identity) as GameObject;
            }
            else
            {
                end_point = Instantiate(exitPrefab, new Vector3((_dungeon.goalRoom.x + Mathf.FloorToInt(_dungeon.goalRoom.w / 2)) * tileScaling, 0, (_dungeon.goalRoom.y + Mathf.FloorToInt(_dungeon.goalRoom.h / 2)) * tileScaling), Quaternion.identity) as GameObject;
                start_point = Instantiate(startPrefab, new Vector3((_dungeon.startRoom.x + Mathf.FloorToInt(_dungeon.startRoom.w / 2)) * tileScaling, 0, (_dungeon.startRoom.y + Mathf.FloorToInt(_dungeon.startRoom.h / 2)) * tileScaling), Quaternion.identity) as GameObject;
            }
			if (bossPrefab != null)
			{
				Vector3 bossPos = end_point.transform.position; 
				GameObject boss = Instantiate(bossPrefab, bossPos, Quaternion.identity);
				boss.transform.parent = transform;

				EnemyAI bossAI = boss.GetComponent<EnemyAI>();
				if(bossAI != null) 
				{
					// 1. 보스에게 Exit 룸 ID 부여
					bossAI.myRoomId = _dungeon.goalRoom.room_id;

					// 2. 씬에 있는 모든 RoomManager 중에서 이 방의 매니저를 찾음
					RoomManager[] managers = FindObjectsByType<RoomManager>(FindObjectsSortMode.None);
					bool foundManager = false;

					foreach(var rm in managers)
					{
						if(rm.roomId == _dungeon.goalRoom.room_id)
						{
							rm.enemiesInRoom.Add(bossAI); // 리스트에 강제로 추가
							foundManager = true;
							Debug.Log($"보스가 {rm.roomId}번 방 매니저에 성공적으로 등록되었습니다.");
							break;
						}
					}

					if (!foundManager)
					{
						Debug.LogError("보스 방의 RoomManager를 찾을 수 없습니다! 생성 순서를 확인하세요.");
					}
				}
			}
            end_point.transform.parent = transform;
            start_point.transform.parent = transform;
        }

        // Finds suitable locations to spawn objects and returns a list of spawn locations
        private List<SpawnList> FindSpawnLocations()
        {
            List<SpawnList> spawnedObjectLocations = new List<SpawnList>();

            foreach (MapTile mapTile in _dungeon.AllTiles())
            {
                if (mapTile.type == 1
                    // Do not spawn anything on player's start location or finish
                    && !(mapTile.x == _dungeon.startRoom.x + Mathf.FloorToInt(_dungeon.startRoom.w / 2) && mapTile.y == _dungeon.startRoom.y + Mathf.FloorToInt(_dungeon.startRoom.h / 2))
                    && !(mapTile.x == _dungeon.goalRoom.x + Mathf.FloorToInt(_dungeon.goalRoom.w / 2) && mapTile.y == _dungeon.goalRoom.y + Mathf.FloorToInt(_dungeon.goalRoom.h / 2)))
                {
                    SpawnList location = new SpawnList
                    {
                        byWall = mapTile.isEdge,
                        wallLocation = mapTile.edgeLocation,
                        x = mapTile.x,
                        y = mapTile.y,
                        byCorridor = mapTile.byCorridor,
                        room = mapTile.room
                    };

                    int roomCenterX = (int)Mathf.Floor(location.room.w / 2) + location.room.x;
                    int roomCenterY = (int)Mathf.Floor(location.room.h / 2) + location.room.y;

                    if (mapTile.x == roomCenterX + 1 && mapTile.y == roomCenterY + 1)
                    {
                        location.inTheMiddle = true;
                    }

                    spawnedObjectLocations.Add(location);
                }
                else if (mapTile.type == 3)
                {
                    if (mapTile.isDoor)
                    {
                        SpawnList location = new SpawnList
                        {
                            x = mapTile.x,
                            y = mapTile.y,
                            byCorridor = true,
                            asDoor = mapTile.doorDirection,
                            room = mapTile.room
                        };
                        spawnedObjectLocations.Add(location);
                    }
                }
            }

            return spawnedObjectLocations;
        }

        // Shuffles the list of spawn locations
        private void ShuffleSpawnLocations(List<SpawnList> spawnedObjectLocations)
        {
            for (int i = 0; i < spawnedObjectLocations.Count; i++)
            {
                SpawnList temp = spawnedObjectLocations[i];
                int randomIndex = Random.Range(i, spawnedObjectLocations.Count);
                spawnedObjectLocations[i] = spawnedObjectLocations[randomIndex];
                spawnedObjectLocations[randomIndex] = temp;
            }
        }

        // Spawns objects at suitable locations based on spawn options
        private void SpawnObjects(List<SpawnList> spawnedObjectLocations)
        {
            int objectCountToSpawn = 0;

            // Uncomment the following lines if you are going to use dynamic pathfinding
            // and have a NavMeshSurface component attached to the same gameobject.
            //surface = GetComponent<NavMeshSurface>();
            //surface.BuildNavMesh();

            // Now instantiating gameobjects wanted to "spawn"
            foreach (SpawnOption objectToSpawn in spawnOptions)
            {
                objectCountToSpawn = Random.Range(objectToSpawn.minSpawnCount, objectToSpawn.maxSpawnCount);
                while (objectCountToSpawn > 0)
                {
                    bool created = false;

                    for (int i = 0; i < spawnedObjectLocations.Count; i++)
                    {
                        if (CanSpawnObjectAtLocation(objectToSpawn, spawnedObjectLocations[i]))
                        {
                            SpawnList spawnLocation = spawnedObjectLocations[i];
                            GameObject newObject = InstantiateObjectAtLocation(objectToSpawn, spawnLocation);
                            newObject.transform.parent = transform;
							EnemyAI enemyScript = newObject.GetComponent<EnemyAI>();
							if(enemyScript != null)
							{
								enemyScript.myRoomId = spawnLocation.room.room_id;
								RoomManager[] allManagers = FindObjectsOfType<RoomManager>();
								foreach(var rm in allManagers)
								{
									if(rm.roomId == spawnLocation.room.room_id)
									{
										rm.enemiesInRoom.Add(enemyScript);
										break;
									}
								}
							}
                            spawnedObjectLocations[i].spawnedObject = newObject;
                            objectCountToSpawn--;
                            created = true;
                            break;
                        }
                    }

                    if (!created) // If can't find anywhere to put, don't put (prevents endless loops)
                    {
                        objectCountToSpawn--;
                    }
                }
            }
        }

        // Instantiates doors at door locations
        private void InstantiateDoors(List<SpawnList> spawnedObjectLocations)
        {
            if (doorPrefab)
            {
                foreach (SpawnList spawnLocation in spawnedObjectLocations)
                {
                    if (spawnLocation.asDoor > 0)
                    {
                        GameObject doorPrefabToUse = GetDoorPrefab(spawnLocation);
                        GameObject newObject = InstantiateDoorAtLocation(doorPrefabToUse, spawnLocation);
                        newObject.transform.parent = transform;
                        spawnLocation.spawnedObject = newObject;
                    }
                }
            }
        }

        // Helper method to get the floor prefab to use for a map tile
        private GameObject GetFloorPrefab(MapTile mapTile)
        {
            GameObject floorPrefabToUse = floorPrefab;
            Room room = mapTile.room;
            if (room != null)
            {
                foreach (CustomRoom customroom in customRooms)
                {
                    if (customroom.roomId == room.room_id)
                    {
                        floorPrefabToUse = customroom.floorPrefab;
                        break;
                    }
                }
            }
            return floorPrefabToUse;
        }

        // Helper method to get the wall prefab to use for a map tile
        private GameObject GetWallPrefab(MapTile mapTile)
        {
            GameObject wallPrefabToUse = wallPrefab;
            Room room = mapTile.room;
            if (room != null)
            {
                foreach (CustomRoom customroom in customRooms)
                {
                    if (customroom.roomId == room.room_id)
                    {
                        wallPrefabToUse = customroom.wallPrefab;
                        break;
                    }
                }
            }
            return wallPrefabToUse;
        }

        // Helper method to get the corridor wall prefab to use for a map tile
        private GameObject GetCorridorWallPrefab(MapTile mapTile)
        {
            GameObject wallPrefabToUse = corridorWallPrefab;
            Room room = mapTile.room;
            if (room != null)
            {
                foreach (CustomRoom customroom in customRooms)
                {
                    if (customroom.roomId == room.room_id)
                    {
                        wallPrefabToUse = customroom.wallPrefab;
                        break;
                    }
                }
            }
            return wallPrefabToUse;
        }

        // Helper method to get the corner prefab to use for a map tile
        private GameObject GetCornerPrefab(MapTile mapTile)
        {
            GameObject cornerPrefabToUse = cornerPrefab;
            Room room = mapTile.room;
            if (room != null)
            {
                foreach (CustomRoom customroom in customRooms)
                {
                    if (customroom.roomId == room.room_id)
                    {
                        cornerPrefabToUse = customroom.cornerPrefab;
                        break;
                    }
                }
            }
            return cornerPrefabToUse;
        }

        // Helper method to rotate a wall tile
        private void RotateWall(GameObject created_tile, int tile)
        {
            if (!makeIt3d)
            {
                created_tile.transform.Rotate(Vector3.forward * (-90 * (tile - 4)));
            }
            else
            {
                created_tile.transform.Rotate(Vector3.up * (90 * (tile - 2))); // 3D corner rotation
            }
        }

        // Helper method to rotate a corner tile
        private void RotateCorner(GameObject created_tile, int tile)
        {
            if (cornerRotation)
            {
                if (!makeIt3d)
                {
                    created_tile.transform.Rotate(Vector3.forward * (-90 * (tile - 4)));
                }
                else
                {
                    created_tile.transform.Rotate(Vector3.up * (90 * (tile - 4)));
                }
            }
        }

        // Checks if an object can be spawned at a given location based on spawn options
        private bool CanSpawnObjectAtLocation(SpawnOption objectToSpawn, SpawnList spawnLocation)
        {
            bool createHere = false;

            if (!spawnLocation.spawnedObject && !spawnLocation.byCorridor)
            {
				if(spawnLocation.room.room_id == _dungeon.startRoom.room_id||spawnLocation.room.room_id == _dungeon.goalRoom.room_id)
				{
					return false;
				}
                if (objectToSpawn.spawnRoom > maximumRoomCount)
                {
                    objectToSpawn.spawnRoom = 0;
                }
                if (objectToSpawn.spawnRoom == 0)
                {
                    if (objectToSpawn.spawnByWall)
                    {
                        if (spawnLocation.byWall)
                        {
                            createHere = true;
                        }
                    }
                    else if (objectToSpawn.spawmInTheMiddle)
                    {
                        if (spawnLocation.inTheMiddle)
                        {
                            createHere = true;
                        }
                    }
                    else
                    {
                        createHere = true;
                    }
                }
                else
                {
                    if (spawnLocation.room.room_id == objectToSpawn.spawnRoom)
                    {
                        if (objectToSpawn.spawnByWall)
                        {
                            if (spawnLocation.byWall)
                            {
                                createHere = true;
                            }
                        }
                        else
                        {
                            createHere = true;
                        }
                    }
                }
            }

            return createHere;
        }

        // Instantiates an object at a given spawn location
        private GameObject InstantiateObjectAtLocation(SpawnOption objectToSpawn, SpawnList spawnLocation)
        {
            GameObject newObject;
            Quaternion spawnRotation = Quaternion.identity;

            if (!makeIt3d)
            {
                newObject = Instantiate(objectToSpawn.gameObject, new Vector3(spawnLocation.x * tileScaling, spawnLocation.y * tileScaling, 0), spawnRotation) as GameObject;
            }
            else
            {
                if (spawnLocation.byWall)
                {
                    switch (spawnLocation.wallLocation)
                    {
                        case "s":
                            spawnRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                            break;
                        case "w":
                            spawnRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                            break;
                        case "n":
                            spawnRotation = Quaternion.Euler(new Vector3(0, 180, 0));
                            break;
                        case "e":
                            spawnRotation = Quaternion.Euler(new Vector3(0, 270, 0));
                            break;
                    }
                }
                else
                {
                    if (objectToSpawn.spawnRotated)
                    {
                        spawnRotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
                    }
                    else
                    {
                        spawnRotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 2) * 90, 0));
                    }
                }

                newObject = Instantiate(objectToSpawn.gameObject, new Vector3(spawnLocation.x * tileScaling, 0 + objectToSpawn.heightFix, spawnLocation.y * tileScaling), spawnRotation) as GameObject;
            }

            return newObject;
        }

        // Helper method to get the door prefab to use for a spawn location
        private GameObject GetDoorPrefab(SpawnList spawnLocation)
        {
            GameObject doorPrefabToUse = doorPrefab;
            Room room = spawnLocation.room;
            if (room != null)
            {
                foreach (CustomRoom customroom in customRooms)
                {
                    if (customroom.roomId == room.room_id)
                    {
                        doorPrefabToUse = customroom.doorPrefab;
                        break;
                    }
                }
            }
            return doorPrefabToUse;
        }

        // Instantiates a door at a given spawn location
        private GameObject InstantiateDoorAtLocation(GameObject doorPrefabToUse, SpawnList spawnLocation)
        {
            GameObject newObject;

            if (!makeIt3d)
            {
                newObject = Instantiate(doorPrefabToUse, new Vector3(spawnLocation.x * tileScaling, spawnLocation.y * tileScaling, 0), Quaternion.identity) as GameObject;
            }
            else
            {
                newObject = Instantiate(doorPrefabToUse, new Vector3(spawnLocation.x * tileScaling, 0, spawnLocation.y * tileScaling), Quaternion.identity) as GameObject;
            }

            RotateDoor(newObject, spawnLocation.asDoor);

            return newObject;
        }

        // Rotates a door based on its direction
        private void RotateDoor(GameObject newObject, int doorDirection)
        {
            if (!makeIt3d)
            {
                newObject.transform.Rotate(Vector3.forward * (-90 * (doorDirection - 1)));
            }
            else
            {
                // 3D Door Rotation
                newObject.transform.Rotate(Vector3.up * (-90 * (doorDirection - 1)));
            }
        }

		private GameObject GetRandomPrefabFromList(List<GameObject> list, GameObject fallback)
		{
			if(list != null && list.Count > 0)
			{
				return list[Random.Range(0, list.Count)];
			}
			return fallback;
		}
		public Dungeon GetDungeonData() { 
			return _dungeon; 
		}
		
		// 1. 방 관리자와 플레이어 감지용 Area 생성 함수
private void SetupRoomManagersAndAreas()
{
    foreach (Room room in _dungeon.rooms)
    {
        Vector3 centerPos;
        if (!makeIt3d) centerPos = new Vector3(room.x + room.w / 2f, room.y + room.h / 2f, 0) * tileScaling;
        else centerPos = new Vector3(room.x + room.w / 2f, 0, room.y + room.h / 2f) * tileScaling;

        // A. RoomManager 생성 및 ID 부여
        if (roomManagerPrefab != null)
        {
            GameObject rmObj = Instantiate(roomManagerPrefab, centerPos, Quaternion.identity);
            rmObj.transform.parent = transform;
            RoomManager rm = rmObj.GetComponent<RoomManager>();
            if (rm != null) rm.roomId = room.room_id;
        }

        // B. 플레이어 진입 감지용 RoomArea 트리거 생성
        GameObject areaObj = new GameObject("RoomArea_" + room.room_id);
        areaObj.transform.position = centerPos;
        areaObj.transform.parent = transform;
        areaObj.tag = "RoomArea"; // 유니티 에디터에서 Tag를 미리 만들어야 함

        BoxCollider2D col = areaObj.AddComponent<BoxCollider2D>();
		float padding = 1.5f * tileScaling;
        col.size = new Vector2((room.w * tileScaling)-padding, (room.h * tileScaling)-padding);
        col.isTrigger = true;

        RoomIDHolder holder = areaObj.AddComponent<RoomIDHolder>();
        holder.roomId = room.room_id;
    }
}

// 2. 생성된 문들을 가장 가까운 RoomManager에 연결하는 함수
private void SetupDoorsToManagers()
{
    DungeonDoor[] allDoors = FindObjectsOfType<DungeonDoor>();
    RoomManager[] allManagers = FindObjectsOfType<RoomManager>();

    foreach (DungeonDoor door in allDoors)
    {
        RoomManager closestRM = null;
        float minDistance = float.MaxValue;

        foreach (RoomManager rm in allManagers)
        {
            float dist = Vector3.Distance(door.transform.position, rm.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestRM = rm;
            }
        }

        if (closestRM != null)
        {
            closestRM.doors.Add(door);
            door.roomId = closestRM.roomId;
            door.Unlock(); // 처음엔 열어둠
        }
    }
}

        // Use this for initialization
        void Start () {
			if (generate_on_load){
				ClearOldDungeon();
				Generate();

			}
		}





	}
}