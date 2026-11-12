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

    //rope swing
    private Rigidbody heldRope;
    private Collider heldHandle;
    private Vector3 ropeOffset;
    private Vector3 releaseHorizontalVelocity;
    private bool carryingReleaseMomentum;
    private float grabAgainAt;
    public bool IsHoldingRope => heldRope != null;
    public bool CanGrabRope => !IsHoldingRope && releasedHandle == null && Time.time >= grabAgainAt;
    private Collider releasedHandle;
    [SerializeField] private float playerRopeMass = 2f;
    private float unloadedRopeMass;

    //rope climb
    [SerializeField] private float ropeClimbSpeed = 12f;
    [SerializeField] private float ropeSwingAcceleration = 65f;
    [SerializeField] private float ropeMaxSwingAngle = 30f;
    private RopeMoment heldRopePath;
    private float ropeClimbPosition;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 direction = Vector3.zero;
        Keyboard keyboard = Keyboard.current;

        //rope collider
        if (releasedHandle != null && !controller.bounds.Intersects(releasedHandle.bounds))
        {
            Physics.IgnoreCollision(controller, releasedHandle, false);
            releasedHandle = null;
        }

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
        
        Vector3 movement = carryingReleaseMomentum ? releaseHorizontalVelocity : walkDirection * speed;

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
            carryingReleaseMomentum = false;
        }

        if (fanRotation != null
            && (jumpingWithFan || (touchedFanBelow && controller.isGrounded)))
        {
            fanLocalPosition = fanRotation.InverseTransformPoint(transform.position);
        }

        
    }

    private void LateUpdate()
    {
        if (heldRope == null)
            return;

        Vector3 ropePoint = heldRope.transform.position;
        float nextPosition = ropeClimbPosition;

        if (heldRopePath != null && heldRopePath.IsEndpointsValid)
        {
            Keyboard keyboard = Keyboard.current;
            float climbDirection = 0f;

            if (keyboard != null)
            {
                if (keyboard.eKey.isPressed)
                    climbDirection -= 1f;

                if (keyboard.qKey.isPressed)
                    climbDirection += 1f;
            }

            float ropeDistance = Mathf.Max(0.01f, Vector3.Distance(
                heldRopePath.startPoint.position,
                heldRopePath.endPoint.position
            ));

            // Leave room for the top of the player's capsule.
            float highestPosition = Mathf.Clamp01(
                (controller.bounds.extents.y + 0.5f) / ropeDistance
            );

            nextPosition = Mathf.Clamp(
                ropeClimbPosition + climbDirection * ropeClimbSpeed
                    * Time.deltaTime / ropeDistance,
                highestPosition,
                1f
            );

            ropePoint = heldRopePath.GetPointAt(nextPosition);
        }

        Vector3 target = ropePoint + ropeOffset;
        controller.Move(target - transform.position);

        // Only advance the climb if the capsule reached its target.
        if (Vector3.Distance(transform.position, target) < 0.1f)
            ropeClimbPosition = nextPosition;
    }

    private void FixedUpdate()
    {
        if (heldRope == null) return;

        UpdateRopeMass();

        if (heldRopePath == null ||
            heldRopePath.startPoint == null || walkDirection == Vector3.zero)
            return;


        Vector3 fromAnchor = heldRope.position - heldRopePath.startPoint.position;
        float ropeLength = fromAnchor.magnitude;
        if (ropeLength < 0.01f)
            return;

        Vector3 swingInput = Vector3.ProjectOnPlane(walkDirection, fromAnchor);
        if (swingInput.sqrMagnitude < 0.0001f)
            return;

        Vector3 pushDirection = swingInput.normalized;
        Vector3 swingVelocity = Vector3.ProjectOnPlane(heldRope.linearVelocity, fromAnchor);
        float speedInPushDirection = Vector3.Dot(swingVelocity, pushDirection);

        float leverage = Mathf.Clamp01(ropeClimbPosition);
        float loadScale = unloadedRopeMass / heldRope.mass;
        float speedChange = ropeSwingAcceleration * leverage * loadScale * swingInput.magnitude * Time.fixedDeltaTime;

        if (speedInPushDirection < 0f)
        {
            // Slow the swing before accelerating in the opposite direction.
            speedChange = Mathf.Min(speedChange, -speedInPushDirection);
        }
        else
        {
            // Limit how much height the controls can add.
            float maxRise = ropeLength * (1f - Mathf.Cos(ropeMaxSwingAngle * Mathf.Deg2Rad));
            float currentRise = ropeLength + fromAnchor.y;
            float remainingRise = Mathf.Max(0f, maxRise - currentRise);
            float speedLimit = Mathf.Sqrt(2f * Physics.gravity.magnitude * remainingRise);
            speedChange = Mathf.Min(speedChange, speedLimit - swingVelocity.magnitude);
        }

        if (speedChange > 0f)
            heldRope.AddForce(pushDirection * speedChange, ForceMode.VelocityChange);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //rope swing
        if (heldRope != null) return;
        Rigidbody ropeBody = hit.collider.attachedRigidbody;


        if (ropeBody != null && ropeBody.TryGetComponent<RopeSwingEnd>(out _))
        {
            if (Vector3.Dot(walkDirection, hit.normal) < -0.01f)
            {
                float speedAlongPush = Vector3.Dot(ropeBody.linearVelocity, walkDirection);
                const float ropePushSpeed = 6f;
                float speedToAdd = Mathf.Clamp(ropePushSpeed - speedAlongPush, 0f, ropePushSpeed);
                ropeBody.AddForce(walkDirection * speedToAdd, ForceMode.VelocityChange);
            }

            return;
        }

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

    private void GrabRope(Collider handle, Vector3 incomingVelocity)
    {
        heldHandle = handle;
        heldRope = handle.attachedRigidbody;
        heldRopePath = heldRope.GetComponent<RopeSwingEnd>().RopePath;
        ropeClimbPosition = 1f;
        unloadedRopeMass = heldRope.mass;
        UpdateRopeMass();
        ropeOffset = transform.position - controller.bounds.center;

        Physics.IgnoreCollision(controller, heldHandle, true);
        jumpingWithFan = false;
        touchedFanBelow = false;

        ConfigurableJoint joint = heldRope.GetComponent<ConfigurableJoint>();
        Vector3 radial = heldRope.position - joint.connectedAnchor;
        Vector3 relativeVelocity = incomingVelocity - heldRope.linearVelocity;
        Vector3 tangent = Vector3.ProjectOnPlane(relativeVelocity, radial);

        float playerMassShare = playerRopeMass / heldRope.mass;
        Vector3 grabKick = Vector3.ClampMagnitude(tangent * playerMassShare, 6f);
        heldRope.AddForce(grabKick, ForceMode.VelocityChange);

        if (heldRopePath != null)
            heldRopePath.AddGrabBend(grabKick);
    }

    private void ReleaseRope(Vector3 velocity)
    {

        releasedHandle = heldHandle;
        heldRope.mass = unloadedRopeMass;

        heldRope = null;
        heldHandle = null;
        grabAgainAt = Time.time + 0.3f;
        carryingReleaseMomentum = true;
        verticalVelocity = velocity.y;
        releaseHorizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
    }
    public void InteractWithRope(Collider handle)
    {
        if (IsHoldingRope)
            ReleaseRope(heldRope.linearVelocity);
        else if (CanGrabRope && handle != null)
            GrabRope(handle, controller.velocity);
    }

    private void UpdateRopeMass()
    {
        float position = Mathf.Clamp01(ropeClimbPosition);
        float loadedMass = unloadedRopeMass + playerRopeMass * position * position;

        if (!Mathf.Approximately(heldRope.mass, loadedMass))
            heldRope.mass = loadedMass;
    }
}
