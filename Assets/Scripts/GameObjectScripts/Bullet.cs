using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public enum BulletType {
	Zero,
	Odd,
	Even
}

public class Bullet : MonoBehaviour {
	public BulletType type;
	public float travelSpeed = 10;
	public Func<GameObject, bool> releaseBackToPool;

	public void Reset()
	{
		type = BulletType.Zero;
	}

	public void Fired(Vector3 fromPoint, Vector3[] toPoints)
	{
		if (toPoints.Length == 0)
			return;

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

	public void EndMoving()
	{
		Debug.Log ("Ended moving of bullet");
		if (ShouldCreatePortalGate ()) {
			CreatePortalGate ();
		}
		EndLifeTime ();
	}

	private bool ShouldCreatePortalGate()
	{
		bool retVal = false;

		return retVal;
	}

	public void CreatePortalGate()
	{

	}

	public void EndLifeTime()
	{
		releaseBackToPool (gameObject);
	}
}
