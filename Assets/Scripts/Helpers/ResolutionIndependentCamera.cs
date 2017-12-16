using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResolutionIndependentCamera : MonoBehaviour {
	public int designResolutionWidth;
	public int designResolutionHeight;

	private float initialOrthoSize; 

	void Awake() {
		Camera cam = GetComponent<Camera> ();
		initialOrthoSize = cam.orthographicSize;
	}

	void Start() {
		ResolutionIndependent ();
	}

	void ResolutionIndependent () {

		float designAspect = (float)designResolutionWidth / (float)designResolutionHeight;

		Camera cam = GetComponent<Camera> ();
			
		if (cam.aspect < designAspect)
		{
			// respect width (modify default behavior)
			Camera.main.orthographicSize = initialOrthoSize * (designAspect / cam.aspect);
		}
		else
		{
			// respect height (change back to default behavior)
			Camera.main.orthographicSize = initialOrthoSize;
		}
	}
}
