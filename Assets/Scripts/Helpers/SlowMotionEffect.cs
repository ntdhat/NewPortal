using UnityEngine;

public class SlowMotionEffect : MonoBehaviour {
	public float slowDownFactor = 0.005f;

	private bool isPlaying = false;

	public void StartEffect () {
		if (isPlaying) {
			return;
		}

		Time.timeScale = slowDownFactor;
		Time.fixedDeltaTime = Time.timeScale * 0.02f;
		isPlaying = true;
	}

	public void EndEffect () {
		if (!isPlaying) {
			return;
		}

		Time.timeScale = 1f;
		Time.fixedDeltaTime = Time.fixedUnscaledDeltaTime;
		isPlaying = false;
	}
}
