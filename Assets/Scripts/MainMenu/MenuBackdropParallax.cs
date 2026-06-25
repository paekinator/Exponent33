using UnityEngine;

public class MenuBackdropParallax : MonoBehaviour
{
    [SerializeField] float horizontalOffset = 0.22f;
    [SerializeField] float verticalOffset = 0.09f;
    [SerializeField] float yawDegrees = 0.55f;
    [SerializeField] float pitchDegrees = 0.28f;
    [SerializeField] float smoothing = 8f;
    [SerializeField] bool layerChildrenByDepth = true;
    [SerializeField] float childHorizontalOffset = 0.08f;
    [SerializeField] float childVerticalOffset = 0.035f;
    [SerializeField] float childDepthScale = 0.035f;

    Vector3 startPosition;
    Quaternion startRotation;
    Transform[] childTransforms;
    Vector3[] childStartPositions;
    float[] childWeights;

    void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        CacheChildren();
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

        UpdateChildLayers(normalized, blend);
    }

    void CacheChildren()
    {
        int count = transform.childCount;
        childTransforms = new Transform[count];
        childStartPositions = new Vector3[count];
        childWeights = new float[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            childTransforms[i] = child;
            childStartPositions[i] = child.localPosition;
            childWeights[i] = Mathf.Clamp(child.localPosition.z * childDepthScale, -0.45f, 0.45f);
        }
    }

    void UpdateChildLayers(Vector2 normalized, float blend)
    {
        if (!layerChildrenByDepth || childTransforms == null)
        {
            return;
        }

        for (int i = 0; i < childTransforms.Length; i++)
        {
            Transform child = childTransforms[i];
            if (child == null)
            {
                continue;
            }

            float weight = childWeights[i];
            Vector3 target = childStartPositions[i] + new Vector3(
                -normalized.x * childHorizontalOffset * weight,
                normalized.y * childVerticalOffset * weight,
                0f);

            child.localPosition = Vector3.Lerp(child.localPosition, target, blend);
        }
    }
}
