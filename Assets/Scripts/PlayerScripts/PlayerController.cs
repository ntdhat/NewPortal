using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnControls;

public class PlayerController : MonoBehaviour {
	public float speed;

	private float prevHorizontalInput = 0f;
	private bool isFacedRight = true;

	private Rigidbody2D _rigidBody;
	private Renderer _renderer;

	// Use this for initialization
	void Awake () {
		_rigidBody = GetComponent<Rigidbody2D>();
		_renderer = GetComponent<Renderer> ();
	}
	
	// Update is called once per frame
	void Update () {
		ClampPositionToScreenBounds_ExceptBottom ();
		if (!IsInsideScreenBounds ()) {
			// Fell out of screen => Dead
			//WannaDie();
		}
	}

	void ClampPositionToScreenBounds_ExceptBottom(){
		Vector3 minScreenBounds = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0));
		Vector3 maxScreenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));

		transform.position = new Vector3(Mathf.Clamp(transform.position.x, minScreenBounds.x + 1, maxScreenBounds.x - 1),
			Mathf.Min(transform.position.y, maxScreenBounds.y - 1),
			transform.position.z);
	}

	bool IsInsideScreenBounds () {
		return _renderer.isVisible;
	}

	void WannaDie () {
		//Debug.Log ("Wanna Die");
	}

	private void FixedUpdate()
	{
		MakeMovement ();
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		Debug.Log ("Player collide with: " + other.gameObject.tag);
		if (other.gameObject.tag == "terrain_spike_tile") {
			WannaDie ();
		}
	}

	void MakeMovement(){
		float h = CnInputManager.GetAxis("Horizontal");

		// Facing Direction
		if ((h > 0 && !isFacedRight) || (h < 0 && isFacedRight))
			Flip ();

		// Movement
		if (IsMovable (h))
			_rigidBody.velocity = new Vector2 (Mathf.Sign (h) * speed, _rigidBody.velocity.y);
		else
			_rigidBody.velocity = new Vector2 (0, _rigidBody.velocity.y);

		prevHorizontalInput = h;
	}

	bool IsMovable (float value) {
		return ((value > 0 && value > prevHorizontalInput) ||
				(value < 0 && value < prevHorizontalInput) ||
				(value == prevHorizontalInput && value == 1) ||
				(value == prevHorizontalInput && value == -1));
	}

	void Flip(){
		isFacedRight = !isFacedRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
