using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InteriorMapGenerator : MonoBehaviour {
	public int[,] map;
	Area self;
	int mapSize;
	public int numberOfRooms;
	Area testArea;
	public int mainHallSize;
	public int sideHallSize;
	public int divideAmount;
	public int minRoomSize;
	public int maxRoomSize;
	public int doorwaySize;

	/*
	// Use this for initialization
	void Start () {
		
		testArea = new Area (0, 50, 0, 0, 0, 0, 0, 2, true);
		testArea.SetChildEdge (1, 5);
		testArea.SetChildEdge (3, 2);
		testArea.SetChildEdge (0, 1);
		CreateMap (100, testArea, "bunny");
	}

	void OnDrawGizmos() {
		if (map != null) {
			for (int x = 0; x < mapSize; x ++) {
				for (int y = 0; y < mapSize; y ++) {
					Gizmos.color = (map[x,y] == 1)?Color.black:Color.white;
					if (map [x, y] == -2)
						Gizmos.color = Color.blue;
					if (map [x, y] == 9)
						Gizmos.color = Color.red;
					if (map [x, y] == 2)
						Gizmos.color = Color.green;
					if (map [x, y] == -3)
						Gizmos.color = Color.yellow;
					if (map [x, y] == -1)
						Gizmos.color = Color.cyan;
					if (map [x, y] == 4)
						Gizmos.color = Color.magenta;
					if (map [x, y] == 3)
						Gizmos.color = Color.yellow;
					Vector3 pos = new Vector3(-mapSize/2 + x + .5f,0, -mapSize/2 + y+.5f);
					Gizmos.DrawCube(pos,Vector3.one);
				}
			}
		}
	}
	*/

	// Update is called once per frame
	public void CreateMap(Area _self, int _mapSize, bool _start = false){
		mapSize = _mapSize;
		self = _self;
		//roomNum = _roomNum;
		map = new int[mapSize, mapSize];
		PrepareMap ();
		CreateMainHall ();
		DivideMap ();
		CreateRooms ();
		int[,] final = FinalizeMap(); 
		MeshGenerator mesh = GetComponent<MeshGenerator> ();
		mesh.GenerateMesh (final, 1f, "donkey", self);
	}

	int[,] FinalizeMap(){
		int[,] finalMap = new int[ mapSize, mapSize];
		for (int x = 0; x < mapSize; x++) {
			for (int y = 0; y < mapSize; y++) {
				if (map [x, y] <= 0 || map [x, y] == 9) {
					finalMap [x, y] = 0;
				} else {
					finalMap [x, y] = 1;
				}

			}
		}
		return finalMap;
	}

	void PrepareMap(){
		for(int x = 0; x < mapSize; x ++){
			for(int y = 0; y < mapSize; y ++){
				map [x, y] = 0;
			}
		}
		for (int x = 0; x < mapSize; x++) {
			map [x, 0] = 9;
			map [x, mapSize - 1] = 9;
		}
		for (int y = 0; y < mapSize; y++) {
			map [0,y] = 9;
			map [mapSize - 1, y] = 9;
		}
	}
	void CreateRooms(){
		List<List<Tiles>> possibleRoomAreas = GetAllAreas (0);
		int roomsCreated = 0;
		possibleRoomAreas.Sort ((a, b) => a.Count - b.Count);
		foreach (List<Tiles> room in possibleRoomAreas) {
			int minX = room[0].x, minY = room[0].y, maxX = room[0].x, maxY = room[0].y;
			foreach (Tiles tile in room) {
				if (tile.x < minX)
					minX = tile.x;
				if (tile.y < minY)
					minY = tile.y;
				if (tile.x > maxX)
					maxX = tile.x;
				if (tile.y > maxY)
					maxY = tile.y;
			}
			if (maxX - minX < minRoomSize || maxY - minY < minRoomSize)
				continue;
			MarkTilesAs (room, 3);
			CreateDoorway (room);
			roomsCreated++;
			if (roomsCreated >= numberOfRooms)
				break;
		}
	}
	void CreateDoorway(List<Tiles> room){
		List<Tiles> expansionTiles = new List<Tiles> ();
		int checkedTile = 0;
		while (checkedTile > -4 && expansionTiles.Count == 0) {
			checkedTile--;
			foreach (Tiles tile in room) {
				for (int checkX = tile.x - 1; checkX <= tile.x + 1; checkX++) {
					if (InRange (checkX, tile.y)) {
						if (map [checkX, tile.y] == checkedTile) {
							expansionTiles.Add (new Tiles(checkX,tile.y));
						}
					}
				}
				for (int checkY = tile.y - 1; checkY <= tile.y + 1; checkY++) {
					if (InRange (tile.x, checkY)) {
						if (map [tile.x, checkY] == checkedTile) {
							expansionTiles.Add (new Tiles(tile.x,checkY));
						}
					}
				}
			}
		}
		Tiles possibleTile = new Tiles(0,0);
		List<Tiles> doorTiles = new List<Tiles> ();
		bool doorFound = false;
		while(!doorFound){
			possibleTile = expansionTiles[Random.Range(0,expansionTiles.Count)];
			expansionTiles.Remove (possibleTile);
			//Check if tile can fit hall with edge nieghbors in cardinal directions
			for (int x = possibleTile.x - doorwaySize + 1; x <= possibleTile.x && !doorFound; x++) {
				if (x < 0) {
					doorTiles.Clear ();
					break;
				}
				if (map [x, possibleTile.y] != checkedTile || map[x-1,possibleTile.y] != checkedTile || map[x+1,possibleTile.y] != checkedTile) {
					doorTiles.Clear ();
					break;
				}
				doorTiles.Add (new Tiles (x, possibleTile.y));
				if (x == possibleTile.x) {
					doorFound = true;
				}
			}
			for (int x = possibleTile.x + doorwaySize - 1; x >= possibleTile.x && !doorFound; x--) {
				if (x >= mapSize) {
					doorTiles.Clear ();
					break;
				}
				if (map [x, possibleTile.y] != checkedTile || map[x-1,possibleTile.y] != checkedTile || map[x+1,possibleTile.y] != checkedTile) {
					doorTiles.Clear ();
					break;
				}
				doorTiles.Add (new Tiles (x, possibleTile.y));
				if (x == possibleTile.x) {
					doorFound = true;
				}
			}
			for (int y = possibleTile.y + doorwaySize - 1; y >= possibleTile.y && !doorFound; y--) {
				if (y >= mapSize) {
					doorTiles.Clear ();
					break;
				}
				if (map [possibleTile.x, y] != checkedTile || map[possibleTile.x, possibleTile.y - 1] != checkedTile || map[possibleTile.x, possibleTile.y + 1] != checkedTile) {
					doorTiles.Clear ();
					break;
				}
				doorTiles.Add (new Tiles (possibleTile.x, y));
				if (y == possibleTile.y) {
					doorFound = true;
				}
			}
			for (int y = possibleTile.y - doorwaySize + 1; y <= possibleTile.y && !doorFound; y++) {
				if (y < 0) {
					doorTiles.Clear ();
					break;
				}
				if (map [possibleTile.x, y] != checkedTile || map[possibleTile.x, possibleTile.y - 1] != checkedTile || map[possibleTile.x, possibleTile.y + 1] != checkedTile) {
					doorTiles.Clear ();
					break;
				}
				doorTiles.Add (new Tiles (possibleTile.x, y));
				if (y == possibleTile.y) {
					doorFound = true;
				}
			}
			//Verify Door is not on edge
		}
		MarkTilesAs (doorTiles, 4);
	}
	void DivideMap(){
		//Gather all areas and make halls until the entire map has been segmented.
		List<List<Tiles>> allOpenAreas = GetAllAreas(0);
		for (int i = 0; i < divideAmount; i++) {
			allOpenAreas = GetAllAreas (0);

			foreach (List<Tiles> area in allOpenAreas) {
				if (area.Count > maxRoomSize) {
					CreateDividingHall (area);
				}
			}
		}
	}
	void CreateDividingHall(List<Tiles> hallArea){
		//Initilize everything for looping and determin if halls have allready been created.
		List<Tiles> expansionTiles = new List<Tiles> ();
		int checkedTile = 0;
		while (checkedTile > -4 && expansionTiles.Count == 0) {
			checkedTile--;
			foreach (Tiles tile in hallArea) {
				for (int checkX = tile.x - 1; checkX <= tile.x + 1; checkX++) {
					if (InRange (checkX, tile.y)) {
						if (map [checkX, tile.y] == checkedTile) {
							expansionTiles.Add (new Tiles(checkX,tile.y));
						}
					}
				}
				for (int checkY = tile.y - 1; checkY <= tile.y + 1; checkY++) {
					if (InRange (tile.x, checkY)) {
						if (map [tile.x, checkY] == checkedTile) {
							expansionTiles.Add (new Tiles(tile.x,checkY));
						}
					}
				}
			}
		}
		if (checkedTile == -4 && expansionTiles.Count == 0) {
			//If no halls can be created Mark as unusable
			MarkTilesAs (hallArea, -9);
			return;
		}

		//Find RandomTile
		Tiles possibleTile = new Tiles(0,0);
		List<Tiles> pathTiles = new List<Tiles> ();
		bool startFound = false;
		while(!startFound){
			possibleTile = expansionTiles[Random.Range(0,expansionTiles.Count)];
			expansionTiles.Remove (possibleTile);
		//Check if tile can fit hall with edge nieghbors in cardinal directions
			for (int x = possibleTile.x - sideHallSize + 1; x <= possibleTile.x && !startFound; x++) {
				if (x < 0) {
					pathTiles.Clear ();
					break;
				}
				if (map [x, possibleTile.y] != checkedTile || map[x-1,possibleTile.y] != checkedTile || map[x+1,possibleTile.y] != checkedTile) {
					pathTiles.Clear ();
					break;
				}
				pathTiles.Add (new Tiles (x, possibleTile.y));
				if (x == possibleTile.x) {
					startFound = true;
				}
			}
			for (int x = possibleTile.x + sideHallSize - 1; x >= possibleTile.x && !startFound; x--) {
				if (x >= mapSize) {
					pathTiles.Clear ();
					break;
				}
				if (map [x, possibleTile.y] != checkedTile || map[x-1,possibleTile.y] != checkedTile || map[x+1,possibleTile.y] != checkedTile) {
					pathTiles.Clear ();
					break;
				}
				pathTiles.Add (new Tiles (x, possibleTile.y));
				if (x == possibleTile.x) {
					startFound = true;
				}
			}
			for (int y = possibleTile.y + sideHallSize - 1; y >= possibleTile.y && !startFound; y--) {
				if (y >= mapSize) {
					pathTiles.Clear ();
					break;
				}
				if (map [possibleTile.x, y] != checkedTile || map[possibleTile.x, possibleTile.y - 1] != checkedTile || map[possibleTile.x, possibleTile.y + 1] != checkedTile) {
					pathTiles.Clear ();
					break;
				}
				pathTiles.Add (new Tiles (possibleTile.x, y));
				if (y == possibleTile.y) {
					startFound = true;
				}
			}
			for (int y = possibleTile.y - sideHallSize + 1; y <= possibleTile.y && !startFound; y++) {
				if (y < 0) {
					pathTiles.Clear ();
					break;
				}
				if (map [possibleTile.x, y] != checkedTile || map[possibleTile.x, possibleTile.y - 1] != checkedTile || map[possibleTile.x, possibleTile.y + 1] != checkedTile) {
					pathTiles.Clear ();
					break;
				}
				pathTiles.Add (new Tiles (possibleTile.x, y));
				if (y == possibleTile.y) {
					startFound = true;
				}
			}
		}
		DrawHall (pathTiles);
		//draw hall
	}
	void DrawHall(List<Tiles> startTiles){
		//determin direction
		bool[] directionFinder = {true, true, true, true};
		foreach (Tiles tile in startTiles) {
			if (map [tile.x + 1, tile.y] != 0) {
				directionFinder [1] = false;
			}
			if (map [tile.x - 1, tile.y] != 0) {
				directionFinder [3] = false;
			}
			if (map [tile.x, tile.y + 1] != 0) {
				directionFinder [0] = false;
			}
			if (map [tile.x, tile.y - 1] != 0) {
				directionFinder [2] = false;
			}
		}
		int direction = 99;
		for(int i = 0; i < 4 ; i++){
			if (directionFinder[i]) {
				direction = i;
			}
		}
		if (direction == 99) {
			return;
		}
		//determin max length
		int length = mapSize;
		switch (direction) {
		case 0:
			foreach (Tiles tile in startTiles) {
				bool endFound = false;
				for (int y = tile.y + 1; !endFound ; y++) {
					if (map [tile.x, y] > 0) {
						if (y - tile.y < length) {
							length = y - tile.y;
						}
						endFound = true;
					}
				}
			}
			break;
		case 1:	
			foreach (Tiles tile in startTiles) {
				bool endFound = false;
				for (int x = tile.x + 1; !endFound ; x++) {
					if (map [x, tile.y] > 0) {
						if (x - tile.x < length) {
							length = x - tile.x;
						}
						endFound = true;
					}
				}
			}
			break;
		case 2:
			foreach (Tiles tile in startTiles) {
				bool endFound = false;
				for (int y = tile.y - 1; !endFound ; y--) {
					if (map [tile.x, y] > 0) {
						if (tile.y - y < length) {
							length = tile.y - y ;
						}
						endFound = true;
					}
				}
			}
			break;
		case 3:
			foreach (Tiles tile in startTiles) {
				bool endFound = false;
				for (int x = tile.x - 1; !endFound ; x--) {
					if (map [x, tile.y] > 0) {
						if (tile.x - x < length) {
							length = tile.x - x;
						}
						endFound = true;
					}
				}
			}
			break;
		}
		length--;
		//Draw The Hall
		//Debug.Log("Drawing Hall: " + direction + " with Lenght: " + length);
		switch (direction) {
		case 0:
			foreach (Tiles tile in startTiles) {
				for (int y = tile.y; y <= tile.y + length ; y++) {
					map [tile.x, y] = 2;
				}
			}
			break;
		case 1:	
			foreach (Tiles tile in startTiles) {
				for (int x = tile.x; x <= tile.x + length ; x++) {
					map [x, tile.y] = 2;
				}
			}
			break;
		case 2:
			foreach (Tiles tile in startTiles) {
				for (int y = tile.y; y >= tile.y - length ; y--) {
					map [tile.x, y] = 2;
				}
			}
			break;
		case 3:
			foreach (Tiles tile in startTiles) {
				for (int x = tile.x; x >= tile.x - length ; x--) {
					map [x, tile.y] = 2;
				}
			}
			break;
		}
		MarkEdge (2, -1);
	}
	/// <summary>
	/// Gets the parent hall middle point.
	/// </summary>
	/// <returns>The parent hall middle point.</returns>
	int GetParentHallMidPoint(){
		Edge parentEdge = self.parentArea.GetEdge (FlipSide(self.parentSide));
		if (self.parentArea == null) {
			Debug.Log ("GetParentHallMidPoint was Null");
		}
		int midPoint = 0;

		for (int x = 0; x < mapSize; x++) {
			if (parentEdge.pattern [x] == 1) {
				midPoint = x + (mainHallSize/2);
				break;
			}
		}
		return midPoint;
	}
	void MarkTilesAs(List<Tiles> tilesList, int value){
		foreach (Tiles tile in tilesList) {
			map [tile.x, tile.y] = value;
		}

	}

	void CreateMainHall(){
		int maxX = 0, maxY = 0, minX = 0, minY = 0;
		int lengthFromParentEdge = Random.Range (10, mapSize - 10);
		int parentEdgeMidPoint;
		if (self.start) {
			parentEdgeMidPoint = mapSize / 2;
		} else {
			parentEdgeMidPoint = GetParentHallMidPoint ();
		}
		Debug.Log ("Parent Mid Point is: " + parentEdgeMidPoint);
		switch (self.parentSide) {
		case 0:
			for (int x = parentEdgeMidPoint - mainHallSize/2; x < parentEdgeMidPoint + mainHallSize/2; x++) {
				for (int y = mapSize - 1; y > mapSize - lengthFromParentEdge; y--) {
					map [x, y] = 1;
					maxX = x;
					minY = y;
				}
			}
			minX = maxX - mainHallSize;
			maxY = minY + mainHallSize;
			break;
		case 1:
			for (int y = parentEdgeMidPoint - mainHallSize/2; y < parentEdgeMidPoint + mainHallSize/2; y++) {
				for (int x = mapSize - 1; x > mapSize - lengthFromParentEdge; x--) {
					map [x, y] = 1;
					minX = x;
					maxY = y;
				}
			}
			maxX = minX + mainHallSize;
			minY = maxY - mainHallSize;
			break;
		case 2:
			for (int x = parentEdgeMidPoint - mainHallSize/2; x < parentEdgeMidPoint + mainHallSize/2; x++) {
				for (int y = 0; y < lengthFromParentEdge; y++) {
					map [x, y] = 1;
					maxX = x;
					maxY = y;
				}
			}
			minX = maxX - mainHallSize;
			minY = maxY - mainHallSize;
			break;
		case 3:
			for (int y = parentEdgeMidPoint - mainHallSize/2; y < parentEdgeMidPoint + mainHallSize/2; y++) {
				for (int x = 0; x < lengthFromParentEdge; x++) {
					map [x, y] = 1;
					maxX = x;
					maxY = y;
				}
			}
			minX = maxX - mainHallSize;
			minY = maxY - mainHallSize;
			break;
		}
		minX++;
		minY++;
		maxX++;
		maxY++; 
		if(self.IsChildEdge(0)){
			for (int x = minX; x < maxX; x++) {
				for (int y = minY; y < mapSize; y++) {
					map [x, y] = 1;
				}
			}
		}
		if(self.IsChildEdge(1)){
			for (int y = minY; y < maxY; y++) {
				for (int x = minX; x < mapSize; x++) {
					//Debug.Log (x + "," + y);
					map [x, y] = 1;
				}
			}
		}
		if(self.IsChildEdge(2)){
			for (int x = minX; x < maxX; x++) {
				for (int y = minY; y >= 0; y--) {
					map [x, y] = 1;
				}
			}
		}
		if(self.IsChildEdge(3)){
			for (int y = minY; y < maxY; y++) {
				for (int x = minX; x >= 0; x--) {
					map [x, y] = 1;
				}
			}
		}
		for (int x = 0; x < mapSize; x++) {
			for (int y = 0; y < mapSize; y++) {
				if (map [x, y] == 1) {
					for (int checkX = x - 1; checkX <= x + 1; checkX++) {
						for (int checkY = y - 1; checkY <= y + 1; checkY++) {
							if (InRange (checkX, checkY)) {
								if (map [checkX, checkY] == 0) {
									map [checkX, checkY] = -2;
								}
							}
						}
					}
				}
			}
		}
	}
	/// <summary>
	/// Marks the edge of a Hall.
	/// </summary>
	/// <param name="tileType">Tile type to be marked.</param>
	/// <param name="markAs">Mark the tile type with this mark.</param>
	void MarkEdge(int tileType, int markAs){
		for (int x = 0; x < mapSize; x++) {
			for (int y = 0; y < mapSize; y++) {
				if (map [x, y] == tileType) {
					for (int checkX = x - 1; checkX <= x + 1; checkX++) {
						if (InRange (checkX, y)) {
							if (map [checkX, y] == 0) {
								map [checkX, y] = markAs;
							}
						}
					}
					for (int checkY = y - 1; checkY <= y + 1; checkY++) {
						if (InRange (x, checkY)) {
							if (map [x, checkY] == 0) {
								map [x, checkY] = markAs;
							}
						}
					}
				}
			}
		}
	}
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

	void EnsureEdge(){
		if(!self.IsChildEdge(0)){
			for (int x = 0; x < mapSize; x++) {
				map [x, mapSize - 1] = 0;
			}
		}
		if(!self.IsChildEdge(1)){
			for (int y = 0; y < mapSize; y++) {
				map [mapSize - 1, y] = 0;
			}
		}
		if(!self.IsChildEdge(2)){
			for (int x = 0; x < mapSize; x++) {
				map [x, 0] = 0;
			}
		}
		if(!self.IsChildEdge(3)){
			for (int y = 0; y < mapSize; y++) {
				map [ 0, y] = 0;
			}
		}
	}
	struct Tiles{
		public int x;
		public int y;
		public Tiles(int sentX,int sentY){
			x = sentX;
			y = sentY;
		}
	}
	bool InRange(int x, int y){
		return x >= 0 && x < mapSize && y >= 0 && y < mapSize;
	}
	// Returns a list of all tiles of the given type starting at the given location
	List<Tiles> GetArea(int _x, int _y ){
		List<Tiles> roomTiles = new List<Tiles> ();
		int[,] checkedTiles = new int[mapSize, mapSize];
		int tileType = map [_x, _y];

		Queue<Tiles> pendingTiles = new Queue<Tiles> ();
		pendingTiles.Enqueue (new Tiles (_x, _y));
		checkedTiles [_x, _y] = 1;

		while (pendingTiles.Count > 0) {
			Tiles currentTile = pendingTiles.Dequeue ();
			roomTiles.Add (currentTile);
			for (int x = currentTile.x - 1; x <= currentTile.x + 1; x++) {
				for (int y = currentTile.y - 1; y <= currentTile.y + 1; y++) {
					if (InRange (x, y) && (y == currentTile.y || x == currentTile.x)) {
						if (checkedTiles [x, y] == 0 && map [x, y] == tileType) {
							checkedTiles [x, y] = 1;
							pendingTiles.Enqueue (new Tiles (x, y));
						}
					}
				}
			}
		}
		return roomTiles;
	}
	/// <summary>
	/// Gets all areas.
	/// </summary>
	/// <returns>The all areas.</returns>
	/// <param name="tileType">Tile type.</param>
	//Gets all seperate areas in the map to prepare for hall generation
	List<List<Tiles>> GetAllAreas(int tileType){
		List<List<Tiles>> areas = new List<List<Tiles>> ();
		int [,] checkedTile = new int[mapSize,mapSize];

		for(int x = 0; x < mapSize; x++){
			for(int y = 0; y < mapSize; y ++){
				if (checkedTile [x, y] == 0 && map [x, y] == tileType) {
					List<Tiles> newArea = GetArea (x, y);
					areas.Add (newArea);
					foreach (Tiles i in newArea) {
						checkedTile [i.x, i.y] = 1;
					}
				}					
			}
		}
		return areas;
	}

}
