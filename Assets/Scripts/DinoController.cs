using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DinoController : MonoBehaviour {

	[SerializeField] GraphicRaycaster graphicsRayCaster;
	[SerializeField] EventSystem eventSystem;
	[SerializeField] GameObject restartMenu;

	[SerializeField] private float jumpForce = 17.25f;

	private Rigidbody thisRB;
	private Animator animator;

	private bool jumping = false;
	// change in collider's z position and scale while jumping and crouching
	private Vector2 colliderZdims = new Vector2(-0.02f, 0.08f);
	private Vector2 colliderJumpChange = new Vector2(-0.014f, 0.068f);
	private Vector2 colliderCrouchChange = new Vector2(-0.0375f, 0.0451f);

	private void Start() {
		thisRB = GetComponent<Rigidbody>();
		animator = GetComponent<Animator>();
	}

	private void Update() {
		if (Touchscreen.current != null && Pointer.current.position.ReadValue().x <= (Screen.width / 2.0f)) {
			Crouch(Touchscreen.current.press.isPressed);
		}
	}

	public void BeginGame() {
		animator.SetBool("Idling", false);
	}

	public void OnJump(InputAction.CallbackContext context) {
		bool triggered = context.ReadValueAsButton();

		// check whether the pointer is over any UI element
		PointerEventData pointerData = new PointerEventData(eventSystem);
		pointerData.position = Pointer.current.position.ReadValue();
		List<RaycastResult> res = new List<RaycastResult>();
		graphicsRayCaster.Raycast(pointerData, res);
		if (res.Count != 0) {
			return;
		}

		Vector2 cursorPos = Pointer.current.position.ReadValue();
		BoxCollider thisCollider = transform.GetChild(0).GetComponent<BoxCollider>();	// retrieve the collider from the armature
		// crouch
		if (Touchscreen.current == null && cursorPos.x <= (Screen.width / 2.0f)) {
			Crouch(triggered);
			return;
		}

		// jump
		if (triggered && !jumping) {
			thisRB.AddForce(Vector3.up * jumpForce, ForceMode.Force);
			jumping = true;
			animator.SetBool("Jumping", true);
			thisCollider.center = new Vector3(thisCollider.center.x, thisCollider.center.y, colliderJumpChange.x);
			thisCollider.size = new Vector3(thisCollider.size.x, thisCollider.size.y, colliderJumpChange.y);
		}
	}

	private void OnCollisionEnter(Collision collision) {
		if (collision.gameObject.tag == "ground") {
			BoxCollider thisCollider = transform.GetChild(0).GetComponent<BoxCollider>();   // retrieve the collider from the armature
			jumping = false;
			animator.SetBool("Jumping", false);
			thisCollider.center = new Vector3(thisCollider.center.x, thisCollider.center.y, colliderZdims.x);
			thisCollider.size = new Vector3(thisCollider.size.x, thisCollider.size.y, colliderZdims.y);
			if (!PlayerPrefs.HasKey("high_score") || GlobalController.Score > PlayerPrefs.GetInt("high_score")) {
				PlayerPrefs.SetInt("high_score", (int)GlobalController.Score);
			}
		}
	}

	private void Crouch(bool flag) {
		BoxCollider thisCollider = transform.GetChild(0).GetComponent<BoxCollider>();   // retrieve the collider from the armature

		if (flag) {
			animator.SetBool("Crouch", true);
			thisCollider.center = new Vector3(thisCollider.center.x, thisCollider.center.y, colliderCrouchChange.x);
			thisCollider.size = new Vector3(thisCollider.size.x, thisCollider.size.y, colliderCrouchChange.y);
		}
		else {
			animator.SetBool("Crouch", false);
			thisCollider.center = new Vector3(thisCollider.center.x, thisCollider.center.y, colliderZdims.x);
			thisCollider.size = new Vector3(thisCollider.size.x, thisCollider.size.y, colliderZdims.y);
		}
	}

	private void OnTriggerEnter(Collider other) {
		Time.timeScale = 0.0f;
		restartMenu.SetActive(true);
	}
}
 