using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PortType {
	Odd,
	Even
}

public class Port : BaseGameObject {
	public PortType type;

	private List<Vector3Int> tilesOccupied = new List<Vector3Int> ();
	public List<Vector3Int> TilesOccupied {
		get { return tilesOccupied; }
		set	{ tilesOccupied = value; }
	}

	[SerializeField]
	private bool isFree = true;
	public bool IsFree {
		get { return isFree; }
		set	{ isFree = value; }
	}

//	public static string NameTriggerTall = "Trigger Tall";
//	public static string NameTriggerShort = "Trigger Short";
	public static string NameTrigger = "Trigger";

	// Use this for initialization
	void Start () {
		float cellSize = gameController.GetGridCellSize ();
		transform.localScale = new Vector3(cellSize * gameController.portWidthInTile, transform.localScale.y, transform.localScale.z);
	}
	
	// Update is called once per frame
	void Update () {
	}

	public void Reset () {
		EnableTriggerCollider (false);
		tilesOccupied.Clear ();
		isFree = true;
	}

	public void EnableTriggerCollider (bool enable) {
//		transform.Find ("Trigger Tall").gameObject.SetActive (enable);
//		transform.Find ("Trigger Short").gameObject.SetActive (enable);
		transform.Find ("Trigger").gameObject.SetActive (enable);
	}

	public bool IsOccupieTileAt (Vector3Int tilePos) {
		foreach (var item in tilesOccupied) {
			if (item == tilePos) {
				return true;
			}
		}
		return false;
	}

	public GameObject GetOppositePort () {
		return gameController.GetOppositePort (gameObject);
	}
}
