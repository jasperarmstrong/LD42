using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxSpawner : MonoBehaviour {
	public BoxSpawner instance;

	float defaultMinSpawnTime = 4;
	float defaultMaxSpawnTime = 10;
	float minSpawnTime = 5;
	float maxSpawnTime = 5;
	float nextSpawnTime = 4;

	float halfX, halfY, halfZ;

	float minBoxScale = 0.75f;
	float maxBoxScale = 1.4424f;

	BoxCollider col;

	int numSpawned = 0;
	int numSpawnedFree = 0;
	float spawnTimeMultiplier = 1;

	void Start() {
		instance = this;

		col = GetComponent<BoxCollider>();

		halfX = col.size.x / 2;
		halfY = col.size.y / 2;
		halfZ = col.size.z / 2;

		SpawnBox(false);
		SpawnBox(false);
		SpawnBox(false);
	}
	
	Vector3 RandomSpawnLocation() {
		Vector3 vec;

		vec.x = Random.Range(transform.position.x - halfX, transform.position.x + halfX);
		vec.y = Random.Range(transform.position.y - halfY, transform.position.y + halfY);
		vec.z = Random.Range(transform.position.z - halfZ, transform.position.z + halfZ);

		return vec;
	}

	public void SpawnBox(bool countForDifficulty = true) {
		GameObject boxObj = (GameObject)Instantiate(BoxTypes.instance.prefabNormal, RandomSpawnLocation(), Quaternion.identity);
		float scale = Random.Range(minBoxScale, maxBoxScale);
		boxObj.transform.localScale = new Vector3(scale, scale, scale);
		
		numSpawned++;
		if (!countForDifficulty) numSpawnedFree++;

		spawnTimeMultiplier = Mathf.Clamp(1 - ((numSpawned - numSpawnedFree) * 0.05f), 0.3f, 1);
		minSpawnTime = defaultMinSpawnTime * spawnTimeMultiplier;
		maxSpawnTime = defaultMaxSpawnTime * spawnTimeMultiplier;

		float nextTime = Random.Range(minSpawnTime, maxSpawnTime);
		#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.Period)) nextTime = -0.1f;
		#endif
		nextSpawnTime = nextTime;
	}

	void Update () {
		if (nextSpawnTime < 0) {
			SpawnBox();
		}
		DebugInfo.Add($"spawnTimeMultiplier: {spawnTimeMultiplier:0.000} [{minSpawnTime:0.00}s, {maxSpawnTime:0.00}s]");
		DebugInfo.Add($"nextSpawnTime: {nextSpawnTime:0.00}s");

		if (GameManager.isPaused) return;

		nextSpawnTime -= Time.deltaTime;
	}
}
