using UnityEngine;

/// <summary>
/// Attach this component to narrow beams, pipes, or sticks (like 'Stick One Way').
/// When the player steps on this object, they must move carefully:
/// - If they move faster than 'safeWalkSpeed', or step too close to the curved edge,
///   they slip off and fall down.
/// - Crouching / gentle tapping / walking slowly keeps their balance.
/// </summary>
public class SlipperyStick : MonoBehaviour
{
    [Header("Speed & Balance Settings")]
    [Tooltip("Maximum allowed speed on the stick without slipping. Default is slow/cautious speed.")]
    public float safeWalkSpeed = 12f;

    [Tooltip("How strongly the player is pushed off to the side when slipping.")]
    public float slipPushForce = 25f;

    [Tooltip("Additional downward pull when slipping so the player slides off the edge.")]
    public float slipDownwardForce = 15f;

    [Header("Curvature / Edge Deflection")]
    [Tooltip("If enabled, being off-center on a curved stick deflects the player outward.")]
    public bool deflectFromCenter = true;

    [Tooltip("How strongly the curved edge pushes the player off as they deviate from center.")]
    public float edgeDeflectionStrength = 18f;

    [Header("Jump Rule")]
    [Tooltip("If true, trying to jump while on this slippery stick causes an immediate slip.")]
    public bool jumpingCausesSlip = true;

    /// <summary>
    /// Calculates the slipping velocity to apply to the player.
    /// Returns Vector3.zero if the player is balanced and safe.
    /// </summary>
    public Vector3 CalculateSlip(Vector3 playerPosition, Vector3 playerMoveVelocity, Vector3 surfaceNormal, bool isJumping)
    {
        Vector3 slipVelocity = Vector3.zero;

        // 1. If player tried jumping on the narrow stick, make them lose footing
        if (isJumping && jumpingCausesSlip)
        {
            Vector3 jumpSlip = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up).normalized;
            if (jumpSlip.sqrMagnitude < 0.01f)
                jumpSlip = transform.right; // fallback lateral push
            return (jumpSlip * slipPushForce) + (Vector3.down * slipDownwardForce);
        }

        // 2. Check movement speed: rushing / running too fast causes a slip
        float horizontalSpeed = new Vector2(playerMoveVelocity.x, playerMoveVelocity.z).magnitude;
        bool isMovingTooFast = horizontalSpeed > safeWalkSpeed;

        // 3. Check normal slope: if the surface under feet is curved/slanted away from pure up
        float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
        bool isOffCenter = slopeAngle > 8f; // rounded edge of the cylinder

        if (isMovingTooFast || isOffCenter)
        {
            // Slip outward along the surface slope/normal direction
            Vector3 outwardSlope = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up).normalized;
            
            // If normal is nearly straight up, push perpendicular to the stick's long axis
            if (outwardSlope.sqrMagnitude < 0.01f)
            {
                // Push sideways relative to player movement or stick forward
                outwardSlope = Vector3.Cross(Vector3.up, transform.forward).normalized;
            }

            float forceMultiplier = 1f;
            if (isMovingTooFast)
            {
                // Faster movement = much harsher slip
                float excessRatio = (horizontalSpeed - safeWalkSpeed) / Mathf.Max(1f, safeWalkSpeed);
                forceMultiplier += excessRatio * 1.5f;
            }

            slipVelocity = outwardSlope * (slipPushForce * forceMultiplier) + (Vector3.down * slipDownwardForce);

            // Add extra momentum in the direction they were running
            if (isMovingTooFast && playerMoveVelocity.sqrMagnitude > 0.1f)
            {
                slipVelocity += playerMoveVelocity.normalized * (slipPushForce * 0.4f);
            }
        }

        return slipVelocity;
    }
}

