using UnityEngine;

/// <summary>
/// Loops the scene's background music and slowly raises its volume over time.
/// </summary>
public class GameMusicPlayer : MonoBehaviour
{
    public AudioClip musicClip;
    [Range(0f, 1f)] public float startVolume = 0.02f;
    [Range(0f, 1f)] public float maxVolume = 0.06f;
    public float rampSeconds = 360f;

    AudioSource source;
    float startedAt;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = musicClip;
        source.volume = startVolume;
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
        source.volume = Mathf.Lerp(startVolume, maxVolume, ramp);
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
        }
    }
}
