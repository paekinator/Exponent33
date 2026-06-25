using System.Collections;
using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    [Header("State")]
    public bool isHidden = false;

    [Header("Auto Found References")]
    public MonoBehaviour movementController;
    public Transform cameraJoint;
    public Camera playerCamera;
    public Rigidbody playerRigidbody;

    [Header("Hidden Look Settings")]
    public bool allowLookWhileHidden = true;
    public float hiddenLookSensitivity = 2f;
    public float maxVerticalLookAngle = 45f;
    public float maxHorizontalLookAngle = 75f;

    [Header("Enter/Exit Sound")]
    [Tooltip("Played on both entering AND exiting a locker — only the lockerSoundStartTime-to-lockerSoundEndTime slice of the clip, not the whole thing.")]
    public AudioClip lockerSound;
    [Range(0f, 1f)] public float lockerSoundVolume = 0.85f;
    public float lockerSoundStartTime = 0.5f;
    public float lockerSoundEndTime = 3f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 cameraJointNeutralLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private bool wasRigidbodyKinematic;

    private Quaternion hiddenStartRotation;
    private float hiddenYaw = 0f;
    private float hiddenPitch = 0f;

    private AudioSource lockerAudioSource;
    private Coroutine lockerSoundRoutine;

    void Awake()
    {
        FindMovementController();
        FindCameraJoint();

        playerRigidbody = GetComponent<Rigidbody>();

        lockerAudioSource = gameObject.AddComponent<AudioSource>();
        lockerAudioSource.playOnAwake = false;
        lockerAudioSource.loop = false;
        lockerAudioSource.spatialBlend = 0f;
        lockerAudioSource.clip = lockerSound;

        // Captured here (before any movement/headbob has happened) so it's
        // guaranteed to be the true resting position to snap back to on hide.
        if (cameraJoint != null)
        {
            cameraJointNeutralLocalPosition = cameraJoint.localPosition;
        }
    }

    void Update()
    {
        if (isHidden && allowLookWhileHidden)
        {
            HandleHiddenLook();
        }
    }

    void FindMovementController()
    {
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null) continue;

            if (script.GetType().Name == "FirstPersonController")
            {
                movementController = script;
                return;
            }
        }

        Debug.LogWarning("Could not find FirstPersonController on this player.");
    }

    void FindCameraJoint()
    {
        Transform foundJoint = transform.Find("Joint");

        if (foundJoint != null)
        {
            cameraJoint = foundJoint;
            // FirstPersonController only ever moves Joint's LOCAL POSITION
            // (headbob) — mouse-look pitch is applied directly to the camera
            // itself, a child of Joint. Pitch must target the camera, not
            // Joint, or the two fight/compound into a broken-looking view.
            playerCamera = foundJoint.GetComponentInChildren<Camera>();
        }
        else
        {
            Debug.LogWarning("Could not find Joint child object on player.");
        }
    }

    public void EnterHideSpot(Transform hideCameraPoint)
    {
        if (isHidden) return;

        isHidden = true;
        PlayLockerSound();

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Cancel any in-progress headbob offset and any leftover pitch from
        // the instant before E was pressed — without this the view while
        // hidden inherits whatever stale look angle/bob the player happened
        // to have, instead of looking dead level out of the locker.
        if (cameraJoint != null)
        {
            cameraJoint.localPosition = cameraJointNeutralLocalPosition;
        }
        if (playerCamera != null)
        {
            originalCameraLocalRotation = playerCamera.transform.localRotation;
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        transform.position = hideCameraPoint.position;
        transform.rotation = hideCameraPoint.rotation;

        hiddenStartRotation = hideCameraPoint.rotation;
        hiddenYaw = 0f;
        hiddenPitch = 0f;

        if (movementController != null)
        {
            movementController.enabled = false;
        }

        // Sticks the player to the locker: a kinematic rigidbody ignores
        // gravity, residual velocity, and collisions, but can still be moved
        // directly via transform (the snap above, and the clamped look-around
        // below) — so nothing physical can drag the player off the spot.
        if (playerRigidbody != null)
        {
            wasRigidbodyKinematic = playerRigidbody.isKinematic;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }
    }

    public void ExitHideSpot()
    {
        if (!isHidden) return;

        isHidden = false;
        PlayLockerSound();

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = wasRigidbodyKinematic;
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = originalCameraLocalRotation;
        }

        if (movementController != null)
        {
            movementController.enabled = true;
        }
    }

    /// <summary>Plays only the lockerSoundStartTime-to-lockerSoundEndTime slice
    /// of the clip (e.g. 0.5s-3s of a 7s file) rather than the whole thing —
    /// jumps playback to the start time, then stops it after that slice's
    /// duration instead of letting it run to the clip's actual end.</summary>
    void PlayLockerSound()
    {
        if (lockerSound == null || lockerAudioSource == null) return;

        if (lockerSoundRoutine != null)
        {
            StopCoroutine(lockerSoundRoutine);
        }

        lockerAudioSource.clip = lockerSound;
        lockerAudioSource.time = Mathf.Clamp(lockerSoundStartTime, 0f, Mathf.Max(0f, lockerSound.length - 0.05f));
        lockerAudioSource.volume = lockerSoundVolume * GameAudioSettings.SfxOutputMultiplier;
        lockerAudioSource.Play();

        float sliceDuration = Mathf.Max(0.05f, lockerSoundEndTime - lockerSoundStartTime);
        lockerSoundRoutine = StartCoroutine(StopAfter(sliceDuration));
    }

    IEnumerator StopAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (lockerAudioSource != null) lockerAudioSource.Stop();
        lockerSoundRoutine = null;
    }

    void HandleHiddenLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * hiddenLookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * hiddenLookSensitivity;

        hiddenYaw += mouseX;
        hiddenPitch -= mouseY;

        hiddenYaw = Mathf.Clamp(hiddenYaw, -maxHorizontalLookAngle, maxHorizontalLookAngle);
        hiddenPitch = Mathf.Clamp(hiddenPitch, -maxVerticalLookAngle, maxVerticalLookAngle);

        transform.rotation = hiddenStartRotation * Quaternion.Euler(0f, hiddenYaw, 0f);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(hiddenPitch, 0f, 0f);
        }
    }
}
