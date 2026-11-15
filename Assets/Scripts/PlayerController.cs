using System.Collections.Generic;
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
    private readonly HashSet<Rigidbody> boxPushCandidates = new();
    private readonly HashSet<Rigidbody> boxSupports = new();
    private bool collectingBoxContacts;
    private float feetBeforeMove;

    [Header("Stick & Slippery Surface Balance")]
    [Tooltip("LayerMask used to check the ground/stick beneath the player's feet.")]
    public LayerMask groundCheckLayers = ~0;
    private Vector3 slipVelocity;
    private SlipperyStick activeSlipperyStick;
    private Vector3 groundNormal = Vector3.up;

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
    [Tooltip("How far the arms pitch forward from their resting pose in the air.")]
    public float jumpArmRaiseAngle = 180f;
    [Tooltip("Outward spread angle for the arms in the air.")]
    public float jumpArmSpreadAngle = 0f;
    [Tooltip("How quickly the arms rise in the air.")]
    public float jumpArmSpeed = 26f;
    private bool jumpedFromGround;
    private float jumpArmRaiseProgress;
    private Quaternion jumpStartLeftHandRot;
    private Quaternion jumpStartRightHandRot;

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
    private bool leftHandFollowsShoulder;
    private Vector3 leftHandAnchor;
    private Quaternion leftHandPose;

    //move with fan control
    public Transform fanSurface;
    public Transform fanRotation;
    private bool touchedFanBelow;
    private bool jumpingWithFan;
    private Vector3 fanLocalPosition;

    [Header("Rope Climbing")]
    [SerializeField] private float ropeClimbSpeed = 12f;
    [SerializeField, Min(0.01f)] private float ropeClimbCycleDistance = 8f;
    private RopeMoment heldRopePath;
    private float grabAgainAt;
    private float ropeClimbPhase;

    public bool IsHoldingRope => heldRopePath != null;
    public bool CanGrabRope => !IsHoldingRope && Time.time >= grabAgainAt;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        // Move still hits boxes, but the physics solver cannot shove them out of the capsule.
        controller.detectCollisions = false;
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
        {
            leftHandFollowsShoulder = !leftHand.IsChildOf(transform);
            if (leftHandFollowsShoulder)
            {
                leftHandAnchor = transform.InverseTransformPoint(leftHand.position);
                initialLeftHandRot = Quaternion.Inverse(transform.rotation) * leftHand.rotation;
            }
            else
            {
                initialLeftHandRot = leftHand.localRotation;
            }
            leftHandPose = initialLeftHandRot;
        }
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

        // Check if player is standing on a narrow/slippery stick
        CheckGroundSurface();

        // Stop downward velocity from building up while on the ground.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        bool jumpPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;

        //check jump and placed after holding rope check since there is no space key option when climbing rope
        if (jumpPressed && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpedFromGround = true;
            jumpingWithFan = fanRotation != null && touchedFanBelow;

            if (activeSlipperyStick != null && activeSlipperyStick.jumpingCausesSlip)
            {
                slipVelocity = activeSlipperyStick.CalculateSlip(transform.position, walkDirection * speed, groundNormal, true);
            }
        }

        //horizontal movement relative to the player's facing direction.
        Vector3 movement = walkDirection * speed;

        // Apply slippery surface balance and deflection physics
        if (activeSlipperyStick != null && controller.isGrounded)
        {
            Vector3 calculatedSlip = activeSlipperyStick.CalculateSlip(transform.position, movement, groundNormal, false);
            if (calculatedSlip.sqrMagnitude > 0.01f)
            {
                // Gradually increase slip force when rushing or near edge
                slipVelocity = Vector3.Lerp(slipVelocity, calculatedSlip, Time.deltaTime * 8f);
            }
            else
            {
                // Balanced / moving slow - recover balance
                slipVelocity = Vector3.MoveTowards(slipVelocity, Vector3.zero, Time.deltaTime * 20f);
            }
        }
        else
        {
            // Decelerate slip once airborne or on normal ground
            slipVelocity = Vector3.MoveTowards(slipVelocity, Vector3.zero, Time.deltaTime * 15f);
        }

        movement += slipVelocity;

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

        // Let Move finish before deciding whether a contact was a push or a landing.
        CollisionFlags collisions = MovePlayer(movement * Time.deltaTime + fanMovement);

        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            jumpedFromGround = false;
            jumpingWithFan = false;
        }

        if (fanRotation != null
            && (jumpingWithFan || (touchedFanBelow && controller.isGrounded)))
        {
            fanLocalPosition = fanRotation.InverseTransformPoint(transform.position);
        }

        HandleBodyRotationAndLegs(direction);
    }

    private CollisionFlags MovePlayer(Vector3 displacement)
    {
        boxPushCandidates.Clear();
        boxSupports.Clear();
        feetBeforeMove = controller.bounds.min.y;
        collectingBoxContacts = controller.isGrounded && verticalVelocity <= 0f;
        CollisionFlags collisions = controller.Move(displacement);
        collectingBoxContacts = false;

        if ((collisions & CollisionFlags.Below) != 0)
        {
            foreach (Rigidbody box in boxPushCandidates)
            {
                if (!boxSupports.Contains(box))
                    PushBox(box);
            }
        }

        return collisions;
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
        bool animateJump = jumpedFromGround && !controller.isGrounded;
        bool carryingBox = playerPickup != null && playerPickup.IsHoldingBox;

        if (!animateJump || carryingBox)
            jumpArmRaiseProgress = 0f;

        if (carryingBox)
        {
            float carrySpeed = Time.deltaTime * carryArmSpeed;
            Quaternion forwardReach = Quaternion.AngleAxis(-carryArmForwardAngle, Vector3.right);
            // Lift both arms in the player's forward direction, then turn them toward the box center.
            Quaternion leftCarryPose = Quaternion.AngleAxis(carryArmInwardAngle, Vector3.up) * forwardReach * initialLeftHandRot;
            Quaternion rightCarryPose = Quaternion.AngleAxis(-carryArmInwardAngle, Vector3.up) * forwardReach * initialRightHandRot;

            if (leftHand != null)
                SetLeftHandPose(Quaternion.Slerp(leftHandPose, leftCarryPose, carrySpeed));
            if (rightHand != null)
                rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, rightCarryPose, carrySpeed);

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
        else if (animateJump)
        {
            if (jumpArmRaiseProgress == 0f)
            {
                if (leftHand != null)
                    jumpStartLeftHandRot = leftHandPose;
                if (rightHand != null)
                    jumpStartRightHandRot = rightHand.localRotation;
            }

            float armRaiseBlend = 1f - Mathf.Exp(-jumpArmSpeed * Time.deltaTime);
            jumpArmRaiseProgress = Mathf.Lerp(jumpArmRaiseProgress, 1f, armRaiseBlend);
            // Apply the pitch separately so the 180-degree rotation always sweeps forward.
            Quaternion pitch = Quaternion.AngleAxis(-jumpArmRaiseAngle * jumpArmRaiseProgress, Vector3.right);

            if (leftHand != null)
            {
                Quaternion spreadPose = initialLeftHandRot * Quaternion.AngleAxis(-jumpArmSpreadAngle, Vector3.forward);
                SetLeftHandPose(pitch * Quaternion.Slerp(jumpStartLeftHandRot, spreadPose, jumpArmRaiseProgress));
            }
            if (rightHand != null)
            {
                Quaternion spreadPose = initialRightHandRot * Quaternion.AngleAxis(jumpArmSpreadAngle, Vector3.forward);
                rightHand.localRotation = pitch * Quaternion.Slerp(jumpStartRightHandRot, spreadPose, jumpArmRaiseProgress);
            }

            // Legs gently tuck or return to neutral in mid-air
            float legReturnBlend = Time.deltaTime * 12f;
            if (leftLeg != null)
                leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, initialLeftLegRot, legReturnBlend);
            if (rightLeg != null)
                rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, initialRightLegRot, legReturnBlend);
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

            // Each arm swings opposite the leg on the same side.
            float armSwing = swing * handSwingAngle;
            if (leftHand != null)
                SetLeftHandPose(initialLeftHandRot * Quaternion.Euler(-armSwing, 0f, 0f));
            if (rightHand != null)
                rightHand.localRotation = initialRightHandRot * Quaternion.Euler(armSwing, 0f, 0f);
        }
        else
        {
            // Relax the limbs while idle or stepping off a ledge.
            legCycleTime = 0f;
            float returnSpeed = Time.deltaTime * 10f;

            if (leftLeg != null)
                leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, initialLeftLegRot, returnSpeed);
            if (rightLeg != null)
                rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, initialRightLegRot, returnSpeed);

            if (leftHand != null)
                SetLeftHandPose(Quaternion.Slerp(leftHandPose, initialLeftHandRot, returnSpeed));
            if (rightHand != null)
                rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, initialRightHandRot, returnSpeed);
        }
    }

    private void SetLeftHandPose(Quaternion pose)
    {
        leftHandPose = pose;
        if (!leftHandFollowsShoulder)
            leftHand.localRotation = pose;
    }

    private void LateUpdate()
    {
        if (IsHoldingRope)
        {
            Keyboard keyboard = Keyboard.current;
            float climb = 0f;
            if (keyboard != null)
            {
                if (keyboard.eKey.isPressed) climb -= 1f;
                if (keyboard.qKey.isPressed) climb += 1f;
            }

            float distanceClimbed = MoveAlongRope(climb * ropeClimbSpeed * Time.deltaTime);
            AnimateRopeClimb(distanceClimbed);
        }

        // Keep the pivot on the shoulder without inheriting the player's uneven scale.
        if (leftHandFollowsShoulder && leftHand != null)
            leftHand.SetPositionAndRotation(transform.TransformPoint(leftHandAnchor), transform.rotation * leftHandPose);
    }

    private float MoveAlongRope(float distance)
    {
        if (!heldRopePath.IsEndpointsValid)
            return 0f;

        Vector3 start = heldRopePath.startPoint.position;
        Vector3 rope = heldRopePath.endPoint.position - start;
        float length = rope.magnitude;
        if (length < 0.01f)
            return 0f;

        Vector3 playerCenter = transform.TransformPoint(controller.center);
        // Collisions can stop the capsule short of the target while attaching or climbing.
        float positionOnRope = Vector3.Dot(playerCenter - start, rope) / rope.sqrMagnitude;
        float topLimit = Mathf.Clamp01((controller.bounds.extents.y + 0.5f) / length);
        positionOnRope = Mathf.Clamp(positionOnRope, topLimit, 1f);
        float next = Mathf.Clamp(positionOnRope + distance / length, topLimit, 1f);
        controller.Move(heldRopePath.GetPointAt(next) - playerCenter);

        // Count movement along the rope, excluding the snap into place when grabbing it.
        Vector3 actualMovement = transform.TransformPoint(controller.center) - playerCenter;
        float travel = Vector3.Dot(actualMovement, rope / length);
        float requestedTravel = (next - positionOnRope) * length;
        return Mathf.Clamp(travel, Mathf.Min(0f, requestedTravel), Mathf.Max(0f, requestedTravel));
    }

    private void AnimateRopeClimb(float distanceClimbed)
    {
        ropeClimbPhase = Mathf.Repeat(ropeClimbPhase - distanceClimbed / ropeClimbCycleDistance, 1f);
        float reach = Mathf.Sin(ropeClimbPhase * 2f * Mathf.PI);
        float blend = 1f - Mathf.Exp(-20f * Time.deltaTime);

        // The wrists stay raised and turned toward the rope throughout the cycle.
        Quaternion leftGrip = Quaternion.Euler(-130f - 16f * reach, 0f, 34f) * initialLeftHandRot;
        Quaternion rightGrip = Quaternion.Euler(-130f + 16f * reach, 0f, -34f) * initialRightHandRot;
        if (leftHand != null)
            SetLeftHandPose(Quaternion.Slerp(leftHandPose, leftGrip, blend));
        if (rightHand != null)
            rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, rightGrip, blend);

        if (leftLeg != null)
        {
            Quaternion leftBrace = initialLeftLegRot * Quaternion.Euler(-28f + 16f * reach, 0f, 12f);
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, leftBrace, blend);
        }
        if (rightLeg != null)
        {
            Quaternion rightBrace = initialRightLegRot * Quaternion.Euler(-28f - 16f * reach, 0f, -12f);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, rightBrace, blend);
        }
    }

    private void CheckGroundSurface()
    {
        activeSlipperyStick = null;
        groundNormal = Vector3.up;

        Vector3 origin = transform.position + Vector3.up * 0.2f;
        float checkDist = 0.6f;

        if (Physics.SphereCast(origin, 0.25f, Vector3.down, out RaycastHit hit, checkDist, groundCheckLayers, QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
            activeSlipperyStick = hit.collider.GetComponentInParent<SlipperyStick>();
        }
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
        if (!collectingBoxContacts)
            return;

        Rigidbody box = hit.collider.attachedRigidbody;

        if (box == null || box.isKinematic || !box.CompareTag("Pickupable"))
            return;

        float skinHeight = controller.skinWidth * Mathf.Abs(controller.transform.lossyScale.y);
        // Unity can report both a side hit and a support hit during one Move.
        if (hit.normal.y > 0.5f || feetBeforeMove >= hit.collider.bounds.max.y - skinHeight)
        {
            boxSupports.Add(box);
            return;
        }

        if (hit.moveDirection.y >= -0.3f && Vector3.Dot(walkDirection, hit.normal) < -0.01f)
            boxPushCandidates.Add(box);
    }

    private void PushBox(Rigidbody box)
    {
        float remainingSpeed =
            maxPushSpeed - Vector3.Dot(box.linearVelocity, walkDirection);
        if (remainingSpeed <= 0f)
            return;

        float impulse = Mathf.Min(pushForce * Time.deltaTime, remainingSpeed * box.mass);
        box.AddForce(walkDirection * impulse, ForceMode.Impulse);
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
        ropeClimbPhase = 0.25f;
        verticalVelocity = 0f;
        jumpedFromGround = false;
        jumpArmRaiseProgress = 0f;
        jumpingWithFan = false;
        touchedFanBelow = false;
    }
}
