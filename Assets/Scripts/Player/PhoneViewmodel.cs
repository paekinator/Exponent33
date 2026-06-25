using UnityEngine;

/// <summary>
/// Spawns the held phone gadget as a child of the camera, like a first-person
/// viewmodel. Avoids hand-placing it in the scene — just drag the CellPhone
/// prefab into 'phonePrefab' and this parents/positions it on Start.
///
/// SETUP: put this on the Player (or the camera itself). If 'cam' is left
/// empty it uses Camera.main.
/// </summary>
public class PhoneViewmodel : MonoBehaviour
{
    public Camera cam;
    public GameObject phonePrefab;
    public PlayerStats stats;
    public KeyCode toggleKey = KeyCode.Alpha1;
    public KeyCode torchKey = KeyCode.T;
    public bool startsHeld = true;

    [Header("Placement (local to camera)")]
    public Vector3 localPosition = new Vector3(0.22f, -0.26f, 0.4f);
    public Vector3 localEulerAngles = new Vector3(18f, -90f, -6f);
    public Vector3 localScale = new Vector3(1.8f, 1.8f, 1.8f);

    [Header("Torch")]
    public Vector3 torchLocalPosition = new Vector3(0.18f, -0.08f, 0.28f);
    public Color torchColor = new Color(1f, 0.92f, 0.72f, 1f);
    public float torchIntensity = 22f;
    public float torchRange = 70f;
    public float torchSpotAngle = 92f;

    GameObject _instance;
    Light _torchLight;
    bool _isHeld;
    bool _torchOn;

    public bool IsHeld => _isHeld;
    public bool TorchOn => _torchOn;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (stats == null) stats = GetComponent<PlayerStats>();
        _isHeld = startsHeld;

        if (cam == null || phonePrefab == null) return;

        _instance = Instantiate(phonePrefab, cam.transform);
        _instance.transform.localPosition = localPosition;
        _instance.transform.localEulerAngles = localEulerAngles;
        _instance.transform.localScale = localScale;

        // Viewmodels shouldn't be culled if held close inside walls/geometry.
        foreach (var col in _instance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        CreateTorchLight();
        _instance.SetActive(_isHeld);
        UpdateTorchState();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetHeld(!_isHeld);
        }

        if (Input.GetKeyDown(torchKey))
        {
            SetTorch(!_torchOn);
        }

        if (_torchOn && (!_isHeld || stats == null || stats.phone <= 0f))
        {
            SetTorch(false);
        }
    }

    void OnDisable()
    {
        SetTorch(false);
    }

    public void SetHeld(bool held)
    {
        _isHeld = held;
        if (_instance != null)
        {
            _instance.SetActive(_isHeld);
        }

        if (!_isHeld)
        {
            SetTorch(false);
        }
        else
        {
            UpdateTorchState();
        }
    }

    public void SetTorch(bool active)
    {
        _torchOn = active && _isHeld && stats != null && stats.phone > 0f;
        if (stats != null)
        {
            stats.phoneTorchActive = _torchOn;
        }

        UpdateTorchState();
    }

    void CreateTorchLight()
    {
        GameObject torchObject = new GameObject("Phone_Torch_Light");
        torchObject.transform.SetParent(cam.transform, false);
        torchObject.transform.localPosition = torchLocalPosition;
        torchObject.transform.localRotation = Quaternion.identity;

        _torchLight = torchObject.AddComponent<Light>();
        _torchLight.type = LightType.Spot;
        _torchLight.color = torchColor;
        _torchLight.intensity = torchIntensity;
        _torchLight.range = torchRange;
        _torchLight.spotAngle = torchSpotAngle;
        _torchLight.innerSpotAngle = torchSpotAngle * 0.72f;
        _torchLight.shadows = LightShadows.Soft;
        _torchLight.enabled = false;
    }

    void UpdateTorchState()
    {
        if (_torchLight == null)
        {
            return;
        }

        _torchLight.enabled = _torchOn && _isHeld;
    }
}
