using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnControls;
using UnityEngine.EventSystems;
using Gamelogic.Extensions;
using System;
using UnityEngine.Tilemaps;

public class Gun : BaseGameObject {
	public Vector2 gunBarrel;
	public int maxReflectedCount;
	public SlowMotionEffect sloMoEffect;

	private GameObject terrainSurfaces;
	private PlayerController playerCtrl;
	private Touchpad touchpad;

	private LineRenderer shootingLine;
	private List<Vector3> shootingLinePoints;
	private int raycastLayerMask;
	private float raycastLength;
	private Vector2 normalOfFinalRaycastHit;
	private Rect screenRect;

	private bool isShooting;

	new void Awake () {
		base.Awake ();
		gunBarrel = Vector2.zero;
		touchpad = GameObject.FindGameObjectWithTag ("touchpad").GetComponent <Touchpad>();
	}

	// Use this for initialization
	void Start () {
		terrainSurfaces = GameObject.FindGameObjectWithTag ("terrain_surfaces");
		playerCtrl = gameObject.GetComponentInParent <PlayerController> ();

		InitializeShootingLine ();

		isShooting = false;
	}

	private void InitializeShootingLine () {
		SetupShootingLineRenderer ();
		SetupShootingRayCast ();
	}

	private void SetupShootingLineRenderer () {
		shootingLine = GetComponent<LineRenderer> ();
		shootingLinePoints = new List<Vector3>();
	}

