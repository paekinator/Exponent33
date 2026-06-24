using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime survival HUD for quick scene testing. If no UI is assigned in the
/// inspector, it builds water/phone bars and a Roblox-style phone slot at play.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    public PlayerStats stats;
    public PhoneViewmodel phoneViewmodel;
    public PlayerInteractor interactor;

    [Header("Bars")]
    public Image waterFill;
    public Image phoneFill;
    public TextMeshProUGUI waterText;
    public TextMeshProUGUI phoneText;

    [Header("Phone Slot")]
    public Image phoneSlotBackground;
    public TextMeshProUGUI phoneSlotText;

    [Header("Prompt")]
    public TextMeshProUGUI promptText;

    [Header("Colors")]
    public Color waterColor = new Color(0.15f, 0.55f, 1f);
    public Color phoneColor = new Color(0.2f, 0.95f, 0.45f);
    public Color lowColor = new Color(0.9f, 0.15f, 0.15f);
    public Color slotHeldColor = new Color(1f, 0.9f, 0.25f, 0.9f);
    public Color slotStoredColor = new Color(0.08f, 0.08f, 0.08f, 0.75f);
    [Range(0f, 1f)] public float lowThreshold = 0.25f;

    void Awake()
    {
        if (stats == null) stats = Object.FindAnyObjectByType<PlayerStats>();
        if (phoneViewmodel == null) phoneViewmodel = Object.FindAnyObjectByType<PhoneViewmodel>();
        if (interactor == null) interactor = Object.FindAnyObjectByType<PlayerInteractor>();

        if (waterFill == null || phoneFill == null || phoneSlotBackground == null || promptText == null)
        {
            BuildRuntimeHud();
        }

        if (interactor != null && interactor.promptText == null)
        {
            interactor.promptText = promptText;
        }
    }

    void Update()
    {
        if (stats != null)
        {
            UpdateBar(waterFill, waterText, stats.WaterNormalized, stats.water, waterColor, "Water");
            UpdateBar(phoneFill, phoneText, stats.PhoneNormalized, stats.phone, phoneColor, "Phone");
        }

        if (phoneSlotBackground != null)
        {
            bool held = phoneViewmodel != null && phoneViewmodel.IsHeld;
            phoneSlotBackground.color = held ? slotHeldColor : slotStoredColor;
        }

        if (phoneSlotText != null)
        {
            phoneSlotText.text = "1\nPhone";
        }
    }

    void UpdateBar(Image fill, TextMeshProUGUI label, float normalized, float value, Color normalColor, string name)
    {
        normalized = Mathf.Clamp01(normalized);

        if (fill != null)
        {
            fill.fillAmount = normalized;
            fill.color = normalized <= lowThreshold ? lowColor : normalColor;
        }

        if (label != null)
        {
            label.text = $"{name}: {Mathf.CeilToInt(value)}%";
        }
    }

    void BuildRuntimeHud()
    {
        GameObject canvasObject = GameObject.Find("Player_Runtime_HUD");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (canvas == null)
        {
            canvasObject = new GameObject("Player_Runtime_HUD");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform root = canvas.transform;
        if (waterFill == null)
        {
            waterFill = CreateBar(root, "Water_Bar", new Vector2(24f, 74f), waterColor, out waterText);
        }

        if (phoneFill == null)
        {
            phoneFill = CreateBar(root, "Phone_Bar", new Vector2(24f, 34f), phoneColor, out phoneText);
        }

        if (phoneSlotBackground == null)
        {
            phoneSlotBackground = CreatePanel(root, "Phone_Slot", new Vector2(86f, 86f), new Vector2(-76f, 60f), slotStoredColor);
            phoneSlotBackground.rectTransform.anchorMin = new Vector2(1f, 0f);
            phoneSlotBackground.rectTransform.anchorMax = new Vector2(1f, 0f);
            phoneSlotBackground.rectTransform.pivot = new Vector2(1f, 0f);
            phoneSlotBackground.rectTransform.anchoredPosition = new Vector2(-24f, 24f);
            phoneSlotText = CreateText(phoneSlotBackground.transform, "Phone_Slot_Label", "1\nPhone", 18, TextAlignmentOptions.Center);
            Stretch(phoneSlotText.rectTransform, Vector2.zero);
        }

        if (promptText == null)
        {
            promptText = CreateText(root, "Interact_Prompt", "", 26, TextAlignmentOptions.Center);
            RectTransform rect = promptText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(520f, 60f);
        }
    }

    Image CreateBar(Transform parent, string name, Vector2 bottomLeftOffset, Color fillColor, out TextMeshProUGUI label)
    {
        Image background = CreatePanel(parent, name, new Vector2(260f, 24f), bottomLeftOffset, new Color(0.05f, 0.05f, 0.05f, 0.78f));
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.zero;

        GameObject fillObject = new GameObject(name + "_Fill");
        fillObject.transform.SetParent(background.transform, false);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        Stretch(fill.rectTransform, new Vector2(3f, 3f));

        label = CreateText(background.transform, name + "_Label", "", 16, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero);
        return fill;
    }

    Image CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return image;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    void Stretch(RectTransform rect, Vector2 padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = padding;
        rect.offsetMax = -padding;
    }
}
