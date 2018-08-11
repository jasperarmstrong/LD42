using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour {
	const string TAG_GRABBABLE = "Grabbable";

	float walkSpeed = 7;
	public float lookSensitivity = 1;
	const float lookMultiplier = 200;

	Vector2 moveVector;
	float lastMouseX, lastMouseY;
	float mouseX, mouseY;
	bool invertX, invertY = false;

	bool isGrounded = false;
	float groundDistance = 0;

	bool shouldJump = false;
	float jumpForce = 40;
	float jumpTime = 0;
	float maxJumpTime = 0.15f;

	[SerializeField] Transform camera;
	[SerializeField] Transform grabSpot;
	Rigidbody rb;

	RaycastHit[] hits;

	int layerNotHeld = 0; // Default
	int layerHeld = 9; // HeldItem

	Coroutine grabCoroutine;
	float maxGrabDistance = 4;
	float grabSpeed = 5;
	float maxGrabbedItemVelocity = 4;
	Vector3 grabbedItemVelocity;
	Vector3 grabbedItemLastPos = Vector3.zero;
	Transform grabbedItem;
	Transform lookingAt;

	void Start () {
		rb = GetComponent<Rigidbody>();

		hits = new RaycastHit[8];

		LockMouse();
	}

	void LockMouse() {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void UnlockMouse() {
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	void ToggleMouse() {
		if (Cursor.visible) {
			LockMouse();
		} else {
			UnlockMouse();
		}
	}

	void UpdateIsGrounded() {
		int numHits = Physics.RaycastNonAlloc(transform.position + (Vector3.up * 0.004f), Vector3.down, hits, 50);
		if (numHits > 0) {
			Transform minDistanceTransform = null;
			float minDistance = 50;
			for (int i = 0; i < numHits; i++) {
				if (hits[i].transform == transform) continue;
				if (hits[i].distance < minDistance) {
					minDistance = hits[i].distance;
					minDistanceTransform = hits[i].transform;
				}
			}
			groundDistance = minDistance;
			if (groundDistance < 0.2f) {
				isGrounded = true;
			} else {
				isGrounded = false;
			}
		}
		DebugInfo.Add($"isGrounded: {isGrounded} ({groundDistance:0.00}m)");
	}

	void GetInput() {
		moveVector = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1);

		if (Cursor.visible) {
			mouseX = mouseY = 0;
		} else {
			mouseX = Input.GetAxis("Mouse X");
			mouseY = Input.GetAxis("Mouse Y");
		}

		bool oldShouldJump = shouldJump;
		shouldJump = (shouldJump && Input.GetKey(KeyCode.Space)) || (Input.GetKeyDown(KeyCode.Space) && isGrounded);
		if (shouldJump && !oldShouldJump) jumpTime = 0;
		DebugInfo.Add($"shouldJump: {shouldJump}");
	}

	void UpdateVelocity() {
		Vector3 velocity = rb.velocity;
		velocity.x = velocity.z = 0;

		velocity += transform.right * moveVector.x * walkSpeed;
		velocity += transform.forward * moveVector.y * walkSpeed;
		rb.velocity = velocity;

		DebugInfo.Add($"Position: {transform.position}");
	}

	void UpdateLookDirection() {
		float lookFactor = lookSensitivity * lookMultiplier * Time.deltaTime;
		transform.Rotate(0, mouseX * lookFactor * (invertY ? -1 : 1), 0);
		camera.Rotate(-mouseY * lookFactor * (invertY ? -1 : 1), 0, 0);
		if (camera.localRotation.x > 0.707f) {
			camera.localRotation = Quaternion.Euler(90, 0, 0);
		} else if (camera.localRotation.x < -0.707f) {
			camera.localRotation = Quaternion.Euler(-90, 0, 0);
		}
	}

	void UpdateLookingAt() {
		lookingAt = null;
		int numHits = Physics.RaycastNonAlloc(camera.position, camera.forward, hits, maxGrabDistance);
		if (numHits > 0) {
			Transform minDistanceTransform = null;
			float minDistance = maxGrabDistance + 1;
			for (int i = 0; i < numHits; i++) {
				if (hits[i].transform == transform) continue;
				if (hits[i].distance < minDistance) {
					minDistance = hits[i].distance;
					minDistanceTransform = hits[i].transform;
				}
			}
			lookingAt = minDistanceTransform;
		}
		DebugInfo.Add($"lookingAt: {lookingAt?.name ?? "nothing"}");
	}

	IEnumerator GrabCoroutine() {
		Rigidbody girb = grabbedItem.GetComponent<Rigidbody>();
		if (girb != null) {
			Destroy(girb);
		}

		grabbedItem.gameObject.layer = layerHeld;

		float progress = 0;
		Vector3 initPos = grabbedItem.position;
		Quaternion initRot = grabbedItem.localRotation;

		while (progress < 1) {
			progress += grabSpeed * Time.deltaTime;
			grabbedItem.position = Vector3.Lerp(initPos, grabSpot.position, progress);
			grabbedItem.rotation = Quaternion.Lerp(initRot, transform.rotation, progress);
			yield return null;
		}

		grabbedItem.SetParent(grabSpot);
		grabbedItem.localPosition = Vector3.zero;
		grabbedItem.rotation = transform.rotation;

		grabCoroutine = null;
	}

	void UpdateGrab() {
		if (Input.GetMouseButtonDown(0)) {
			if (grabbedItem == null) {
				if (lookingAt?.CompareTag(TAG_GRABBABLE) ?? false) {
					grabbedItem = lookingAt;
					grabCoroutine = StartCoroutine(GrabCoroutine());
				}
			} else {
				if (grabCoroutine != null) {
					StopCoroutine(grabCoroutine);
					grabCoroutine = null;
				}
				grabbedItem.SetParent(null);
				Rigidbody girb = (Rigidbody)grabbedItem.gameObject.AddComponent(typeof(Rigidbody));
				if (girb != null) {
					girb.velocity = rb.velocity + Vector3.ClampMagnitude(grabbedItemVelocity, maxGrabbedItemVelocity);
				}

				grabbedItem.gameObject.layer = layerNotHeld;
				grabbedItem = null;
			}
		}

		DebugInfo.Add($"grabbedItem: {grabbedItem?.name ?? "nothing"}");
		
		if (grabbedItem != null) {
			if (grabCoroutine == null) {
				grabbedItem.rotation = transform.rotation;
			}

			grabbedItemVelocity = ((grabbedItem.position - grabbedItemLastPos) / Time.deltaTime) - rb.velocity;
			DebugInfo.Add($"grabbedItem Speed: {grabbedItemVelocity.magnitude}");
			grabbedItemLastPos = grabbedItem.position;
		}
	}
	
	void Update () {
		#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.M)) ToggleMouse();
		#endif

		UpdateIsGrounded();
		GetInput();
		UpdateVelocity();
		UpdateLookDirection();
		UpdateLookingAt();
		UpdateGrab();
	}

	void FixedUpdate() {
		if (shouldJump) {
			if (jumpTime < maxJumpTime) {
				rb.AddForce(Vector3.up * jumpForce);
				jumpTime += Time.fixedDeltaTime;
			} else {
				shouldJump = false;
			}
		}
	}
}
