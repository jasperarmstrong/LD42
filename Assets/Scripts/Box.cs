using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGrabbable {
	void OnGrab();
	void OnLetGo();
}

public class Box : MonoBehaviour, IGrabbable {
	static string TAG_GROUND = "Ground";

	static string[] itemNames = {
		"GameSphere",
		"GameSphere X",
		"XStation Pro",
		"Inspirational Cat Posters, 50ct",
		"Omazan Lexi",
		"Omazan Blaze Stick",
		"100lbs. of Rice",
		"15lbs. of Chocolate",
		"500 Assorted Cat Toys",
		"eyePhone X",
		"Raisin Cookies",
		"Too Many Boxes: GOTY Edition",
		"10 Collapsed Cardboard Boxes",
		"More Pens than You Will Ever Need",
		"A Hat for Your Cat",
		"12in. Gummy Bear",
		"Hamburger Phone",
	};

	public string contents;

	Transform groundTouching;
	public float volume = 0;
	static float timeToCallCopsForLittering = 10;
	float timeLittered = 0;

	public bool isHeld = false;
	public bool isFolded = false;

	void Start () {
		volume = transform.localScale.x * transform.localScale.x * transform.localScale.x;
		GameManager.boxVolume += volume;
		contents = itemNames[Random.Range(0, itemNames.Length)];
	}

	public void OnGrab() {
		groundTouching = null;
		isHeld = true;
	}

	public void OnLetGo() {
		isHeld = false;
	}

	public void Fold() {
		isFolded = true;
		GetComponent<MeshFilter>().mesh = BoxTypes.instance.meshFolded;
		BoxCollider col = GetComponent<BoxCollider>();
		col.size = new Vector3(1.396605f, 1.577678f, 0.15f);
	}
	
	void OnCollisionEnter(Collision col) {
		if (col.transform.CompareTag(TAG_GROUND) && gameObject.layer != PhysicsLayers.HELD_ITEM) {
			groundTouching = col.transform;
			timeLittered = 0;
		} else {
			groundTouching = null;
		}
	}

	public void Destroy() {
		GameManager.boxVolume -= volume;
		GameManager.score++;
		Destroy(gameObject);
	}

	void Update() {
		if (GameManager.gameOver) {
			groundTouching = null;
			return;
		}

		if (groundTouching != null) {
			timeLittered += Time.deltaTime;
			if (timeLittered < timeToCallCopsForLittering) {
				float timeLeft = timeToCallCopsForLittering - timeLittered;
				PlayerMessage.Add($"You can\'t leave your boxes on the ground outside, that\'s littering. Your neighbors will call the cops in <color={(((int)(timeLeft * 2)) % 2 == 0 ? Colors.WEE : Colors.WOO)}>{timeLeft:0.00}s</color>.");
			} else {
				if (!GameManager.gameOver) GameManager.GameOver(GameOverReasons.LITTERING);
			}
		}
	}
}
