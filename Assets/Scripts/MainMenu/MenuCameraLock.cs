using UnityEngine;

public class MenuCameraLock : MonoBehaviour
{
    [Header("Cursor Camera Parallax")]
    [SerializeField] bool enableCursorParallax = true;
    [SerializeField] float horizontalMove = 0.28f;
    [SerializeField] float verticalMove = 0.12f;
    [SerializeField] float yawDegrees = 1.2f;
    [SerializeField] float pitchDegrees = 0.65f;
    [SerializeField] float zoomOutFov = 2.5f;
    [SerializeField] float smoothing = 7f;

    Vector3 lockedPosition;
    Quaternion lockedRotation;
    Transform lockedParent;
    Vector3 lockedParentPosition;
    Quaternion lockedParentRotation;
    Camera lockedCamera;
    Rigidbody parentRigidbody;
    float lockedFov;

    void Awake()
    {
        lockedCamera = GetComponent<Camera>();
        parentRigidbody = GetComponentInParent<Rigidbody>();
        LockToCurrentTransform();
        FreezeParentPhysics();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnEnable()
    {
        LockToCurrentTransform();
        FreezeParentPhysics();
    }

    void LateUpdate()
    {
        RestoreParentTransform();

        if (!enableCursorParallax || Screen.width <= 0 || Screen.height <= 0)
        {
            transform.SetPositionAndRotation(lockedPosition, lockedRotation);
            if (lockedCamera != null) lockedCamera.fieldOfView = lockedFov;
            return;
        }

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 normalized = ((Vector2)Input.mousePosition - center);
        normalized.x = Mathf.Clamp(normalized.x / center.x, -1f, 1f);
        normalized.y = Mathf.Clamp(normalized.y / center.y, -1f, 1f);

        Vector3 targetPosition = lockedPosition
            + transform.right * (normalized.x * horizontalMove)
            + transform.up * (normalized.y * verticalMove);

        Quaternion targetRotation = lockedRotation * Quaternion.Euler(
            -normalized.y * pitchDegrees,
            normalized.x * yawDegrees,
            0f);

        float blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, targetPosition, blend),
            Quaternion.Slerp(transform.rotation, targetRotation, blend));

        if (lockedCamera != null)
        {
            float distanceFromCenter = Mathf.Clamp01(normalized.magnitude);
            float targetFov = lockedFov + distanceFromCenter * zoomOutFov;
            lockedCamera.fieldOfView = Mathf.Lerp(lockedCamera.fieldOfView, targetFov, blend);
        }
    }

    void LockToCurrentTransform()
    {
        lockedParent = transform.parent;
        if (lockedParent != null)
        {
            lockedParentPosition = lockedParent.position;
            lockedParentRotation = lockedParent.rotation;
        }

        lockedPosition = transform.position;
        lockedRotation = transform.rotation;
        if (lockedCamera != null) lockedFov = lockedCamera.fieldOfView;
    }

    void FreezeParentPhysics()
    {
        if (parentRigidbody == null)
        {
            return;
        }

        parentRigidbody.useGravity = false;
        parentRigidbody.isKinematic = true;
        parentRigidbody.linearVelocity = Vector3.zero;
        parentRigidbody.angularVelocity = Vector3.zero;
    }

    void RestoreParentTransform()
    {
        if (lockedParent == null)
        {
            return;
        }

        lockedParent.SetPositionAndRotation(lockedParentPosition, lockedParentRotation);
    }
}
