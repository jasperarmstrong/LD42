using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecyclingCan : MonoBehaviour {
	[SerializeField] Transform[] boxSpots;
	float maxCapacity = 6;

	float timeToEmpty = 120;
	float timeUntilEmpty = 120;

	List<Box> boxes;
	Box candidate;
	
	static string atMaxCapacity = "This recycling can cannot hold any more boxes. You need to wait for it to be emptied or find another method to dispose of the boxes.";
	static string notFolded = "You need to fold the box before it will fit in the recycling can.";

	void Start() {
		boxes = new List<Box>();
	}

	void OnCollisionEnter(Collision col) {
		Box b = col.transform.GetComponent<Box>();
		if (b != null) {
			candidate = b;
		}
	}

	void OnCollisionExit(Collision col) {
		if (col.transform == candidate) {
			candidate = null;
		}
	}

	void Empty() {
		foreach (Box b in boxes) {
			if (b != null) {
				b.Destroy();
			}
		}
		boxes.Clear();
	}

	void Update () {
		if (GameManager.gameOver) return;

		if (candidate != null) {
			if (candidate.isHeld) {
				candidate = null;
			} else if (boxes.Count >= maxCapacity) {
				PlayerMessage.Add(atMaxCapacity);
			} else if (!candidate.isFolded) {
				PlayerMessage.Add(notFolded);
			} else {
				GameManager.player.LetGo();

				Collider col = candidate.GetComponent<Collider>();
				if (col != null) Destroy(col);

				Rigidbody rb = candidate.GetComponent<Rigidbody>();
				if (rb != null) Destroy(rb);

				candidate.transform.SetParent(boxSpots[boxes.Count]);
				candidate.transform.localPosition = Vector3.zero;
				candidate.transform.localRotation = Quaternion.identity;
				candidate.transform.localScale = Vector3.one * 0.48f;

				boxes.Add(candidate);

				candidate = null;
			}
		}

		int count = boxes.Count;
		if (count > 0) {
			PlayerMessage.Add($"Recycling space left: <color={Colors.THINGS_REMAINING}>{maxCapacity - count}</color> / Time until next collection: <color={Colors.THINGS_REMAINING}>{timeUntilEmpty:0.00}s</color>");
		}

		timeUntilEmpty -= Time.deltaTime;
		if (timeUntilEmpty < 0) {
			Empty();
			timeUntilEmpty = timeToEmpty;
		}
	}
}
