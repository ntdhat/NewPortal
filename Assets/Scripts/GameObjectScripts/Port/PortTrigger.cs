using UnityEngine;

public class PortTrigger : MonoBehaviour {
	private Port port;

	void Start () {
		port = gameObject.GetComponentInParent <Port> ();
	}

	void OnTriggerEnter2D (Collider2D other) {
		//Debug.Log ("Port triggered with: " + other.gameObject.tag);
		if (other.gameObject.tag == "Player") {
			CollideObject (other);
		}
	}

	void OnTriggerStay2D (Collider2D other) {
		if (other.gameObject.tag == "Player") {
			CollideObject (other);
		}
	}

	void OnTriggerExit2D (Collider2D other) {
		if (other.gameObject.tag == "Player") {
			EndCollideObject (other);
		}
	}

	void CollideObject (Collider2D other) {
		Vector3 portFace = transform.up;
		Vector3 portPos = transform.parent.position;

		PlayerController playerCtrl = other.GetComponent <PlayerController> ();
		Vector2 othVelocity = other.attachedRigidbody.velocity;

//		Vector2 tempOthMin = new Vector2 (other.bounds.min.x, other.bounds.min.y);
//		Vector2 tempOthMax = new Vector2 (other.bounds.max.x, other.bounds.max.y);
		Vector2 tempOthPos = new Vector2 (other.transform.position.x, other.transform.position.y);
//		Vector2 nextUpdateOthMin = tempOthMin + (othVelocity + Physics2D.gravity) * Time.fixedDeltaTime;
//		Vector2 nextUpdateOthMax= tempOthMax + (othVelocity + Physics2D.gravity) * Time.fixedDeltaTime;
		Vector2 nextUpdateOthPos = tempOthPos + (othVelocity + Physics2D.gravity) * Time.fixedDeltaTime;
		/*
		if (playerCtrl.CurrentState == PlayerState.HoverHorPort) {
			if (portFace == Vector3.up) {
				port.IsFree = portPos.y < nextUpdateOthMin.y;
			} else {
				port.IsFree = true;
			}
		} else if (playerCtrl.CurrentState == PlayerState.HoverVerPort) {
			port.IsFree = true;
		} else if (playerCtrl.CurrentState == PlayerState.EnterHorPort ||
			playerCtrl.CurrentState == PlayerState.EnterVerPort ||
			playerCtrl.CurrentState == PlayerState.ExitHorPort ||
			playerCtrl.CurrentState == PlayerState.ExitVerPort) {
			port.IsFree = false;
		} else {
			port.IsFree = true;
		}*/

		if (playerCtrl.CurrentState == PlayerState.HoverHorPort) {
			port.IsFree = true;
		} else if (playerCtrl.CurrentState == PlayerState.HoverVerPort) {
			port.IsFree = true;
		} else if (playerCtrl.CurrentState == PlayerState.EnterHorPort) {
			if (portFace == Vector3.up) {
				port.IsFree = portPos.y < nextUpdateOthPos.y;
			} else {
				port.IsFree = true;
			}			
		} else if (playerCtrl.CurrentState == PlayerState.ExitHorPort) {
			if (portFace == Vector3.up) {
				port.IsFree = portPos.y < nextUpdateOthPos.y;
			} else if (portFace == Vector3.down) {
				port.IsFree = portPos.y > nextUpdateOthPos.y;
			} else {
				port.IsFree = true;
			}	
		} else if ( playerCtrl.CurrentState == PlayerState.EnterVerPort ||
			playerCtrl.CurrentState == PlayerState.ExitVerPort) {
			port.IsFree = false;
		} else {
			port.IsFree = true;
		}
	}

	void EndCollideObject (Collider2D other) {
		port.IsFree = true;
	}
}
