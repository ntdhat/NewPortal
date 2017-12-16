using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnControls;
using Gamelogic.Extensions;

public enum PlayerState
{
	Normal,
	HoverHorPort,
	HoverVerPort,
	EnterHorPort,
	EnterVerPort,
	ExitHorPort,
	ExitVerPort
}

public class PlayerController : BaseGameObject {
	public float speed;
	public float speedScale;
	public float maxSpeed;
	public float dragFactor;

	private float prevHorizontalInput = 0f;
	private Vector2 facing;

	private Gun gun;

	private Rigidbody2D rigidBody;
	private BoxCollider2D boxCollider;
	private SpriteRenderer spriteRenderer;
	private float originalGravityScale;

	private bool canAimAndShoot = true;
	public bool CanAimAndShoot {
		get { return canAimAndShoot; }
	}

	private StateMachine<PlayerState> stateMachine;
	public PlayerState CurrentState {
		get { return stateMachine.CurrentState; }
	}

	// Use this for initialization
	void Start () {
		gun = transform.Find ("Gun").GetComponent <Gun>();

		rigidBody = GetComponent<Rigidbody2D>();
		rigidBody.drag = 0f;
		boxCollider = GetComponent <BoxCollider2D> ();
		spriteRenderer = GetComponent<SpriteRenderer> ();
		originalGravityScale = rigidBody.gravityScale;
		facing = Vector2.right;

		InitStateMachine ();
	}

	void InitStateMachine () {
		stateMachine = new StateMachine<PlayerState>();

		// State Normal
		stateMachine.AddState(
			PlayerState.Normal,
			() => {
				canAimAndShoot = true;
				GetComponent <BoxCollider2D>().isTrigger = false;
				speedScale = 1f;
				rigidBody.gravityScale = originalGravityScale;
				spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
			});
		
		// State Hover Port
		stateMachine.AddState(
			PlayerState.HoverHorPort,
			() => {
				canAimAndShoot = true;
				GetComponent <BoxCollider2D>().isTrigger = true;
				speedScale = 1f;
				rigidBody.gravityScale = originalGravityScale;
				spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
			}
		);
		stateMachine.AddState(
			PlayerState.HoverVerPort,
			() => {
				Debug.Log ("State is HoverVerPort");
				canAimAndShoot = true;
				GetComponent <BoxCollider2D>().isTrigger = true;
				speedScale = 1f;
				rigidBody.gravityScale = 0f;
				spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
			}
		);

		// State Enter Port
		stateMachine.AddState(
			PlayerState.EnterHorPort,
			() => {
				canAimAndShoot = false;
				GetComponent <BoxCollider2D>().isTrigger = true;
				speedScale = 0f;
				rigidBody.gravityScale = originalGravityScale;
				spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			}
		);
		stateMachine.AddState(
			PlayerState.EnterVerPort,
			() => {
				canAimAndShoot = false;
				GetComponent <BoxCollider2D>().isTrigger = true;
				speedScale = 1f;
				rigidBody.gravityScale = 0f;
				spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			}
		);

		// State Exit Port
		stateMachine.AddState(
			PlayerState.ExitHorPort,
			() => {
				canAimAndShoot = false;
				GetComponent <BoxCollider2D>().isTrigger = true;
				speedScale = 0f;
				rigidBody.gravityScale = originalGravityScale;
				spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			}
		);
		stateMachine.AddState(
			PlayerState.ExitVerPort,
			() => {
				canAimAndShoot = false;
				GetComponent <BoxCollider2D>().isTrigger = true;
				speedScale = 0f;
				rigidBody.gravityScale = 0f;
				spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			}
		);

		stateMachine.CurrentState = PlayerState.Normal;
	}

