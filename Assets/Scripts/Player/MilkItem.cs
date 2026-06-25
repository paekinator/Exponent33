using UnityEngine;

/// <summary>
/// Milk in the player's inventory — stackable, you can carry many at once.
/// Each pickup (MilkPickupInteractable calls AddMilk()) adds one to the
/// stack; the HUD shows one slot per carton held (slot 2, 3, 4...).
///   - Press '2' to hold/un-hold a carton in view (same pattern as the
///     phone's '1'). Doesn't matter which one — they're all identical.
///   - While holding it, press E (away from any other interactable) to drink
///     it: restores water and consumes one carton. If more remain, you stay
///     holding one; if that was the last, it's un-held automatically.
///
/// SETUP: put this on the Player. Drag the milk PREFAB (visual only, e.g.
/// `Assets/Interactables/MilkCarton.prefab`) into 'milkViewPrefab'.
/// </summary>
public class MilkItem : MonoBehaviour
{
    public Camera cam;
    public PlayerStats stats;
    public PlayerInteractor interactor;
    public GameObject milkViewPrefab;

    public KeyCode toggleKey = KeyCode.Alpha2;
    public float waterRestoreAmount = 50f;

    [Header("Placement (local to camera)")]
    public Vector3 localPosition = new Vector3(-0.28f, -0.3f, 0.5f);
    public Vector3 localEulerAngles = new Vector3(0f, 0f, 0f);
    public Vector3 localScale = Vector3.one;

    GameObject _instance;
    int _milkCount;
    bool _isHeld;

    public bool HasMilk => _milkCount > 0;
    public int MilkCount => _milkCount;
    public bool IsHeld => _isHeld;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (interactor == null) interactor = GetComponent<PlayerInteractor>();
    }

    void Update()
    {
        if (_milkCount > 0 && Input.GetKeyDown(toggleKey))
        {
            SetHeld(!_isHeld);
        }

        // Drink only while holding it, and only if not currently targeting
        // some other world interactable (so this doesn't steal its E press).
        if (_isHeld && Input.GetKeyDown(KeyCode.E) && (interactor == null || !interactor.HasTarget))
        {
            Drink();
        }
    }

    /// <summary>Called by MilkPickupInteractable when a carton is picked up.</summary>
    public void AddMilk()
    {
        _milkCount++;
    }

    public void SetHeld(bool held)
    {
        if (held && _milkCount <= 0) return;

        _isHeld = held;

        if (_isHeld && _instance == null && cam != null && milkViewPrefab != null)
        {
            _instance = Instantiate(milkViewPrefab, cam.transform);
            _instance.transform.localPosition = localPosition;
            _instance.transform.localEulerAngles = localEulerAngles;
            _instance.transform.localScale = localScale;

            foreach (var col in _instance.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        if (_instance != null)
        {
            _instance.SetActive(_isHeld);
        }
    }

    void Drink()
    {
        if (stats != null)
        {
            stats.AddWater(waterRestoreAmount);
        }

        _milkCount--;

        if (_milkCount <= 0)
        {
            SetHeld(false);
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }
        }
    }
}
