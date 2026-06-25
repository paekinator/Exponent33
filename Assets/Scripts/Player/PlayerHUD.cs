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
    public PlayerNoiseMeter noiseMeter;
    public MilkItem milkItem;
    public GameTimer gameTimer;
    public IntroDialogueSequencer introSequencer;

    [Header("Stopwatch")]
    public Image timerBackground;
    public TextMeshProUGUI timerText;

    [Header("Bars")]
    public Image waterFill;
    public Image phoneFill;
    public TextMeshProUGUI waterText;
    public TextMeshProUGUI phoneText;

    [Header("Noise")]
    public Image noisePanel;
    public Image noiseFill;
    public TextMeshProUGUI noiseTitleText;
    public TextMeshProUGUI noiseLabelText;
    public TextMeshProUGUI noiseValueText;
    [Tooltip("How long the displayed noise bar takes to ease toward the real value — purely visual, the underlying noise number (and boss hearing range) is unaffected.")]
    public float noiseDisplaySmoothTime = 0.3f;
    float displayedNoise;
    bool noiseHudWasVisible;

    [Header("Phone Slot")]
    public Image phoneSlotBackground;
    public Outline phoneSlotOutline;
    public TextMeshProUGUI phoneSlotText;
    public Image phoneIconBody;
    public Image phoneIconScreen;

    [Header("Milk Slots (one per carton held, up to maxMilkSlots)")]
    public int maxMilkSlots = 16;
    public Image[] milkSlotBackgrounds;
    public Outline[] milkSlotOutlines;
    public TextMeshProUGUI[] milkSlotTexts;

    [Header("Prompt")]
    public Image promptBackground;
    public TextMeshProUGUI promptText;
    public TMP_FontAsset horrorPromptFont;
    public Color promptBackgroundColor = new Color(0.035f, 0.025f, 0.025f, 0.82f);
    public Color promptTextColor = new Color(0.95f, 0.78f, 0.68f, 1f);
    public Color promptAccentColor = new Color(0.6f, 0.05f, 0.04f, 0.95f);
    public Color consumeHintColor = new Color(0.35f, 0.65f, 1f, 1f);

    [Header("Reticle")]
    public Image centerDot;
    public Color centerDotColor = new Color(1f, 1f, 1f, 0.95f);
    public float centerDotSize = 5f;

    [Header("Colors")]
    public Color waterColor = new Color(0.15f, 0.55f, 1f, 0.68f);
    public Color waterMidColor = new Color(0.55f, 0.9f, 1f, 0.68f);
    public Color phoneColor = new Color(0.2f, 0.95f, 0.45f, 0.68f);
    public Color lowColor = new Color(0.9f, 0.15f, 0.15f, 0.82f);
    public Color slotHeldColor = new Color(0.34f, 0.34f, 0.34f, 0.86f);
    public Color slotStoredColor = new Color(0.08f, 0.08f, 0.08f, 0.28f);
    public Color slotOutlineColor = new Color(0.86f, 0.86f, 0.86f, 0.98f);
    [Range(0f, 1f)] public float lowThreshold = 0.25f;

    void Awake()
    {
        if (stats == null) stats = Object.FindAnyObjectByType<PlayerStats>();
        if (phoneViewmodel == null) phoneViewmodel = Object.FindAnyObjectByType<PhoneViewmodel>();
        if (interactor == null) interactor = Object.FindAnyObjectByType<PlayerInteractor>();
        if (noiseMeter == null) noiseMeter = Object.FindAnyObjectByType<PlayerNoiseMeter>();
        if (milkItem == null) milkItem = Object.FindAnyObjectByType<MilkItem>();
        if (gameTimer == null) gameTimer = Object.FindAnyObjectByType<GameTimer>();
        if (introSequencer == null) introSequencer = Object.FindAnyObjectByType<IntroDialogueSequencer>();

        if (waterFill == null || phoneFill == null || phoneSlotBackground == null || milkSlotBackgrounds == null || milkSlotBackgrounds.Length == 0 || promptText == null || promptBackground == null || centerDot == null || noisePanel == null || timerText == null)
        {
            BuildRuntimeHud();
        }

        StylePromptText(promptText);

        if (interactor != null && interactor.promptText == null)
        {
            interactor.promptText = promptText;
        }
    }

    void Update()
    {
        if (stats != null)
        {
            UpdateBar(waterFill, waterText, stats.WaterNormalized, stats.water, GetWaterColor(stats.WaterNormalized), "Water");
            UpdateBar(phoneFill, phoneText, stats.PhoneNormalized, stats.phone, GetPhoneColor(stats.PhoneNormalized), "Phone");
        }

        // Hidden during any dialogue chapter — you can't pull out or select
        // the phone while everything is frozen for a forced story beat.
        bool dialogueActive = introSequencer != null && introSequencer.IsActive;

        if (phoneSlotBackground != null)
        {
            phoneSlotBackground.gameObject.SetActive(!dialogueActive);

            if (!dialogueActive)
            {
                bool held = phoneViewmodel != null && phoneViewmodel.IsHeld;
                phoneSlotBackground.color = held ? slotHeldColor : slotStoredColor;
                if (phoneSlotOutline != null)
                {
                    phoneSlotOutline.enabled = held;
                }
            }
        }

        if (phoneSlotText != null)
        {
            phoneSlotText.text = "1";
        }

        // One slot per carton held (slot 2, 3, 4...), capped at maxMilkSlots —
        // slot index 0 ("2") is the one that highlights when actually held,
        // since the cartons are interchangeable; the rest are just count.
        if (milkSlotBackgrounds != null)
        {
            bool milkHeld = milkItem != null && milkItem.IsHeld;
            int milkCount = milkItem != null ? milkItem.MilkCount : 0;
            int visibleSlots = Mathf.Min(milkCount, milkSlotBackgrounds.Length);

            for (int i = 0; i < milkSlotBackgrounds.Length; i++)
            {
                if (milkSlotBackgrounds[i] == null) continue;

                bool show = !dialogueActive && i < visibleSlots;
                milkSlotBackgrounds[i].gameObject.SetActive(show);
                if (!show) continue;

                bool highlightThis = milkHeld && i == 0;
                milkSlotBackgrounds[i].color = highlightThis ? slotHeldColor : slotStoredColor;

                if (milkSlotOutlines != null && i < milkSlotOutlines.Length && milkSlotOutlines[i] != null)
                {
                    milkSlotOutlines[i].enabled = highlightThis;
                }

                if (milkSlotTexts != null && i < milkSlotTexts.Length && milkSlotTexts[i] != null)
                {
                    bool overflow = i == milkSlotBackgrounds.Length - 1 && milkCount > milkSlotBackgrounds.Length;
                    milkSlotTexts[i].text = overflow ? (2 + i) + "+" : (2 + i).ToString();
                }
            }
        }

        if (timerText != null)
        {
            timerText.text = gameTimer != null ? gameTimer.FormattedTime : "0:00";
        }

        UpdateNoiseHud();
    }

    void LateUpdate()
    {
        ApplyMilkPromptOverride();
        UpdatePromptVisibility();
    }

    /// <summary>Lets the held-milk drink hint take over the shared prompt label
    /// without fighting PlayerInteractor's own Update() — running in LateUpdate
    /// guarantees PlayerInteractor has already written this frame's value first.</summary>
    void ApplyMilkPromptOverride()
    {
        if (promptText == null)
        {
            return;
        }

        bool showDrinkHint = milkItem != null && milkItem.IsHeld && (interactor == null || !interactor.HasTarget);
        if (showDrinkHint)
        {
            promptText.text = "Click E to consume";
            promptText.color = consumeHintColor;
        }
        else
        {
            promptText.color = promptTextColor;
        }
    }

    void UpdateBar(Image fill, TextMeshProUGUI label, float normalized, float value, Color barColor, string name)
    {
        normalized = Mathf.Clamp01(normalized);
        int percent = Mathf.Clamp(Mathf.CeilToInt(value), 0, 100);
        float steppedNormalized = percent / 100f;

        if (fill != null)
        {
            fill.fillAmount = steppedNormalized;
            fill.color = barColor;

            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(steppedNormalized, 1f);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
        }

        if (label != null)
        {
            label.text = $"{name}: {percent}%";
        }
    }

    Color GetPhoneColor(float normalized)
    {
        return Color.Lerp(lowColor, phoneColor, Mathf.Clamp01(normalized));
    }

    Color GetWaterColor(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);
        if (normalized <= lowThreshold)
        {
            return lowColor;
        }

        float blueAmount = Mathf.InverseLerp(lowThreshold, 1f, normalized);
        return Color.Lerp(waterMidColor, waterColor, blueAmount);
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
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform root = canvas.transform;
        if (timerText == null)
        {
            timerBackground = CreatePanel(root, "Stopwatch_Background", new Vector2(170f, 56f), Vector2.zero, new Color(0.03f, 0.02f, 0.02f, 0.78f));
            RectTransform timerBgRect = timerBackground.rectTransform;
            timerBgRect.anchorMin = new Vector2(0.5f, 1f);
            timerBgRect.anchorMax = new Vector2(0.5f, 1f);
            timerBgRect.pivot = new Vector2(0.5f, 1f);
            timerBgRect.anchoredPosition = new Vector2(0f, -16f);

            Outline timerOutline = timerBackground.gameObject.AddComponent<Outline>();
            timerOutline.effectColor = promptAccentColor;
            timerOutline.effectDistance = new Vector2(2f, -2f);

            timerText = CreateText(timerBackground.transform, "Stopwatch_Text", "0:00", 32, TextAlignmentOptions.Center);
            Stretch(timerText.rectTransform, Vector2.zero);
            timerText.fontStyle = FontStyles.Bold;
            StylePromptText(timerText);
        }

        if (waterFill == null)
        {
            waterFill = CreateTopBar(root, "Water_Bar", new Vector2(-250f, -84f), waterColor, out waterText);
        }

        if (phoneFill == null)
        {
            phoneFill = CreateTopBar(root, "Phone_Bar", new Vector2(250f, -84f), phoneColor, out phoneText);
        }

        if (noisePanel == null)
        {
            BuildNoisePanel(root);
        }

        if (phoneSlotBackground == null)
        {
            phoneSlotBackground = CreatePanel(root, "Phone_Slot", new Vector2(44f, 44f), Vector2.zero, slotStoredColor);
            RectTransform slotRect = phoneSlotBackground.rectTransform;
            slotRect.anchorMin = new Vector2(0.5f, 0f);
            slotRect.anchorMax = new Vector2(0.5f, 0f);
            slotRect.pivot = new Vector2(0.5f, 0f);
            slotRect.anchoredPosition = new Vector2(0f, 22f);

            phoneSlotOutline = phoneSlotBackground.gameObject.AddComponent<Outline>();
            phoneSlotOutline.effectColor = slotOutlineColor;
            phoneSlotOutline.effectDistance = new Vector2(2f, -2f);
            phoneSlotOutline.enabled = false;

            phoneSlotText = CreateText(phoneSlotBackground.transform, "Phone_Slot_Number", "1", 12, TextAlignmentOptions.TopLeft);
            RectTransform numberRect = phoneSlotText.rectTransform;
            numberRect.anchorMin = new Vector2(0f, 1f);
            numberRect.anchorMax = new Vector2(0f, 1f);
            numberRect.pivot = new Vector2(0f, 1f);
            numberRect.anchoredPosition = new Vector2(4f, -2f);
            numberRect.sizeDelta = new Vector2(16f, 16f);

            phoneIconBody = CreatePanel(phoneSlotBackground.transform, "Phone_Icon_Body", new Vector2(17f, 27f), Vector2.zero, new Color(0.08f, 0.08f, 0.09f, 0.95f));
            RectTransform bodyRect = phoneIconBody.rectTransform;
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0f, -1f);

            phoneIconScreen = CreatePanel(phoneIconBody.transform, "Phone_Icon_Screen", new Vector2(11f, 17f), new Vector2(0f, 1f), new Color(0.12f, 0.35f, 0.42f, 0.95f));
            RectTransform screenRect = phoneIconScreen.rectTransform;
            screenRect.anchorMin = new Vector2(0.5f, 0.5f);
            screenRect.anchorMax = new Vector2(0.5f, 0.5f);
            screenRect.pivot = new Vector2(0.5f, 0.5f);
        }

        if (milkSlotBackgrounds == null || milkSlotBackgrounds.Length == 0)
        {
            milkSlotBackgrounds = new Image[maxMilkSlots];
            milkSlotOutlines = new Outline[maxMilkSlots];
            milkSlotTexts = new TextMeshProUGUI[maxMilkSlots];

            for (int i = 0; i < maxMilkSlots; i++)
            {
                float xOffset = 54f * (i + 1);

                Image background = CreatePanel(root, "Milk_Slot_" + i, new Vector2(44f, 44f), Vector2.zero, slotStoredColor);
                RectTransform slotRect = background.rectTransform;
                slotRect.anchorMin = new Vector2(0.5f, 0f);
                slotRect.anchorMax = new Vector2(0.5f, 0f);
                slotRect.pivot = new Vector2(0.5f, 0f);
                slotRect.anchoredPosition = new Vector2(xOffset, 22f);

                Outline outline = background.gameObject.AddComponent<Outline>();
                outline.effectColor = slotOutlineColor;
                outline.effectDistance = new Vector2(2f, -2f);
                outline.enabled = false;

                TextMeshProUGUI numberText = CreateText(background.transform, "Milk_Slot_Number_" + i, (2 + i).ToString(), 12, TextAlignmentOptions.TopLeft);
                RectTransform numberRect = numberText.rectTransform;
                numberRect.anchorMin = new Vector2(0f, 1f);
                numberRect.anchorMax = new Vector2(0f, 1f);
                numberRect.pivot = new Vector2(0f, 1f);
                numberRect.anchoredPosition = new Vector2(4f, -2f);
                numberRect.sizeDelta = new Vector2(20f, 16f);

                Image iconBody = CreatePanel(background.transform, "Milk_Icon_Body_" + i, new Vector2(16f, 22f), Vector2.zero, new Color(0.92f, 0.92f, 0.88f, 0.95f));
                RectTransform bodyRect = iconBody.rectTransform;
                bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
                bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
                bodyRect.pivot = new Vector2(0.5f, 0.5f);
                bodyRect.anchoredPosition = new Vector2(0f, -2f);

                Image iconCap = CreatePanel(iconBody.transform, "Milk_Icon_Cap_" + i, new Vector2(10f, 7f), new Vector2(0f, 1f), new Color(0.25f, 0.55f, 0.85f, 0.95f));
                RectTransform capRect = iconCap.rectTransform;
                capRect.anchorMin = new Vector2(0.5f, 1f);
                capRect.anchorMax = new Vector2(0.5f, 1f);
                capRect.pivot = new Vector2(0.5f, 1f);
                capRect.anchoredPosition = new Vector2(0f, 7f);

                background.gameObject.SetActive(false);

                milkSlotBackgrounds[i] = background;
                milkSlotOutlines[i] = outline;
                milkSlotTexts[i] = numberText;
            }
        }

        if (promptBackground == null)
        {
            promptBackground = CreatePanel(root, "Interact_Prompt_Background", new Vector2(540f, 58f), Vector2.zero, promptBackgroundColor);
            RectTransform bgRect = promptBackground.rectTransform;
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(0f, -122f);
            promptBackground.raycastTarget = false;

            Outline bgOutline = promptBackground.gameObject.AddComponent<Outline>();
            bgOutline.effectColor = promptAccentColor;
            bgOutline.effectDistance = new Vector2(2f, -2f);
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

        if (centerDot == null)
        {
            centerDot = CreatePanel(root, "Center_Interact_Dot", new Vector2(centerDotSize, centerDotSize), Vector2.zero, centerDotColor);
            RectTransform dotRect = centerDot.rectTransform;
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            centerDot.raycastTarget = false;
        }
    }

    Image CreateTopBar(Transform parent, string name, Vector2 anchoredPosition, Color fillColor, out TextMeshProUGUI label)
    {
        Image background = CreatePanel(parent, name, new Vector2(440f, 44f), anchoredPosition, new Color(0.04f, 0.04f, 0.04f, 0.6f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 1f);

        GameObject fillObject = new GameObject(name + "_Fill");
        fillObject.transform.SetParent(background.transform, false);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Simple;
        Stretch(fill.rectTransform, new Vector2(4f, 4f));

        label = CreateText(background.transform, name + "_Label", "", 24, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero);
        return fill;
    }

    void BuildNoisePanel(Transform root)
    {
        noisePanel = CreatePanel(root, "Noise_Levels_Panel", new Vector2(300f, 92f), Vector2.zero, new Color(0.025f, 0.022f, 0.018f, 0.85f));
        RectTransform panelRect = noisePanel.rectTransform;
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-30f, 32f);
        noisePanel.raycastTarget = false;

        Outline outline = noisePanel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.58f, 0.06f, 0.04f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Title across the top.
        noiseTitleText = CreateText(noisePanel.transform, "Noise_Title", "NOISE LEVEL", 18, TextAlignmentOptions.Left);
        RectTransform titleRect = noiseTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(14f, -30f);
        titleRect.offsetMax = new Vector2(-14f, -8f);

        // Label (left) and value (right) sit in their own row ABOVE the bar,
        // so neither can ever overlap the bar itself.
        noiseLabelText = CreateText(noisePanel.transform, "Noise_Label", "SOUND", 14, TextAlignmentOptions.Left);
        RectTransform labelRect = noiseLabelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0.6f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.offsetMin = new Vector2(14f, -52f);
        labelRect.offsetMax = new Vector2(0f, -32f);

        noiseValueText = CreateText(noisePanel.transform, "Noise_Value", "0 / 100", 14, TextAlignmentOptions.Right);
        RectTransform valueRect = noiseValueText.rectTransform;
        valueRect.anchorMin = new Vector2(0.4f, 1f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(0f, -52f);
        valueRect.offsetMax = new Vector2(-14f, -32f);

        // Bar spans the full width near the bottom, clear of every label.
        Image barBack = CreatePanel(noisePanel.transform, "Noise_Bar_Background", Vector2.zero, Vector2.zero, new Color(0.08f, 0.075f, 0.065f, 0.95f));
        RectTransform barRect = barBack.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.offsetMin = new Vector2(14f, 12f);
        barRect.offsetMax = new Vector2(-14f, 30f);

        GameObject fillObject = new GameObject("Noise_Bar_Fill");
        fillObject.transform.SetParent(barBack.transform, false);
        noiseFill = fillObject.AddComponent<Image>();
        noiseFill.color = new Color(0.72f, 0.08f, 0.045f, 0.92f);
        Stretch(noiseFill.rectTransform, new Vector2(2f, 2f));

        StylePromptText(noiseTitleText);
        StylePromptText(noiseLabelText);
        StylePromptText(noiseValueText);
        SetNoiseHudVisible(false);
    }

    void UpdateNoiseHud()
    {
        if (noisePanel == null)
        {
            return;
        }

        bool visible = noiseMeter != null && noiseMeter.IsRevealed;
        SetNoiseHudVisible(visible);
        if (!visible)
        {
            noiseHudWasVisible = false;
            return;
        }

        float actualNoise = noiseMeter != null ? noiseMeter.CurrentNoise : 0f;
        if (!noiseHudWasVisible)
        {
            // Snap on first reveal so it doesn't visibly ramp up from 0.
            displayedNoise = actualNoise;
            noiseHudWasVisible = true;
        }

        // Smooth the DISPLAYED bar only — gameplay (boss hearing range) always
        // reads the real, unsmoothed value so it can never lag behind reality.
        displayedNoise = Mathf.Lerp(displayedNoise, actualNoise, Time.deltaTime / Mathf.Max(noiseDisplaySmoothTime, 0.01f));

        float maxNoise = noiseMeter != null ? noiseMeter.maxNoise : 100f;
        float normalized = maxNoise > 0f ? Mathf.Clamp01(displayedNoise / maxNoise) : 0f;
        int percent = Mathf.RoundToInt(normalized * 100f);

        if (noiseFill != null)
        {
            noiseFill.color = Color.Lerp(new Color(0.28f, 0.35f, 0.24f, 0.9f), new Color(0.85f, 0.05f, 0.035f, 0.95f), normalized);
            RectTransform fillRect = noiseFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalized, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
        }

        if (noiseValueText != null)
        {
            noiseValueText.text = $"{percent} / 100";
        }
    }

    void SetNoiseHudVisible(bool visible)
    {
        SetGraphicVisible(noisePanel, visible);
        SetGraphicVisible(noiseFill, visible);
        SetTextVisible(noiseTitleText, visible);
        SetTextVisible(noiseLabelText, visible);
        SetTextVisible(noiseValueText, visible);

        if (noisePanel != null)
        {
            foreach (Image childImage in noisePanel.GetComponentsInChildren<Image>(true))
            {
                childImage.enabled = visible;
            }
        }
    }

    void SetGraphicVisible(Graphic graphic, bool visible)
    {
        if (graphic != null)
        {
            graphic.enabled = visible;
        }
    }

    void SetTextVisible(TextMeshProUGUI text, bool visible)
    {
        if (text != null)
        {
            text.enabled = visible;
        }
    }

    void StylePromptText(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        if (horrorPromptFont == null)
        {
            horrorPromptFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");
        }

        if (horrorPromptFont != null)
        {
            text.font = horrorPromptFont;
        }

        // Keep whatever fontSize the caller set via CreateText — don't force
        // one size onto every label, or small boxes (like the noise panel)
        // overflow and become unreadable.
        text.color = promptTextColor;
        text.characterSpacing = 0.5f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = false;

        Shadow shadow = text.gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    void UpdatePromptVisibility()
    {
        if (promptText == null)
        {
            return;
        }

        bool show = !string.IsNullOrWhiteSpace(promptText.text);
        promptText.enabled = show;

        if (promptBackground != null)
        {
            promptBackground.enabled = show;
        }
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
