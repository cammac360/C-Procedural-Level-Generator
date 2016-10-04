using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RegionGenerator : MonoBehaviour {
	public int[,,] regionMap;
	List<Area> areaList = new List<Area> ();
	Queue<Area> areaQue = new Queue<Area> ();
	public int regionWidth;
	public int regionHeight;
	public int regionFloors;
	public int regionBudget;
	[Range(30,100)]
	public int roomSize;
	public GameObject caveGenerator;
	public GameObject interiorGenerator;
	public Dictionary<int, Area> areaDictionary = new Dictionary<int, Area> ();
	List<GameObject> finalRegions = new List<GameObject>();
	int[] startEdge;
	int roomIDNUM;
	int endFace;
	public int[,] floorMap;


	void Start(){
		GenerateRegion ();
		//CreateNavMap ();
	}
	void Update(){
		if(Input.GetKeyDown(KeyCode.F2)){
			foreach (GameObject del in finalRegions) {
				Destroy (del);
			}
			areaDictionary.Clear ();
			finalRegions.Clear ();
			areaList.Clear ();
			GenerateRegion ();
		}
		if(Input.GetKeyDown(KeyCode.Escape)){
			Application.Quit();
		}
	}
	//Generates a 3d array of room structs 
	void GenerateRegion(){
		floorMap = new int[regionWidth * roomSize, regionHeight * roomSize];
		// set room ID to 0 in prep for new region gen
		roomIDNUM = 0;
		//Initialize region map
		regionMap = new int [regionWidth, regionHeight, regionFloors];
		//Set entire region map to open
		for (int x = 0; x < regionWidth; x++) {
			for (int y = 0; y < regionHeight; y++) {
				for (int z = 0; z < regionFloors; z++) {
					regionMap [x, y, z] = 0;
				}
			}
		}
		//Set basic doorway for first room
		startEdge = new int[roomSize];
		for (int x = 0; x < roomSize; x++) {
			startEdge [x] = 0;
		}
		for (int x = roomSize / 2 - 4; x < roomSize / 2 + 4; x++) {
			startEdge [x] = 1;
		}
		//For each floor find rooms
		int startX = regionWidth/2;
		int startY = 0;
		int startDirection = 2;
		for (int floor = 0; floor < regionFloors; floor++) {
			Area temp = GenerateFloor (startX, startY, floor, startDirection);
			startX = temp.x;
			startY = temp.y;
			startDirection = endFace;
		}

		//Generate the random maps for each room. This also initializes the MeshGen for that room in MapGenerator.
		while(areaQue.Count > 0){
			Area room = areaQue.Dequeue ();
			if (room.start == true) {
				GameObject clone = Instantiate (caveGenerator, new Vector3 ((float)((room.x - (regionWidth/2)) * roomSize) - (room.x * 1), -(float)(room.floor * 12), (float)(room.y * roomSize) - (room.y * 1)), Quaternion.identity) as GameObject;
				MapGenerator maker = clone.GetComponent<MapGenerator> ();
				maker.CreateMap(room, new Edge( 2, startEdge), roomSize);
				finalRegions.Add (clone);
				room.SetMap (maker.map);
			} 
			if (room.start == false) {
				Area parent = null;
				areaDictionary.TryGetValue(room.parent, out parent);
				Edge parentEdge = parent.GetEdge (FlipSide(room.parentSide));
				GameObject clone = Instantiate (caveGenerator, new Vector3 ((float)((room.x - (regionWidth/2)) * roomSize) - (room.x * 1), -(float)(room.floor * 12), (float)(room.y * roomSize) - (room.y * 1)), Quaternion.identity) as GameObject;
				MapGenerator maker = clone.GetComponent<MapGenerator> ();
				maker.CreateMap (room, parentEdge, roomSize, false, room.end);
				finalRegions.Add (clone);
				room.SetMap (maker.map);
			}
			/*
			if (room.start == true && room.cost > 2) {
				GameObject clone = Instantiate (interiorGenerator, new Vector3 ((float)((room.x - (regionWidth/2)) * roomSize) - (room.x * 1), -(float)(room.floor * 6), (float)(room.y * roomSize) - (room.y * 1)), Quaternion.identity) as GameObject;
				InteriorMapGenerator maker = clone.GetComponent<InteriorMapGenerator> ();
				maker.CreateMap(room, roomSize);
				finalRegions.Add (clone);
				room.SetMap (maker.map);
			}
			if (room.start == false && room.cost > 2) {
				Area parent = null;
				areaDictionary.TryGetValue(room.parent, out parent);
				Edge parentEdge = parent.GetEdge (FlipSide(room.parentSide));
				GameObject clone = Instantiate (interiorGenerator, new Vector3 ((float)((room.x - (regionWidth/2)) * roomSize) - (room.x * 1), -(float)(room.floor * 6), (float)(room.y * roomSize) - (room.y * 1)), Quaternion.identity) as GameObject;
				InteriorMapGenerator maker = clone.GetComponent<InteriorMapGenerator> ();
				maker.CreateMap (room, roomSize);
				finalRegions.Add (clone);
				room.SetMap (maker.map);
			}
			*/
		}
	}
	void CreateNavMap(){
		/*
		List<NavMap> navList = new List<NavMap> ();
		foreach (GameObject map in finalRegions) {
			NavMap nav = map.AddComponent<NavMap> ();
			nav.mapSize = new Vector2 (roomSize - 1, roomSize - 1);
			nav.nodeRadius = .5f;
			nav.CreateGrid ();
			map.GetComponent<MapGenerator> ().self.navMap = nav;
			navList.Add (nav);
		}
		*/
		GameObject navigation = new GameObject ();
		navigation.name = "navigation";
		navigation.transform.position = new Vector3 (0, 0, (regionHeight/2) * roomSize);
		NavMap nav = navigation.AddComponent<NavMap> ();
		nav.mapSize = new Vector2 (regionWidth * roomSize, regionHeight * roomSize);
		nav.nodeRadius = .5f;
		nav.CreateGrid ();
		PathFinding finder = navigation.AddComponent<PathFinding> ();
		PathManager manager = navigation.AddComponent<PathManager> ();
		finder.requestManager = manager;
	}

	/// <summary>
	/// Check adjacent tiles for availbility and place newroom. Return downward stair room same floor.
	/// </summary>
	/// <returns>The floor.</returns>
	/// <param name="floor">Floor.</param>
	/// <param name="startRoom">Start room.</param>
	Area GenerateFloor (int startX, int startY, int floor, int startDirection = 2)
	{
		List<Area> floorRooms = new List<Area> ();
		List<Area> DeadRooms = new List<Area> ();
		List<int> testedDir = new List<int>();
		int floorBudget = 0;
		//Create and store first area
		roomIDNUM++;
		//NEED TO IMPLEMENT A WAY TO TELL WHAT DIRECTION STAIRS WILL COME FROM ON FLOOR OTHER THAN 1st!!!!!!!!!!!!!!!
		Area startArea = new Area (roomIDNUM, roomSize, startX, startY, floor, 0, -1, startDirection, true, false);
		regionMap[startX, startY, floor] = 1;
		areaDictionary.Add (roomIDNUM, startArea);
		floorRooms.Add (startArea);
		areaQue.Enqueue (startArea);
		startArea.SetParentEdge (startDirection);
		Area parent = startArea;
		//continue seaching for and createing rooms until you hit the budget;
		roomIDNUM++;
		bool floorComplete = false;
		while (!floorComplete) {
			//Find already generated area with the highest cost
			foreach (Area i in floorRooms) {
				if ((i.cost > parent.cost || floorBudget + parent.cost + 1 < regionBudget) && !DeadRooms.Contains(i)) {
					parent = i;
				}
			}
			//From Parent Find A Clear Adjescent Untested Area Randomly And Create Area;
			int test = Random.Range (0, 4);
			while (testedDir.Contains (test)) {
				test = Random.Range (0, 4);
				if (testedDir.Count >= 4) {
					break;
				}
			}
			testedDir.Add (test);
			switch (test) {
			case 0:
				testedDir.Add (0);
				if (InRange (parent.x, parent.y + 1, floor) && regionMap [parent.x, parent.y + 1, floor] == 0) {
					regionMap [parent.x, parent.y + 1, floor] = 1;
					Area newArea = new Area (roomIDNUM, roomSize, parent.x, parent.y + 1, floor, parent.cost + 1, parent.identity, 2, false, false);
					newArea.parentArea = parent;
					newArea.SetParentEdge (2);
					parent.SetChildEdge(0, roomIDNUM);
					floorRooms.Add (newArea);
					areaQue.Enqueue (newArea);
					areaDictionary.Add (roomIDNUM, newArea);
					floorBudget = floorBudget + parent.cost + 1;
					roomIDNUM++;
					testedDir.Clear ();
				}
				break;
			case 1:
				testedDir.Add (1);
				if (InRange (parent.x + 1, parent.y, floor) && regionMap [parent.x + 1, parent.y, floor] == 0) {
					regionMap [parent.x + 1, parent.y, floor] = 1;
					Area newArea = new Area (roomIDNUM, roomSize, parent.x + 1, parent.y, floor, parent.cost + 1, parent.identity, 3, false, false);
					newArea.parentArea = parent;
					newArea.SetParentEdge (3);
					parent.SetChildEdge (1, roomIDNUM);
					floorRooms.Add (newArea);
					areaQue.Enqueue (newArea);
					areaDictionary.Add (roomIDNUM, newArea);
					floorBudget = floorBudget + parent.cost + 1;
					roomIDNUM++;
					testedDir.Clear ();
				}
				break;
			case 2:
				testedDir.Add (2);
				if (InRange (parent.x, parent.y - 1, floor) && regionMap [parent.x, parent.y - 1, floor] == 0) {
					regionMap [parent.x, parent.y - 1, floor] = 1;
					Area newArea = new Area (roomIDNUM, roomSize, parent.x, parent.y - 1, floor, parent.cost + 1, parent.identity, 0, false, false);
					newArea.parentArea = parent;
					newArea.SetParentEdge (0);
					parent.SetChildEdge (2, roomIDNUM);
					floorRooms.Add (newArea);
					areaQue.Enqueue (newArea);
					areaDictionary.Add (roomIDNUM, newArea);
					floorBudget = floorBudget + parent.cost + 1;
					roomIDNUM++;
					testedDir.Clear ();
				}
				break;
			case 3:
				testedDir.Add (3);
				if (InRange (parent.x - 1, parent.y, floor) && regionMap [parent.x - 1, parent.y, floor] == 0) {
					regionMap [parent.x - 1, parent.y, floor] = 1;
					Area newArea = new Area (roomIDNUM, roomSize, parent.x - 1, parent.y, floor, parent.cost + 1, parent.identity, 1, false, false);
					newArea.parentArea = parent;
					newArea.SetParentEdge (1);
					parent.SetChildEdge (3, roomIDNUM);
					floorRooms.Add (newArea);
					areaQue.Enqueue (newArea);
					areaDictionary.Add (roomIDNUM, newArea);
					floorBudget = floorBudget + parent.cost + 1;
					roomIDNUM++;
					testedDir.Clear ();
				}
				break;
			}

			if(testedDir.Count >= 4){
				DeadRooms.Add(parent);
				testedDir.Clear ();
			}

			if (DeadRooms.Equals(floorRooms)) {
				break;
			}
			if (floorBudget > regionBudget) {
				floorComplete = true;

			}
		}
		Area returnRoom = CreateDownStair (floorRooms, floor);
		return returnRoom;
	}
	/// <summary>
	/// Creates down stair on available .
	/// </summary>
	/// <param name="floorList">Floor list.</param>
	/// <param name="floor">Floor.</param>
	Area CreateDownStair (List<Area> floorList, int floor){
		Area connectingArea = floorList[0];
		List<Area> checkedAreas = new List<Area> ();
		bool stairCreated = false;
		while (stairCreated) {
			foreach (Area area in floorList) {
				if (connectingArea.cost > area.cost && !checkedAreas.Contains(area)) {
					connectingArea = area;
				}
			}
			checkedAreas.Add (connectingArea);
			List<int> checkedDirection = new List<int> ();
			int testDir = Random.Range (0, 4);
			bool locationValid = true;
			while (locationValid) {
				while (checkedDirection.Contains (testDir)) {
					if (checkedDirection.Count >= 4) {
						locationValid = false;
						break;
					}
					testDir = Random.Range (0, 4);
				}
				checkedDirection.Add (testDir);
				switch (testDir) {
				case 0:
					if (!checkedDirection.Contains(0) && InRange (connectingArea.x, connectingArea.y + 1, floor)) {
						if (regionMap [connectingArea.x, connectingArea.y + 1, floor] == 0) {
							regionMap [connectingArea.x, connectingArea.y + 1, floor] = 1;
							regionMap [connectingArea.x, connectingArea.y + 1, floor + 1] = 1;
							connectingArea.SetChildEdge (0, -1);
							connectingArea.end = true;
							endFace = 0;

						}
					}
					break;
				case 1:
					if (!checkedDirection.Contains(1) && InRange (connectingArea.x + 1, connectingArea.y, floor)) {
						if (regionMap [connectingArea.x + 1, connectingArea.y, floor] == 0) {
							regionMap [connectingArea.x, connectingArea.y + 1, floor] = 1;
							regionMap [connectingArea.x, connectingArea.y + 1, floor + 1] = 1;
							connectingArea.SetChildEdge (1, -1);
							connectingArea.end = true;
							endFace = 1;
						}
					}
					break;
				case 2:
					if (!checkedDirection.Contains(2) && InRange (connectingArea.x, connectingArea.y - 1, floor)) {
						if (regionMap [connectingArea.x, connectingArea.y - 1, floor] == 0) {
							regionMap [connectingArea.x, connectingArea.y + 1, floor] = 1;
							regionMap [connectingArea.x, connectingArea.y + 1, floor + 1] = 1;
							connectingArea.SetChildEdge (2, -1);
							connectingArea.end = true;
							endFace = 2;
						}
					}
					break;
				case 3:
					if (!checkedDirection.Contains(3) && InRange (connectingArea.x - 1, connectingArea.y, floor)) {
						if (regionMap [connectingArea.x - 1, connectingArea.y, floor] == 0) {
							regionMap [connectingArea.x, connectingArea.y + 1, floor] = 1;
							regionMap [connectingArea.x, connectingArea.y + 1, floor + 1] = 1;
							connectingArea.SetChildEdge (3, -1);
							connectingArea.end = true;
							endFace = 3;
						}
					}
					break;
				}
			}
		}
		return connectingArea;
	}
	//returns true if lacation is within region
	bool InRange(int x, int y, int z){
		return x >= 0 && x < regionWidth && y >= 0 && y < regionHeight && z >= 0 && z < regionFloors;
	}
	/// <summary>
	/// Returns the opposite side of the tile.
	/// </summary>
	/// <returns>The side.</returns>
	/// <param name="_side">Side.</param>
	int FlipSide(int _side){
		
		switch (_side) {
		case 0:
			return 2;
		case 1:
			return 3;
		case 2:
			return 0;
		case 3:
			return 1;
		}
		return 0;
	}
				
}
