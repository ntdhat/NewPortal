using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamelogic.Extensions;

public class GameController : MonoBehaviour {
	public GameObject portPrefab;
	private GameObject[] ports;
	private int currentPortIndex;

	public GameObject bulletPrefab;
	private Pool<GameObject> bulletPool;

	public int portWidthInTile;

	private float _gridCellSize;
	public float GetGridCellSize()
	{
		return _gridCellSize;
	}

	// Use this for initialization
	void Awake () {
		InitPools ();
		InitPorts ();

		GridLayout grid = (GridLayout) GameObject.FindGameObjectWithTag ("Grid").GetComponent<GridLayout> ();
		_gridCellSize = grid.cellSize.x;
	}

	void InitPorts ()
	{
		currentPortIndex = 0;

		ports = new GameObject[2];

		GameObject port1 = Instantiate (portPrefab);
		Port portScript1 = port1.GetComponent<Port> ();
		portScript1.type = PortType.Even;
		port1.transform.Find ("Renderer").GetComponent<SpriteRenderer> ().color = new Color(0, 255, 0, 185);
		port1.SetActive (false);
		ports [0] = port1;

		GameObject port2 = Instantiate (portPrefab);
		Port portScript2 = port2.GetComponent<Port> ();
		portScript2.type = PortType.Odd;
		port2.transform.Find ("Renderer").GetComponent<SpriteRenderer> ().color = new Color(0, 0, 255, 185);
		port2.SetActive (false);
		ports [1] = port2;
	}

	void InitPools()
	{
		bulletPool = new Pool<GameObject> (10, 
			() => {
				// Create
				GameObject bulletClone = Instantiate (bulletPrefab);
				return bulletClone;
			},
			(bullet) =>  {
				// Destroy
			},
			(bullet) =>  {
				// Awake
				bullet.SetActive(true);
				Bullet bulletScript = bullet.GetComponent<Bullet> ();
				bulletScript.Reset ();
			},
			(bullet) =>  {
				// Set to sleep
				bullet.SetActive(false);
			});
	}

	public int GetActivePorts () {
		int count = 0;
		foreach (var item in ports) {
			if (item.activeSelf) {
				count++;
			}
		}
		return count;
	}

	public GameObject CreatePort() {		
		GameObject port = ports [currentPortIndex];
		port.GetComponent<Port> ().Reset ();

		if (!port.activeSelf) {
			port.SetActive (true);
		}

		if (GetActivePorts () == 2) {
			foreach (var item in ports) {
				item.GetComponent<Port> ().EnableTriggerCollider (true);
			}
		}

		CalculateNextPortIndex ();
		
		return port;
	}

	public GameObject GetWillReplacedPort () {
		GameObject oppPort = ports [1 - currentPortIndex];
		return oppPort.GetComponent <Port> ().IsFree ? oppPort : null;
	}

	public GameObject GetOppositePort (GameObject fromPort) {
		if (fromPort == ports [0])
			return ports [1];
		else
			return ports [0];
	}

	void CalculateNextPortIndex () {
		if (currentPortIndex >= 1)
			currentPortIndex = 0;
		else
			currentPortIndex++;
	}

	public GameObject GetBullet () {
		return bulletPool.GetNewObject ();
	}

	public void ReleaseBullet (GameObject bullet) {
		bulletPool.Release (bullet);
	}
}
