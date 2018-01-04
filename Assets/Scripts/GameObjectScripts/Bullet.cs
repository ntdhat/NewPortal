using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;

public class Bullet : BaseGameObject {
	public float travelSpeed = 70;

	private static Tilemap tilemap;
	private float cellSize;
	private int portalGateWidthInTile;
	private Vector3 hitPoint;
	private Vector3 hitNormal;

	public class HitResult {
		public bool canCreatePort = false;
		public Vector3 portPosition = Vector3.zero;
		public Vector3 portNormal = Vector3.zero;
		public List<Vector3Int> tilesOccupied = new List<Vector3Int>();
	}

	new void Awake () {
		base.Awake ();
		tilemap = GameObject.FindGameObjectWithTag ("terrain_surfaces").GetComponent<Tilemap> ();
	}

	void Start() {
		cellSize = tilemap.cellSize.x;
		portalGateWidthInTile = gameController.portWidthInTile;
	}

	public void Reset() {
		
	}

	public void Fired(Vector3 fromPoint, Vector3[] toPoints, Vector3 normal) {
		if (toPoints.Length == 0)
			return;

		hitNormal = normal.normalized;
		hitPoint = toPoints [toPoints.Length - 1];

		transform.position = fromPoint;

		float travelDuration = TravelDistance (fromPoint, toPoints) / travelSpeed;

		Sequence seq = DOTween.Sequence();
		seq.Append (transform.DOPath (toPoints, travelDuration, PathType.Linear, PathMode.Ignore));
		seq.AppendCallback (() => EndMoving());
	}

	public static HitResult PredictHitResult (Vector3 fromPoint, Vector3[] toPoints, Vector3 normal) {
		HitResult result = new HitResult ();
		if (toPoints.Length == 0)
			return result;

		Vector3 hitNormal = normal.normalized;
		Vector3 hitPoint = toPoints [toPoints.Length - 1];

		GameController gameController = (GameController)GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();
		GameObject willReplacedPort = gameController.GetWillReplacedPort ();

		// Move the hit point slightly against the normal vector to ensure that it is located inside tile's edges.
		Vector3 safeHitPoint = hitPoint - (hitNormal * 0.01f);
		Vector3Int centerTilePosition = tilemap.WorldToCell (safeHitPoint);
		if (willReplacedPort == null || !IsTileValid (centerTilePosition) || IsTileOccupied (centerTilePosition, willReplacedPort)) {
			result.canCreatePort = false;
			return result;
		}

		Vector3Int face = new Vector3Int ((int)hitNormal.x, (int)hitNormal.y, (int)hitNormal.z);
		Vector3Int dominator = FindDominatorVector (safeHitPoint, hitNormal);

		List<Vector3Int> occupiedTiles = new List<Vector3Int> ();
		occupiedTiles.Add (centerTilePosition);

		int tilesNeedCheck = gameController.portWidthInTile - 1;
		int validTiles = 0;
		bool dominantSide = true, minorSide = true;
		Vector3 portPosition = tilemap.GetCellCenterWorld (centerTilePosition) + (hitNormal * tilemap.cellSize.x * 0.5f);

		for (int i = 1; validTiles < tilesNeedCheck; i++) {
			if (dominantSide) {
				Vector3Int main = centerTilePosition + (dominator * i);
				Vector3Int sub = centerTilePosition + face + (dominator * i);
				if (IsTileExistAt (sub) || !IsTileValid (main) || IsTileOccupied (main, willReplacedPort)) {
					dominantSide = false;
				} else {
					validTiles++;
					Vector3 temp = new Vector3(dominator.x * tilemap.cellSize.x, dominator.y * tilemap.cellSize.x, 0);
					portPosition += temp * 0.5f;
					occupiedTiles.Add (main);
				}
			}

			if (minorSide && validTiles < tilesNeedCheck) {
				Vector3Int main = centerTilePosition + (dominator * i * -1);
				Vector3Int sub = centerTilePosition + face + (dominator * i * -1);
				if (IsTileExistAt (sub) || !IsTileValid (main) || IsTileOccupied (main, willReplacedPort)) {
					minorSide = false;
				} else {
					validTiles++;
					Vector3 temp = new Vector3(dominator.x * tilemap.cellSize.x, dominator.y * tilemap.cellSize.x, 0);
					portPosition += (temp * -1) * 0.5f;
					occupiedTiles.Add (main);
				}
			}

			if (!dominantSide && !minorSide)
				break;
		}

		result.canCreatePort = (validTiles == tilesNeedCheck);
		if (result.canCreatePort) {
			result.portNormal = hitNormal;
			result.portPosition = portPosition;
			result.tilesOccupied = occupiedTiles;
		}

		return result;
	}

