using UnityEngine;

public class FanSwitch : MonoBehaviour
{
    [SerializeField] private FanSpin fan;
    [SerializeField] private Vector3 flipAngles = new Vector3(0f, 0f, 90f);
    [SerializeField] private Transform leverPivot;

    [Header("Audio SFX")]
    [Tooltip("Sound played when toggling the switch.")]
    public AudioClip switchSFX;
    [Range(0f, 2f)] public float sfxVolume = 1f;
    [Tooltip("If true, plays as global 2D audio (loud & clear everywhere). If false, plays as 3D audio at the switch position.")]
    public bool playAs2DSound = true;
    public AudioSource audioSource;

    private Quaternion offRotation;

    public bool IsOn => fan != null && fan.IsPowered;

    void Awake()
    {
        if (leverPivot != null)
            offRotation = leverPivot.localRotation;

        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.enabled = true;

        if (playAs2DSound)
        {
            audioSource.spatialBlend = 0f; // 2D sound: crisp, direct and loud in both ears
        }
        else
        {
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.minDistance = 50f;
            audioSource.maxDistance = 500f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    public bool IsInRange(Vector3 playerPosition, float distance)
    {
        return isActiveAndEnabled && fan != null && leverPivot != null &&
            Vector3.Distance(playerPosition, leverPivot.position) <= distance;
    }

    public void Toggle()
    {
        if (fan == null || leverPivot == null)
            return;

        bool turnOn = !IsOn;
        leverPivot.localRotation = turnOn
            ? offRotation * Quaternion.Euler(flipAngles)
            : offRotation;

        fan.IsPowered = turnOn;

        if (switchSFX != null)
        {
            EnsureAudioSource();

            if (audioSource != null)
            {
                audioSource.enabled = true;
                audioSource.PlayOneShot(switchSFX, sfxVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(switchSFX, Camera.main != null ? Camera.main.transform.position : transform.position, sfxVolume);
            }
        }
    }

    public void FlipUp()
    {
        if (!IsOn)
            Toggle();
    }
}