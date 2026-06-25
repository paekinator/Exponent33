using UnityEngine;

/// <summary>
/// Loops the scene's background music and slowly raises its volume over time.
/// </summary>
public class GameMusicPlayer : MonoBehaviour
{
    public AudioClip musicClip;
    [Range(0f, 1f)] public float startVolume = 0.1f;
    [Range(0f, 1f)] public float maxVolume = 0.2f;
    public float rampSeconds = 360f;
    [Tooltip("Jumps past this many seconds of the clip on the very first play — skips a slow/quiet intro so the music is immediately audible. Only applied once; later loops play the full clip from the top.")]
    public float skipIntroSeconds = 5f;

    AudioSource source;
    float startedAt;
    bool hasStartedOnce;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = musicClip;
        source.volume = ScaledMusicVolume(startVolume);
        startedAt = Time.time;

        PlayIfReady();
    }

    void Start()
    {
        PlayIfReady();
    }

    void Update()
    {
        PlayIfReady();

        float ramp = rampSeconds <= 0f ? 1f : Mathf.Clamp01((Time.time - startedAt) / rampSeconds);
        source.volume = ScaledMusicVolume(Mathf.Lerp(startVolume, maxVolume, ramp));
    }

    void PlayIfReady()
    {
        if (source.clip == null && musicClip != null)
        {
            source.clip = musicClip;
        }

        if (source.clip != null && !source.isPlaying)
        {
            source.Play();

            if (!hasStartedOnce)
            {
                source.time = Mathf.Clamp(skipIntroSeconds, 0f, Mathf.Max(0f, source.clip.length - 0.1f));
                hasStartedOnce = true;
            }
        }
    }

    public void StopMusic()
    {
        if (source != null)
        {
            source.Stop();
        }
    }

    float ScaledMusicVolume(float baseVolume)
    {
        return Mathf.Clamp01(baseVolume * GameAudioSettings.MusicOutputMultiplier);
    }
}
