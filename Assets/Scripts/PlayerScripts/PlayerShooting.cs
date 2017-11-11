using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnControls;
using UnityEngine.EventSystems;

public class PlayerShooting : MonoBehaviour {

	private LineRenderer _shootingLine;
	private List<Vector3> _shootingLinePoints;
	private int _raycastLayerMask;
	private float _raycastLength;

	private Vector2 _prevAxis;

	public int maxReflectedCount;

	// Use this for initialization
	void Start () {
		InitializeShootingLine ();
		_prevAxis = Vector2.zero;
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
		Vector2 startPoint = transform.TransformPoint (Vector3.zero);

		CastShootingRay (startPoint, direction);
		EndCastShootingRay ();
	}

	private void ResetShootingLine()
	{
		_shootingLinePoints.Clear ();
		_shootingLinePoints.Add (Vector3.zero);
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
		if (!shouldShoot ())
			return;

		for (int i = 0; i < _shootingLinePoints.Count; i++) {
			Vector3 p = transform.TransformPoint (_shootingLinePoints [i]);
		}
	}

	private bool shouldShoot()
	{
		return _prevAxis != Vector2.zero && CnInputManager.GetAxis ("ShootingX") == 0 && CnInputManager.GetAxis ("ShootingY") == 0;
	}
	#endregion
}
