using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour {
	const string TAG_GRABBABLE = "Grabbable";
	static string foldBoxMessage = "(Hold F) Fold Box";
	static string dropBoxMessage = "(LMB) Drop Box";
	static string pickUpBoxMessage = "(LMB) Pick Up Box";

	float walkSpeed = 7;
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

	float foldTime = 0.9f;
	float foldProgress = 0;

	[SerializeField] LayerMask lookAtLayerMask;
	[SerializeField] Transform cam;
	[SerializeField] Transform grabSpot;
	Rigidbody rb;

	RaycastHit[] hits;
	
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

		GameManager.player = this;
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
	}

	void UpdateVelocity() {
		if (!GameManager.isPaused) {
			Vector3 velocity = rb.velocity;
			velocity.x = velocity.z = 0;

			velocity += transform.right * moveVector.x * walkSpeed;
			velocity += transform.forward * moveVector.y * walkSpeed;
			rb.velocity = velocity;
		}

		DebugInfo.Add($"position: {transform.position}");
	}

	void UpdateLookDirection() {
		if (GameManager.isPaused) return;

		float lookFactor = GameManager.mouseSensitivity * lookMultiplier * Time.deltaTime;
		transform.Rotate(0, mouseX * lookFactor * (invertY ? -1 : 1), 0);
		cam.Rotate(-mouseY * lookFactor * (invertY ? -1 : 1), 0, 0);
		if (cam.localRotation.x > 0.707f) {
			cam.localRotation = Quaternion.Euler(90, 0, 0);
		} else if (cam.localRotation.x < -0.707f) {
			cam.localRotation = Quaternion.Euler(-90, 0, 0);
		}
	}

	void UpdateFolding(Transform t) {
		if (t != null) {
			Box b = t.GetComponent<Box>();
			if (b != null && !b.isFolded) {
				if (Input.GetKey(KeyCode.F)) {
					float adjustedFloatTime = foldTime * b.transform.localScale.x;
					if (foldProgress > adjustedFloatTime && !GameManager.isPaused) {
						b.Fold();
						foldProgress = 0;
					} else {
						PlayerMessage.Add($"Folding box... <color={Colors.PROGRESS}>{(foldProgress/adjustedFloatTime) * 100:0}%</color>");
						foldProgress += Time.deltaTime;
					}
				} else {
					PlayerMessage.Add(foldBoxMessage);
				}
			}
		}
	}

	void WhatsInTheBox(Transform t) {
		Box box = t.GetComponent<Box>();
		if (box != null) {
			PlayerMessage.Add($"<color={Colors.ITEM_NAMES}>{box.contents}</color>");
		
			if (t == grabbedItem) {
				PlayerMessage.Add(dropBoxMessage);
			} else if (t == lookingAt) {
				PlayerMessage.Add(pickUpBoxMessage);
			}
		}
	}

	void UpdateLookingAt() {
		lookingAt = null;
		int numHits = Physics.RaycastNonAlloc(cam.position, cam.forward, hits, maxGrabDistance, lookAtLayerMask, QueryTriggerInteraction.Ignore);
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

		if (Input.GetKeyUp(KeyCode.F) && !GameManager.isPaused) {
			foldProgress = 0;
		}
	}

	IEnumerator GrabCoroutine() {
		Rigidbody girb = grabbedItem.GetComponent<Rigidbody>();
		if (girb != null) {
			Destroy(girb);
		}

		grabbedItem.gameObject.layer = PhysicsLayers.DEFAULT;
		grabbedItem.GetComponent<IGrabbable>()?.OnGrab();

		float progress = 0;
		Vector3 initPos = grabbedItem.position;
		Quaternion initRot = grabbedItem.localRotation;

		while (progress < 1) {
			if (GameManager.isPaused) yield return null;

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

	public void LetGo() {
		if (grabCoroutine != null) {
			StopCoroutine(grabCoroutine);
			grabCoroutine = null;
		}

		if (grabbedItem == null) return;

		grabbedItem.SetParent(null);
		Rigidbody girb = (Rigidbody)grabbedItem.gameObject.AddComponent(typeof(Rigidbody));
		if (girb != null) {
			girb.velocity = rb.velocity + Vector3.ClampMagnitude(grabbedItemVelocity, maxGrabbedItemVelocity);
		}

		grabbedItem.gameObject.layer = PhysicsLayers.DEFAULT;
		grabbedItem.GetComponent<IGrabbable>()?.OnLetGo();
		grabbedItem = null;
	}

	void UpdateGrab() {
		if (Input.GetMouseButtonDown(0) && !GameManager.isPaused) {
			if (grabbedItem == null) {
				if (lookingAt?.CompareTag(TAG_GRABBABLE) ?? false) {
					grabbedItem = lookingAt;
					grabCoroutine = StartCoroutine(GrabCoroutine());
				}
			} else {
				LetGo();
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
		if (GameManager.gameOver) return;

		UpdateIsGrounded();
		GetInput();
		UpdateVelocity();
		UpdateLookDirection();
		UpdateLookingAt();
		UpdateGrab();

		if (grabbedItem != null) {
			WhatsInTheBox(grabbedItem);
			UpdateFolding(grabbedItem);
		} else if (lookingAt != null) {
			WhatsInTheBox(lookingAt);
			UpdateFolding(lookingAt);
		}
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
