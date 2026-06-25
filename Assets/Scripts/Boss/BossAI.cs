using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// BossAI — three animation states, picked every frame by distance/hearing/sight.
///
///   PHONE (idle) — default. The boss is always aware of the player but stays
///   on a soft leash: within `leashDistance` it paces/circles near its current
///   spot; beyond that it paths (via NavMesh, so it never cuts through walls)
///   back toward the player without beelining straight at them.
///
///   WALK (heard) — continuous range check against PlayerNoiseMeter: if the
///   boss is within the player's current AudibleRangeMeters, it walks toward
///   the player's live position. The instant the player is out of range again
///   it drops straight back to PHONE — no lingering timer.
///
///   RUN (spotted) — line-of-sight raycast (blocked by walls, and by hiding).
///   The moment the player is actually spotted (not on every frame Run stays
///   active, and not for the distress override below) it screams once, then
///   charges the last-seen position at full speed; if sight is lost it keeps
///   running to that last-seen spot (investigating) until it arrives, then
///   falls back to WALK/PHONE.
///
///   DISTRESS (phone dead, set externally by PlayerStats) overrides everything
///   and beelines the player's exact live position. No scream — that's reserved
///   for an actual visual spotting.
///
///   CATCH — within catchDistance of the player fires onCatchPlayer (wire to
///   BossEndScreen.ShowEndScreen()).
///
/// AUDIO: builds its own AudioSources at runtime (phone/walk/run loops +
/// scream one-shot) — just drag clips into the fields below, nothing to add
/// in the Inspector by hand. The three loops crossfade against each other as
/// the mode changes, the same way the player's own footstep audio does.
///
/// ANIMATOR: one Int parameter "MoveMode" (0 phone / 1 walk / 2 run), driving
/// three AnyState transitions with no exit time — already set up on
/// Assets/Animations/Boss/Boss.controller.
///
/// ACTIVATION: stays dormant (hidden, NavMeshAgent stopped) until something
/// external calls Activate() — BossLaughEvent calls it the moment the laugh
/// plays, so the boss "wakes up" exactly when the player hears it, not on its
/// own independent timer (which would drift out of sync with the real game
/// clock if the intro dialogue took a different amount of time to click
/// through).
///
/// SETUP CHECKLIST:
///   1. Put this on the Boss GameObject (the one with the skinned mesh).
///   2. Add components: NavMeshAgent, Animator (Boss.controller).
///   3. Tag the player object "Player" (top-left of Inspector) so it's auto-found.
///   4. Bake a NavMesh over the floor — required, nothing here can move without one.
///   5. Wire onCatchPlayer -> BossEndScreen.ShowEndScreen() in the Inspector.
///   6. Drag this Boss GameObject into BossLaughEvent's 'bossAI' field so it
///      wakes up at the 20-second laugh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
{
    public enum Mode { Phone = 0, Walk = 1, Run = 2 }

    [Header("Target")]
    [Tooltip("Auto-found by the 'Player' tag if left empty.")]
    public Transform player;

    [Header("Activation")]
    [Tooltip("Hide the boss's renderers and stop the NavMeshAgent until Activate() is called.")]
    public bool hideUntilActivation = true;

    [Header("Base Speeds (metres/second)")]
    public float phoneSpeed = 1.5f;
    public float walkSpeed = 2.5f;
    [Tooltip("Set this ~= your player's sprint speed so it can actually catch up.")]
    public float runSpeed = 5f;

    [Header("State 1 — Idle Leash")]
    [Tooltip("Always-aware leash: within this distance the boss just paces near its spot; beyond it, it paths back toward the player.")]
    public float leashDistance = 70f;
    [Tooltip("How far from its current spot the boss wanders while pacing.")]
    public float idleWanderRadius = 6f;
    [Tooltip("How often the boss picks a new wander point while pacing.")]
    public float idleWanderInterval = 4f;

    [Header("State 3 — Sight (Spotting)")]
    public float sightRange = 20f;
    [Tooltip("If true, a wall between boss and player blocks spotting.")]
    public bool requireLineOfSight = true;
    [Tooltip("A hidden player (PlayerHiding) can't be spotted.")]
    public bool respectsHiding = true;
    [Tooltip("Distance from the last-seen spot at which the boss gives up investigating and drops back down.")]
    public float investigateArriveDistance = 1.5f;

    [Header("Catch")]
    public float catchDistance = 1.5f;

    [Header("Distress (phone dead)")]
    [Tooltip("Set by PlayerStats when the player's phone dies. Overrides everything else and beelines the player.")]
    public bool distressActive = false;

    [Header("Speed Escalation Over Time")]
    [Tooltip("Uses GameTimer's elapsed seconds — the visible stopwatch on screen, which pauses during dialogue — never level-load or any other clock.")]
    public GameTimer gameTimer;
    public float escalationDelay = 60f;
    public float escalationInterval = 60f;
    [Tooltip("Compounds every escalationInterval seconds of the timer: 1.1 = +10% speed per minute.")]
    public float escalationFactor = 1.1f;

    [Header("Noise Hearing")]
    [Tooltip("Auto-found if left empty. Boss hears the player whenever it's within their current AudibleRangeMeters.")]
    public PlayerNoiseMeter noiseMeter;

    [Header("Audio — Movement Loops (clips only, sources are built at runtime)")]
    public AudioClip phoneClip;
    [Tooltip("Can go above 1 to boost past the clip's original recorded loudness.")]
    [Range(0f, 2f)] public float phoneVolume = 1.6f;
    public AudioClip walkClip;
    [Tooltip("Can go above 1 to boost past the clip's original recorded loudness.")]
    [Range(0f, 3f)] public float walkVolume = 2.6f;
    public AudioClip runClip;
    [Range(0f, 1f)] public float runVolume = 0.7f;
    [Tooltip("How long each loop takes to fade in/out when the mode switches.")]
    public float loopFadeSeconds = 0.5f;

    [Header("Audio — Scream (plays once per spotting, rate-limited)")]
    public AudioClip screamClip;
    [Range(0f, 1f)] public float screamVolume = 1f;
    [Tooltip("Minimum time between screams. Re-spotting the player within this window stays silent; the next scream only happens once this much time has passed AND the player is spotted again.")]
    public float screamCooldownSeconds = 20f;

    [Header("Audio — Suspense Emitter (plays only while WALKING or RUNNING)")]
    [Tooltip("Continuous sound from the boss's position, active only in the Walk and Run tiers (silent while idling on the phone). Base volume can go above 1 to boost past the clip's original loudness; on top of that, it's real 3D positional audio, so Unity's own distance falloff still makes it louder the closer the boss actually is.")]
    public AudioClip emitClip;
    [Range(0f, 2f)] public float emitVolume = 1.6f;
    [Tooltip("Within this distance, the emitter is at full volume.")]
    public float emitMinDistance = 4f;
    [Tooltip("Beyond this distance, the emitter is inaudible.")]
    public float emitMaxDistance = 60f;

    const string P_MOVE_MODE = "MoveMode";

    NavMeshAgent _agent;
    Animator _animator;
    PlayerHiding _playerHiding;
    Renderer[] _renderers;

    AudioSource _phoneLoop;
    AudioSource _walkLoop;
    AudioSource _runLoop;
    AudioSource _screamSource;
    AudioSource _emitSource;

    bool _activated;
    bool _caught;
    Mode _mode = Mode.Phone;
    bool _wasSpotted;
    float _lastScreamTime = -999f;

    Vector3 _lastSeenPos;
    bool _investigating;

    Vector3 _wanderTarget;
    float _nextWanderTime;

    [Header("Events")]
    [Tooltip("Auto-found if left empty. Fires alongside onCatchPlayer below, so the end screen works with zero manual wiring.")]
    public BossEndScreen endScreen;
    [Tooltip("Optional extra listeners beyond the auto-found endScreen above.")]
    public UnityEngine.Events.UnityEvent onCatchPlayer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _renderers = GetComponentsInChildren<Renderer>();

        // Forced regardless of the Inspector checkbox: if the clip has any
        // baked-in root motion and Apply Root Motion is on, the Animator and
        // the NavMeshAgent both try to move the transform independently —
        // that fight is exactly what "freezes then teleports" looks like.
        // NavMeshAgent must be the only thing moving this transform.
        _animator.applyRootMotion = false;

        // Also forced: if culling is anything other than AlwaysAnimate, the
        // Animator can stop updating while not directly in a camera's view,
        // which looks exactly like "moves sometimes" / freezing intermittently.
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        _phoneLoop = CreateLoopSource(phoneClip);
        _walkLoop = CreateLoopSource(walkClip);
        _runLoop = CreateLoopSource(runClip);

        _screamSource = gameObject.AddComponent<AudioSource>();
        _screamSource.playOnAwake = false;
        _screamSource.spatialBlend = 1f;

        _emitSource = gameObject.AddComponent<AudioSource>();
        _emitSource.playOnAwake = false;
        _emitSource.loop = true;
        _emitSource.spatialBlend = 1f;
        _emitSource.rolloffMode = AudioRolloffMode.Logarithmic;
        _emitSource.minDistance = emitMinDistance;
        _emitSource.maxDistance = emitMaxDistance;
        _emitSource.volume = 0f;
        _emitSource.clip = emitClip;

        if (gameTimer == null) gameTimer = Object.FindAnyObjectByType<GameTimer>();
        if (noiseMeter == null) noiseMeter = Object.FindAnyObjectByType<PlayerNoiseMeter>();
        if (endScreen == null) endScreen = Object.FindAnyObjectByType<BossEndScreen>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null)
        {
            _playerHiding = player.GetComponent<PlayerHiding>();
            _lastSeenPos = player.position;
        }
    }

    AudioSource CreateLoopSource(AudioClip clip)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 1f;
        src.volume = 0f;
        src.clip = clip;
        return src;
    }

    void Start()
    {
        _agent.isStopped = true;
        if (hideUntilActivation) SetRenderersEnabled(false);
        _animator.SetInteger(P_MOVE_MODE, (int)Mode.Phone);
    }

    void Update()
    {
        if (_caught || !_activated || player == null)
        {
            return;
        }

        EnsureAnimationPlaying();
        EnsureOnNavMesh();

        float dist = Vector3.Distance(transform.position, player.position);

        if (distressActive)
        {
            SetMode(Mode.Run);
            _agent.speed = CurrentSpeed(Mode.Run);
            SetDestinationSafe(player.position);
            UpdateLoopAudio(Mode.Run);
            CheckStuck();
            if (dist <= catchDistance) CatchPlayer();
            return;
        }

        bool hidden = respectsHiding && _playerHiding != null && _playerHiding.isHidden;
        bool spotted = !hidden && IsPlayerSpotted(dist);

        // Scream on the frame the player is actually (re-)spotted — independent
        // of which Mode that happens to put us in, so the distress override
        // above can never trigger it — but rate-limited: re-spotting within
        // screamCooldownSeconds of the last scream stays silent.
        if (spotted && !_wasSpotted && Time.time - _lastScreamTime >= screamCooldownSeconds)
        {
            PlayScream();
            _lastScreamTime = Time.time;
        }
        _wasSpotted = spotted;

        if (spotted)
        {
            _lastSeenPos = player.position;
            _investigating = true;
        }
        else if (_investigating && Vector3.Distance(transform.position, _lastSeenPos) <= investigateArriveDistance)
        {
            _investigating = false;
        }

        bool running = spotted || _investigating;

        bool heard = !hidden && !running && noiseMeter != null
                     && noiseMeter.CurrentNoise > 0f
                     && dist <= noiseMeter.AudibleRangeMeters;

        Mode newMode;
        Vector3 target;

        if (running)
        {
            newMode = Mode.Run;
            target = _lastSeenPos;
        }
        else if (heard)
        {
            newMode = Mode.Walk;
            target = player.position;
        }
        else
        {
            newMode = Mode.Phone;
            target = dist > leashDistance ? player.position : GetWanderTarget();
        }

        SetMode(newMode);
        _agent.speed = CurrentSpeed(newMode);
        SetDestinationSafe(target);
        UpdateLoopAudio(newMode);
        CheckStuck();

        if (dist <= catchDistance) CatchPlayer();
    }

    // =========================================================================
    // PATHFINDING SAFETY — never feed an off-mesh point, and recover if the
    // agent stalls (e.g. clipping a wall corner, a stale/partial path).
    // =========================================================================

    [Header("Pathfinding Safety")]
    [Tooltip("How far off the navmesh a destination is allowed to be before it gets snapped to the nearest valid point.")]
    public float navMeshSnapRadius = 5f;
    [Tooltip("If the agent makes less than this much progress for stuckTimeout seconds while it should be moving, force a fresh path.")]
    public float stuckMoveThreshold = 0.3f;
    public float stuckTimeout = 1.5f;

    Vector3 _stuckCheckPos;
    float _stuckCheckTimer;

    void SetDestinationSafe(Vector3 target)
    {
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, navMeshSnapRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    void CheckStuck()
    {
        if (_agent.pathPending || !_agent.hasPath)
        {
            _stuckCheckTimer = 0f;
            _stuckCheckPos = transform.position;
            return;
        }

        bool shouldBeMoving = _agent.remainingDistance > _agent.stoppingDistance + 0.25f;
        if (!shouldBeMoving)
        {
            _stuckCheckTimer = 0f;
            _stuckCheckPos = transform.position;
            return;
        }

        _stuckCheckTimer += Time.deltaTime;
        if (_stuckCheckTimer < stuckTimeout)
        {
            return;
        }

        float moved = Vector3.Distance(transform.position, _stuckCheckPos);
        _stuckCheckTimer = 0f;
        _stuckCheckPos = transform.position;

        if (moved < stuckMoveThreshold)
        {
            // Stalled (clipped a corner, stale/partial path, etc.) — force a
            // fresh path calculation to the same destination.
            Vector3 currentDestination = _agent.destination;
            _agent.ResetPath();
            SetDestinationSafe(currentDestination);
        }
    }

    // =========================================================================
    // AUDIO — crossfade the three movement loops toward whichever mode is active
    // =========================================================================

    void UpdateLoopAudio(Mode activeMode)
    {
        FadeLoop(_phoneLoop, activeMode == Mode.Phone, phoneVolume);
        FadeLoop(_walkLoop, activeMode == Mode.Walk, walkVolume);
        FadeLoop(_runLoop, activeMode == Mode.Run, runVolume);
        FadeLoop(_emitSource, activeMode == Mode.Walk || activeMode == Mode.Run, emitVolume);
    }

    void FadeLoop(AudioSource src, bool shouldPlay, float targetVolume)
    {
        if (src.clip == null) return;

        float goal = shouldPlay ? targetVolume : 0f;
        src.volume = Mathf.MoveTowards(src.volume, goal, Time.deltaTime * targetVolume / Mathf.Max(0.01f, loopFadeSeconds));

        if (shouldPlay && !src.isPlaying)
        {
            src.Play();
        }
        else if (!shouldPlay && src.isPlaying && src.volume <= 0.001f)
        {
            src.Pause();
        }
    }

    // =========================================================================
    // STATE 1 — idle wander
    // =========================================================================

    Vector3 GetWanderTarget()
    {
        bool needNewTarget = Time.time >= _nextWanderTime
            || (!_agent.pathPending && _agent.remainingDistance <= 0.5f);

        if (needNewTarget)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * idleWanderRadius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, idleWanderRadius, NavMesh.AllAreas))
            {
                _wanderTarget = hit.position;
            }
            else
            {
                _wanderTarget = transform.position;
            }

            _nextWanderTime = Time.time + idleWanderInterval;
        }

        return _wanderTarget;
    }

    // =========================================================================
    // SPEED / ESCALATION
    // =========================================================================

    float CurrentSpeed(Mode m)
    {
        float baseSpeed = m == Mode.Run ? runSpeed : m == Mode.Walk ? walkSpeed : phoneSpeed;
        return baseSpeed * EscalationMultiplier();
    }

    /// <summary>1.0 until escalationDelay, then escalationFactor^steps, gaining
    /// one step every escalationInterval — compounding, tied to GameTimer.</summary>
    float EscalationMultiplier()
    {
        float elapsed = gameTimer != null ? gameTimer.ElapsedSeconds : 0f;
        if (elapsed < escalationDelay) return 1f;
        int steps = 1 + Mathf.FloorToInt((elapsed - escalationDelay) / Mathf.Max(0.01f, escalationInterval));
        return Mathf.Pow(escalationFactor, steps);
    }

    // =========================================================================
    // SIGHT
    // =========================================================================

    bool IsPlayerSpotted(float dist)
    {
        if (dist > sightRange) return false;
        if (!requireLineOfSight) return true;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir = (player.position + Vector3.up) - origin;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, sightRange))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return false;
    }

    // =========================================================================
    // EXTERNAL HOOKS
    // =========================================================================

    /// <summary>Wakes the boss up — call this exactly when the player should
    /// first become aware of it (BossLaughEvent calls it on the laugh).</summary>
    public void Activate()
    {
        if (_activated) return;

        _activated = true;
        EnsureOnNavMesh(instant: true);
        _agent.isStopped = false;
        _mode = Mode.Phone;
        SetRenderersEnabled(true);
        if (player != null) _lastSeenPos = player.position;
    }

    float _offMeshTimer;

    /// <summary>Hard guarantee that SOME animation is always actively playing.
    /// If the current clip has Loop Time off (a model-import setting that
    /// can't always be fixed from outside the Editor), Mecanim just holds
    /// the last frame forever once it finishes — indistinguishable from a
    /// freeze. The instant a clip reaches its end without looping, force it
    /// to restart from frame 0, regardless of its own loop setting.</summary>
    void EnsureAnimationPlaying()
    {
        if (_animator.IsInTransition(0)) return;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime >= 1f)
        {
            _animator.Play(state.fullPathHash, 0, 0f);
        }
    }

    /// <summary>If the agent isn't actually sitting on the baked navmesh
    /// (wrong spawn height, bake doesn't reach this spot, etc.), every
    /// SetDestination() call silently fails and the boss just stands there
    /// forever — this snaps it onto the nearest valid point so it can move.
    /// 'instant' skips the debounce — only safe to use before the boss is
    /// visible (Activate()); during live gameplay a single off-mesh frame
    /// (e.g. a tiny bake seam) shouldn't cause a visible snap, so Update()
    /// requires it to stay off-mesh for a sustained moment first.</summary>
    void EnsureOnNavMesh(bool instant = false)
    {
        if (_agent.isOnNavMesh)
        {
            _offMeshTimer = 0f;
            return;
        }

        if (!instant)
        {
            _offMeshTimer += Time.deltaTime;
            if (_offMeshTimer < 0.5f) return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSnapRadius, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            _offMeshTimer = 0f;
        }
        else
        {
            Debug.LogWarning("[BossAI] Not on a NavMesh and no valid point found nearby — bake (or re-bake) a NavMesh covering this position.", this);
        }
    }

    /// <summary>Called by PlayerStats when the phone dies (distress signal).</summary>
    public void SetDistress(bool on)
    {
        distressActive = on;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    void SetMode(Mode m)
    {
        _mode = m;
        _animator.SetInteger(P_MOVE_MODE, (int)m);
    }

    void CatchPlayer()
    {
        _caught = true;
        _agent.isStopped = true;
        if (endScreen != null) endScreen.ShowEndScreen();
        onCatchPlayer?.Invoke();
    }

    void PlayScream()
    {
        if (screamClip == null) return;
        _screamSource.PlayOneShot(screamClip, screamVolume);
    }

    void SetRenderersEnabled(bool on)
    {
        if (_renderers == null) return;
        foreach (Renderer r in _renderers) if (r != null) r.enabled = on;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, leashDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        // Live hearing range — grows/shrinks every frame with the player's
        // actual noise level, so you can watch it expand as you sprint/walk
        // and confirm Walk-mode kicks in exactly when this sphere reaches you.
        if (noiseMeter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, noiseMeter.AudibleRangeMeters);
        }
    }
}
