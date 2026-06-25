using UnityEngine;

/// <summary>
/// Continuous player noise meter for boss sensing.
/// Owns only the player-side noise value; boss movement can read this later.
/// </summary>
public class PlayerNoiseMeter : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController controller;

    [Header("Timing")]
    public float revealAfterSeconds = 20f;

    [Header("Noise")]
    public float maxNoise = 100f;
    public float sprintNoisePerSecond = 20f;
    public float walkNoisePerSecond = 2f;
    public float quietDecayPerSecond = 10f;
    public float landingNoise = 20f;
    public float wallHitNoise = 5f;
    [Tooltip("Minimum time between wall-hit noise spikes, so sliding along a wall doesn't spam +5 every frame.")]
    public float wallHitCooldown = 0.5f;

    [Header("Smoothing")]
    [Tooltip("How long it takes the noise RATE (decay/walk) to ease into its new target when movement state changes, so speeding up or stopping doesn't snap instantly.")]
    public float rateSmoothTime = 0.5f;
    [Tooltip("Same idea but specifically for entering/leaving SPRINT, which is the biggest jump (walk 2/s -> sprint 20/s) and the one most likely to feel sudden.")]
    public float sprintRateSmoothTime = 1.1f;

    [Header("Future Boss Sensing")]
    [Tooltip("At 100 noise, the boss can hear this many meters away.")]
    public float maxAudibleRangeMeters = 50f;

    float currentNoise;
    float smoothedRate;
    bool wasGroundedLastFrame;
    bool wasJustLanded;
    bool wasWallHit;
    float lastWallHitTime = -999f;

    float enabledAtTime;

    public float CurrentNoise => currentNoise;
    public float NormalizedNoise => maxNoise > 0f ? currentNoise / maxNoise : 0f;
    public bool IsRevealed => Time.time - enabledAtTime >= revealAfterSeconds;
    public float AudibleRangeMeters => maxNoise > 0f ? (currentNoise / maxNoise) * maxAudibleRangeMeters : 0f;

    /// <summary>Makes IsRevealed true immediately, e.g. once a story beat
    /// (the boss laugh) establishes that the boss is now actively listening.</summary>
    public void ForceReveal()
    {
        enabledAtTime = Time.time - revealAfterSeconds;
    }

    void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<FirstPersonController>();
        }

        // Assume grounded at start so the player doesn't register a phantom
        // "landing" spike on the very first frame.
        wasGroundedLastFrame = true;
    }

    // Counts the reveal-after-seconds delay from when the meter actually
    // becomes active (game start), not from level load — otherwise time spent
    // clicking through the intro dialogue would eat into the 20s countdown.
    void OnEnable()
    {
        enabledAtTime = Time.time;
    }

    void Update()
    {
        TrackLanding();
        UpdateNoiseLevel();
    }

    void TrackLanding()
    {
        bool grounded = controller == null || controller.IsGrounded;
        if (grounded && !wasGroundedLastFrame)
        {
            wasJustLanded = true;
        }

        wasGroundedLastFrame = grounded;
    }

    void UpdateNoiseLevel()
    {
        if (wasJustLanded)
        {
            currentNoise += landingNoise;
            wasJustLanded = false;
        }

        if (wasWallHit)
        {
            currentNoise += wallHitNoise;
            wasWallHit = false;
        }

        // Continuous rate (decay/walk/sprint) eases toward its target instead of
        // snapping instantly, so speeding up, slowing down, or stopping reads as
        // a smooth, natural transition rather than a sudden gear-shift. Sprint
        // gets its own (longer) smoothing time since it's the biggest jump.
        bool sprinting = controller != null && controller.IsSprinting;
        float targetRate = GetContinuousNoiseRate();
        float effectiveTargetRate = targetRate > 0f ? targetRate : -quietDecayPerSecond;
        float smoothTime = sprinting || smoothedRate > walkNoisePerSecond ? sprintRateSmoothTime : rateSmoothTime;

        // True exponential decay (not a linear dt/tau ratio) so the ease-in
        // feels the same regardless of frame rate and never jumps in one step.
        float decay = Mathf.Exp(-Time.deltaTime / Mathf.Max(smoothTime, 0.01f));
        smoothedRate = Mathf.Lerp(effectiveTargetRate, smoothedRate, decay);

        currentNoise += smoothedRate * Time.deltaTime;
        currentNoise = Mathf.Clamp(currentNoise, 0f, maxNoise);
    }

    // Crouching makes zero noise of its own (no contribution either way) —
    // it does NOT force the meter to 0; it just decays at the normal rate.
    float GetContinuousNoiseRate()
    {
        if (controller == null || controller.IsCrouched || !controller.IsGrounded || !HasMovementInput())
        {
            return 0f;
        }

        if (controller.IsSprinting)
        {
            return sprintNoisePerSecond;
        }

        return walkNoisePerSecond;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time - lastWallHitTime < wallHitCooldown)
        {
            return;
        }

        // Roughly horizontal hit normal = a wall, not the floor/ceiling.
        if (Mathf.Abs(hit.normal.y) < 0.5f)
        {
            wasWallHit = true;
            lastWallHitTime = Time.time;
        }
    }

    bool HasMovementInput()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f
            || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;
    }
}
