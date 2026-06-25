using UnityEngine;

/// <summary>
/// Handles drinking audio for both held dispenser drinking and future inventory
/// water bottles.
/// </summary>
public class PlayerDrinkAudio : MonoBehaviour
{
    public AudioClip drinkingClip;
    [Range(0f, 1f)] public float volume = 0.85f;
    public float holdGraceSeconds = 0.12f;
    public float bottleDrinkSeconds = 5f;

    AudioSource source;
    float lastHoldTickTime = -999f;
    float burstUntilTime = -999f;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = ScaledVolume();
        source.clip = drinkingClip;
    }

    void Update()
    {
        if (source.clip == null && drinkingClip != null)
        {
            source.clip = drinkingClip;
        }

        bool dispenserHeld = Time.time - lastHoldTickTime <= holdGraceSeconds;
        bool bottleDrinking = Time.time < burstUntilTime;
        bool shouldPlay = source.clip != null && (dispenserHeld || bottleDrinking);

        if (shouldPlay)
        {
            if (!source.isPlaying)
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

    public void HoldDrinkTick()
    {
        lastHoldTickTime = Time.time;
    }

    public void PlayBottleDrink()
    {
        burstUntilTime = Time.time + bottleDrinkSeconds;
        if (source != null && source.clip != null)
        {
            source.time = 0f;
            source.Play();
        }
    }

    float ScaledVolume()
    {
        return Mathf.Clamp01(volume * GameAudioSettings.SfxOutputMultiplier);
    }
}
