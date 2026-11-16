using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FallDeath : MonoBehaviour
{
    private const float FlopDuration = 0.35f;
    private const float DeathPoseHold = 0.75f;

    [SerializeField, Min(0.1f)] private float lethalFallDistance = 80f;
    [SerializeField] private GameObject loseMenu;

    private CharacterController characterController;
    private PlayerController playerController;
    private PlayerPickup playerPickup;
    private MouseLook mouseLook;
    private Transform deathCamera;
    private Transform[] limbPivots;
    private Quaternion[] deathLimbRotations;

    private bool isDead = false;
    private bool showDeathMenu;

    private float? lastSupportedHeight;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        playerPickup = GetComponent<PlayerPickup>();
        mouseLook = GetComponentInChildren<MouseLook>();
        Camera playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
            deathCamera = playerCamera.transform;

        if (playerController == null)
            return;

        limbPivots = new[] { playerController.leftHand, playerController.rightHand,
            playerController.leftLeg, playerController.rightLeg };
        float[] spreadAngles = { -120f, 120f, -40f, 40f };
        deathLimbRotations = new Quaternion[limbPivots.Length];
        for (int i = 0; i < limbPivots.Length; i++)
        {
            if (limbPivots[i] != null)
                deathLimbRotations[i] = Quaternion.AngleAxis(spreadAngles[i], Vector3.forward)
                    * limbPivots[i].localRotation;
        }
    }

    private void LateUpdate()
    {
        if (Time.timeScale == 0f || isDead || characterController == null || !characterController.enabled)
            return;

        float currentHeight = transform.position.y;

        if (playerController != null && playerController.IsHoldingRope)
        {
            lastSupportedHeight = currentHeight;
            return;
        }

        if (!characterController.isGrounded)
            return;

        float fallDistance = lastSupportedHeight.GetValueOrDefault(currentHeight) - currentHeight;
        lastSupportedHeight = currentHeight;
        if (fallDistance >= lethalFallDistance)
            TriggerDeath();
    }

    private void TriggerDeath()
    {
        isDead = true;
        Bounds capsuleBounds = characterController.bounds;

        if (playerController != null)
            playerController.enabled = false;

        if (playerPickup != null)
            playerPickup.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        StartCoroutine(PlayDeathPose(capsuleBounds));
    }

    private IEnumerator PlayDeathPose(Bounds capsuleBounds)
    {
        FindLandingSurface(capsuleBounds, out Vector3 floorPoint, out Vector3 floorNormal);
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 forwardOnFloor = Vector3.ProjectOnPlane(transform.forward, floorNormal).normalized;
        Quaternion faceDown = Quaternion.LookRotation(-floorNormal, forwardOnFloor);
        Renderer[] bodyParts = GetComponentsInChildren<Renderer>();

        Quaternion[] startLimbRotations = new Quaternion[limbPivots?.Length ?? 0];
        for (int i = 0; i < startLimbRotations.Length; i++)
        {
            if (limbPivots[i] != null)
                startLimbRotations[i] = limbPivots[i].localRotation;
        }

        Vector3 cameraStartPosition = deathCamera != null ? deathCamera.position : Vector3.zero;
        Quaternion cameraStartRotation = deathCamera != null ? deathCamera.rotation : Quaternion.identity;
        Vector3 cameraOffset = (floorNormal * 1.1f - forwardOnFloor * 0.55f) * capsuleBounds.size.y;

        float elapsed = 0f;
        while (true)
        {
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / FlopDuration);
            transform.SetPositionAndRotation(startPosition, Quaternion.Slerp(startRotation, faceDown, blend));

            for (int i = 0; i < startLimbRotations.Length; i++)
            {
                if (limbPivots[i] != null)
                    limbPivots[i].localRotation = Quaternion.Slerp(startLimbRotations[i], deathLimbRotations[i], blend);
            }

            RestOnFloor(bodyParts, floorPoint, floorNormal, blend);

            if (deathCamera != null)
            {
                Quaternion cameraRotation = Quaternion.LookRotation(-cameraOffset, floorNormal);
                deathCamera.SetPositionAndRotation(
                    Vector3.Lerp(cameraStartPosition, transform.position + cameraOffset, blend),
                    Quaternion.Slerp(cameraStartRotation, cameraRotation, blend));
            }

            if (elapsed >= FlopDuration)
                break;

            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        yield return new WaitForSecondsRealtime(DeathPoseHold);

        showDeathMenu = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (loseMenu != null)
            loseMenu.SetActive(true);
    }

    private void FindLandingSurface(Bounds capsuleBounds, out Vector3 floorPoint, out Vector3 floorNormal)
    {
        floorPoint = new Vector3(capsuleBounds.center.x, capsuleBounds.min.y, capsuleBounds.center.z);
        floorNormal = Vector3.up;
        float nearest = capsuleBounds.extents.y + 1f;
        RaycastHit[] hits = Physics.RaycastAll(capsuleBounds.center, Vector3.down, nearest,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (!hit.transform.IsChildOf(transform) && hit.distance < nearest)
            {
                nearest = hit.distance;
                floorPoint = hit.point;
                floorNormal = hit.normal;
            }
        }
    }

    private void RestOnFloor(Renderer[] bodyParts, Vector3 floorPoint, Vector3 floorNormal, float blend)
    {
        float lowest = float.PositiveInfinity;
        foreach (Renderer part in bodyParts)
        {
            Bounds bounds = part.localBounds;
            Matrix4x4 toWorld = part.localToWorldMatrix;
            Vector3 center = toWorld.MultiplyPoint3x4(bounds.center);
            float extent = Mathf.Abs(Vector3.Dot(floorNormal, toWorld.MultiplyVector(Vector3.right * bounds.extents.x)))
                + Mathf.Abs(Vector3.Dot(floorNormal, toWorld.MultiplyVector(Vector3.up * bounds.extents.y)))
                + Mathf.Abs(Vector3.Dot(floorNormal, toWorld.MultiplyVector(Vector3.forward * bounds.extents.z)));
            lowest = Mathf.Min(lowest, Vector3.Dot(center - floorPoint, floorNormal) - extent);
        }

        if (bodyParts.Length > 0)
            transform.position -= floorNormal * Mathf.Lerp(Mathf.Min(0f, lowest), lowest, blend);
    }

    private void OnGUI()
    {
        if (!showDeathMenu || loseMenu != null) return;

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTexture(
            new Color(1f, 1f, 1f, 0.95f)
        );

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 36;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.red;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;
        buttonStyle.fontStyle = FontStyle.Bold;

        float panelWidth = 350;
        float panelHeight = 200;

        float x = (Screen.width - panelWidth) / 2;
        float y = (Screen.height - panelHeight) / 2;

        GUI.Box(
            new Rect(x, y, panelWidth, panelHeight),
            "",
            panelStyle
        );

        GUI.Label(
            new Rect(x + 20, y + 20, panelWidth - 40, 60),
            "YOU DIED",
            titleStyle
        );

        if (GUI.Button(
            new Rect(x + 75, y + 110, 200, 55),
            "RESTART",
            buttonStyle))
        {
            RestartGame();
        }
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    public void RestartGame()
    {
        ReloadScene(skipStartMenu: true);
    }

    public void ReturnToStartMenu()
    {
        ReloadScene(skipStartMenu: false);
    }

    private void ReloadScene(bool skipStartMenu)
    {
        Time.timeScale = 1f;

        Scene scene = SceneManager.GetActiveScene();
        if (skipStartMenu)
            MainMenuManager.StartImmediatelyOnReload(scene.path);
#if UNITY_EDITOR
        if (SceneUtility.GetBuildIndexByScenePath(scene.path) < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(scene.buildIndex);
    }
}
