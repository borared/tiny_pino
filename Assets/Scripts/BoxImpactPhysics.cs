using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoxImpactPhysics : MonoBehaviour
{
    [SerializeField] private float dropGravity = 45f;
    [SerializeField] private float floatGravity = 4f;
    [SerializeField] private float liftSpeed = 5f;
    [SerializeField] private float floatDuration = 2.5f;
    [SerializeField] private float impactRadius = 12f;
    private float liftGameTime = -1f;
    private float liftRealTime;

    private Rigidbody body;
    private bool dropping;
    private bool touchedGround;
    private float floatUntil;

    private void Awake() => body = GetComponent<Rigidbody>();

    public void BeginDrop()
    {
        CancelEffect();
        dropping = true;
    }

    public void CancelEffect()
    {
        dropping = false;
        touchedGround = false;
        floatUntil = 0f;
    }

    private void FixedUpdate()
    {
        if (touchedGround)
        {
            dropping = false;
            touchedGround = false;
        }

        if (liftGameTime >= 0f && body.linearVelocity.y <= 0f)
        {
            Debug.Log($"{name} reached the top in " +
                      $"{Time.fixedTime - liftGameTime:F2}s game time, " +
                      $"{Time.realtimeSinceStartup - liftRealTime:F2}s real time", this);
            liftGameTime = -1f;
        }

        if (!body.useGravity || body.isKinematic)
            return;

        float verticalSpeed = body.linearVelocity.y;

        if (Time.fixedTime < floatUntil && verticalSpeed > 0f)
            ApplyGravity(floatGravity);
        else if (dropping || Mathf.Abs(verticalSpeed) > 0.1f)
            ApplyGravity(dropGravity);
    }

    private void ApplyGravity(float strength)
    {
        // Add the difference because Unity is already applying scene gravity.
        body.AddForce(Vector3.down * strength - Physics.gravity,
                      ForceMode.Acceleration);
    }

    private void OnCollisionEnter(Collision hit) => HandleContact(hit);
    private void OnCollisionStay(Collision hit) => HandleContact(hit);

    private void HandleContact(Collision hit)
    {
        if (!dropping)
            return;

        if (hit.rigidbody != null &&
            hit.rigidbody.TryGetComponent(out BoxImpactPhysics struck) &&
            struck.isActiveAndEnabled)
        {
            Lift();
            struck.Lift();

            Vector3 center = hit.GetContact(0).point;
            foreach (Collider nearby in Physics.OverlapSphere(
                         center, impactRadius, Physics.AllLayers,
                         QueryTriggerInteraction.Ignore))
            {
                Rigidbody other = nearby.attachedRigidbody;
                if (other != null && other.TryGetComponent(out BoxImpactPhysics box))
                    box.Lift();
            }

            return;
        }

        for (int i = 0; i < hit.contactCount; i++)
        {
            if (hit.GetContact(i).normal.y > 0.5f)
                touchedGround = true;
        }
    }

    private void Lift()
    {
        if (!isActiveAndEnabled || !body.useGravity || body.isKinematic ||
            Time.fixedTime < floatUntil)
            return;

        dropping = false;
        floatUntil = Time.fixedTime + floatDuration;

        Vector3 velocity = body.linearVelocity;
        velocity.y = liftSpeed;
        liftGameTime = Time.fixedTime;
        liftRealTime = Time.realtimeSinceStartup;
        Debug.Log($"{name} lift: speed {liftSpeed}, gravity {floatGravity}", this);
        body.WakeUp();
    }
}