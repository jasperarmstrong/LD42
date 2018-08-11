using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class DebugInfo : MonoBehaviour {
	public static DebugInfo instance;
	static TextMeshProUGUI text;

	static StringBuilder sb;

	static bool isEnabled = true;

	void ResetString() {
		if (sb == null) sb = new StringBuilder();
		sb.Clear();
	}

	void Start () {
		instance = this;
		text = GetComponent<TextMeshProUGUI>();
		ResetString();
		transform.parent.gameObject.SetActive(isEnabled);
	}

	void LateUpdate() {
		text.text = sb.ToString();
		ResetString();
	}

	public static void Add(string info) {
		if (instance.transform.parent.gameObject.activeInHierarchy) sb.AppendLine(info);
	}
}
