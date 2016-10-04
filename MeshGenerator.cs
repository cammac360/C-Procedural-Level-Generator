using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;


public class MeshGenerator : MonoBehaviour {

	public SquareGrid squareGrid;
	public MeshFilter walls;
	public MeshFilter cave;
	public MeshFilter ceiling;

	private float[] bounds = {0.0f,0.0f,0.0f,0.0f};

	float squareSize;
	public float wallHeight;
	public string seed;
	List<Vector3> vertices;
	List<int> triangles;
	List<Line> meshLines = new List<Line> ();
	HashSet<Line> staticEdges = new HashSet<Line>();
	HashSet<Vector3> staticVert = new HashSet<Vector3>();
	List<GameObject> WallDoodads = new List<GameObject> ();
	Area self;
	Dictionary<int,List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>> ();

	List<List<int>> outlines = new List<List<int>> ();
	HashSet<int> checkedVertices = new HashSet<int> ();

	public void GenerateMesh(int[,] map, float _squareSize, string s, Area _self){ 
		foreach(MeshCollider c in GetComponents<MeshCollider> ()) {
			Destroy (c);
		}
		foreach (GameObject del in WallDoodads) {
			Destroy (del);
		}
		wallHeight = 5;
		squareSize = _squareSize;
		self = _self;
		seed = s;
		triangleDictionary.Clear ();
		outlines.Clear ();
		checkedVertices.Clear ();

		squareGrid = new SquareGrid(map, squareSize);

		vertices = new List<Vector3>();
		triangles = new List<int>();

		for (int x = 0; x < squareGrid.squares.GetLength(0); x ++) {
			for (int y = 0; y < squareGrid.squares.GetLength(1); y ++) {
				TriangulateSquare(squareGrid.squares[x,y]);
			}
		}

		Mesh mesh = new Mesh();


		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		cave.mesh = mesh;

		int width = map.GetLength(0);
		int height = map.GetLength(1);
		Vector2 topLeft = new Vector2(-width/2,-height/2);
		Vector2 size = new Vector2(width,height);
		Vector2[] uvs = new Vector2[vertices.Count];
		for (int i = 0; i < uvs.Length; i++) {
			Vector2 pos = new Vector2(vertices[i].x, vertices[i].z);
			float tiling = 1;
			float percentX = Mathf.InverseLerp(topLeft.x,topLeft.x + size.x, pos.x) * tiling;
			float percentY = Mathf.InverseLerp(topLeft.y,topLeft.y + size.y, pos.y) * tiling;
			uvs[i] = new Vector2(percentX%1f, percentY%1f);
		}
		cave.mesh.uv = uvs;﻿
		GameObject floorObject = transform.FindChild ("Floor").gameObject;
		MeshCollider floorCollider = floorObject.AddComponent<MeshCollider> ();
		floorCollider.sharedMesh = mesh;
		CreateCeilingMesh (cave.mesh);
		CreateWallMesh ();

		cave.mesh.RecalculateNormals ();
		mesh.RecalculateNormals ();
		walls.mesh.RecalculateNormals ();
		List<int> theChildren = self.GetChildrenList();
		foreach(int child in theChildren){
			//Debug.Log(self.identity + " has a child at " + child);
		}
		//if(self.cost < 3)
		//CreateCeilingDoodads (ceiling.mesh);
	}

	struct Line{
		public Vector3 indexA;
		public Vector3 indexB;
		public Line(Vector3 a, Vector3 b){
			indexA = a;
			indexB = b;
		}

	}

