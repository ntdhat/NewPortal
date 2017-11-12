using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnControls;
using UnityEngine.EventSystems;
using Gamelogic.Extensions;
using System;

public class Gun : MonoBehaviour {
	public Vector2 gunBarrel;
	public int maxReflectedCount;
	public GameObject bulletPrefab;
	public Pool<GameObject> bulletPool;

	private LineRenderer _shootingLine;
	private List<Vector3> _shootingLinePoints;
	private int _raycastLayerMask;
	private float _raycastLength;

	private Vector2 _prevAxis;

	private int _shotCount;

	// Use this for initialization
	void Awake () {
		Initialize ();
	}

	private void Initialize()
	{
		InitializeShootingLine ();
		InitializeBulletPool ();

		_prevAxis = Vector2.zero;
		gunBarrel = Vector2.zero;
		_shotCount = 0;
	}

	private void InitializeShootingLine()
	{
		InitializeShootingLineRenderer ();
		InitializeShootingRayCast ();
	}

	private void InitializeShootingLineRenderer()
	{
		_shootingLine = GetComponent<LineRenderer> ();
		_shootingLinePoints = new List<Vector3>();
	}

	private void InitializeShootingRayCast()
	{
		int playerMask = 1 << gameObject.layer;	// layer mask of 'Player' layer
		int defaultIgnoreRaycastLayer = 1 << 2;	// layer mask of default 'Ignore Raycast' layer
		_raycastLayerMask = ~(playerMask | defaultIgnoreRaycastLayer); // ignore layers 'Player' and default 'Ignore Raycast' 

		Vector3 tempPoint1 = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0));
		Vector3 tempPoint2 = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
		_raycastLength = Vector3.Distance(tempPoint1, tempPoint2);
	}

	private void InitializeBulletPool()
	{
		bulletPool = new Pool<GameObject> (10, 
			() =>
			{
				// Create
				GameObject bulletClone = Instantiate (bulletPrefab);
				bulletClone.GetComponent<Bullet> ().releaseBackToPool = bullet => DoneAShot (bullet);
				return bulletClone;
			},

			(bullet) => 
			{
				// Destroy
				bullet.GetComponent<Bullet> ().releaseBackToPool = null;
				Destroy (bullet);
			},

			(bullet) => 
			{
				// Awake
				bullet.SetActive(true);

				Bullet bulletScript = bullet.GetComponent<Bullet> ();
				bulletScript.Reset ();
				bulletScript.type = _shotCount % 2 == 0 ? BulletType.Even : BulletType.Odd;

				_shotCount++;
			},

			(bullet) => 
			{
				// Set to sleep
				bullet.SetActive(false);
			});
	}

	// Update is called once per frame
	void Update () {
		RenderShootingLine ();
		MakeAShot ();
	}

	#region Shooting's Aiming Line
	private void RenderShootingLine()
	{
		if (ShouldRenderShootingLine ()) {
			EnableShootingLine (true);

			if (ShouldCastShooting ()) {
				_prevAxis = new Vector2 (CnInputManager.GetAxis ("ShootingX"), CnInputManager.GetAxis ("ShootingY"));
				StartCastShooting ();
			}
		} else {
			EnableShootingLine (false);
		}
	}

	private bool ShouldRenderShootingLine()
	{
		return CnInputManager.GetAxis ("ShootingX") != 0 || CnInputManager.GetAxis ("ShootingY") != 0;
	}

	private bool ShouldCastShooting()
	{
		return CnInputManager.GetAxis ("ShootingX") != _prevAxis.x || CnInputManager.GetAxis ("ShootingY") != _prevAxis.y;
	}

	private void EnableShootingLine(bool enabled)
	{
		_shootingLine.enabled = enabled;
	}

	private void StartCastShooting() 
	{
		ResetShootingLine ();

		Vector2 direction = new Vector2 (CnInputManager.GetAxis ("ShootingX"), CnInputManager.GetAxis ("ShootingY"));
		Vector2 startPoint = transform.TransformPoint (new Vector3 (gunBarrel.x, gunBarrel.y, 0));

		CastShootingRay (startPoint, direction);
		EndCastShootingRay ();
	}

	private void ResetShootingLine()
	{
		_shootingLinePoints.Clear ();
		_shootingLinePoints.Add (new Vector3 (gunBarrel.x, gunBarrel.y, 0));
	}

	private void CastShootingRay(Vector2 startPoint, Vector2 direction)
	{
		RaycastHit2D hit = Physics2D.Raycast (startPoint, direction, _raycastLength, _raycastLayerMask);
		if (hit.collider == null) {
			_shootingLinePoints.Add (transform.InverseTransformPoint (ShootingLineIntersectScreenBounds (direction, startPoint)));
		}
		else {
			_shootingLinePoints.Add (transform.InverseTransformPoint (hit.point));
			ProcessRaycastHit (hit, direction);
		}
	}

	private void ProcessRaycastHit(RaycastHit2D hit, Vector2 direction)
	{
		if (hit == null || hit.collider == null)
			return;

		// If the hit's collider is not reflective tile, end the ray cast
		if (hit.collider.gameObject.tag != "terrain_reflective_tile")
			return;
	
		// If it is, reflect the ray and continue casting
		if (_shootingLinePoints.Count >= maxReflectedCount)
			return;

		Vector3 rv = Vector3.Reflect (direction, hit.normal);
		Vector2 reflectedDir = new Vector2 (rv.x, rv.y);
		CastShootingRay(hit.point + reflectedDir.normalized * 0.01f, reflectedDir);
	}

	private void EndCastShootingRay()
	{
		_shootingLine.positionCount = _shootingLinePoints.Count;

		Vector3[] shootingLinePoints = _shootingLinePoints.ToArray ();
		_shootingLine.SetPositions (shootingLinePoints);
	}

	private Vector2 ShootingLineIntersectScreenBounds(Vector2 direction, Vector2 startPoint)
	{
		Vector2 shootingDir = direction.normalized;
		Vector2 tempEndPoint = startPoint + (shootingDir * 100);

		Vector3 tempPoint1 = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0));
		Vector3 tempPoint2 = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));

		Vector2 screenBL = new Vector2 (tempPoint1.x, tempPoint1.y);
		Vector2 screenTR = new Vector2 (tempPoint2.x, tempPoint2.y);
		Vector2 screenTL = new Vector2 (screenBL.x, screenTR.y);
		Vector2 screenBR = new Vector2 (screenTR.x, screenBL.y);

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
	private void MakeAShot()
	{
		if (!ShouldShoot ())
			return;
		//Debug.Log ("Make a shot!");

		_prevAxis = Vector2.zero;

		try {
			GameObject bullet = bulletPool.GetNewObject ();
			bullet.GetComponent<Bullet> ().Fired (transform.TransformPoint (gunBarrel), TransfromWayPointsToWorldSpace (_shootingLinePoints.ToArray ()));
		} catch (InvalidOperationException ex) {
			Debug.Log (ex);
			return;
		}
	}

	private bool ShouldShoot()
	{
		return _prevAxis != Vector2.zero && CnInputManager.GetAxis ("ShootingX") == 0 && CnInputManager.GetAxis ("ShootingY") == 0;
	}

	private Vector3[] TransfromWayPointsToWorldSpace(Vector3[] wayPoints)
	{
		Vector3[] result = new Vector3[wayPoints.Length];
		for (int i = 0; i < wayPoints.Length; i++) {
			result[i] = transform.TransformPoint (wayPoints[i]);
		}
		return result;
	}

	public bool DoneAShot(GameObject bulletDone)
	{
		bulletPool.Release (bulletDone);
		return true;
	}
	#endregion
}
