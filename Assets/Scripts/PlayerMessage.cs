using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class PlayerMessage : MonoBehaviour {
	public static PlayerMessage instance;
	static TextMeshProUGUI text;

	static StringBuilder sb;

	void ResetString() {
		if (sb == null) sb = new StringBuilder();
		sb.Clear();
	}

	void Start () {
		instance = this;
		text = GetComponent<TextMeshProUGUI>();
		ResetString();
	}

	void LateUpdate() {
		text.text = sb.ToString();
		ResetString();
	}

	public static void Add(string info) {
		if (instance.transform.parent.gameObject.activeInHierarchy) sb.AppendLine(info);
	}
}
