using UnityEngine;

/// <summary>
/// PlayerStats — the player's survival resources: WATER and PHONE CHARGE.
///
/// RULES (from the game design):
///   • Both drain by 1 per second (≈100 s from full to empty at 100 max).
///   • Refill at a station by HOLDING E — 10% per second (handled by the
///     Water/Charger interactables calling AddWater / AddPhone).
///   • WATER hits 0  -> player FAINTS: movement disabled + faint screen.
///   • PHONE hits 0  -> distress signal: the Boss is told to run straight at
///     you (BossAI.SetDistress(true)); cleared once the phone is charged again.
///   • Resources keep draining even while hiding (this script never pauses).
///
/// SETUP:
///   Put this on the Player (same object as FirstPersonController).
///   Wire onFaint -> your Faint screen's BossEndScreen.ShowEndScreen().
///   (Hook up UI bars to 'water' / 'phone' however you like — 0..max range.)
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Water")]
    public float maxWater = 100f;
    public float water    = 100f;
    [Tooltip("Water lost per second at a normal pace.")]
    public float waterDrainPerSecond = 1f;
    [Tooltip("Water drain multiplier while sprinting.")]
    public float sprintWaterMultiplier = 1.5f;

    [Header("Phone Charge")]
    public float maxPhone = 100f;
    public float phone    = 100f;
    [Tooltip("1% every 1.5 seconds = 1/1.5 per second.")]
    public float phoneDrainPerSecond = 1f / 1.5f;
    [Tooltip("Total phone drain multiplier while the phone torch is on.")]
    public float phoneTorchDrainMultiplier = 1.5f;
    [HideInInspector] public bool phoneTorchActive;

    [Header("Refill")]
    [Tooltip("Units restored per second while holding E at a station (10 = 10%/s at max 100).")]
    public float refillPerSecond = 10f;

    [Header("References (auto-found if left empty)")]
    public FirstPersonController controller;
    public BossAI boss;

    [Header("Events")]
    [Tooltip("Fired once when water reaches 0. Wire to your faint screen's ShowEndScreen().")]
    public UnityEngine.Events.UnityEvent onFaint;

    bool _fainted  = false;
    bool _distress = false;

    void Awake()
    {
        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (boss == null)       boss       = Object.FindAnyObjectByType<BossAI>();
        water = Mathf.Clamp(water, 0f, maxWater);
        phone = Mathf.Clamp(phone, 0f, maxPhone);
    }

    void Update()
    {
        if (_fainted) return;

        bool sprinting = controller != null && controller.IsSprinting;
        float waterRate = waterDrainPerSecond * (sprinting ? sprintWaterMultiplier : 1f);
        water = Mathf.Max(0f, water - waterRate * Time.deltaTime);

        // ── Phone drains ──────────────────────────────────────────────────────
        float phoneRate = phoneDrainPerSecond * (phoneTorchActive ? phoneTorchDrainMultiplier : 1f);
        phone = Mathf.Max(0f, phone - phoneRate * Time.deltaTime);
        if (phone <= 0f)
        {
            phoneTorchActive = false;
        }

        // ── Out of water -> faint ─────────────────────────────────────────────
        if (water <= 0f) Faint();

        // ── Phone dead -> distress (boss runs straight to you) ────────────────
        bool deadNow = phone <= 0f;
        if (deadNow != _distress)
        {
            _distress = deadNow;
            if (boss != null) boss.SetDistress(_distress);
            Debug.Log(_distress
                ? "[PlayerStats] PHONE DEAD — distress signal! The boss is coming straight for you."
                : "[PlayerStats] Phone charged — distress cleared.");
        }
    }

    /// <summary>Restore water (called by the water station while E is held).</summary>
    public void AddWater(float amount) => water = Mathf.Clamp(water + amount, 0f, maxWater);

    /// <summary>Restore phone charge (called by the charger while E is held).</summary>
    public void AddPhone(float amount) => phone = Mathf.Clamp(phone + amount, 0f, maxPhone);

    /// <summary>Convenient 0..1 values for UI bars.</summary>
    public float WaterNormalized => maxWater > 0f ? water / maxWater : 0f;
    public float PhoneNormalized => maxPhone > 0f ? phone / maxPhone : 0f;

    void Faint()
    {
        if (_fainted) return;
        _fainted = true;

        if (controller != null) controller.enabled = false; // stop moving/looking
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log("[PlayerStats] You have fainted (out of water).");
        onFaint?.Invoke();
    }
}
