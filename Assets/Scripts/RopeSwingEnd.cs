using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
public class RopeSwingEnd : MonoBehaviour
{
    [SerializeField] private Transform ceilingAnchor;
    [SerializeField] private float ropeMass = 3f;
    [SerializeField] private RopeMoment ropePath;
    public RopeMoment RopePath => ropePath;

    private void Awake()
    {
        if (ceilingAnchor == null)
        {
            Debug.LogError("Assign the ceiling anchor on RopeSwingEnd.", this);
            return;
        }

        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        joint.connectedBody = null;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = ceilingAnchor.position;

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        SoftJointLimit limit = joint.linearLimit;
        limit.limit = Vector3.Distance(transform.position, ceilingAnchor.position);
        joint.linearLimit = limit;

        Rigidbody body = GetComponent<Rigidbody>();
        body.mass += ropeMass / 3f;
    }
}