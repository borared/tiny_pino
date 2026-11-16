using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Animator), typeof(Canvas), typeof(CanvasGroup))]
public class PauseMenuManager : MonoBehaviour
{
    private static readonly int FadeOut = Animator.StringToHash("Base Layer.StartMenuFadeOut");

    [SerializeField] private PlayerController playerController;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private FallDeath fallDeath;

    private Animator animator;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private float previousTimeScale;
    private bool isPaused;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.enabled = false;
        canvas = GetComponent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        SetMenuVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            return;

        if (isPaused)
            ContinueGame();
        else if (playerController.isActiveAndEnabled && mouseLook.isActiveAndEnabled)
            PauseGame();
    }

    private void PauseGame()
    {
        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        playerController.enabled = false;
        mouseLook.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvasGroup.alpha = 1f;
        SetMenuVisible(true);
    }

    public void ContinueGame()
    {
        if (!isPaused || !canvasGroup.interactable)
            return;

        canvasGroup.interactable = false;
        animator.enabled = true;
        animator.Play(FadeOut, 0, 0f);
    }

    // The shared menu fade clip calls this at the end of the animation.
    private void StartGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        animator.enabled = false;
        SetMenuVisible(false);
        Time.timeScale = previousTimeScale;
        playerController.enabled = true;
        mouseLook.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToStartMenu()
    {
        if (!isPaused || !canvasGroup.interactable)
            return;

        isPaused = false;
        canvasGroup.interactable = false;
        fallDeath.ReturnToStartMenu();
    }

    private void SetMenuVisible(bool visible)
    {
        canvas.enabled = visible;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void OnDisable()
    {
        if (isPaused)
            Time.timeScale = previousTimeScale;
    }
}
