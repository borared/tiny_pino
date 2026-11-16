using UnityEngine;

public class InteractionOutline : MonoBehaviour
{
    [SerializeField] private GameObject outlineVisual;

    public void SetVisible(bool visible)
    {
        if (outlineVisual != null)
            outlineVisual.SetActive(visible);
    }
}