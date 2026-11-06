using UnityEngine;
using UnityEngine.InputSystem;

// [RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float speed = 35f;
    public float jumpHeight = 2f;
    public float gravity = -20f;

    private float verticalVelocity;

    private CharacterController controller;

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
        Vector3 movement =
            transform.TransformDirection(direction.normalized) * speed;

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
}
