using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetWarning : MonoBehaviour {
	static string warning = "You can't go in the street, that's dangerous.";
	bool playerInTrigger = false;

	void OnTriggerEnter(Collider col) {
		if (col.gameObject.layer == PhysicsLayers.PLAYER) {
			playerInTrigger = true;
		}
	}

	void OnTriggerExit(Collider col) {
		if (col.gameObject.layer == PhysicsLayers.PLAYER) {
			playerInTrigger = false;
		}
	}

	void Update() {
		if (playerInTrigger) {
			PlayerMessage.Add(warning);
		}
	}
}
