using UnityEngine;

/// <summary>
/// PlayerNoise — tells the Boss how much sound the player is making, based on
/// HOW they move. The Boss reacts to noise by speeding up to its WALK tier.
///
///   Crouch-walking (press C) -> SILENT. Boss stays on the phone (slow creep).
///   Walking                  -> a footstep noise every walkInterval seconds.
///   Sprinting (Shift)        -> noise more often (sprintInterval) — louder presence.
///   Jumping                  -> an immediate noise.
///
/// SETUP:
///   Put this on the Player (alongside FirstPersonController + Rigidbody).
///   No wiring needed — it finds the controller and broadcasts to every BossAI.
/// </summary>
public class PlayerNoise : MonoBehaviour
{
    [Header("References (auto-found)")]
    public FirstPersonController controller;

    [Header("Input")]
    [Tooltip("Must match FirstPersonController.jumpKey.")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Noise Tuning")]
    [Tooltip("Minimum horizontal speed (m/s) to count as moving.")]
    public float moveThreshold = 0.6f;
    [Tooltip("Seconds between footstep noises while walking.")]
    public float walkInterval = 0.6f;
    [Tooltip("Seconds between footstep noises while sprinting (smaller = louder).")]
    public float sprintInterval = 0.3f;

    [Header("Debug")]
    public bool showGizmo = true;
    public float gizmoRadius = 10f;

    Rigidbody _rb;
    float     _timer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (controller == null) controller = GetComponent<FirstPersonController>();
    }

    void Update()
    {
        // Jump is always an instant noise
        if (Input.GetKeyDown(jumpKey)) Emit();

        // Crouch-walking is completely silent
        if (controller != null && controller.IsCrouched) { _timer = 0f; return; }

        // How fast are we actually moving along the ground?
        float speed = 0f;
        if (_rb != null)
        {
            Vector3 v = _rb.linearVelocity; v.y = 0f;
            speed = v.magnitude;
        }
        if (speed < moveThreshold) { _timer = 0f; return; } // standing still = silent

        bool  sprinting = controller != null && controller.IsSprinting;
        float interval  = sprinting ? sprintInterval : walkInterval;

        _timer += Time.deltaTime;
        if (_timer >= interval) { _timer = 0f; Emit(); }
    }

    void Emit()
    {
        // Static helper alerts every BossAI in the scene
        BossAI.BroadcastNoise(transform.position);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}
