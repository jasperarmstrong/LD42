using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
	public static GameManager instance;

	static int fpsAverage = 0;
	static int[] fpsSamples;
	static int fpsSampleCount = 30;
	static int fpsSampleIndex = 0;

	void Awake () {
		if (instance == null) {
			instance = this;

			fpsSamples = new int[fpsSampleCount];

			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
		}
	}

	void CalculateFPS() {
		fpsSamples[fpsSampleIndex] = Mathf.RoundToInt(1.0f / Time.deltaTime);
		fpsSampleIndex++;
		if (fpsSampleIndex > (fpsSampleCount - 1)) fpsSampleIndex = 0;

		fpsAverage = Mathf.RoundToInt((float)fpsSamples.Average());
		DebugInfo.Add($"{fpsAverage} FPS");
	}
	
	void Update () {
		#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.R)) {
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
		#endif

		if (Input.GetKeyDown(KeyCode.F3)) {
			GameObject debugInfoParent = DebugInfo.instance.transform.parent.gameObject;
			debugInfoParent.SetActive(!debugInfoParent.activeInHierarchy);
		}

		CalculateFPS();
	}
}