	void CreateCeilingMesh(Mesh mesh){
		ceiling.mesh = mesh;
		ceiling.mesh.triangles = ceiling.mesh.triangles.Reverse ().ToArray ();
		ceiling.mesh.RecalculateNormals ();
	}
	bool IsEdgeWall(Vector3 pointA, Vector3 pointB){	
		//Make it so they equal a base standard size.
		if (pointA.z == bounds [0] && pointB.z == bounds [0] && (self.parentSide == 0 || self.IsChildEdge(0))) return true;
		if (pointA.x == bounds [1] && pointB.x == bounds [1] && (self.parentSide == 1 || self.IsChildEdge(1))) return true;
		if (pointA.z == bounds [2] && pointB.z == bounds [2] && (self.parentSide == 2 || self.IsChildEdge(2))) return true;
		if (pointA.x == bounds [3] && pointB.x == bounds [3] && (self.parentSide == 3 || self.IsChildEdge(3))) return true;

		if (pointA.z == bounds [0] && pointB.z == bounds [0] && (self.parentSide == 0 || self.IsChildEdge(0))) return true;
		if (pointA.x == bounds [1] && pointB.x == bounds [1] && (self.parentSide == 1 || self.IsChildEdge(1))) return true;
		if (pointA.z == bounds [2] && pointB.z == bounds [2] && (self.parentSide == 2 || self.IsChildEdge(2))) return true;
		if (pointA.x == bounds [3] && pointB.x == bounds [3] && (self.parentSide == 3 || self.IsChildEdge(3))) return true;

		return false;
	}
	void FindEdgeWall(){
		foreach (List<int> outline in outlines) {
			//Top
			for (int i = 0; i < outline.Count; i++) {
				if (vertices [outline [i]].z > bounds[0]) {
					bounds [0] = vertices [outline [i]].z;
				}
			//Right
				if (vertices [outline [i]].x > bounds[1]) {
					bounds [1] = vertices [outline [i]].x;
				}
			//Bottom
				if (vertices [outline [i]].z < bounds[2]) {
					bounds [2] = vertices [outline [i]].z;
				}

			//Left
			if (vertices [outline [i]].x < bounds[3]) {
					bounds [3] = vertices [outline [i]].x;
				}
			}
		}
		//Debug.Log ("Top: " + bounds [0] + " Right: " + bounds [1] + " Bottom: " + bounds [2] + " Left: " + bounds [3]);
		//Debug.Log ("Id: " + self.identity + " ParentSide: " + self.parentSide);
	}
	void CreateWallMesh() {
		CalculateMeshOutlines ();

		List<Vector3> wallVertices = new List<Vector3> ();
		List<int> wallTriangles = new List<int> ();
		Mesh wallMesh = new Mesh ();
		bool firstPass = true;
		float wallBottomPosition;
		foreach (List<int> outline in outlines) {
			bool flip = true;
			if (firstPass) {
				firstPass = false;
				flip = isClockwise (outline);
				FindEdgeWall ();
			}
			for (int wallH = 0; wallH < wallHeight; wallH++) {
				for (int i = 0; i < outline.Count - 1; i++) {
					int startIndex = wallVertices.Count;
					wallVertices.Add (vertices [outline [i]] + (Vector3.up * wallH)); // left
					wallVertices.Add (vertices [outline [i + 1]] + (Vector3.up * wallH)); // right
					wallVertices.Add (vertices [outline [i]] + (Vector3.up * (wallH + 1))); // top left
					wallVertices.Add (vertices [outline [i + 1]] + (Vector3.up * (wallH + 1))); // top right

					if (!IsEdgeWall(vertices [outline [i]],vertices [outline [i + 1]])) {
						if (!flip) {
							wallTriangles.Add (startIndex + 3);
							wallTriangles.Add (startIndex + 2);
							wallTriangles.Add (startIndex + 0);

							wallTriangles.Add (startIndex + 0);
							wallTriangles.Add (startIndex + 1);
							wallTriangles.Add (startIndex + 3);
						} else {
							wallTriangles.Add (startIndex + 0);
							wallTriangles.Add (startIndex + 2);
							wallTriangles.Add (startIndex + 3);

							wallTriangles.Add (startIndex + 3);
							wallTriangles.Add (startIndex + 1);
							wallTriangles.Add (startIndex + 0);
						}
					}
				}
			}
		}
		wallMesh.vertices = wallVertices.ToArray ();
		wallMesh.triangles = wallTriangles.ToArray ();
		walls.mesh.Clear ();
		walls.mesh = wallMesh;
		GameObject wallObject = transform.FindChild ("Walls").gameObject;
		MeshCollider wallCollider = wallObject.AddComponent<MeshCollider> ();
		wallCollider.sharedMesh = wallMesh;


		Stopwatch watch = new Stopwatch ();
		watch.Start ();
		Vector2 [] wallUvs = new Vector2[wallVertices.Count];
		/*
		for (int i = 0, y = walls.mesh.vertexCount; i < y; i=i+4) {
			wallUvs[i] = new Vector2(0,0);//bottom-left 
			wallUvs[i+1] = new Vector2(1f,0); //bottom-right
			wallUvs[i+2] =  new Vector2(0,1f);//top-left
			wallUvs[i+3] = new Vector2(1f,1f); //top-right
		}

		Vector2 topLeft = new Vector2(-width/2,-height/2);
		Vector2 size = new Vector2(width,height);
		Vector2[] uvs = new Vector2[vertices.Count];
		for (int i = 0; i < uvs.Length; i++) {
			Vector2 pos = new Vector2(vertices[i].x, vertices[i].z);
			float tiling = 1;
			float percentX = Mathf.InverseLerp(topLeft.x,topLeft.x + size.x, pos.x) * tiling;
			float percentY = Mathf.InverseLerp(topLeft.y,topLeft.y + size.y, pos.y) * tiling;
			uvs[i] = new Vector2(percentX%1f, percentY%1f);
		}
		*/
		for (int i = 0; i < wallVertices.Count; i++) {
			float x = wallVertices [i].x;
			if (i + 1 < wallVertices.Count && i > 1){
				if (x == wallVertices [i + 1].x && wallVertices [i - 1].x == x) {
					x = wallVertices [i].z;
				}
			}
			wallUvs[i] = new Vector2(x,wallVertices[i].y);

		}
		watch.Stop ();
		//UnityEngine.Debug.Log(self.identity + " took " + watch.ElapsedMilliseconds + " to set uv");
		walls.mesh.uv = wallUvs;
		walls.mesh.RecalculateNormals ();
		walls.mesh.RecalculateBounds ();
		walls.mesh.Optimize ();

		foreach (GameObject del in WallDoodads) {
			Destroy (del);
		}
		/*
		if (self.cost >= 3) {
			return;
		}
		Object[] wallRocks = Resources.LoadAll ("Cave_Rock/Wall_Rock");
		for (int i = 0, len = wallVertices.Count; i < len; i++) {
			if (Random.Range (0, 100) > 75) {
				
				//GameObject clone = Instantiate (obj, new Vector3 (wallVertices [i].x - (self.x)  , wallVertices [i].y * (self.floor + 1), wallVertices [i].z * (self.y + 1)), Random.rotation) as GameObject;
				if (!IsEdgeWall (wallVertices [i], wallVertices [i])) {
					GameObject clone = Instantiate (wallRocks[Random.Range(0,wallRocks.Length)], transform.TransformPoint (new Vector3 (wallVertices [i].x, wallVertices [i].y, wallVertices [i].z)), Random.rotation) as GameObject;
					//clone.transform.parent = this.transform;
					WallDoodads.Add (clone);
				}
				i += 2;
			}
		}
		*/
	
	}
	void CreateCeilingDoodads(Mesh doodadMesh){
		Object[] Rocks = Resources.LoadAll ("Cave_Rock/Ceiling_Rock");
		for (int i = 0; i < doodadMesh.vertices.Length; i++) {
			if (Random.Range (0, 100) > 90) {
				GameObject clone = Instantiate (Rocks[Random.Range(0,Rocks.Length)], transform.TransformPoint (doodadMesh.vertices[i].x, doodadMesh.vertices[i].y + 5f, doodadMesh.vertices[i].z), Random.rotation) as GameObject;
				//clone.transform.parent = this.gameObject.transform;
				WallDoodads.Add (clone);
			}
		}

	}
	/*
	void OnDrawGizmos() {
		foreach (List<int> outline in outlines) {
			float change = 0f;
			for (int i = 0; i < outline.Count; i++) {
				Gizmos.color = Color.Lerp (Color.white, col, change);
				Gizmos.DrawCube (vertices[outline [i]] , Vector3.one);
			}
		}
	}
	*/
	bool isClockwise(List<int> outline){
		float sum = 0;
		for (int i = 0; i < outline.Count; i++) {
			if (i == outline.Count - 1) {
				sum +=  (vertices[outline [0]].x - vertices[outline [i]].x)*(vertices[outline [i]].z + vertices[outline [0]].z);
			} else {
				sum += (vertices[outline [i+1]].x - vertices[outline [i]].x)*(vertices[outline [i]].z + vertices[outline [i+1]].z);
			}
		}
		return sum >= 0;
	}
	void TriangulateSquare(Square square) {
		switch (square.configuration) {
		case 0:
			break;

			// 1 points:
		case 1:
			MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
			break;
		case 2:
			MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
			break;
		case 4:
			MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
			break;
		case 8:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
			break;

			// 2 points:
		case 3:
			MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			break;
		case 6:
			MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
			break;
		case 9:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
			break;
		case 12:
			MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
			break;
		case 5:
			MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
			break;
		case 10:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
			break;

			// 3 point:
		case 7:
			MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			break;
		case 11:
			MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
			break;
		case 13:
			MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
			break;
		case 14:
			MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
			break;

			// 4 point:
		case 15:
			MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);

			break;
		}

	}

	void MeshFromPoints(params Node[] points) {
		AssignVertices(points);

		if (points.Length >= 3)
			CreateTriangle(points[0], points[1], points[2]);
		if (points.Length >= 4)
			CreateTriangle(points[0], points[2], points[3]);
		if (points.Length >= 5) 
			CreateTriangle(points[0], points[3], points[4]);
		if (points.Length >= 6)
			CreateTriangle(points[0], points[4], points[5]);

	}

	/// <summary>
	/// Assigns the vertices.
	/// </summary>
	/// <param name="points">Points.</param>
	void AssignVertices(Node[] points) {
		for (int i = 0; i < points.Length; i ++) {
			if (points[i].vertexIndex == -1) {
				points[i].vertexIndex = vertices.Count;
				vertices.Add(points[i].position);
			}
		}
	}

	/// <summary>
	/// Creates the triangle.
	/// </summary>
	/// <param name="a">The alpha component.</param>
	/// <param name="b">The blue component.</param>
	/// <param name="c">C.</param>
	void CreateTriangle(Node a, Node b, Node c) {
		triangles.Add(a.vertexIndex);
		triangles.Add(b.vertexIndex);
		triangles.Add(c.vertexIndex);

		Triangle triangle = new Triangle (a.vertexIndex, b.vertexIndex, c.vertexIndex);
		AddTriangleToDictionary (triangle.vertexIndexA, triangle);
		AddTriangleToDictionary (triangle.vertexIndexB, triangle);
		AddTriangleToDictionary (triangle.vertexIndexC, triangle);
	}

	/// <summary>
	/// Adds the triangle to dictionary.
	/// </summary>
	/// <param name="vertexIndexKey">Vertex index key.</param>
	/// <param name="triangle">Triangle.</param>
	void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle) {
		if (triangleDictionary.ContainsKey (vertexIndexKey)) {
			triangleDictionary [vertexIndexKey].Add (triangle);
		} else {
			List<Triangle> triangleList = new List<Triangle>();
			triangleList.Add(triangle);
			triangleDictionary.Add(vertexIndexKey, triangleList);
		}
	}

	/// <summary>
	/// Calculates the mesh outlines.
	/// </summary>
	void CalculateMeshOutlines() {
		for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex ++) {
			if (!checkedVertices.Contains(vertexIndex)) {
				int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
				if (newOutlineVertex != -1) {
					checkedVertices.Add(vertexIndex);

					List<int> newOutline = new List<int>();
					newOutline.Add(vertexIndex);
					outlines.Add(newOutline);
					FollowOutline(newOutlineVertex, outlines.Count-1);
					outlines[outlines.Count-1].Add(vertexIndex);
				}
			}
		}
		//SimplifyMeshOutlines ();
	}

	/// <summary>
	/// Simplifies the mesh outlines.
	/// </summary>
	void SimplifyMeshOutlines() {
		for (int outlineIndex = 0; outlineIndex < outlines.Count; outlineIndex ++) {
			List<int> simplifiedOutline = new List<int>();
			Vector3 dirOld = Vector3.zero;
			for (int i = 0; i < outlines[outlineIndex].Count; i ++) {
				Vector3 p1 = vertices[outlines[outlineIndex][i]];
				Vector3 p2 = vertices[outlines[outlineIndex][(i+1)%outlines[outlineIndex].Count]];
				Vector3 dir = p2 - p1;
				if (dir != dirOld) {
					dirOld = dir;
					simplifiedOutline.Add(outlines[outlineIndex][i]);
				}
			}
			outlines[outlineIndex] = simplifiedOutline;
		}
	}

	/// <summary>
	/// Follows the outline.
	/// </summary>
	/// <param name="vertexIndex">Vertex index.</param>
	/// <param name="outlineIndex">Outline index.</param>
	void FollowOutline(int vertexIndex, int outlineIndex) {
		outlines [outlineIndex].Add (vertexIndex);
		checkedVertices.Add (vertexIndex);
		int nextVertexIndex = GetConnectedOutlineVertex (vertexIndex);

		if (nextVertexIndex != -1) {
			FollowOutline(nextVertexIndex, outlineIndex);
		}
	}

	/// <summary>
	/// Gets the connected outline vertex.
	/// </summary>
	/// <returns>The connected outline vertex.</returns>
	/// <param name="vertexIndex">Vertex index.</param>
	int GetConnectedOutlineVertex(int vertexIndex) {
		List<Triangle> trianglesContainingVertex = triangleDictionary [vertexIndex];

		for (int i = 0; i < trianglesContainingVertex.Count; i ++) {
			Triangle triangle = trianglesContainingVertex[i];

			for (int j = 0; j < 3; j ++) {
				int vertexB = triangle[j];
				if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB)) {
					if (IsOutlineEdge(vertexIndex, vertexB)) {
						return vertexB;
					}
				}
			}
		}

		return -1;
	}

	/// <summary>
	/// Determines whether this instance is outline edge the specified vertexA vertexB.
	/// </summary>
	/// <returns><c>true</c> if this instance is outline edge the specified vertexA vertexB; otherwise, <c>false</c>.</returns>
	/// <param name="vertexA">Vertex a.</param>
	/// <param name="vertexB">Vertex b.</param>
	bool IsOutlineEdge(int vertexA, int vertexB) {
		List<Triangle> trianglesContainingVertexA = triangleDictionary [vertexA];
		int sharedTriangleCount = 0;

		for (int i = 0; i < trianglesContainingVertexA.Count; i ++) {
			if (trianglesContainingVertexA[i].Contains(vertexB)) {
				sharedTriangleCount ++;
				if (sharedTriangleCount > 1) {
					break;
				}
			}
		}
		return sharedTriangleCount == 1;
	}

	struct Triangle {
		public int vertexIndexA;
		public int vertexIndexB;
		public int vertexIndexC;
		int[] vertices;

		public Triangle (int a, int b, int c) {
			vertexIndexA = a;
			vertexIndexB = b;
			vertexIndexC = c;

			vertices = new int[3];
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
		}

		public int this[int i] {
			get {
				return vertices[i];
			}
		}


		public bool Contains(int vertexIndex) {
			return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
		}
	}

	public class SquareGrid {
		public Square[,] squares;

		public SquareGrid(int[,] map, float squareSize) {
			int nodeCountX = map.GetLength(0);
			int nodeCountY = map.GetLength(1);
			float mapWidth = nodeCountX * squareSize;
			float mapHeight = nodeCountY * squareSize;

			ControlNode[,] controlNodes = new ControlNode[nodeCountX,nodeCountY];

			for (int x = 0; x < nodeCountX; x ++) {
				for (int y = 0; y < nodeCountY; y ++) {
					Vector3 pos = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, 0, -mapHeight/2 + y * squareSize + squareSize/2);
					controlNodes[x,y] = new ControlNode(pos,map[x,y] == 1, squareSize);
				}
			}

			squares = new Square[nodeCountX -1,nodeCountY -1];
			for (int x = 0; x < nodeCountX-1; x ++) {
				for (int y = 0; y < nodeCountY-1; y ++) {
					squares[x,y] = new Square(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1,y], controlNodes[x,y]);
				}
			}

		}
	}

	public class Square {

		public ControlNode topLeft, topRight, bottomRight, bottomLeft;
		public Node centreTop, centreRight, centreBottom, centreLeft;
		public int configuration;

		public Square (ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
			topLeft = _topLeft;
			topRight = _topRight;
			bottomRight = _bottomRight;
			bottomLeft = _bottomLeft;

			centreTop = topLeft.right;
			centreRight = bottomRight.above;
			centreBottom = bottomLeft.right;
			centreLeft = bottomLeft.above;

			if (topLeft.active)
				configuration += 8;
			if (topRight.active)
				configuration += 4;
			if (bottomRight.active)
				configuration += 2;
			if (bottomLeft.active)
				configuration += 1;
		}

	}

	public class Node {
		public Vector3 position;
		public int vertexIndex = -1;

		public Node(Vector3 _pos) {
			position = _pos;
		}
	}

	public class ControlNode : Node {

		public bool active;
		public Node above, right;

		public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos) {
			active = _active;
			above = new Node(position + Vector3.forward * squareSize/2f);
			right = new Node(position + Vector3.right * squareSize/2f);
		}

	}

}