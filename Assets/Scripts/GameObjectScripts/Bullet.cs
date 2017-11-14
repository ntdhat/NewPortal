using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;

public enum BulletType {
	Zero,
	Odd,
	Even
}

public class Bullet : MonoBehaviour {
	private BulletType _type;
	public BulletType getType() { return _type; }
	public void setType(BulletType value) { _type = value; }

	public float travelSpeed = 70;
	public Func<GameObject, bool> releaseBackToPool;

	private GameObject _tilemapGO;
	private GameController _gameController;

	private Vector3[] _wayPoints;
	private Vector3 _hitNormalVector;
	private int _portalGateWidthInTile;

	void Start()
	{
		_tilemapGO = GameObject.FindGameObjectWithTag ("terrain_surfaces");
		_gameController = (GameController)GameObject.FindGameObjectWithTag ("GameController").GetComponent ("GameController");
		_portalGateWidthInTile = _gameController.GetPortWidthInTile ();
	}

	public void Reset()
	{
		_type = BulletType.Zero;
	}

	public void Fired(Vector3 fromPoint, Vector3[] toPoints, Vector3 hitNormal)
	{
		if (toPoints.Length == 0)
			return;

		_hitNormalVector = hitNormal;
		_wayPoints = toPoints;
		transform.position = fromPoint;

		Sequence seq = DOTween.Sequence();
		seq.Append (transform.DOPath (toPoints, TravelDistance (fromPoint, toPoints)/travelSpeed, PathType.Linear, PathMode.Ignore));
		seq.AppendCallback (() => EndMoving());
	}

	private float TravelDistance (Vector3 fromPoint, Vector3[] toPoints)
	{
		float distance = 0f;
		distance += Vector3.Distance (toPoints[0], fromPoint);
		for (int i = 0; i < toPoints.Length - 1; i++) {
			distance += Vector3.Distance (toPoints[i + 1], toPoints[i]);
		}
		return distance;
	}

	public void EndMoving ()
	{
		Vector3 placeToPutPort;
		if (IsBulletHitValidArea (out placeToPutPort)) {
			CreatePortalGate (placeToPutPort, _hitNormalVector.normalized);
		}
		EndLifeTime ();
	}

	private bool IsBulletHitValidArea (out Vector3 placeToPutPort)
	{
		placeToPutPort = Vector3.zero;

		Tilemap tilemap = _tilemapGO.GetComponent<Tilemap> ();
		
		Vector3 hitPoint = _wayPoints [_wayPoints.Length - 1];
		Vector3 hitNormal = _hitNormalVector.normalized;

		// Move the hit point slightly against the normal vector to ensure that it is located inside tile's edges.
		Vector3 safeHitPoint = hitPoint - (hitNormal * 0.01f);
		Vector3Int hitTilePosition = tilemap.WorldToCell (safeHitPoint);

		// Validate the tile at hit point
		PlatformTile hitTile = (PlatformTile)tilemap.GetTile (hitTilePosition);
		if (IsTileValid (hitTile)) {
			return IsAdjacentTilesValid (hitTilePosition, out placeToPutPort);
		} else 
			return false;
	}

	private bool IsTileValid (PlatformTile tile)
	{
		return tile != null && tile.type == PlatformType.Plain;
	}

	private Vector3Int FindDominatorVector (Vector3 atPoint, Vector3 normal)
	{
		Tilemap tilemap = _tilemapGO.GetComponent<Tilemap> ();
		Vector3Int tilePos = tilemap.WorldToCell (atPoint);
		Vector3Int dominator = Vector3Int.zero;
		if (normal == Vector3.up || normal == Vector3.down) {
			if (atPoint.x >= tilemap.GetCellCenterWorld (tilePos).x) {
				dominator = Vector3Int.right;
			} else {
				dominator = Vector3Int.left;
			}
		}
		else if (normal == Vector3.right || normal == Vector3.left) {
			if (atPoint.y >= tilemap.GetCellCenterWorld (tilePos).y) {
				dominator = Vector3Int.up;
			} else {
				dominator = Vector3Int.down;
			}
		}
		return dominator;
	}
		
	private bool IsAdjacentTilesValid (Vector3Int centerTile, out Vector3 placeToPutPort)
	{
		Tilemap tilemap = _tilemapGO.GetComponent<Tilemap> ();
		int tilesNeedCheck = _portalGateWidthInTile - 1;

		GameObject go = GameObject.FindGameObjectWithTag ("GameController");
		GameController gameController = go.GetComponent<GameController> ();
		float cellSize = gameController.GetGridCellSize ();

		Vector3 hitPoint = _wayPoints [_wayPoints.Length - 1];
		Vector3 hitNormal = _hitNormalVector.normalized;
		// Move the hit point slightly against the normal vector to ensure that it is located inside tile's edges.
		Vector3 safeHitPoint = hitPoint - (hitNormal * 0.01f);

		placeToPutPort = tilemap.CellToWorld (centerTile) + (hitNormal * cellSize * 0.5f);

		Vector3Int face = new Vector3Int ((int)hitNormal.x, (int)hitNormal.y, (int)hitNormal.z);
		Vector3Int dominator = FindDominatorVector (safeHitPoint, hitNormal);
	
		int validTiles = 0;
		bool positive = true, negative = true;

		for (int i = 1; validTiles < tilesNeedCheck; i++) {
			if (positive) {
				//Vector3Int pRU = new Vector3Int (cellPosition.x + (i * right), cellPosition.y + 1, cellPosition.z);
				PlatformTile tilePU = (PlatformTile)tilemap.GetTile (centerTile + face + (dominator * i));
				PlatformTile tileP = (PlatformTile)tilemap.GetTile (centerTile + (dominator * i));
				if (tilePU != null || !IsTileValid (tileP)) {
					positive = false;
				} else {
					validTiles++;
					Vector3 temp = new Vector3(dominator.x * cellSize, dominator.y * cellSize, 0);
					placeToPutPort += temp * 0.5f;
				}
			}

			if (negative && validTiles < tilesNeedCheck) {
				PlatformTile tileNU = (PlatformTile)tilemap.GetTile (centerTile + face + (dominator * i * -1));
				PlatformTile tileN = (PlatformTile)tilemap.GetTile (centerTile + (dominator * i * -1));
				if (tileNU != null || !IsTileValid (tileN)) {
					negative = false;
				} else {
					validTiles++;
					Vector3 temp = new Vector3(dominator.x * cellSize, dominator.y * cellSize, 0);
					placeToPutPort += (temp * -1) * 0.5f;
				}
			}
		}

		return validTiles == tilesNeedCheck;
	}

	public void CreatePortalGate (Vector3 atPosition, Vector3 faceTo)
	{
		GameObject aPort = _gameController.portPool.GetNewObject ();
		aPort.transform.position = atPosition;
		aPort.transform.up = faceTo;
	}

	public void EndLifeTime()
	{
		releaseBackToPool (gameObject);
	}
}
