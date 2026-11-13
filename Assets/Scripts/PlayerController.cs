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

    [Header("Visual Model & Free-Rotate Movement")]
    [Tooltip("The visual model container (or torso/mesh) that rotates towards WASD movement direction.")]
    public Transform visualModel;
    public float rotationSpeed = 12f;

    [Header("Leg Animation (Walking)")]
    public Transform leftLeg;
    public Transform rightLeg;
    public float legSwingAngle = 35f;
    public float legSwingSpeed = 10f;
    private Quaternion initialLeftLegRot;
    private Quaternion initialRightLegRot;
    private float legCycleTime;

    [Header("Hand Animation (Walking & Jumping)")]
    public Transform leftHand;
    public Transform rightHand;
    public float handSwingAngle = 30f;
    [Tooltip("Angle to raise hands forward/overhead when jumping.")]
    public float jumpArmRaiseAngle = 180f;
    [Tooltip("Outward spread angle for the left and right arms when jumping (V-shape).")]
    public float jumpArmSpreadAngle = 20f;
    [Tooltip("Speed arms raise when jumping and lower when landing.")]
    public float jumpArmSpeed = 12f;

    [Header("Hand Animation (Carry / Pickup Box)")]
    [Tooltip("Arm pitch forward when carrying a box (in front of chest).")]
    public float carryArmForwardAngle = 85f;
    [Tooltip("Arm inward angle towards the box center.")]
    public float carryArmInwardAngle = 15f;
    [Tooltip("Transition speed into carry pose.")]
    public float carryArmSpeed = 10f;
    public PlayerPickup playerPickup;
    private Quaternion initialLeftHandRot;
    private Quaternion initialRightHandRot;

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
        playerPickup = GetComponent<PlayerPickup>();
        if (playerPickup == null)
            playerPickup = GetComponentInParent<PlayerPickup>();
        if (playerPickup == null)
            playerPickup = GetComponentInChildren<PlayerPickup>();

        if (leftLeg != null)
            initialLeftLegRot = leftLeg.localRotation;
        if (rightLeg != null)
            initialRightLegRot = rightLeg.localRotation;

        if (leftHand != null)
            initialLeftHandRot = leftHand.localRotation;
        if (rightHand != null)
            initialRightHandRot = rightHand.localRotation;
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

        HandleBodyRotationAndLegs(direction);
    }

    private void HandleBodyRotationAndLegs(Vector3 inputDirection)
    {
        bool isMoving = inputDirection.sqrMagnitude > 0.01f && controller.isGrounded && !IsHoldingRope;

        // 1. Rotate visual body towards movement direction (Free camera / non-shiftlock)
        if (visualModel != null && walkDirection.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = new Vector3(walkDirection.x, 0f, walkDirection.z).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                visualModel.rotation = Quaternion.Slerp(visualModel.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        // 2. Procedural leg & arm animation (Walking, Jumping & Box Carry)
        bool inAir = !controller.isGrounded && !IsHoldingRope;
        bool carryingBox = playerPickup != null && playerPickup.IsHoldingBox;

        if (carryingBox)
        {
            // Carry animation: Arms hold forward in front of torso hugging/supporting the box
            float carrySpeed = Time.deltaTime * carryArmSpeed;
            Quaternion leftCarryRot = Quaternion.Euler(-carryArmForwardAngle, 0f, carryArmInwardAngle);
            Quaternion rightCarryRot = Quaternion.Euler(-carryArmForwardAngle, 0f, -carryArmInwardAngle);

            if (leftHand != null)
                leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, initialLeftHandRot * leftCarryRot, carrySpeed);
            if (rightHand != null)
                rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, initialRightHandRot * rightCarryRot, carrySpeed);

            // Legs still walk if player moves while carrying box
            if (isMoving)
            {
                legCycleTime += Time.deltaTime * legSwingSpeed;
                float swing = Mathf.Sin(legCycleTime);
                if (leftLeg != null)
                    leftLeg.localRotation = initialLeftLegRot * Quaternion.Euler(swing * legSwingAngle, 0f, 0f);
                if (rightLeg != null)
                    rightLeg.localRotation = initialRightLegRot * Quaternion.Euler(-swing * legSwingAngle, 0f, 0f);
            }
            else
            {
                legCycleTime = 0f;
                float returnSpeed = Time.deltaTime * 10f;
                if (leftLeg != null)
                    leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, initialLeftLegRot, returnSpeed);
                if (rightLeg != null)
                    rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, initialRightLegRot, returnSpeed);
            }
        }
        else if (inAir)
        {
            // When jumping / in air, raise both hands up overhead in symmetrical V-shape
            float armRaiseSpeed = Time.deltaTime * jumpArmSpeed;

            // Pitch up 180 degrees (mirrored axes between left and right)
            Quaternion leftRaisedRot = Quaternion.Euler(jumpArmRaiseAngle, 0f, -jumpArmSpreadAngle);
            Quaternion rightRaisedRot = Quaternion.Euler(-jumpArmRaiseAngle, 0f, jumpArmSpreadAngle);

            if (leftHand != null)
                leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, initialLeftHandRot * leftRaisedRot, armRaiseSpeed);
            if (rightHand != null)
                rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, initialRightHandRot * rightRaisedRot, armRaiseSpeed);

            // Legs gently tuck or return to neutral in mid-air
            if (leftLeg != null)
                leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, initialLeftLegRot, armRaiseSpeed);
            if (rightLeg != null)
                rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, initialRightLegRot, armRaiseSpeed);
        }
        else if (isMoving)
        {
            legCycleTime += Time.deltaTime * legSwingSpeed;
            float swing = Mathf.Sin(legCycleTime);

            // Legs swing alternately
            if (leftLeg != null)
                leftLeg.localRotation = initialLeftLegRot * Quaternion.Euler(swing * legSwingAngle, 0f, 0f);
            if (rightLeg != null)
                rightLeg.localRotation = initialRightLegRot * Quaternion.Euler(-swing * legSwingAngle, 0f, 0f);

            // Natural human walk: arms swing opposite to legs (mirrored pitch for left arm)
            if (leftHand != null)
                leftHand.localRotation = initialLeftHandRot * Quaternion.Euler(swing * handSwingAngle, 0f, 0f);
            if (rightHand != null)
                rightHand.localRotation = initialRightHandRot * Quaternion.Euler(swing * handSwingAngle, 0f, 0f);
        }
        else
        {
            // Smoothly return limbs to resting pose when stopped on ground
            legCycleTime = 0f;
            float returnSpeed = Time.deltaTime * 10f;

            if (leftLeg != null)
                leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, initialLeftLegRot, returnSpeed);
            if (rightLeg != null)
                rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, initialRightLegRot, returnSpeed);

            if (leftHand != null)
                leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, initialLeftHandRot, returnSpeed);
            if (rightHand != null)
                rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, initialRightHandRot, returnSpeed);
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
