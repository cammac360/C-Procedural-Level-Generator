using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Area{
	public int cost;
	public int x;
	public int y;
	public int floor;
	//Room number
	public int identity;
	public int parent;
	public bool start;
	public bool end;
	bool [] child = new bool[4];
	public int[,] map;
	public int size;
	public int parentSide;
	List<int> childList = new List<int> ();
	public NavMap navMap;
	public Area parentArea;
	public int areaType;
	//Stores the values about a room
	public Area(int _identity, int _size, int _x, int _y, int _floor, int _cost, int _parent, int _parentSide, bool _start, bool _end){
		cost = _cost;
		x = _x;
		y = _y;
		size = _size;
		//areaType = _type;
		end = _end;
		floor = _floor;
		parent = _parent;
		parentSide = _parentSide;
		identity = _identity;
		start = _start;
		child [0]= false;
		child [1]= false;
		child [2]= false;
		child [3]= false;
	}
	public void SetParentArea(Area parent){
		parentArea = parent;
	}
	public void SetChildEdge(int _side, int _childID){
		child [_side] = true;
		childList.Add (_childID);
	}
	public void SetParentEdge(int _side){
		child [_side] = true;
	}
	public bool IsChildEdge(int _side){
		if (_side >= child.Length|| _side < 0) {
			return false;
		}
		return child [_side];
	}

	public List<int> GetChildrenList(){
		return childList;
	}

	public Edge GetEdge(int _side){
		
		int[] edge = new int[size];
		switch(_side){
		case 0:
			for (int x = 0; x < size; x++) {
				edge[x] = (map [x, size - 1] == 1)? 1 : 0;
			}
			Edge request = new Edge (_side, edge);
			return request;
		case 1:
			for (int y = 0; y < size; y++) {
				edge[y] = (map [size - 1, y] == 1)? 1 : 0;
			}
			Edge request1 = new Edge (_side, edge);
			return request1;
		case 2:
			for(int x = 0; x < size; x++){
				edge[x] = (map [x, 0] == 1)? 1 : 0;
			}
			Edge request2 = new Edge (_side, edge);
			return request2;
		case 3:
			for (int y = 0; y < size; y++) {
				edge[y] = (map [0, y] == 1)? 1 : 0;
			}
			Edge request3 = new Edge (_side, edge);
			return request3;
		}
		Edge request4 = new Edge (_side, edge);
		return request4;
			
	}
	public void SetMap(int[,] _map){
		map = _map;
	}
	public int GetType(){
		return areaType;
	}

}
