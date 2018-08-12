using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxTypes : MonoBehaviour {
	public static BoxTypes instance;

	public GameObject prefabNormal;
	public Mesh meshFolded;
	public Texture textureOpen;

	void Awake () {
		if (instance == null) instance = this;		
	}
}
