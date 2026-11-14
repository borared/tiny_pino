using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerPickup : MonoBehaviour
{
    //rope interact
    [SerializeField] private float ropeGrabDistance = 3f;
    private PlayerController ropePlayer;

    //box pick up
    private Rigidbody box;
    public Transform holdPoint;
    public TMP_Text prompt;
    public float pickupDistance = 3f;
    [SerializeField, Min(0f)] private float carryDistance = 1.5f;
    public float followStrength = 28f;
    public float maxCarrySpeed = 65f;

    private RigidbodyConstraints originalConstraints;
    private Quaternion heldFaceToBoxRotation;
    private readonly List<(Collider boxCollider, Collider playerCollider)> ignoredCarryCollisions = new();
    private bool holdingBox;
    public bool IsHoldingBox => holdingBox;
    public Rigidbody HeldBox => holdingBox ? box : null;

    //box outline
    private InteractionOutline currentOutline;

    //fan
    [SerializeField] private FanSwitch fanSwitch;
    [SerializeField] private float switchDistance = 12f;

    void Start()
    {
        ropePlayer = GetComponent<PlayerController>();
        prompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (TryHandleRope())
            return;
        bool nearSwitch = !holdingBox && fanSwitch != null &&
            fanSwitch.IsInRange(transform.position, switchDistance);

        if (!holdingBox)
            FindNearestBox();

        bool nearBox = box != null;

        InteractionOutline nextOutline = null;

        if (nearSwitch)
            nextOutline = fanSwitch.GetComponent<InteractionOutline>();
        else if (!holdingBox && nearBox)
            nextOutline = box.GetComponent<InteractionOutline>();

        SetOutline(nextOutline);

        prompt.gameObject.SetActive(holdingBox || nearSwitch || nearBox);

        if (holdingBox)
            prompt.text = "Press F to drop";
        else if (nearSwitch)
            prompt.text = fanSwitch.IsOn
                ? "Press F to turn off switch"
                : "Press F to turn on switch";
        else
            prompt.text = "Press F to pick up";

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.fKey.wasPressedThisFrame)
            return;

        if (holdingBox)
            DropBox();
        else if (nearSwitch)
            fanSwitch.Toggle();
        else if (nearBox)
            PickUpBox();
    }

    void FixedUpdate()
    {
        if (!holdingBox)
            return;

        if (box == null)
        {
            DropBox();
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(DirectionToPlayer(), Vector3.up)
                                    * heldFaceToBoxRotation;
        box.MoveRotation(targetRotation);

        // Use the new rotation when the collider is offset from the box's pivot.
        Vector3 rotatedCenterOfMass = box.position + targetRotation * box.centerOfMass;
        Vector3 localHoldPosition = transform.InverseTransformPoint(holdPoint.position);
        localHoldPosition.z = carryDistance;
        Vector3 difference = transform.TransformPoint(localHoldPosition) - rotatedCenterOfMass;

        // Keep the correction within the distance to the hold point in one physics step.
        float correctionRate = Mathf.Min(followStrength, 1f / Time.fixedDeltaTime);
        Vector3 desiredVelocity = difference * correctionRate;

        box.linearVelocity = Vector3.ClampMagnitude(
            desiredVelocity,
            maxCarrySpeed
        );
    }

    void PickUpBox()
    {
        holdingBox = true;
        SetOutline(null);
        originalConstraints = box.constraints;

        // Imported boxes can use a different local axis for their top face.
        Quaternion worldToBox = Quaternion.Inverse(box.rotation);
        Vector3 localUp = ClosestBoxAxis(worldToBox * Vector3.up);
        Vector3 localSide = Vector3.ProjectOnPlane(worldToBox * DirectionToPlayer(), localUp);
        Vector3 localFace = ClosestBoxAxis(localSide);
        heldFaceToBoxRotation = Quaternion.Inverse(Quaternion.LookRotation(localFace, localUp));

        box.isKinematic = false;
        box.detectCollisions = true;
        box.useGravity = false;

        box.GetComponent<BoxImpactPhysics>()?.CancelEffect();

        box.linearVelocity = Vector3.zero;
        box.angularVelocity = Vector3.zero;
        box.constraints = RigidbodyConstraints.FreezeRotation;

        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        foreach (Collider boxCollider in box.GetComponentsInChildren<Collider>())
        {
            if (boxCollider.attachedRigidbody != box || !boxCollider.enabled || boxCollider.isTrigger)
                continue;

            foreach (Collider playerCollider in playerColliders)
            {
                if (!playerCollider.enabled || playerCollider.isTrigger ||
                    Physics.GetIgnoreCollision(boxCollider, playerCollider))
                    continue;

                Physics.IgnoreCollision(boxCollider, playerCollider, true);
                ignoredCarryCollisions.Add((boxCollider, playerCollider));
            }
        }
    }

    private Vector3 DirectionToPlayer()
    {
        Vector3 direction = transform.position - box.worldCenterOfMass;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.ProjectOnPlane(-transform.forward, Vector3.up);

        return direction;
    }

    private static Vector3 ClosestBoxAxis(Vector3 direction)
    {
        Vector3 absolute = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
        if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            return Vector3.right * Mathf.Sign(direction.x);
        if (absolute.y >= absolute.z)
            return Vector3.up * Mathf.Sign(direction.y);

        return Vector3.forward * Mathf.Sign(direction.z);
    }

    void DropBox()
    {
        holdingBox = false;

        if (box != null)
        {
            box.useGravity = true;
            box.constraints = originalConstraints;
            box.linearVelocity = Vector3.zero;
            box.GetComponent<BoxImpactPhysics>()?.BeginDrop();
            box.angularVelocity = Vector3.zero;
        }

        foreach (var (boxCollider, playerCollider) in ignoredCarryCollisions)
        {
            if (boxCollider != null && playerCollider != null)
                Physics.IgnoreCollision(boxCollider, playerCollider, false);
        }
        ignoredCarryCollisions.Clear();
    }

    void FindNearestBox()
    {
        box = null;

        float closestDistance = pickupDistance;

        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            pickupDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider candidate in nearbyColliders)
        {
            Rigidbody body = candidate.attachedRigidbody;

            if (body == null || !body.CompareTag("Pickupable"))
                continue;

            Vector3 nearestPoint =
                candidate.ClosestPoint(transform.position);

            float distance =
                Vector3.Distance(transform.position, nearestPoint);

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                box = body;
            }
        }
    }

    private void SetOutline(InteractionOutline next)
    {
        if (currentOutline == next)
            return;

        if (currentOutline != null)
            currentOutline.SetVisible(false);

        currentOutline = next;

        if (currentOutline != null)
            currentOutline.SetVisible(true);
    }

    private void OnDisable()
    {
        if (holdingBox)
            DropBox();

        SetOutline(null);
    }

    private bool TryHandleRope()
    {
        bool holdingRope = ropePlayer.IsHoldingRope;
        Collider handle = !holdingBox && ropePlayer.CanGrabRope
            ? FindNearestRopeHandle()
            : null;

        if (!holdingRope && handle == null)
            return false;

        SetOutline(null);
        prompt.gameObject.SetActive(true);
        prompt.text = holdingRope ? "E to climb up, Q to climb down, F to release" : "Press F to grab rope";

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            ropePlayer.InteractWithRope(handle);

        return true;
    }

    private Collider FindNearestRopeHandle()
    {
        Collider nearest = null;
        float bestDistance = float.PositiveInfinity;

        foreach (Collider candidate in Physics.OverlapSphere(
            transform.position, ropeGrabDistance, Physics.AllLayers,
            QueryTriggerInteraction.Collide))
        {
            if (!candidate.TryGetComponent(out RopeSwingEnd rope) || !rope.isActiveAndEnabled || rope.RopePath == null || !rope.RopePath.IsEndpointsValid)
                continue;

            float distance = (candidate.ClosestPoint(transform.position)
                              - transform.position).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }
}
