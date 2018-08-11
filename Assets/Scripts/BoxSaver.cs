using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxSaver : MonoBehaviour {
	void OnTriggerEnter(Collider other) {
		Vector3 newPos = other.transform.position;
		newPos.y = 3;
		other.transform.position = newPos;
	}
}
