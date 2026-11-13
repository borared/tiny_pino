using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

// [RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    //player control
    public float speed = 35f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    private float verticalVelocity;

    //box pickup control
    public float pushForce = 150f;
    public float maxPushSpeed = 10f;

    //also player control
    private CharacterController controller;
    private Vector3 walkDirection;

    //move with fan control
    public Transform fanSurface;
    public Transform fanRotation;
    private bool touchedFanBelow;
    private bool jumpingWithFan;
    private Vector3 fanLocalPosition;

    [SerializeField] private float ropeClimbSpeed = 12f;
    private RopeMoment heldRopePath;
    private float ropeClimbPosition;
    private float grabAgainAt;

    public bool IsHoldingRope => heldRopePath != null;
    public bool CanGrabRope => !IsHoldingRope && Time.time >= grabAgainAt;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 direction = Vector3.zero;
        Keyboard keyboard = Keyboard.current;

        //check before rope so there is wasd to rope movement
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
        }

        walkDirection = transform.TransformDirection(direction.normalized);

        if (IsHoldingRope) return;

        // Stop downward velocity from building up while on the ground.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        //check jump and placed after holding rope check since there is no space key option when climbing rope
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame && controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpingWithFan = fanRotation != null && touchedFanBelow;
            }
        }

        //horizontal movement relative to the player's facing direction.

        Vector3 movement = walkDirection * speed;

        //gravity changes vertical speed over time.
        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;

        Vector3 fanMovement = Vector3.zero;

        bool standingOnFan = touchedFanBelow && controller.isGrounded && verticalVelocity <= 0f;

        if (fanRotation != null && (standingOnFan || jumpingWithFan))
        {
            Vector3 newPosition = fanRotation.TransformPoint(fanLocalPosition);
            fanMovement = newPosition - transform.position;
            fanMovement.y = 0f;
        }

        touchedFanBelow = false;

        CollisionFlags collisions =
            controller.Move(movement * Time.deltaTime + fanMovement);

        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            jumpingWithFan = false;
        }

        if (fanRotation != null
            && (jumpingWithFan || (touchedFanBelow && controller.isGrounded)))
        {
            fanLocalPosition = fanRotation.InverseTransformPoint(transform.position);
        }


    }

    private void LateUpdate()
    {
        if (!IsHoldingRope)
            return;

        Keyboard keyboard = Keyboard.current;
        float climb = 0f;
        if (keyboard != null)
        {
            if (keyboard.eKey.isPressed) climb -= 1f;
            if (keyboard.qKey.isPressed) climb += 1f;
        }

        float length = Vector3.Distance(heldRopePath.startPoint.position,
                                        heldRopePath.endPoint.position);
        if (length < 0.01f)
            return;

        float topLimit = Mathf.Clamp01((controller.bounds.extents.y + 0.5f) / length);
        float next = Mathf.Clamp(
            ropeClimbPosition + climb * ropeClimbSpeed * Time.deltaTime / length,
            topLimit, 1f);

        Vector3 target = heldRopePath.GetPointAt(next)
            - transform.TransformVector(controller.center);
        controller.Move(target - transform.position);

        if ((transform.position - target).sqrMagnitude < 0.01f)
            ropeClimbPosition = next;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //fan switch
        if (verticalVelocity > 0f && hit.collider.TryGetComponent<FanSwitch>(out var fanSwitch))
        {
            fanSwitch.FlipUp();
        }

        //fan
        if (fanSurface != null && hit.collider.transform.IsChildOf(fanSurface) && hit.moveDirection.y < 0f && hit.normal.y > 0.5f)
        {
            touchedFanBelow = true;
        }
        //box
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
        Vector3 pushPoint = hit.point;
        pushPoint.y = box.worldCenterOfMass.y;
        box.AddForceAtPosition(walkDirection * impulse, pushPoint, ForceMode.Impulse);
    }
    public void InteractWithRope(Collider handle)
    {
        if (IsHoldingRope)
        {
            heldRopePath = null;
            grabAgainAt = Time.time + 0.3f;
            verticalVelocity = 0f;
            return;
        }

        if (!CanGrabRope || handle == null ||
            !handle.TryGetComponent(out RopeSwingEnd rope) ||
            rope.RopePath == null || !rope.RopePath.IsEndpointsValid)
            return;

        heldRopePath = rope.RopePath;
        ropeClimbPosition = 1f;
        verticalVelocity = 0f;
        jumpingWithFan = false;
        touchedFanBelow = false;
    }
}
