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
    public KeyCode toggleKey = KeyCode.Alpha1;
    public bool startsHeld = true;

    [Header("Placement (local to camera)")]
    public Vector3 localPosition = new Vector3(0.25f, -0.2f, 0.4f);
    public Vector3 localEulerAngles = new Vector3(10f, -90f, 0f);
    public Vector3 localScale = Vector3.one;

    GameObject _instance;
    bool _isHeld;

    public bool IsHeld => _isHeld;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        _isHeld = startsHeld;

        if (cam == null || phonePrefab == null) return;

        _instance = Instantiate(phonePrefab, cam.transform);
        _instance.transform.localPosition = localPosition;
        _instance.transform.localEulerAngles = localEulerAngles;
        _instance.transform.localScale = localScale;

        // Viewmodels shouldn't be culled if held close inside walls/geometry.
        foreach (var col in _instance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        _instance.SetActive(_isHeld);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetHeld(!_isHeld);
        }
    }

    public void SetHeld(bool held)
    {
        _isHeld = held;
        if (_instance != null)
        {
            _instance.SetActive(_isHeld);
        }
    }
}
