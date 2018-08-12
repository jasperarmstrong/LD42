using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameOverReasons {
	TOO_MANY_BOXES, LITTERING
}

public class GameManager : MonoBehaviour {
	public static GameManager instance;
	public static Player player;

	static float dtAverage = 0;
	static int fpsAverage = 0;
	static float[] dtSamples;
	static int fpsSampleCount = 30;
	static int fpsSampleIndex = 0;

	public static bool isPaused = false;
	public static bool gameOver = false;
	public static GameOverReasons gameOverReason;

	public static float boxVolume = 0;
	public static float maxBoxVolume = 200;

	public static int score = 0;
	public static int highScore = 0;
	static string highScoreString = "highScore";

	public static float mouseSensitivity;
	public static string mouseSensitivityString = "mouseSensitivity";

	static string whatToDoMessage = "Your roommate will just not stop ordering things online. Find ways to dispose of the boxes before your house is overrun.";

	static string gameOverMessage = "Game Over. The stress of having so much junk in your house has caused you to collapse. Press \"R\" to restart.";
	static string gameOverMessageLittering = "Game Over. You littered and got arrested. Press \"R\" to restart.";
	static string gameOverMessageUnknown = "Game Over. Unknown reason. Press \"R\" to restart.";

	static string pauseMenuTag = "PauseMenu";

	void Awake () {
		if (instance == null) {
			instance = this;

			dtSamples = new float[fpsSampleCount];

			highScore = PlayerPrefs.GetInt(highScoreString, 0);
			mouseSensitivity = PlayerPrefs.GetFloat(mouseSensitivityString, 1);

			Pause();

			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
		}
	}

	IEnumerator DoNextFrame(System.Action action) {
		yield return null;
		action?.Invoke();
	}

	void Reset() {
		gameOver = false;
		boxVolume = 0;
		score = 0;
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		StartCoroutine(DoNextFrame(UnPause));
	}

	void CalculateFPS() {
		dtSamples[fpsSampleIndex] = Time.unscaledDeltaTime;
		fpsSampleIndex++;
		if (fpsSampleIndex > (fpsSampleCount - 1)) fpsSampleIndex = 0;

		dtAverage = (float)dtSamples.Average();
		fpsAverage = Mathf.RoundToInt(1.0f / dtAverage);
		DebugInfo.Add($"{fpsAverage} FPS ({(dtAverage * 1000):0.00}ms)");
	}

	void CheckBoxVolume() {
		DebugInfo.Add($"boxVolume: {boxVolume:0.00} / {maxBoxVolume}m³");
		if (boxVolume > maxBoxVolume && !gameOver) {
			GameManager.GameOver(GameOverReasons.TOO_MANY_BOXES);
		}
	}

	public static void GameOver(GameOverReasons reason) {
		gameOver = true;
		gameOverReason = reason;

		if (score > highScore) {
			highScore = score;
			PlayerPrefs.SetInt(highScoreString, highScore);
		}

		Rigidbody rb = player.GetComponent<Rigidbody>();
		if (rb != null) {
			rb.constraints = RigidbodyConstraints.None;
			Vector2 vec = Random.insideUnitCircle;
			rb.AddForceAtPosition(new Vector3(vec.x, 0, vec.y) * 25, player.transform.position + Vector3.up * 1.8f);
		}
	}

	public void LockMouse() {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public void UnlockMouse() {
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void ToggleMouse() {
		if (Cursor.visible) {
			LockMouse();
		} else {
			UnlockMouse();
		}
	}

	GameObject pauseMenu;

	public void Pause() {
		Time.timeScale = 0;
		isPaused = true;
		UnlockMouse();
		
		if (pauseMenu == null) pauseMenu = GameObject.FindGameObjectWithTag(pauseMenuTag);
		pauseMenu?.SetActive(true);
	}

	public void UnPause() {
		Time.timeScale = 1;
		isPaused = false;
		LockMouse();

		if (pauseMenu == null) pauseMenu = GameObject.FindGameObjectWithTag(pauseMenuTag);
		pauseMenu?.SetActive(false);
		
	}

	public void TogglePause() {
		if (isPaused) UnPause();
		else Pause();
	}

	public void ChangeMouseSensitivity(float value) {
		mouseSensitivity = value;
		PlayerPrefs.SetFloat(mouseSensitivityString, mouseSensitivity);
	}

	public void ShowDebugMenu() {
		GameObject debugInfoParent = DebugInfo.instance.transform.parent.gameObject;
		debugInfoParent.SetActive(true);
	}

	public void HideDebugMenu() {
		GameObject debugInfoParent = DebugInfo.instance.transform.parent.gameObject;
		debugInfoParent.SetActive(false);
	}

	public void ToggleDebugMenu() {
		GameObject debugInfoParent = DebugInfo.instance.transform.parent.gameObject;
		debugInfoParent.SetActive(!debugInfoParent.activeInHierarchy);
	}

	public void Quit() {
		Application.Quit();
	}
	
	void Update () {
		#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.M)) ToggleMouse();
		if (Input.GetKeyDown(KeyCode.O)) ScreenCapture.CaptureScreenshot($"screenshot-{System.DateTime.Now.ToLongTimeString()}.png");
		#endif

		if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();

		if (Input.GetKeyDown(KeyCode.F3)) {
			ToggleDebugMenu();
		}

		CalculateFPS();
		CheckBoxVolume();

		#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.R)) {
		#else
		if (gameOver && Input.GetKeyDown(KeyCode.R)) {
		#endif
			Reset();
		}

		if (score > 0) {
			PlayerMessage.Add($"Score: {score} (Best: {highScore})");
		} else {
			PlayerMessage.Add(whatToDoMessage);
		}

		if (gameOver) DebugInfo.Add($"gameOver: {gameOver} ({gameOverReason})");
		else DebugInfo.Add($"gameOver: {gameOver}");

		if (gameOver) {
			switch (gameOverReason) {
				case GameOverReasons.TOO_MANY_BOXES:
					PlayerMessage.Add(gameOverMessage);
					break;
				case GameOverReasons.LITTERING:
					PlayerMessage.Add(gameOverMessageLittering);
					break;
				default:
					PlayerMessage.Add(gameOverMessageUnknown);
					break;
			}
			return;
		}
	}
}
