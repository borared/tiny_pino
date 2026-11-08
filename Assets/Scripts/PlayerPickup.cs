using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerPickup : MonoBehaviour
{
    //box pick up
    private Rigidbody box;
    private Collider boxCollider;
    public Transform holdPoint;
    public TMP_Text prompt;
    public float pickupDistance = 3f;
    public float followStrength = 10f;
    public float maxCarrySpeed = 50f;

    private RigidbodyConstraints originalConstraints;
    private bool holdingBox;

    //box outline
    private InteractionOutline currentOutline;

    //fan
    [SerializeField] private FanSwitch fanSwitch;
    [SerializeField] private float switchDistance = 12f;

    void Start()
    {
        prompt.gameObject.SetActive(false);
    }

    void Update()
    {
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

        Vector3 difference =
            holdPoint.position - box.worldCenterOfMass;

        Vector3 desiredVelocity = difference * followStrength;

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

        box.isKinematic = false;
        box.detectCollisions = true;
        box.useGravity = false;

        box.linearVelocity = Vector3.zero;
        box.angularVelocity = Vector3.zero;
        box.constraints = RigidbodyConstraints.FreezeRotation;

        // Ignore the player's body while carrying.
        Physics.IgnoreCollision(
            boxCollider,
            GetComponent<CharacterController>(),
            true
        );
    }

    void DropBox()
    {
        holdingBox = false;

        box.useGravity = true;
        box.constraints = originalConstraints;
        box.linearVelocity = Vector3.zero;
        box.angularVelocity = Vector3.zero;

        Physics.IgnoreCollision(
            boxCollider,
            GetComponent<CharacterController>(),
            false
        );
    }

    void FindNearestBox()
    {
        box = null;
        boxCollider = null;

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
                boxCollider = candidate;
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
        SetOutline(null);
    }
}