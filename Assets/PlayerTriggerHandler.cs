using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTriggerHandler : MonoBehaviour {
	private PlayerController _playerController;

	// Use this for initialization
	void Start () {
		_playerController = gameObject.GetComponentInParent<PlayerController> ();
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		if (other.gameObject.tag == "terrain_spike_tile") {
			_playerController.WannaDie ();
		}
	}
}
