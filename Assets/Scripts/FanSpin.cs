using UnityEngine;

[DefaultExecutionOrder(-10)]
public class FanSpin : MonoBehaviour
{
    public float speed = 50f;

    [SerializeField] private bool startsPowered = true;
    [SerializeField, Min(0.01f)] private float coastSeconds = 2f;

    public bool IsPowered { get; set; }

    [Header("Audio SFX")]
    [Tooltip("Looping hum/wind sound when the fan is spinning.")]
    public AudioClip fanRunningSFX;
    [Range(0f, 2f)] public float fanVolume = 1f;
    [Tooltip("How many seconds the audio takes to fade in when turned on.")]
    public float fadeInSeconds = 1f;
    [Tooltip("How many seconds the audio takes to fade out when turned off.")]
    public float fadeOutSeconds = 3.5f;
    [Tooltip("Whether the pitch drops slightly as the fan slows down (for realistic deceleration).")]
    public bool dropPitchOnSlowdown = true;

    private float currentSpeed;
    private AudioSource audioSource;
    private float targetAudioVolume;
    private float basePitch = 1f;

    void Awake()
    {
        IsPowered = startsPowered;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.clip = fanRunningSFX;
        audioSource.spatialBlend = 0f; // Clear audible loop across the room
        audioSource.volume = 0f;
        basePitch = audioSource.pitch;

        if (IsPowered && fanRunningSFX != null)
        {
            audioSource.Play();
            targetAudioVolume = fanVolume;
            audioSource.volume = fanVolume;
        }
    }

    void Update()
    {
        if (IsPowered)
        {
            currentSpeed = speed;
            targetAudioVolume = fanVolume;

            if (audioSource != null && !audioSource.isPlaying && fanRunningSFX != null)
            {
                audioSource.clip = fanRunningSFX;
                audioSource.Play();
            }
        }
        else
        {
            float slowdown = Mathf.Abs(speed) / coastSeconds;
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, 0f, slowdown * Time.deltaTime);

            targetAudioVolume = 0f;
        }

        // Smoothly fade audio volume in and out matching the fan spinning
        if (audioSource != null && audioSource.isPlaying)
        {
            float currentDuration = IsPowered ? fadeInSeconds : fadeOutSeconds;
            float fadeRate = (fanVolume / Mathf.Max(0.01f, currentDuration)) * Time.deltaTime;
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetAudioVolume, fadeRate);

            if (dropPitchOnSlowdown)
            {
                // Gradually pitch down as it slows down (down to 0.75x) for an organic fadeout
                float speedRatio = Mathf.Abs(speed) > 0.001f ? Mathf.Clamp01(Mathf.Abs(currentSpeed) / Mathf.Abs(speed)) : 1f;
                audioSource.pitch = Mathf.Lerp(basePitch * 0.75f, basePitch, speedRatio);
            }

            if (audioSource.volume <= 0.001f && !IsPowered)
            {
                audioSource.Stop();
                audioSource.pitch = basePitch;
            }
        }

        transform.Rotate(0f, currentSpeed * Time.deltaTime, 0f, Space.Self);
    }
}