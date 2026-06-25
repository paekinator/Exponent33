using UnityEngine;

/// <summary>
/// Handles the phone-charging sound while the player holds E at a charger.
/// Same hold-grace pattern as PlayerDrinkAudio: HoldChargeTick() refreshes a
/// timestamp every frame E is held, and the loop keeps playing as long as
/// that timestamp is recent — so releasing E stops it almost immediately
/// without needing the interactable to call a separate "stop" method.
/// </summary>
public class PlayerChargeAudio : MonoBehaviour
{
    public AudioClip chargingClip;
    [Range(0f, 1f)] public float volume = 0.15f;
    public float holdGraceSeconds = 0.12f;

    AudioSource source;
    float lastHoldTickTime = -999f;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = ScaledVolume();
        source.clip = chargingClip;
    }

    void Update()
    {
        if (source.clip == null && chargingClip != null)
        {
            source.clip = chargingClip;
        }

        bool chargerHeld = Time.time - lastHoldTickTime <= holdGraceSeconds;

        if (chargerHeld && source.clip != null)
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

    public void HoldChargeTick()
    {
        lastHoldTickTime = Time.time;
    }

    float ScaledVolume()
    {
        return Mathf.Clamp01(volume * GameAudioSettings.SfxOutputMultiplier);
    }
}
