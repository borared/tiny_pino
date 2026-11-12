using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Target & Input")]
    public Transform player;
    public float sensitivity = 0.15f;

    [Header("Camera Distance & Collision")]
    [Tooltip("The camera transform. If left empty, it will auto-detect the Camera child.")]
    public Transform cameraTransform;

    [Tooltip("Layers the camera should collide with (walls, obstacles, platforms).")]
    public LayerMask collisionLayers = ~0;

    [Tooltip("Radius of the collision spherecast to keep camera away from wall edges.")]
    public float collisionRadius = 0.3f;

    [Tooltip("Minimum distance from the pivot the camera can pull into when an obstacle is close.")]
    public float minDistance = 0.5f;

    [Tooltip("Offset distance from the obstacle surface so the camera's near clip plane doesn't penetrate.")]
    public float surfaceOffset = 0.15f;

    [Tooltip("Speed of camera adjustment when pulling in or extending back out.")]
    public float smoothSpeed = 20f;

    private float pitch;
    private Vector3 defaultCameraLocalPos;
    private Vector3 localDirection;
    private float defaultDistance;
    private float currentDistance;

    // Cache colliders belonging to the player hierarchy to prevent self-hits
    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

    void Awake()
    {
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }

        if (cameraTransform != null)
        {
            defaultCameraLocalPos = cameraTransform.localPosition;
            defaultDistance = defaultCameraLocalPos.magnitude;
            localDirection = defaultDistance > 0.001f ? defaultCameraLocalPos.normalized : Vector3.back;
            currentDistance = defaultDistance;
        }

        CachePlayerColliders();
    }

    void OnValidate()
    {
        if (cameraTransform != null && defaultDistance <= 0.001f)
        {
            defaultCameraLocalPos = cameraTransform.localPosition;
            defaultDistance = defaultCameraLocalPos.magnitude;
            localDirection = defaultDistance > 0.001f ? defaultCameraLocalPos.normalized : Vector3.back;
        }
    }

    private void CachePlayerColliders()
    {
        playerColliders.Clear();

        // Check assigned player or root transform
        Transform root = player != null ? player.root : transform.root;
        if (root != null)
        {
            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in cols)
            {
                playerColliders.Add(c);
            }
        }
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (mouse == null)
            return;

        // Escape releases the mouse.
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // Click inside the game to capture the mouse.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            return;
        }

        Vector2 look = mouse.delta.ReadValue() * sensitivity;

        // Turn the whole player left and right.
        if (player != null)
        {
            player.Rotate(0f, look.x, 0f);
        }

        // Tilt the camera pivot up and down.
        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, -60f, 60f);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void LateUpdate()
    {
        HandleCameraCollision();
    }

    private void HandleCameraCollision()
    {
        if (cameraTransform == null)
            return;

        // Origin at the pivot in world space
        Vector3 origin = transform.position;

        // Target position where the camera naturally wants to be in world space
        Vector3 targetWorldPos = transform.TransformPoint(defaultCameraLocalPos);
        Vector3 direction = targetWorldPos - origin;
        float maxWorldDist = direction.magnitude;

        if (maxWorldDist < 0.001f)
            return;

        direction.Normalize();

        float targetDist = maxWorldDist;

        // Use RaycastAll to filter out player/child colliders and triggers reliably
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            collisionRadius,
            direction,
            maxWorldDist,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestValidDistance = maxWorldDist;
        bool foundHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            // Ignore player character colliders and own children
            if (playerColliders.Contains(hit.collider))
                continue;

            if (player != null && (hit.transform == player || hit.transform.IsChildOf(player)))
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            // Take the closest obstacle hit
            if (hit.distance < closestValidDistance)
            {
                closestValidDistance = hit.distance;
                foundHit = true;
            }
        }

        if (foundHit)
        {
            // Back off slightly from the surface so near clipping plane doesn't show inside the wall
            targetDist = Mathf.Clamp(closestValidDistance - surfaceOffset, minDistance, maxWorldDist);
        }

        // Convert world distance back to local camera position along its local direction
        float localRatio = defaultDistance / Mathf.Max(0.001f, maxWorldDist);
        float targetLocalDist = targetDist * localRatio;

        // If pulling in closer (collision), snap quickly so wall is never seen through; smooth when pulling out
        float lerpFactor = targetLocalDist < currentDistance ? smoothSpeed * 2f : smoothSpeed;
        currentDistance = Mathf.Lerp(currentDistance, targetLocalDist, Time.deltaTime * lerpFactor);

        cameraTransform.localPosition = localDirection * currentDistance;
    }
}

