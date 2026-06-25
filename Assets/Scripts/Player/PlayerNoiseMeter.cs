using UnityEngine;

/// <summary>
/// Continuous player noise meter for boss sensing.
/// Owns only the player-side noise value; boss movement can read this later.
/// </summary>
public class PlayerNoiseMeter : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController controller;
    [Tooltip("Auto-found if left empty. While hidden, noise is forced to 0 — the boss can't hear a player tucked inside a locker.")]
    public PlayerHiding hiding;

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

    [Tooltip("Crouching resizes the player's collider, which can spuriously fire a landing/wall-hit collision event from the physics engine reacting to the sudden resize — not an actual landing or wall hit. Suppress instant noise spikes for this long after any crouch state change.")]
    public float crouchTransitionGraceSeconds = 0.2f;

    float currentNoise;
    float smoothedRate;
    bool wasGroundedLastFrame;
    bool wasJustLanded;
    bool wasWallHit;
    float lastWallHitTime = -999f;
    bool lastCrouchState;
    float crouchGraceUntil = -999f;

    float enabledAtTime;
    bool wasRevealed;

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
        if (hiding == null)
        {
            hiding = GetComponent<PlayerHiding>();
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
        wasRevealed = false;
        lastCrouchState = controller != null && controller.IsCrouched;
    }

    void Update()
    {
        if (hiding != null && hiding.isHidden)
        {
            // Tucked inside a locker — silent regardless of whatever was
            // happening the instant before E was pressed.
            currentNoise = 0f;
            smoothedRate = 0f;
        }
        else
        {
            TrackLanding();
            TrackCrouchTransition();
            UpdateNoiseLevel();
        }

        // The noise level always starts from 0 the moment it actually becomes
        // visible/active — whether that's the natural reveal timer or a
        // story-beat ForceReveal() — so nothing accumulated silently while
        // hidden ever shows up as a surprise jump.
        bool revealedNow = IsRevealed;
        if (revealedNow && !wasRevealed)
        {
            currentNoise = 0f;
            smoothedRate = 0f;
        }
        wasRevealed = revealedNow;
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

    void TrackCrouchTransition()
    {
        bool crouched = controller != null && controller.IsCrouched;
        if (crouched != lastCrouchState)
        {
            // Crouch resizes the collider, which can spuriously trigger a
            // landing or wall-hit collision event on the same frame — not a
            // real one. Swallow instant spikes for a short grace window.
            crouchGraceUntil = Time.time + crouchTransitionGraceSeconds;
            lastCrouchState = crouched;
        }
    }

    void UpdateNoiseLevel()
    {
        bool inCrouchGrace = Time.time < crouchGraceUntil;

        if (wasJustLanded)
        {
            if (!inCrouchGrace) currentNoise += landingNoise;
            wasJustLanded = false;
        }

        if (wasWallHit)
        {
            if (!inCrouchGrace) currentNoise += wallHitNoise;
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
