using UnityEngine;

public class WinMenuManager : MonoBehaviour
{
    [Header("Menu Panel / Canvas")]
    [SerializeField] private GameObject menuPanel;

    [Header("Player & Camera Controls")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MouseLook mouseLook;

    [Header("Win Condition")]
    [SerializeField] private Collider goalCollider;

    public void CheckGoalHit(Collider hitCollider)
    {
        if (hitCollider != goalCollider || menuPanel == null || menuPanel.activeSelf)
            return;

        if (playerController != null)
            playerController.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menuPanel.SetActive(true);
    }
}