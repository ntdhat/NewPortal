using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseGameObject : MonoBehaviour {

	protected GameController gameController;

	public void Awake () {
		gameController = (GameController)GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();
	}
}