	public void ChangeState(PlayerState newState) {
		// State patterns
		bool shouldChangeState = false;

		switch (newState) {
		case PlayerState.Normal:
			shouldChangeState =
				CurrentState == PlayerState.HoverHorPort ||
				CurrentState == PlayerState.HoverVerPort ||
				CurrentState == PlayerState.ExitHorPort ||
				CurrentState == PlayerState.ExitVerPort;
			break;
		case PlayerState.HoverHorPort:
			shouldChangeState = CurrentState == PlayerState.Normal;
			break;
		case PlayerState.HoverVerPort:
			shouldChangeState = CurrentState == PlayerState.Normal || CurrentState == PlayerState.EnterVerPort;
			break;
		case PlayerState.EnterHorPort:
			shouldChangeState = CurrentState == PlayerState.HoverHorPort || CurrentState == PlayerState.Normal;
			break;
		case PlayerState.EnterVerPort:
			shouldChangeState = CurrentState == PlayerState.HoverVerPort;
			break;
		case PlayerState.ExitHorPort:
			shouldChangeState = CurrentState == PlayerState.EnterHorPort || CurrentState == PlayerState.EnterVerPort;
			break;
		case PlayerState.ExitVerPort:
			shouldChangeState = CurrentState == PlayerState.EnterHorPort || CurrentState == PlayerState.EnterVerPort;
			break;
		default:
			break;
		}

		if (shouldChangeState)
			stateMachine.CurrentState = newState;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private void FixedUpdate() {
		MakeMovement ();
		ApplyDragging ();
		RefineVelocity ();
		ClampPositionToScreenBounds_ExceptBottom ();
	}

	#region Movement
	void MakeMovement () {
		if (CurrentState == PlayerState.EnterHorPort ||
			CurrentState == PlayerState.EnterVerPort ||
			CurrentState == PlayerState.ExitHorPort ||
			CurrentState == PlayerState.ExitVerPort)
			return;

		float movementX = CnInputManager.GetAxis("Horizontal");
		float shootingX = CnInputManager.GetAxis ("ShootingX");

		// Facing player to the movement direction or shooting direction (shooting direction is prefered)
		FaceTo (movementX, shootingX);

		if (ShouldMove (movementX)) {
			float velX = rigidBody.velocity.x;
			float newVelX = Mathf.Abs (velX) > speed ? velX : Mathf.Sign (movementX) * speed * speedScale;
			rigidBody.velocity = new Vector2 (newVelX, rigidBody.velocity.y);
			//_rigidBody.AddForce (new Vector2 (Mathf.Sign (movementX) * speed * 20 * speedScale, 0));
		}

		RememberCurrentMovementValue (movementX);
	}

	bool ShouldMove (float value) {
		return ((value != 0 && Mathf.Abs (value) > Mathf.Abs (prevHorizontalInput)) ||
			(Mathf.Approximately (value, prevHorizontalInput) && value == 1) ||
			(Mathf.Approximately (value, prevHorizontalInput) && value == -1));
	}

	void FaceTo (float movementX, float shootingX) {
		if (movementX == 0 && shootingX == 0)
			return;

		Vector2 newFacing = new Vector2 (shootingX != 0 ? shootingX : movementX, 0);
		newFacing.Normalize ();
		if (facing != newFacing) {
			Vector3 theScale = transform.localScale;
			theScale.x *= -1;
			transform.localScale = theScale;

			facing = newFacing;
		}
	}

	void RememberCurrentMovementValue (float value) {
		prevHorizontalInput = value;
	}

	void ApplyDragging () {
		float tempVal = rigidBody.velocity.x;
		rigidBody.velocity = new Vector2 (tempVal - (tempVal * dragFactor * Time.timeScale), rigidBody.velocity.y);
	}

	void RefineVelocity () {
		if (rigidBody.velocity.magnitude > maxSpeed) {
			rigidBody.velocity = rigidBody.velocity.normalized * maxSpeed;
		}
	}
	#endregion

	#region Collision
	void OnCollisionEnter2D (Collision2D other) {
		if (other.gameObject.tag == "terrain_base") {
			rigidBody.velocity = new Vector2 (0, rigidBody.velocity.y);
		}
	}

	void OnCollisionStay2D (Collision2D other) {
		if (other.gameObject.tag == "terrain_base") {
			rigidBody.velocity = new Vector2 (0, rigidBody.velocity.y);
		}
	}

	public void ShippedToPort (GameObject destPort) {
		ChangeState (destPort.transform.up == Vector3.up || destPort.transform.up == Vector3.down ? PlayerState.ExitHorPort : PlayerState.ExitVerPort);
	}

	void OnTriggerEnter2D (Collider2D other) {
		//Debug.Log ("Port triggered with: " + other.gameObject.tag);
		if (other.gameObject.tag == "Port") {
			CollidePort (other);
		}
	}

	void OnTriggerStay2D (Collider2D other) {
		if (other.gameObject.tag == "Port") {
			CollidePort (other);
		}
	}

	void OnTriggerExit2D (Collider2D other) {
		if (other.gameObject.tag == "Port") {
			EndCollidePort (other);
		}
	}

	void CollidePort (Collider2D port) {
		Vector3 portFace = port.gameObject.transform.up;
		if (portFace == Vector3.up) {
			CollidePortUp (port);
		} else if (portFace == Vector3.down) {
			CollidePortDown (port);
		} else {
			CollidePortVertical (port);
		}

		UpdateAimAndShootAvailability (port);
	}

	void CollidePortUp (Collider2D port) {
		// Go in
		if (CurrentState == PlayerState.Normal) {
			if (IsWithinPort (port) && IsHeadingToPort (port)) {
				ChangeState (PlayerState.HoverHorPort);
			}
		} else if (CurrentState == PlayerState.HoverHorPort) {
			if (IsWithinPort (port)) {
				if (IsReadyToEnterPort (port))
					ChangeState (PlayerState.EnterHorPort);
			} else {
				ChangeState (PlayerState.Normal);
			}
		} else if (CurrentState == PlayerState.EnterHorPort) {
			// Do nothing
		}
		// Go out
		else if (CurrentState == PlayerState.ExitHorPort) {
			if (DidGoOutPort (port))
				ChangeState (PlayerState.Normal);
		}
	}

	void CollidePortDown (Collider2D port) {
		if (CurrentState == PlayerState.ExitHorPort) {
			if (DidGoOutPort (port))
				ChangeState (PlayerState.Normal);
		} else {
			// Do nothing
		}
	}

	void CollidePortVertical (Collider2D port) {
		// Go in
		if (CurrentState == PlayerState.Normal) {
			if (IsWithinPort (port) && IsHeadingToPort (port) && IsAboutToEnterVerticalPort (port)) {
				ChangeState (PlayerState.HoverVerPort);
			}
		} else if (CurrentState == PlayerState.HoverVerPort) {
			if (IsWithinPort (port) && IsHeadingToPort (port)) {
				if (IsReadyToEnterPort (port)) {
					ChangeState (PlayerState.EnterVerPort);
				}
			} else {
				ChangeState (PlayerState.Normal);
			}
		} else if (CurrentState == PlayerState.EnterVerPort) {
			if (!IsReadyToEnterPort (port)) {
				ChangeState (PlayerState.HoverVerPort);
			}
		}
		// Go out
		else if (CurrentState == PlayerState.ExitVerPort) {
			if (DidGoOutPort (port)) {
				ChangeState (PlayerState.Normal);
			}
		}
	}

	void UpdateAimAndShootAvailability (Collider2D port) {
		Vector3 gunBarrelPos = gun.transform.TransformPoint (gun.gunBarrel);
		Vector3 portPos = port.transform.parent.position;
		Vector3 portFace = port.gameObject.transform.up;

		if (portFace == Vector3.up) {
			canAimAndShoot = gunBarrelPos.y > portPos.y;
		} else if (portFace == Vector3.down) {
			canAimAndShoot = gunBarrelPos.y < portPos.y;
		} else if (portFace == Vector3.right) {
			canAimAndShoot = gunBarrelPos.x > portPos.x;
		} else {
			canAimAndShoot = gunBarrelPos.x < portPos.x;
		}
	}

	void EndCollidePort (Collider2D port) {
		Vector3 portFace = port.gameObject.transform.up;
		//		if (playerController.CurrentState == PlayerState.ExitHorPort || playerController.CurrentState == PlayerState.ExitVerPort) {
		//			playerController.ChangeState (PlayerState.Normal);
		//		}

		if (portFace == Vector3.up) {
			if (CurrentState == PlayerState.EnterHorPort)
				Teleport (port.transform.parent.gameObject);
		} else if (portFace == Vector3.down) {
			// Do nothing
		} else {
			if (CurrentState == PlayerState.EnterVerPort)
				Teleport (port.transform.parent.gameObject);
		}
	}

	bool IsWithinPort (Collider2D port) {
		Vector3 portFace = port.gameObject.transform.up;
		if (portFace == Vector3.up || portFace == Vector3.down) {
			float playerLeft = boxCollider.bounds.min.x;
			float playerRight = boxCollider.bounds.max.x;
			return port.bounds.min.x < playerLeft && port.bounds.max.x > playerRight;
		}
		else {
			float playerBottom = boxCollider.bounds.min.y;
			float playerTop = boxCollider.bounds.max.y;
			return port.bounds.min.y < playerBottom && port.bounds.max.y > playerTop;
		}
	}

	bool IsHeadingToPort (Collider2D port) {
		Vector2 vel = rigidBody.velocity.normalized;
		Vector3 portFace = port.gameObject.transform.up;
		if (portFace == Vector3.up) {
			return Vector2.Dot (portFace, vel) <= 0;
		} else if (portFace == Vector3.down) {
			return false;
		} else if (portFace == Vector3.right) {
			return CnInputManager.GetAxis ("Horizontal") < 0;
		} else {
			return CnInputManager.GetAxis ("Horizontal") > 0;
		}
	}

	bool IsAboutToEnterVerticalPort (Collider2D port) {
		Vector3 portFace = port.gameObject.transform.up;
		if (portFace == Vector3.up || portFace == Vector3.down)
			return false;

		if (portFace == Vector3.right) {
			float playerLeft = boxCollider.bounds.min.x;
			float portLeft = port.bounds.min.x;
			float distance = playerLeft - portLeft;
			bool val = distance >= 0 && distance < 0.5f;
			Debug.Log ("IsAboutToEnterVerticalPort: " + val);
			return val;
		} else {
			float playerRight = boxCollider.bounds.max.x;
			float portRight = port.bounds.max.x;
			float distance = playerRight - portRight;
			return distance <= 0 && distance > -0.5f;
		}
	}

	bool IsReadyToEnterPort (Collider2D port) {
		Vector3 portFace = port.gameObject.transform.up;
		Vector3 portPos = port.transform.parent.position;
		if (portFace == Vector3.up) {
			float bottom = boxCollider.bounds.min.y;
			return bottom <= portPos.y;
		} else if (portFace == Vector3.down) {
			return false;
		} else if (portFace == Vector3.right) {
			float playerLeft = boxCollider.bounds.min.x;
			float portLeft = port.bounds.min.x;
			return playerLeft < portLeft;
		} else {
			float playerRight = boxCollider.bounds.max.x;
			float portRight = port.bounds.max.x;
			return playerRight > portRight;
		}
	}

	bool DidGoOutPort (Collider2D port) {
		Vector3 portFace = port.gameObject.transform.up;
		Vector3 portPos = port.transform.parent.position;
		if (portFace == Vector3.up) {
			float playerBottom = boxCollider.bounds.min.y;
			return playerBottom > portPos.y;
		} else if (portFace == Vector3.down) {
			float playerTop = boxCollider.bounds.max.y;
			return playerTop < portPos.y;
		} else if (portFace == Vector3.right) {
			float playerLeft = boxCollider.bounds.min.x;
			return playerLeft > portPos.x;
		} else {
			float playerRight = boxCollider.bounds.max.x;
			return playerRight < portPos.x;
		}
	}

	void Teleport (GameObject fromPort) {
		GameObject destPort = gameController.GetOppositePort (fromPort);

		Vector2 inVeloc;
		if (fromPort.transform.up == Vector3.up || fromPort.transform.up == Vector3.down) {
			inVeloc = new Vector2 (0f, rigidBody.velocity.y);
		} else {
			inVeloc = new Vector2 (rigidBody.velocity.x, 0f);
		}
		Debug.Log ("inVeloc.y: " + inVeloc.y + "; Mag.: " + inVeloc.magnitude);

		Vector2 tempOutVeloc = destPort.transform.up.normalized * inVeloc.magnitude;
		Vector2 outVeloc = tempOutVeloc;
		Vector3 outPos = destPort.transform.position;

		if (destPort.transform.up == Vector3.up) {
			//outPos.y -= boxCollider.size.y * 0.5f;
			float inOffset = fromPort.transform.position.y - rigidBody.position.y;
			outPos.y -= inOffset;

			float minOutVeloc = 22.5f;
			if (tempOutVeloc.magnitude < minOutVeloc) {
				outVeloc *= (minOutVeloc / tempOutVeloc.magnitude);
			} else {
				outVeloc += new Vector2 (0f, 1.3734f);
			}
		} else if (destPort.transform.up == Vector3.down) {
			outPos.y += boxCollider.size.y;
		} else {
			outPos.x += destPort.transform.up == Vector3.right ? -boxCollider.size.x : boxCollider.size.x;
			outPos.y += 0.12f;
			float minOutVeloc = speed * 1.6f;
			if (tempOutVeloc.magnitude < minOutVeloc) {
				outVeloc = tempOutVeloc * minOutVeloc / tempOutVeloc.magnitude;
			}
		}

		Debug.Log ("outVeloc.y: " + outVeloc.y + "; Mag.: " + outVeloc.magnitude);

		rigidBody.transform.position = outPos;
		rigidBody.velocity = outVeloc;

		ShippedToPort (destPort);
	}
	#endregion

	#region Others

	void ClampPositionToScreenBounds_ExceptBottom () {
		if (CurrentState != PlayerState.Normal)
			return;

		Vector3 minScreenBounds = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0));
		Vector3 maxScreenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));

		transform.position = new Vector3(Mathf.Clamp(transform.position.x, minScreenBounds.x + 1, maxScreenBounds.x - 1),
			Mathf.Min(transform.position.y, maxScreenBounds.y - 1),
			transform.position.z);
	}

	bool IsInsideScreenBounds () {
		return spriteRenderer.isVisible;
	}

	public void WannaDie () {
		Debug.Log ("Wanna Die");
	}
	#endregion
}
