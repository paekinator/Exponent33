using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    [Header("State")]
    public bool isHidden = false;

    [Header("Auto Found References")]
    public MonoBehaviour movementController;
    public Transform cameraJoint;

    [Header("Hidden Look Settings")]
    public bool allowLookWhileHidden = true;
    public float hiddenLookSensitivity = 2f;
    public float maxVerticalLookAngle = 45f;
    public float maxHorizontalLookAngle = 75f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Quaternion originalCameraJointLocalRotation;

    private Quaternion hiddenStartRotation;
    private float hiddenYaw = 0f;
    private float hiddenPitch = 0f;

    void Awake()
    {
        FindMovementController();
        FindCameraJoint();
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
                Debug.Log("Found movement controller: " + script.GetType().Name);
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
            Debug.Log("Found camera joint.");
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

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if (cameraJoint != null)
        {
            originalCameraJointLocalRotation = cameraJoint.localRotation;
            cameraJoint.localRotation = Quaternion.identity;
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

        Debug.Log("Player is hiding.");
    }

    public void ExitHideSpot()
    {
        if (!isHidden) return;

        isHidden = false;

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        if (cameraJoint != null)
        {
            cameraJoint.localRotation = originalCameraJointLocalRotation;
        }

        if (movementController != null)
        {
            movementController.enabled = true;
        }

        Debug.Log("Player exited hiding.");
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

        if (cameraJoint != null)
        {
            cameraJoint.localRotation = Quaternion.Euler(hiddenPitch, 0f, 0f);
        }
    }
}