using UnityEngine;
using System.Collections;

public class Edge {
	public int side;
	public int[] pattern;
	public Edge(int _side, int[]_pattern){
		pattern = _pattern;
		side = _side;
	}
}
