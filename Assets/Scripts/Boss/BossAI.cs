using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// BossAI — relentless pursuer.
///
/// DESIGN (matches the game spec):
///   • Spawns hidden, wakes up after `activationTime` seconds (default 20).
///   • ALWAYS knows where the player is and always moves toward them.
///   • Three movement tiers, each with its own speed + animation:
///       PHONE (default) — slow creep on the phone.
///       WALK  — triggered when the player makes NOISE (PlayerNoise -> HearNoise).
///       RUN   — triggered when the player is SPOTTED (line of sight). Charges the
///               player's last-known position at full speed.
///   • A tier "sticks" for a short time after its trigger, then decays back down
///     (RUN -> WALK -> PHONE) so the boss doesn't instantly relax.
///   • ALL speeds escalate over time: flat until `escalationDelay` (3 min), then
///     ×`escalationFactor` (1.2) once, and again every `escalationInterval` (1 min)
///     after that — compounding — until the player is caught.
///
/// ANIMATOR PARAMETER (one Integer):
///   "MoveMode"  ->  0 = phone clip, 1 = walk clip, 2 = run clip
///   In the Animator Controller make 3 states (Phone/Walk/Run) and transition on
///   MoveMode Equals 0 / 1 / 2. Phone can be the default state.
///
/// SETUP CHECKLIST:
///   1. Put this on the Boss GameObject (the one with the skinned mesh).
///   2. Add components: NavMeshAgent, Animator (with your Boss.controller), AudioSource.
///   3. Tag the player object "Player" (Inspector, top-left) so it's auto-found.
///   4. Bake a NavMesh (Window > AI > Navigation) over the floor.
///   5. Wire onCatchPlayer -> BossEndScreen.ShowEndScreen() in the Inspector.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
{
    // =========================================================================
    // INSPECTOR
    // =========================================================================

    [Header("Target")]
    [Tooltip("Auto-found by the 'Player' tag if left empty.")]
    public Transform player;

    [Header("Activation")]
    [Tooltip("Seconds after the level loads before the boss wakes up and appears.")]
    public float activationTime = 20f;
    [Tooltip("Hide the boss's renderers until it activates.")]
    public bool hideUntilActivation = true;

    [Header("Base Speeds (metres/second)")]
    [Tooltip("Default creep speed while on the phone.")]
    public float phoneSpeed = 1.5f;
    [Tooltip("Speed after hearing a noise.")]
    public float walkSpeed = 2.5f;
    [Tooltip("Chase speed once the player is seen. Set this ~= your player's RUN speed so it can catch up.")]
    public float runSpeed = 5f;

    [Header("Tier Triggers")]
    [Tooltip("How long the boss stays in WALK after the last noise (seconds).")]
    public float walkDuration = 4f;
    [Tooltip("How long the boss keeps RUNNING after losing sight of the player (seconds).")]
    public float runDuration = 3f;

    [Header("Sight (Spotting)")]
    [Tooltip("Max distance the boss can spot the player.")]
    public float sightRange = 20f;
    [Tooltip("If true, a wall between boss and player blocks spotting.")]
    public bool requireLineOfSight = true;
    [Tooltip("A hidden player (PlayerHiding) can't be spotted.")]
    public bool respectsHiding = true;

    [Header("Catch")]
    [Tooltip("Distance at which the player is tagged / caught.")]
    public float catchDistance = 1.5f;

    [Header("Distress (phone dead)")]
    [Tooltip("When true, the boss ignores all tiers and RUNS straight at the player's exact position. Set by PlayerStats when the phone dies.")]
    public bool distressActive = false;

    [Header("Speed Escalation Over Time")]
    [Tooltip("Seconds of flat speed before the first ramp (180 = 3 minutes).")]
    public float escalationDelay = 180f;
    [Tooltip("Seconds between each subsequent ramp (60 = every minute).")]
    public float escalationInterval = 60f;
    [Tooltip("Multiplier applied per ramp step (compounds).")]
    public float escalationFactor = 1.2f;

    [Header("Audio (optional)")]
    public AudioSource phoneSource;
    public AudioClip   phoneClip;
    [Range(0f, 1f)] public float phoneVolume = 0.85f;
    public AudioSource chaseSource;
    public AudioClip   chaseAlertClip;

    [Header("Events")]
    [Tooltip("Wire to BossEndScreen.ShowEndScreen().")]
    public UnityEngine.Events.UnityEvent onCatchPlayer;

    // ── Animator parameter ────────────────────────────────────────────────────
    const string P_MOVE_MODE = "MoveMode"; // 0 phone, 1 walk, 2 run

    // =========================================================================
    // PRIVATE
    // =========================================================================

    public enum Mode { Phone = 0, Walk = 1, Run = 2 }

    NavMeshAgent _agent;
    Animator     _animator;
    PlayerHiding _playerHiding;
    Renderer[]   _renderers;

    bool    _activated   = false;
    bool    _caught      = false;
    float   _timer       = 0f;   // time since level load (drives activation + escalation)
    float   _walkTimer   = 0f;   // >0 means at least WALK tier
    float   _runTimer    = 0f;   // >0 means RUN tier
    Vector3 _lastKnownPos;
    Mode    _mode        = Mode.Phone;
    bool    _chaseAlertPlayed = false;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        _agent     = GetComponent<NavMeshAgent>();
        _animator  = GetComponent<Animator>();
        _renderers = GetComponentsInChildren<Renderer>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null)
        {
            _playerHiding = player.GetComponent<PlayerHiding>();
            _lastKnownPos = player.position;
        }
    }

    void Start()
    {
        _agent.isStopped = true;
        if (hideUntilActivation) SetRenderersEnabled(false);
        _animator.SetInteger(P_MOVE_MODE, (int)Mode.Phone);
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_caught) return;

        // ── Wake up ───────────────────────────────────────────────────────────
        if (!_activated)
        {
            if (_timer >= activationTime) Activate();
            return;
        }

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // ── DISTRESS: phone is dead -> drop everything and run straight at the
        //    player's exact position (ignores noise, sight, and hiding). ────────
        if (distressActive)
        {
            if (_mode != Mode.Run) OnModeChanged(Mode.Run);
            _mode        = Mode.Run;
            _agent.speed = CurrentSpeed(Mode.Run);
            _agent.SetDestination(player.position);
            _animator.SetInteger(P_MOVE_MODE, (int)Mode.Run);
            if (dist <= catchDistance) CatchPlayer();
            return;
        }

        bool  hidden = respectsHiding && _playerHiding != null && _playerHiding.isHidden;

        // ── Decay the tier timers ─────────────────────────────────────────────
        if (_walkTimer > 0f) _walkTimer -= Time.deltaTime;
        if (_runTimer  > 0f) _runTimer  -= Time.deltaTime;

        // ── Spotting -> RUN ───────────────────────────────────────────────────
        if (!hidden && IsPlayerSpotted(dist))
        {
            _runTimer     = runDuration;
            _lastKnownPos = player.position;   // remember where we saw them
        }

        // ── Decide tier (RUN beats WALK beats PHONE) ─────────────────────────-
        Mode newMode = _runTimer > 0f ? Mode.Run
                     : _walkTimer > 0f ? Mode.Walk
                     : Mode.Phone;
        if (newMode != _mode) OnModeChanged(newMode);
        _mode = newMode;

        // ── Move ──────────────────────────────────────────────────────────────
        // Boss always knows the player's position, so phone/walk head straight
        // for them. Run heads for the last-known spot (updated while in sight).
        Vector3 target = _mode == Mode.Run ? _lastKnownPos : player.position;
        if (_mode != Mode.Run) _lastKnownPos = player.position;

        _agent.speed = CurrentSpeed(_mode);
        _agent.SetDestination(target);
        _animator.SetInteger(P_MOVE_MODE, (int)_mode);

        // ── Catch ─────────────────────────────────────────────────────────────
        if (dist <= catchDistance) CatchPlayer();
    }

    // =========================================================================
    // SPEED
    // =========================================================================

    /// <summary>Base speed for a tier, scaled by the time-based escalation.</summary>
    float CurrentSpeed(Mode m)
    {
        float baseSpeed = m == Mode.Run ? runSpeed : m == Mode.Walk ? walkSpeed : phoneSpeed;
        return baseSpeed * EscalationMultiplier();
    }

    /// <summary>
    /// 1.0 until escalationDelay, then escalationFactor^steps, gaining one step
    /// every escalationInterval. e.g. flat for 3 min, then ×1.2 at 3:00,
    /// ×1.44 at 4:00, ×1.728 at 5:00, ... compounding until caught.
    /// </summary>
    float EscalationMultiplier()
    {
        if (_timer < escalationDelay) return 1f;
        int steps = 1 + Mathf.FloorToInt((_timer - escalationDelay) / Mathf.Max(0.01f, escalationInterval));
        return Mathf.Pow(escalationFactor, steps);
    }

    // =========================================================================
    // SPOTTING
    // =========================================================================

    bool IsPlayerSpotted(float dist)
    {
        if (dist > sightRange) return false;
        if (!requireLineOfSight) return true;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir    = ((player.position + Vector3.up) - origin);
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, sightRange))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return false;
    }

    // =========================================================================
    // NOISE — called externally by PlayerNoise.cs
    // =========================================================================

    /// <summary>The player made a noise — bump the boss up to (at least) WALK.</summary>
    public void HearNoise(Vector3 noiseWorldPosition)
    {
        if (!_activated || _caught) return;
        _walkTimer = walkDuration;   // boss already knows where you are; just speeds up
    }

    /// <summary>Convenience: alert every BossAI in the scene.</summary>
    public static void BroadcastNoise(Vector3 position)
    {
        BossAI[] bosses = Object.FindObjectsByType<BossAI>(FindObjectsSortMode.None);
        foreach (BossAI b in bosses) b.HearNoise(position);
    }

    /// <summary>
    /// Called by PlayerStats. When on, the boss abandons stealth behaviour and
    /// runs straight at the player (phone-dead distress).
    /// </summary>
    public void SetDistress(bool on)
    {
        distressActive = on;
    }

    // =========================================================================
    // TRANSITIONS / HELPERS
    // =========================================================================

    void Activate()
    {
        _activated       = true;
        _agent.isStopped = false;
        _mode            = Mode.Phone;
        SetRenderersEnabled(true);
        PlayPhoneAudio();
        if (player != null) _lastKnownPos = player.position;
        Debug.Log("[BossAI] Activated — pursuing on phone.");
    }

    void OnModeChanged(Mode m)
    {
        if (m == Mode.Run && !_chaseAlertPlayed && chaseSource != null && chaseAlertClip != null)
        {
            chaseSource.PlayOneShot(chaseAlertClip);
            _chaseAlertPlayed = true;
        }
        if (m != Mode.Run) _chaseAlertPlayed = false;
    }

    void CatchPlayer()
    {
        _caught          = true;
        _agent.isStopped = true;
        _animator.SetInteger(P_MOVE_MODE, (int)Mode.Phone);
        Debug.Log("[BossAI] PLAYER TAGGED.");
        onCatchPlayer?.Invoke();
    }

    void PlayPhoneAudio()
    {
        if (phoneSource == null || phoneClip == null || phoneSource.isPlaying) return;
        phoneSource.clip         = phoneClip;
        phoneSource.loop         = true;
        phoneSource.volume       = phoneVolume;
        phoneSource.spatialBlend = 1f;
        phoneSource.Play();
    }

    void SetRenderersEnabled(bool on)
    {
        if (_renderers == null) return;
        foreach (Renderer r in _renderers) if (r != null) r.enabled = on;
    }

    // =========================================================================
    // GIZMOS — visualise ranges in the Scene view
    // =========================================================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}
