using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles UI visual hover effects: changing text strings (e.g. adding '> <'),
/// color transitions, and scale punching when hovering.
/// Also forwards clicks to MainMenuManager or UI Buttons.
/// </summary>
public class TextHoverEffect : MonoBehaviour
{
    [Header("Menu Manager Reference")]
    [Tooltip("Optional: reference to MainMenuManager. If empty, auto-detected in the scene.")]
    public MainMenuManager menuManager;

    [Header("Start Game Button")]
    [Tooltip("TMP Text component for Start Game")]
    public TMP_Text startGameText;
    public string startGameNormal = "Start Game";
    public string startGameHover = "> Start Game <";

    [Header("Quit Button")]
    [Tooltip("TMP Text component for Quit")]
    public TMP_Text quitText;
    public string quitNormal = "Quit";
    public string quitHover = "> Quit <";

    [Header("Colors")]
    public bool changeColor = true;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.85f, 0.2f); // Golden yellow

    [Header("Scale Punch On Hover (Optional)")]
    public bool scaleOnHover = true;
    public float hoverScaleMultiplier = 1.1f;

    private Vector3 startOriginalScale = Vector3.one;
    private Vector3 quitOriginalScale = Vector3.one;

    void Awake()
    {
        if (menuManager == null)
            menuManager = FindFirstObjectByType<MainMenuManager>();
    }

    void Start()
    {
        SetupButton(startGameText, ref startGameNormal, ref startOriginalScale, OnStartEnter, OnStartExit, null);
        SetupButton(quitText, ref quitNormal, ref quitOriginalScale, OnQuitEnter, OnQuitExit, OnQuitClick);
    }

    private void SetupButton(
        TMP_Text textComp,
        ref string defaultText,
        ref Vector3 originalScale,
        UnityEngine.Events.UnityAction<BaseEventData> onEnter,
        UnityEngine.Events.UnityAction<BaseEventData> onExit,
        UnityEngine.Events.UnityAction<BaseEventData> onClick)
    {
        if (textComp == null) return;

        textComp.raycastTarget = true;

        if (string.IsNullOrEmpty(defaultText))
            defaultText = textComp.text;

        originalScale = textComp.transform.localScale;

        EventTrigger trigger = textComp.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = textComp.gameObject.AddComponent<EventTrigger>();

        // Hover enter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(onEnter);
        trigger.triggers.Add(enterEntry);

        // Hover exit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(onExit);
        trigger.triggers.Add(exitEntry);

        // Click
        if (onClick != null)
        {
            var clickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            clickEntry.callback.AddListener(onClick);
            trigger.triggers.Add(clickEntry);
        }

        // Initial appearance
        textComp.text = defaultText;
        if (changeColor)
            textComp.color = normalColor;
    }

    // --- Start Game Visual Hover Actions ---
    private void OnStartEnter(BaseEventData data)
    {
        if (startGameText == null) return;
        startGameText.text = startGameHover;
        if (changeColor) startGameText.color = hoverColor;
        if (scaleOnHover) startGameText.transform.localScale = startOriginalScale * hoverScaleMultiplier;
    }

    private void OnStartExit(BaseEventData data)
    {
        if (startGameText == null) return;
        startGameText.text = startGameNormal;
        if (changeColor) startGameText.color = normalColor;
        if (scaleOnHover) startGameText.transform.localScale = startOriginalScale;
    }

    // --- Quit Visual Hover Actions ---
    private void OnQuitEnter(BaseEventData data)
    {
        if (quitText == null) return;
        quitText.text = quitHover;
        if (changeColor) quitText.color = hoverColor;
        if (scaleOnHover) quitText.transform.localScale = quitOriginalScale * hoverScaleMultiplier;
    }

    private void OnQuitExit(BaseEventData data)
    {
        if (quitText == null) return;
        quitText.text = quitNormal;
        if (changeColor) quitText.color = normalColor;
        if (scaleOnHover) quitText.transform.localScale = quitOriginalScale;
    }

    public void OnQuitClick(BaseEventData data)
    {
        if (menuManager != null)
            menuManager.QuitGame();
    }
}
