using UnityEngine;

/// <summary>
/// Plays a panting loop while water is below the low-water threshold.
/// The source clip stays intact; this script loops only the first section.
/// </summary>
public class PlayerLowWaterPanting : MonoBehaviour
{
    public PlayerStats stats;
    public AudioClip pantingClip;
    [Range(0f, 1f)] public float waterThreshold = 0.25f;
    public float firstSecondsOnly = 10f;
    [Range(0f, 1f)] public float volume = 0.55f;

    AudioSource source;

    void Awake()
    {
        if (stats == null)
        {
            stats = GetComponent<PlayerStats>();
        }

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = ScaledVolume();
        source.clip = pantingClip;
    }

    void Update()
    {
        if (source.clip == null && pantingClip != null)
        {
            source.clip = pantingClip;
        }

        bool shouldPlay = stats != null
            && source.clip != null
            && stats.WaterNormalized <= waterThreshold
            && stats.water > 0f;

        if (shouldPlay)
        {
            if (!source.isPlaying)
            {
                source.time = 0f;
                source.Play();
            }

            if (source.time >= Mathf.Min(firstSecondsOnly, source.clip.length))
            {
                source.time = 0f;
                source.Play();
            }

            source.volume = ScaledVolume();
        }
        else if (source.isPlaying)
        {
            source.Stop();
        }
    }

    float ScaledVolume()
    {
        return Mathf.Clamp01(volume * GameAudioSettings.SfxOutputMultiplier);
    }
}
