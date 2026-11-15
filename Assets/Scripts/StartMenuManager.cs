using UnityEngine;

/// <summary>
/// Manages starting the game, quitting, freezing/unfreezing player controls,
/// and hiding/showing the main menu panel.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panel / Canvas")]
    [Tooltip("The menu GameObject to hide when the game starts.")]
    public GameObject menuPanel;

    [Header("Player & Camera Controls")]
    [Tooltip("PlayerController to freeze during menu, then enable on start.")]
    public PlayerController playerController;

    [Tooltip("MouseLook to freeze during menu, then enable on start.")]
    public MouseLook mouseLook;

    private bool gameStarted = false;

    void Awake()
    {
        if (menuPanel == null)
            menuPanel = gameObject;

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (mouseLook == null)
            mouseLook = FindFirstObjectByType<MouseLook>();

        // Freeze player movement and camera look while in the menu
        if (playerController != null)
            playerController.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        // Unlock mouse cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Starts the game: hides the menu and unlocks player & camera controls.
    /// Can be called directly or linked to UI Button OnClick().
    /// </summary>
    public void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;

        // 1. Hide the menu panel
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 2. Enable player & camera
        if (playerController != null)
            playerController.enabled = true;

        if (mouseLook != null)
            mouseLook.enabled = true;

        // 3. Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Quits the game or exits playmode in the Unity editor.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

