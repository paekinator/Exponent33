using UnityEngine;

/// <summary>
/// Plays movement audio while grounded movement keys are held. Walk and
/// sprint each get their own dedicated AudioSource so transitioning between
/// them is a real crossfade (one fades out while the other fades in)
/// instead of swapping a single source's clip — a clip swap can only ever
/// be an instant cut, no matter how the volume is faded around it.
/// </summary>
public class PlayerWalkAudio : MonoBehaviour
{
    public FirstPersonController controller;
    public AudioClip walkingClip;
    public AudioClip sprintClip;
    public AudioClip jumpClip;
    public AudioClip landingClip;
    public AudioClip wallHitClip;
    public float firstSecondsOnly = 15f;
    public float fadeInSeconds = 1.1f;
    public float fadeOutSeconds = 1.25f;
    public float sprintFadeInSeconds = 1.4f;
    public float sprintFadeOutSeconds = 1.8f;
    public float jumpStartOffsetSeconds = 0.2f;
    [Range(0f, 1f)] public float walkVolume = 0.2f;
    [Range(0f, 1f)] public float sprintVolume = 0.045f;
    [Range(0f, 1f)] public float jumpVolume = 0.22f;
    [Range(0f, 1f)] public float landingVolume = 0.22f;
    [Range(0f, 1f)] public float wallHitVolume = 0.3f;
    public float wallHitMinSpeed = 2f;
    public float wallHitCooldown = 1f;

    AudioSource walkSource;
    AudioSource sprintSource;
    AudioSource oneShotSource;
    bool jumpedFromGround;
    bool wasGrounded;
    float nextWallHitTime;

    void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<FirstPersonController>();
        }

        walkSource = CreateLoopSource(walkingClip);
        sprintSource = CreateLoopSource(sprintClip);

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;

        wasGrounded = controller != null && controller.IsGrounded;
    }

    AudioSource CreateLoopSource(AudioClip clip)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        src.clip = clip;
        return src;
    }

    void OnDisable()
    {
        // Disabling mid-loop (e.g. a dialogue freeze) only stops Update() —
        // the AudioSources themselves would otherwise keep playing forever
        // at whatever volume they were last at. Cut them instantly.
        walkSource.Pause();
        sprintSource.Pause();
    }

    void Update()
    {
        HandleJumpAndLandingAudio();

        bool grounded = controller != null && controller.IsGrounded && !controller.IsCrouched && IsMovementKeyHeld();
        bool sprinting = grounded && controller.IsSprinting;
        bool walking = grounded && !sprinting;

        UpdateLoop(walkSource, walking, ScaledVolume(walkVolume), fadeInSeconds, fadeOutSeconds);
        UpdateLoop(sprintSource, sprinting, ScaledVolume(sprintVolume), sprintFadeInSeconds, sprintFadeOutSeconds);
    }

    void UpdateLoop(AudioSource src, bool shouldPlay, float targetVolume, float fadeInTime, float fadeOutTime)
    {
        if (src.clip == null)
        {
            return;
        }

        if (shouldPlay)
        {
            if (!src.isPlaying)
            {
                src.Play();
            }

            if (src.time >= Mathf.Min(firstSecondsOnly, src.clip.length))
            {
                src.time = 0f;
                src.Play();
            }

            src.volume = Mathf.MoveTowards(src.volume, targetVolume, Time.deltaTime * targetVolume / Mathf.Max(0.01f, fadeInTime));
        }
        else if (src.isPlaying)
        {
            src.volume = Mathf.MoveTowards(src.volume, 0f, Time.deltaTime * targetVolume / Mathf.Max(0.01f, fadeOutTime));

            if (src.volume <= 0.001f)
            {
                src.Pause();
            }
        }
    }

    bool IsMovementKeyHeld()
    {
        return Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.A)
            || Input.GetKey(KeyCode.S)
            || Input.GetKey(KeyCode.D);
    }

    void HandleJumpAndLandingAudio()
    {
        if (controller == null)
        {
            return;
        }

        bool isGrounded = controller.IsGrounded;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && jumpClip != null)
        {
            jumpedFromGround = true;
            PlayJumpSound();
        }

        if (jumpedFromGround && !wasGrounded && isGrounded && landingClip != null)
        {
            oneShotSource.PlayOneShot(landingClip, ScaledVolume(landingVolume));
            jumpedFromGround = false;
        }

        wasGrounded = isGrounded;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (wallHitClip == null || Time.time < nextWallHitTime || collision.relativeVelocity.magnitude < wallHitMinSpeed)
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.y) < 0.35f)
            {
                oneShotSource.PlayOneShot(wallHitClip, ScaledVolume(wallHitVolume));
                nextWallHitTime = Time.time + wallHitCooldown;
                return;
            }
        }
    }

    void PlayJumpSound()
    {
        oneShotSource.Stop();
        oneShotSource.clip = jumpClip;
        oneShotSource.volume = ScaledVolume(jumpVolume);
        oneShotSource.time = Mathf.Min(jumpStartOffsetSeconds, Mathf.Max(0f, jumpClip.length - 0.01f));
        oneShotSource.Play();
    }

    float ScaledVolume(float baseVolume)
    {
        return Mathf.Clamp01(baseVolume * GameAudioSettings.SfxOutputMultiplier);
    }
}
