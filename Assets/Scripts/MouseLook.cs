using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public Transform player;
    public float sensitivity = 0.15f;

    private float pitch;

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
        player.Rotate(0f, look.x, 0f);

        // Tilt the camera pivot up and down.
        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, -60f, 60f);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}