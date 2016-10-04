using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EdgeFinder : MonoBehaviour
{
	public struct Edge
	{
		public Vector3 v1;
		public Vector3 v2;

		public Edge(Vector3 v1, Vector3 v2)
		{
			if (v1.x < v2.x || (v1.x == v2.x && (v1.y < v2.y || (v1.y == v2.y && v1.z <= v2.z))))
			{
				this.v1 = v1;
				this.v2 = v2;
			}
			else
			{
				this.v1 = v2;
				this.v2 = v1;
			}
		}
	}

	void Start()
	{
		var mesh = this.GetComponent<MeshFilter>().mesh;

		var edges = GetMeshEdges(mesh);
		//for (int i = 0; i < edges.Length; i++)
		//{
		//    print(i + ": " + edges[i].v1 + ", " + edges[i].v2);
		//}
	}

	private Edge[] GetMeshEdges(Mesh mesh)
	{
		HashSet<Edge> edges = new HashSet<Edge>();

		for (int i = 0; i < mesh.triangles.Length; i += 3)
		{
			var v1 = mesh.vertices[mesh.triangles[i]];
			var v2 = mesh.vertices[mesh.triangles[i + 1]];
			var v3 = mesh.vertices[mesh.triangles[i + 2]];
			edges.Add(new Edge(v1, v2));
			edges.Add(new Edge(v1, v3));
			edges.Add(new Edge(v2, v3));
		}

		return edges.ToArray();
	}
}