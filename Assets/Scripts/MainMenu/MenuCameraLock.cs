using UnityEngine;

public class MenuCameraLock : MonoBehaviour
{
    Vector3 lockedPosition;
    Quaternion lockedRotation;

    void Awake()
    {
        LockToCurrentTransform();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnEnable()
    {
        LockToCurrentTransform();
    }

    void LateUpdate()
    {
        transform.SetPositionAndRotation(lockedPosition, lockedRotation);
    }

    void LockToCurrentTransform()
    {
        lockedPosition = transform.position;
        lockedRotation = transform.rotation;
    }
}
