using UnityEngine;

/// <summary>
/// Event-based player noise meter for boss sensing. Noise is NOT an
/// accumulating total — each frame it's just whichever single source is
/// currently loudest (running/walking/landing/wall-hit), snapped to
/// instantly. The only thing that happens gradually is the fall back down:
/// once nothing is making noise, it drains fast (decayPerSecond) toward 0.
/// This means a one-off spike (landing, a wall hit) decays away in about
/// two seconds, and sustained noise (running) only lasts as long as you
/// keep doing it — stand still or go quiet and the boss loses you fast.
/// </summary>
public class PlayerNoiseMeter : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController controller;
    [Tooltip("Auto-found if left empty. While hidden, noise is forced to 0 — the boss can't hear a player tucked inside a locker.")]
    public PlayerHiding hiding;

    [Header("Timing")]
    public float revealAfterSeconds = 20f;

    [Header("Noise Levels (snapped to instantly, not accumulated)")]
    public float maxNoise = 100f;
    [Tooltip("Noise level while sprinting (held, every frame you're actually moving).")]
    public float runningNoise = 80f;
    [Tooltip("Noise level while walking at normal pace (held, every frame you're actually moving).")]
    public float walkingNoise = 30f;
    [Tooltip("One-off spike the instant you land from a fall.")]
    public float fallingNoise = 100f;
    [Tooltip("One-off spike the instant you hit a wall.")]
    public float wallHitNoise = 20f;
    [Tooltip("Minimum time between wall-hit spikes, so sliding along a wall doesn't spam one every frame.")]
    public float wallHitCooldown = 0.5f;
    [Tooltip("How fast noise drains back to 0 once nothing is currently making any — NOT a smoothing time, a flat per-second rate.")]
    public float decayPerSecond = 50f;

    [Header("Future Boss Sensing")]
    [Tooltip("At 100 noise, the boss can hear this many meters away.")]
    public float maxAudibleRangeMeters = 50f;

    [Tooltip("Crouching resizes the player's collider, which can spuriously fire a landing/wall-hit collision event from the physics engine reacting to the sudden resize — not an actual landing or wall hit. Suppress instant noise spikes for this long after any crouch state change.")]
    public float crouchTransitionGraceSeconds = 0.2f;

    float currentNoise;
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

        // Whichever single source is loudest THIS frame — these don't add
        // together, a landing spike doesn't stack on top of running noise.
        float eventLevel = 0f;

        if (wasJustLanded)
        {
            if (!inCrouchGrace) eventLevel = Mathf.Max(eventLevel, fallingNoise);
            wasJustLanded = false;
        }

        if (wasWallHit)
        {
            if (!inCrouchGrace) eventLevel = Mathf.Max(eventLevel, wallHitNoise);
            wasWallHit = false;
        }

        eventLevel = Mathf.Max(eventLevel, GetContinuousNoiseLevel());

        if (eventLevel > currentNoise)
        {
            // Snap straight up — noise isn't something that ramps in, it's
            // either happening this frame or it isn't.
            currentNoise = eventLevel;
        }
        else
        {
            currentNoise = Mathf.Max(0f, currentNoise - decayPerSecond * Time.deltaTime);
        }

        currentNoise = Mathf.Clamp(currentNoise, 0f, maxNoise);
    }

    // Crouching makes zero noise of its own (no contribution either way) —
    // it does NOT force the meter to 0; any noise already in flight just
    // decays at the normal rate, same as going quiet any other way.
    float GetContinuousNoiseLevel()
    {
        if (controller == null || controller.IsCrouched || !controller.IsGrounded || !HasMovementInput())
        {
            return 0f;
        }

        return controller.IsSprinting ? runningNoise : walkingNoise;
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