	private void SetupShootingRayCast () {
		int defaultMask = 1 << 0;
		int terrainSurfacesMask = 1 << terrainSurfaces.layer;
		raycastLayerMask = (terrainSurfacesMask | defaultMask);

		Vector3 screenBL = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0));
		Vector3 screenTR = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
		raycastLength = Vector3.Distance(screenBL, screenTR);
		screenRect = new Rect (screenBL.x, screenBL.y, screenTR.x - screenBL.x, screenTR.y - screenBL.y);
	}

	private enum ShootingInputMethod
	{
		Joystick,
		Touchpad,
		Inactive
	}

	private ShootingInputMethod shootingInputMethod = ShootingInputMethod.Inactive;

	public Vector2 GetShootingAxis () {
		Vector2 shootingAxis = Vector2.zero;
		bool isTouchpadActivated = touchpad.IsCurrentlyTweaking;
		bool isJoystickActivated = CnInputManager.GetAxis ("ShootingX") != 0 || CnInputManager.GetAxis ("ShootingY") != 0;
		if (isTouchpadActivated && shootingInputMethod != ShootingInputMethod.Joystick) {
			shootingInputMethod = ShootingInputMethod.Touchpad;
			Vector2 touchpadWorldPos = Camera.main.ScreenToWorldPoint (touchpad.PointerPosition);
			Vector2 barrelWorldPos = transform.TransformPoint (gunBarrel);
			shootingAxis = touchpadWorldPos - barrelWorldPos;
		} else if (isJoystickActivated) {
			shootingInputMethod = ShootingInputMethod.Joystick;
			shootingAxis.x = CnInputManager.GetAxis ("ShootingX");
			shootingAxis.y = CnInputManager.GetAxis ("ShootingY");
		}
		return shootingAxis; 
	}

	// Update is called once per frame
	void Update () {
		RenderShootingLine ();
		MakeAShot ();
	}

	#region Shooting's Aiming Line
	private void RenderShootingLine () {
		if (ShouldRenderShootingLine ()) {
			EnableShootingLine (true);
			isShooting = true;
			if (gameObject.GetComponentInParent <Rigidbody2D>().velocity.magnitude > playerCtrl.speed * 1.5f) {
				sloMoEffect.StartEffect ();
			}
			CastShooting ();
		} else {
			EnableShootingLine (false);
			gameController.GetDumbPort ().SetActive (false);
		}
	}

	private bool ShouldRenderShootingLine() {
		Vector2 shootingAxis = GetShootingAxis ();
		return playerCtrl.CanAimAndShoot && (shootingAxis.x != 0 || shootingAxis.y != 0);
	}

	private void EnableShootingLine(bool enabled) {
		shootingLine.enabled = enabled;
	}

	private void CastShooting()  {
		if (!isShooting)
			return;

		ResetShootingLine ();

		Vector2 direction = GetShootingAxis ();
		Vector2 startPoint = transform.TransformPoint (new Vector3 (gunBarrel.x, gunBarrel.y, 0));

		DoRayCast (startPoint, direction);
		PredictShootResult ();
		EndCastShooting ();
	}

	private void ResetShootingLine() {
		shootingLinePoints.Clear ();
		shootingLinePoints.Add (transform.TransformPoint (new Vector3 (gunBarrel.x, gunBarrel.y, 0)));
	}

	private void DoRayCast(Vector2 startPoint, Vector2 direction) {
		RaycastHit2D hit = Physics2D.Raycast (startPoint, direction, raycastLength, raycastLayerMask);

		if (hit.collider == null || !screenRect.Contains (hit.point)) {
			shootingLinePoints.Add (ShootingLineIntersectScreenBounds (direction, startPoint));
			normalOfFinalRaycastHit = Vector2.zero;
		}
		else {
			shootingLinePoints.Add (hit.point);
			normalOfFinalRaycastHit = hit.normal;
			ProcessRaycastHit (hit, direction);
		}
	}

	private void PredictShootResult () {
		Bullet.HitResult hitPredictResult = Bullet.PredictHitResult (
			transform.TransformPoint (gunBarrel),
			shootingLinePoints.ToArray (),
			new Vector3(normalOfFinalRaycastHit.x, normalOfFinalRaycastHit.y, 0)
		);

		if (hitPredictResult.portPosition != Vector3.zero) {
			GameObject dumbPort = gameController.GetDumbPort ();
			dumbPort.SetActive (true);
			dumbPort.transform.position = hitPredictResult.portPosition;
			dumbPort.transform.up = hitPredictResult.portNormal;

			SpriteRenderer renderer = dumbPort.transform.Find ("Renderer").GetComponent<SpriteRenderer> ();
			if (hitPredictResult.canCreatePort) {
				Color tempColor = renderer.color;
				renderer.color = new Color (tempColor.r, tempColor.g, tempColor.b, 0.45f);
			} else {
				renderer.color = new Color(1, 0, 0, 0.8f);
			}
		} else {
			gameController.GetDumbPort ().SetActive (false);
		}
	}

	private void EndCastShooting () {
		shootingLine.positionCount = shootingLinePoints.Count;
		Vector3[] points = shootingLinePoints.ToArray ();
		shootingLine.SetPositions (points);
	}

	private void ProcessRaycastHit(RaycastHit2D hit, Vector2 direction) {
		if (!hit || !hit.collider)
			return;

		// Move the hit point slightly against the normal vector to ensure that it is located inside tile's edges.
		Vector2 safeHitPoint = hit.point - (hit.normal * 0.01f);
		PlatformTile tileAtHitPoint = (PlatformTile)GetTileAtWorldPoint (safeHitPoint);
		if (tileAtHitPoint == null || tileAtHitPoint.type != PlatformType.Reflective)
			return;
	
		// If it is, reflect the ray and continue casting
		if (shootingLinePoints.Count >= maxReflectedCount)
			return;

		Vector3 rv = Vector3.Reflect (direction, hit.normal);
		Vector2 reflectedDir = new Vector2 (rv.x, rv.y);
		// Move the hit point forward slightly along the relected vector to prevent collision of the raycast and the collider has just hit.
		Vector2 newRaycastStartPoint = hit.point + reflectedDir.normalized * 0.01f;

		DoRayCast(newRaycastStartPoint, reflectedDir);
	}

	private TileBase GetTileAtWorldPoint(Vector3 worldPoint) {
		Tilemap tilemap = terrainSurfaces.GetComponent<Tilemap> ();
		if (tilemap == null)
			return null;
		
		Vector3Int cellPosition = tilemap.WorldToCell (worldPoint);
		return tilemap.GetTile (cellPosition);
	}

	private Vector2 ShootingLineIntersectScreenBounds(Vector2 direction, Vector2 startPoint)
	{
		Vector2 shootingDir = direction.normalized;
		Vector2 tempEndPoint = startPoint + (shootingDir * 100);

		Vector2 screenBL = screenRect.min;
		Vector2 screenTR = screenRect.max;
		Vector2 screenTL = new Vector2 (screenRect.xMin, screenRect.yMax);
		Vector2 screenBR = new Vector2 (screenRect.xMax, screenRect.yMin);

		Vector2 lineSegmentsIntersectPoint;
		Vector2 ret = Vector2.zero;

		if (direction.x >= 0 && direction.y >= 0) {	// Check Top & Right edges
			if (MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenTL, screenTR, out lineSegmentsIntersectPoint)
				|| MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenBR, screenTR, out lineSegmentsIntersectPoint)) {
				ret = lineSegmentsIntersectPoint;
			}
		} else if (direction.x >= 0 && direction.y < 0) {	// Check Bottom & Right edges
			if (MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenBL, screenBR, out lineSegmentsIntersectPoint)
				|| MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenBR, screenTR, out lineSegmentsIntersectPoint)) {
				ret = lineSegmentsIntersectPoint;
			}
		} else if (direction.x < 0 && direction.y <= 0) {	// Check Bottom & Left edges
			if (MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenBL, screenBR, out lineSegmentsIntersectPoint)
				|| MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenBL, screenTL, out lineSegmentsIntersectPoint)) {
				ret = lineSegmentsIntersectPoint;
			}
		} else {	// Check Top & Left edges
			if (MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenBL, screenTL, out lineSegmentsIntersectPoint)
				|| MathHelper.LineSegmentsIntersect (startPoint, tempEndPoint, screenTL, screenTR, out lineSegmentsIntersectPoint)) {
				ret = lineSegmentsIntersectPoint;
			}
		}

		return ret;
	}
	#endregion

	#region Shooting
	private void MakeAShot() {
		if (!isShooting || !IsShootingBtnReleased ())
			return;

		sloMoEffect.EndEffect ();

		isShooting = false;
		shootingInputMethod = ShootingInputMethod.Inactive;
		gameController.GetDumbPort ().SetActive (false);

		if (playerCtrl.CanAimAndShoot) {
			try {
				GameObject bullet = gameController.GetBullet ();
				Bullet bulletScript = bullet.GetComponent<Bullet> ();
				bulletScript.Fired (
					transform.TransformPoint (gunBarrel),
					shootingLinePoints.ToArray (),
					new Vector3(normalOfFinalRaycastHit.x, normalOfFinalRaycastHit.y, 0)
				);
			} catch (InvalidOperationException ex) {
				Debug.Log (ex);
				return;
			}
		}
	}

	private bool IsShootingBtnReleased () {
		Vector2 shootingAxis = GetShootingAxis ();
		return isShooting && shootingAxis.x == 0 && shootingAxis.y == 0;
	}

	private Vector3[] TransfromWayPointsToWorldSpace(Vector3[] wayPoints) {
		Vector3[] result = new Vector3[wayPoints.Length];
		for (int i = 0; i < wayPoints.Length; i++) {
			result[i] = transform.TransformPoint (wayPoints[i]);
		}
		return result;
	}
	#endregion
}
