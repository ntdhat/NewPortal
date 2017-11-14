using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PortType {
	Odd,
	Even
}

public class Port : MonoBehaviour {
	private PortType _type;
	public PortType getType() { return _type; }
	public void setType(PortType value) { _type = value; }

	public int widthInTile;
	private float cellSize;

	// Use this for initialization
	void Start () {
//		GameObject go = GameObject.FindGameObjectWithTag ("GameController");
//		GameController gameController = go.GetComponent<GameController> ();
//		cellSize = gameController.GetGridCellSize ();
//		transform.localScale = new Vector3(cellSize * widthInTile, transform.localScale.y, transform.localScale.z);
	}
	
	// Update is called once per frame
	void Update () {
	}
}
