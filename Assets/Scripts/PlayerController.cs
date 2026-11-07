using UnityEngine;
using UnityEngine.InputSystem;

// [RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float speed = 35f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    private float verticalVelocity;

    public float pushForce = 150f;
    public float maxPushSpeed = 10f;

    private CharacterController controller;
    private Vector3 walkDirection;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 direction = Vector3.zero;
        Keyboard keyboard = Keyboard.current;

        // Stop downward velocity from building up while on the ground.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed)
                direction.z += 1f;

            if (keyboard.sKey.isPressed)
                direction.z -= 1f;

            if (keyboard.dKey.isPressed)
                direction.x += 1f;

            if (keyboard.aKey.isPressed)
                direction.x -= 1f;
            if (keyboard.spaceKey.wasPressedThisFrame && controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        // Horizontal movement relative to the player's facing direction.
        walkDirection = transform.TransformDirection(direction.normalized);
        Vector3 movement = walkDirection * speed;

        // Gravity changes vertical speed over time.
        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;

        // Apply horizontal and vertical movement together.
        CollisionFlags collisions = controller.Move(movement * Time.deltaTime);

        // Stop rising if the player hits a ceiling.
        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody box = hit.collider.attachedRigidbody;

        if (box == null || box.isKinematic || !box.CompareTag("Pickupable"))
            return;

        // Skip the top of the box so landing on it doesn't push it.
        if (hit.normal.y > 0.5f)
            return;

        // Use the intended walking direction, even if the box stopped the controller.
        if (Vector3.Dot(walkDirection, hit.normal) >= -0.01f)
            return;

        float remainingSpeed =
            maxPushSpeed - Vector3.Dot(box.linearVelocity, walkDirection);
        if (remainingSpeed <= 0f)
            return;

        float impulse = Mathf.Min(pushForce * Time.deltaTime, remainingSpeed * box.mass);
        box.AddForce(walkDirection * impulse, ForceMode.Impulse);
    }
}
