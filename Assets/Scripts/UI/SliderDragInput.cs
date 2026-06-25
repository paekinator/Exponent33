using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Makes a Unity Slider respond predictably to click-and-drag across its full
/// rect. Useful for runtime-built menus where the default handle-only drag can
/// feel like it only jumps on click.
/// </summary>
public class SliderDragInput : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    Slider slider;
    RectTransform sliderRect;

    public static void Attach(Slider target)
    {
        if (target == null) return;

        GameObject obj = target.gameObject;
        if (obj.GetComponent<Graphic>() == null)
        {
            Image hitbox = obj.AddComponent<Image>();
            hitbox.color = new Color(1f, 1f, 1f, 0f);
            hitbox.raycastTarget = true;
        }

        SliderDragInput drag = obj.GetComponent<SliderDragInput>();
        if (drag == null) drag = obj.AddComponent<SliderDragInput>();
        drag.Bind(target);
    }

    void Awake()
    {
        if (slider == null)
        {
            Bind(GetComponent<Slider>());
        }
    }

    public void Bind(Slider target)
    {
        slider = target;
        sliderRect = target != null ? target.GetComponent<RectTransform>() : null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        SetFromPointer(eventData);
    }

    void SetFromPointer(PointerEventData eventData)
    {
        if (slider == null || sliderRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sliderRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float left = sliderRect.rect.xMin;
        float width = Mathf.Max(1f, sliderRect.rect.width);
        float normalized = Mathf.Clamp01((localPoint.x - left) / width);
        slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, normalized);
    }
}
