using UnityEngine;

/// <summary>
/// Plays background music (BGM) across the game in a seamless loop.
/// Automatically creates and configures an AudioSource if one isn't present.
/// Optionally persists across scene loads with DontDestroyOnLoad.
/// </summary>
public class BackgroundMusic : MonoBehaviour
{
    [Header("BGM Settings")]
    [Tooltip("The music track to loop in the background.")]
    public AudioClip musicClip;

    [Range(0f, 3f)]
    [Tooltip("Music volume level (can go above 1 for boost).")]
    public float volume = 2f;

    [Tooltip("Play automatically when the scene starts.")]
    public bool playOnStart = true;

    [Tooltip("Loop the music continuously.")]
    public bool loop = true;

    [Tooltip("Keep music playing seamlessly across scene transitions.")]
    public bool persistBetweenScenes = false;

    private AudioSource audioSource;
    private AudioSource audioSourceBoost;
    private static BackgroundMusic instance;

    void Awake()
    {
        if (persistBetweenScenes)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        SetupAudioSources();
        ApplyVolume();
    }

    private void SetupAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        audioSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        audioSourceBoost = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        ConfigureSource(audioSource);
        ConfigureSource(audioSourceBoost);
    }

    private void ConfigureSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        if (musicClip != null)
            src.clip = musicClip;
    }

    private void ApplyVolume()
    {
        if (audioSource != null)
            audioSource.volume = Mathf.Clamp01(volume);

        if (audioSourceBoost != null)
            audioSourceBoost.volume = Mathf.Clamp01(volume - 1f);
    }

    void Start()
    {
        if (playOnStart && musicClip != null)
        {
            if (audioSource != null && !audioSource.isPlaying)
                audioSource.Play();

            if (volume > 1f && audioSourceBoost != null && !audioSourceBoost.isPlaying)
            {
                audioSourceBoost.timeSamples = audioSource.timeSamples;
                audioSourceBoost.Play();
            }
        }
    }

    void Update()
    {
        ApplyVolume();

        if (audioSource != null && audioSource.isPlaying && volume > 1f && audioSourceBoost != null && !audioSourceBoost.isPlaying)
        {
            audioSourceBoost.timeSamples = audioSource.timeSamples;
            audioSourceBoost.Play();
        }
        else if (volume <= 1f && audioSourceBoost != null && audioSourceBoost.isPlaying)
        {
            audioSourceBoost.Stop();
        }
    }

    /// <summary>
    /// Change volume dynamically (e.g. from pause menus / settings).
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
            audioSource.volume = volume;
    }

    /// <summary>
    /// Pause background music.
    /// </summary>
    public void PauseMusic()
    {
        if (audioSource != null)
            audioSource.Pause();
    }

    /// <summary>
    /// Resume background music.
    /// </summary>
    public void ResumeMusic()
    {
        if (audioSource != null)
            audioSource.UnPause();
    }

    /// <summary>
    /// Stop background music.
    /// </summary>
    public void StopMusic()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}

