
using UnityEngine;
using UnityEngine.SceneManagement;

public class FallDeath : MonoBehaviour
{
    public float shelfHeightThreshold = 8f;

    private CharacterController characterController;
    private PlayerController playerController;
    private PlayerPickup playerPickup;

    private bool wasOnHighPlatform = false;
    private bool leftPlatform = false;
    private bool isDead = false;

    private float platformHeight;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        playerPickup = GetComponent<PlayerPickup>();
    }

    private void Update()
    {
        if (isDead || characterController == null)
            return;

        float currentHeight = transform.position.y;

        if (characterController.isGrounded &&
            currentHeight >= shelfHeightThreshold)
        {
            wasOnHighPlatform = true;
            platformHeight = currentHeight;
        }

        if (wasOnHighPlatform &&
            currentHeight < platformHeight - 0.5f)
        {
            leftPlatform = true;
        }

        if (wasOnHighPlatform &&
            leftPlatform &&
            characterController.isGrounded &&
            currentHeight < shelfHeightThreshold - 1f)
        {
            TriggerDeath();
        }
    }

    private void TriggerDeath()
    {
        isDead = true;

        if (playerController != null)
            playerController.enabled = false;

        if (playerPickup != null)
            playerPickup.enabled = false;

        if (characterController != null)
            characterController.enabled = false;
    }

    private void OnGUI()
    {
        if (!isDead) return;

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
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
