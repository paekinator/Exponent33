using UnityEngine;

public class MenuBackdropParallax : MonoBehaviour
{
    [SerializeField] float horizontalOffset = 0.22f;
    [SerializeField] float verticalOffset = 0.09f;
    [SerializeField] float yawDegrees = 0.55f;
    [SerializeField] float pitchDegrees = 0.28f;
    [SerializeField] float smoothing = 8f;

    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 normalized = ((Vector2)Input.mousePosition - center);
        normalized.x = Mathf.Clamp(normalized.x / center.x, -1f, 1f);
        normalized.y = Mathf.Clamp(normalized.y / center.y, -1f, 1f);

        Vector3 targetPosition = startPosition + new Vector3(
            -normalized.x * horizontalOffset,
            normalized.y * verticalOffset,
            0f);
        Quaternion targetRotation = startRotation * Quaternion.Euler(
            normalized.y * pitchDegrees,
            -normalized.x * yawDegrees,
            0f);

        float blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, blend);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, blend);
    }
}
