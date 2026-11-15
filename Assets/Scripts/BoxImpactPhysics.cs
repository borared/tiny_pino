using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class BoxImpactPhysics : MonoBehaviour
{
    [SerializeField] private float dropGravity = 45f;

    private static readonly HashSet<int> BoxColliders = new HashSet<int>();
    private static readonly Dictionary<int, HashSet<int>> CarriedBoxContacts = new();
    private static bool contactCallbacksEnabled;
    private Rigidbody body;
    private BoxCollider boxCollider;
    private int colliderId;
    private bool dropping;
    private bool supported;

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
            BoxColliders.Add(colliderId);
            UpdateContactCallbacks();
        }

        boxCollider.hasModifiableContacts = true;
    }

    private void OnDisable()
    {
        boxCollider.hasModifiableContacts = false;

        lock (BoxColliders)
        {
            BoxColliders.Remove(colliderId);
            UpdateContactCallbacks();
        }
    }

    internal static void RegisterCarriedBox(Rigidbody carriedBox)
    {
        // Capture IDs on the main thread, including pickupables without BoxImpactPhysics.
        var otherBoxes = new HashSet<int>();
        foreach (GameObject pickupable in GameObject.FindGameObjectsWithTag("Pickupable"))
        {
            if (pickupable.TryGetComponent(out Rigidbody otherBox) && otherBox != carriedBox)
                otherBoxes.Add(otherBox.GetInstanceID());
        }

        lock (BoxColliders)
        {
            CarriedBoxContacts[carriedBox.GetInstanceID()] = otherBoxes;
            UpdateContactCallbacks();
        }
    }

    internal static void UnregisterCarriedBox(int bodyId)
    {
        lock (BoxColliders)
        {
            CarriedBoxContacts.Remove(bodyId);
            UpdateContactCallbacks();
        }
    }

    private static void UpdateContactCallbacks()
    {
        bool enabled = BoxColliders.Count > 0 || CarriedBoxContacts.Count > 0;
        if (enabled == contactCallbacksEnabled)
            return;

        if (enabled)
        {
            Physics.ContactModifyEvent += ModifyContacts;
            Physics.ContactModifyEventCCD += ModifyCcdContacts;
        }
        else
        {
            Physics.ContactModifyEvent -= ModifyContacts;
            Physics.ContactModifyEventCCD -= ModifyCcdContacts;
        }

        contactCallbacksEnabled = enabled;
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
            if (pair.contactCount == 0)
                continue;

            int firstId = pair.colliderInstanceID;
            int secondId = pair.otherColliderInstanceID;
            bool firstCarried;
            bool secondCarried;

            lock (BoxColliders)
            {
                int firstBody = pair.bodyInstanceID;
                int secondBody = pair.otherBodyInstanceID;
                firstCarried = CarriedBoxContacts.TryGetValue(firstBody, out var firstContacts) &&
                    firstContacts.Contains(secondBody);
                secondCarried = CarriedBoxContacts.TryGetValue(secondBody, out var secondContacts) &&
                    secondContacts.Contains(firstBody);

                if (firstCarried == secondCarried &&
                    (!BoxColliders.Contains(firstId) || !BoxColliders.Contains(secondId)))
                    continue;
            }

            // Yield to the stack while the hold point is driving one of the boxes.
            bool firstBoxResponds = firstCarried;
            if (firstCarried != secondCarried)
            {
                // Let the carried box slide out from under the stack.
                for (int i = 0; i < pair.contactCount; i++)
                {
                    pair.SetStaticFriction(i, 0f);
                    pair.SetDynamicFriction(i, 0f);
                }
            }
            else
            {
                Vector3 normal = pair.GetNormal(0) * normalSign;
                float firstApproach = -Vector3.Dot(pair.bodyVelocity, normal);
                float secondApproach = Vector3.Dot(pair.otherBodyVelocity, normal);

                // The upper box absorbs vertical impacts even when the stack bounces.
                firstBoxResponds = normal.y > 0f;
                if (Mathf.Abs(normal.y) <= 0.5f)
                    firstBoxResponds = Mathf.Abs(firstApproach - secondApproach) < 0.02f
                        ? firstId < secondId
                        : firstApproach > secondApproach;
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
        supported = false;
    }

    private void FixedUpdate()
    {
        if (supported)
        {
            dropping = false;
        }
        else if (body.useGravity && !body.isKinematic &&
                 (dropping || body.linearVelocity.y < -0.1f))
        {
            body.AddForce(Vector3.down * dropGravity - Physics.gravity,
                          ForceMode.Acceleration);
        }

        // Enter/Stay renew support after the next physics simulation.
        supported = false;
    }

    private void OnCollisionEnter(Collision hit) => HandleContact(hit);
    private void OnCollisionStay(Collision hit) => HandleContact(hit);

    private void HandleContact(Collision hit)
    {
        for (int i = 0; i < hit.contactCount; i++)
        {
            if (hit.GetContact(i).normal.y > 0.5f)
            {
                supported = true;
                return;
            }
        }
    }
}
