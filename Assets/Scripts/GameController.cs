using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamelogic.Extensions;

public class GameController : MonoBehaviour {
	public GameObject portPrefab;
	public Pool<GameObject> portPool;

	private int _portWidthInTile;
	public int GetPortWidthInTile()
	{
		return _portWidthInTile;
	}

	private float _gridCellSize;
	public float GetGridCellSize()
	{
		return _gridCellSize;
	}

	// Use this for initialization
	void Awake () {
		InitPools ();

		GameObject go = GameObject.FindGameObjectWithTag ("Grid");
		GridLayout grid = (GridLayout) go.GetComponent<GridLayout> ();
		_gridCellSize = grid.cellSize.x;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void InitPools()
	{
		InitPortPool ();
	}

	void InitPortPool()
	{
		portPool = new Pool<GameObject> (3, 
			() => {
				// Create
				GameObject portClone = Instantiate (portPrefab);
				_portWidthInTile = portClone.GetComponent<Port> ().widthInTile;
				return portClone;
			},
			(port) => {
				// Destroy
			},
			(port) => {
				// Awake
				port.SetActive(true);
			},
			(port) => {
				// Set to sleep
				port.SetActive(false);
			});
	}
}
