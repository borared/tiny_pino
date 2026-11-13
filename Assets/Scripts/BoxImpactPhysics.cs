using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class BoxImpactPhysics : MonoBehaviour
{
    [SerializeField] private float dropGravity = 45f;

    private static readonly HashSet<int> BoxColliders = new HashSet<int>();
    private Rigidbody body;
    private BoxCollider boxCollider;
    private int colliderId;
    private bool dropping;
    private bool touchedGround;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        colliderId = boxCollider.GetInstanceID();
    }

    private void OnEnable()
    {
        lock (BoxColliders)
        {
            if (BoxColliders.Count == 0)
            {
                Physics.ContactModifyEvent += ModifyContacts;
                Physics.ContactModifyEventCCD += ModifyCcdContacts;
            }

            BoxColliders.Add(colliderId);
        }

        boxCollider.hasModifiableContacts = true;
    }

    private void OnDisable()
    {
        boxCollider.hasModifiableContacts = false;

        lock (BoxColliders)
        {
            BoxColliders.Remove(colliderId);

            if (BoxColliders.Count == 0)
            {
                Physics.ContactModifyEvent -= ModifyContacts;
                Physics.ContactModifyEventCCD -= ModifyCcdContacts;
            }
        }
    }

    private static void ModifyContacts(
        PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        ApplyBoxContacts(pairs, 1f);
    }

    private static void ModifyCcdContacts(
        PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        // Unity reverses the contact normal in the CCD callback.
        ApplyBoxContacts(pairs, -1f);
    }

    private static void ApplyBoxContacts(
        NativeArray<ModifiableContactPair> pairs, float normalSign)
    {
        foreach (var contactPair in pairs)
        {
            var pair = contactPair;
            int firstId = pair.colliderInstanceID;
            int secondId = pair.otherColliderInstanceID;

            lock (BoxColliders)
            {
                if (!BoxColliders.Contains(firstId) || !BoxColliders.Contains(secondId))
                    continue;
            }

            if (pair.contactCount == 0)
                continue;

            Vector3 normal = pair.GetNormal(0) * normalSign;
            float firstApproach = -Vector3.Dot(pair.bodyVelocity, normal);
            float secondApproach = Vector3.Dot(pair.otherBodyVelocity, normal);

            bool firstBoxResponds = firstApproach > secondApproach;
            if (Mathf.Abs(firstApproach - secondApproach) < 0.02f)
            {
                // Let the upper box respond on a stack. Break side-on ties by ID.
                firstBoxResponds = Mathf.Abs(normal.y) > 0.5f
                    ? normal.y > 0f
                    : firstId < secondId;
            }

            var mass = pair.massProperties;
            if (firstBoxResponds)
            {
                mass.otherInverseMassScale = 0f;
                mass.otherInverseInertiaScale = 0f;
            }
            else
            {
                mass.inverseMassScale = 0f;
                mass.inverseInertiaScale = 0f;
            }

            pair.massProperties = mass;
        }
    }

    public void BeginDrop()
    {
        CancelEffect();
        dropping = true;
    }

    public void CancelEffect()
    {
        dropping = false;
        touchedGround = false;
    }

    private void FixedUpdate()
    {
        if (touchedGround)
            CancelEffect();

        if (body.useGravity && !body.isKinematic &&
            (dropping || Mathf.Abs(body.linearVelocity.y) > 0.1f))
        {
            body.AddForce(Vector3.down * dropGravity - Physics.gravity,
                          ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision hit) => HandleContact(hit);
    private void OnCollisionStay(Collision hit) => HandleContact(hit);

    private void HandleContact(Collision hit)
    {
        if (!dropping)
            return;

        for (int i = 0; i < hit.contactCount; i++)
        {
            if (hit.GetContact(i).normal.y > 0.5f)
            {
                touchedGround = true;
                return;
            }
        }
    }
}