	private float TravelDistance (Vector3 fromPoint, Vector3[] toPoints) {
		float distance = 0f;
		distance += Vector3.Distance (toPoints[0], fromPoint);
		for (int i = 0; i < toPoints.Length - 1; i++) {
			distance += Vector3.Distance (toPoints[i + 1], toPoints[i]);
		}
		return distance;
	}

	public void EndMoving () {
		HitResult hitResult = GetHitResult ();
		if (hitResult.canCreatePort) {
			CreatePort (hitResult);
		}
		EndLifeTime ();
	}

	private HitResult GetHitResult () {
		HitResult result = new HitResult ();

		GameObject willReplacedPort = gameController.GetWillReplacedPort ();

		// Move the hit point slightly against the normal vector to ensure that it is located inside tile's edges.
		Vector3 safeHitPoint = hitPoint - (hitNormal * 0.01f);
		Vector3Int centerTilePosition = tilemap.WorldToCell (safeHitPoint);
		if (willReplacedPort == null || !IsTileValid (centerTilePosition) || IsTileOccupied (centerTilePosition, willReplacedPort)) {
			result.canCreatePort = false;
			return result;
		}

		Vector3Int face = new Vector3Int ((int)hitNormal.x, (int)hitNormal.y, (int)hitNormal.z);
		Vector3Int dominator = FindDominatorVector (safeHitPoint, hitNormal);

		List<Vector3Int> occupiedTiles = new List<Vector3Int> ();
		occupiedTiles.Add (centerTilePosition);

		int tilesNeedCheck = portalGateWidthInTile - 1;
		int validTiles = 0;
		bool dominantSide = true, minorSide = true;
		Vector3 portPosition = tilemap.GetCellCenterWorld (centerTilePosition) + (hitNormal * cellSize * 0.5f);

		for (int i = 1; validTiles < tilesNeedCheck; i++) {
			if (dominantSide) {
				Vector3Int main = centerTilePosition + (dominator * i);
				Vector3Int sub = centerTilePosition + face + (dominator * i);
				if (IsTileExistAt (sub) || !IsTileValid (main) || IsTileOccupied (main, willReplacedPort)) {
					dominantSide = false;
				} else {
					validTiles++;
					Vector3 temp = new Vector3(dominator.x * cellSize, dominator.y * cellSize, 0);
					portPosition += temp * 0.5f;
					occupiedTiles.Add (main);
				}
			}

			if (minorSide && validTiles < tilesNeedCheck) {
				Vector3Int main = centerTilePosition + (dominator * i * -1);
				Vector3Int sub = centerTilePosition + face + (dominator * i * -1);
				if (IsTileExistAt (sub) || !IsTileValid (main) || IsTileOccupied (main, willReplacedPort)) {
					minorSide = false;
				} else {
					validTiles++;
					Vector3 temp = new Vector3(dominator.x * cellSize, dominator.y * cellSize, 0);
					portPosition += (temp * -1) * 0.5f;
					occupiedTiles.Add (main);
				}
			}

			if (!dominantSide && !minorSide)
				break;
		}

		result.canCreatePort = (validTiles == tilesNeedCheck);
		if (result.canCreatePort) {
			result.portNormal = hitNormal;
			result.portPosition = portPosition;
			result.tilesOccupied = occupiedTiles;
		}

		return result;
	}

	static bool IsTileExistAt (Vector3Int pos) {
		TileBase tile = tilemap.GetTile (pos);
		return tile != null;
	}

	static bool IsTileValid (Vector3Int tilePos) {
		PlatformTile tile = (PlatformTile)tilemap.GetTile (tilePos);
		return tile != null && tile.type == PlatformType.Plain;
	}

	static bool IsTileOccupied (Vector3Int tilePos, GameObject otherPort) {
		if (!otherPort.activeSelf) {
			return false;
		}
		return otherPort.GetComponent<Port> ().IsOccupieTileAt (tilePos);
	}

	private static Vector3Int FindDominatorVector (Vector3 atPoint, Vector3 normal) {
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

	public void CreatePort (HitResult hitInfo) {
		GameObject aPort = gameController.CreatePort ();
		aPort.transform.position = hitInfo.portPosition;
		aPort.transform.up = hitInfo.portNormal;
		aPort.GetComponent<Port> ().TilesOccupied = hitInfo.tilesOccupied;
	}

	public void EndLifeTime() {
		gameController.ReleaseBullet (gameObject);
	}
}
