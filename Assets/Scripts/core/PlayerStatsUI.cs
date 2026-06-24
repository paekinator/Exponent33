using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;
    public Image waterFillImage;
    public Image batteryFillImage;
    public Image conditionFillImage;
    public Image coverageFillImage;
    public Slider waterSlider;
    public Text stopwatchText;
    public Text characterInfoText;

    [Header("Animation")]
    public bool animateChanges = true;
    public float animationSpeed = 8f;

    private float displayedWater;
    private float displayedBattery;
    private float displayedCondition;
    private float displayedCoverage;
    private float elapsedSeconds;

    public static PlayerStatsUI CreateDefaultFor(PlayerStats stats)
    {
        GameObject canvasObject = new GameObject("Player Stats Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Character Info Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.015f, 0.018f, 0.02f, 0.82f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(24f, 24f);
        panelRect.sizeDelta = new Vector2(285f, 126f);

        GameObject characterTextObject = new GameObject("Character Info Text");
        characterTextObject.transform.SetParent(panelObject.transform, false);
        Text characterText = characterTextObject.AddComponent<Text>();
        characterText.font = GetDefaultFont();
        characterText.fontSize = 14;
        characterText.alignment = TextAnchor.UpperLeft;
        characterText.color = new Color(0.88f, 0.92f, 0.9f, 1f);
        characterText.text = "Character";

        RectTransform characterTextRect = characterTextObject.GetComponent<RectTransform>();
        characterTextRect.anchorMin = new Vector2(0f, 1f);
        characterTextRect.anchorMax = new Vector2(1f, 1f);
        characterTextRect.pivot = new Vector2(0f, 1f);
        characterTextRect.anchoredPosition = new Vector2(12f, -10f);
        characterTextRect.sizeDelta = new Vector2(-24f, 22f);

        GameObject stopwatchObject = new GameObject("Stopwatch Text");
        stopwatchObject.transform.SetParent(canvasObject.transform, false);
        Text stopwatchText = stopwatchObject.AddComponent<Text>();
        stopwatchText.font = GetDefaultFont();
        stopwatchText.fontSize = 24;
        stopwatchText.alignment = TextAnchor.UpperCenter;
        stopwatchText.color = Color.white;
        stopwatchText.text = "00:00";

        RectTransform stopwatchRect = stopwatchObject.GetComponent<RectTransform>();
        stopwatchRect.anchorMin = new Vector2(0.5f, 1f);
        stopwatchRect.anchorMax = new Vector2(0.5f, 1f);
        stopwatchRect.pivot = new Vector2(0.5f, 1f);
        stopwatchRect.anchoredPosition = new Vector2(0f, -18f);
        stopwatchRect.sizeDelta = new Vector2(180f, 40f);

        Image waterFill = CreateHudBar(panelObject.transform, "Water", "H2O", new Vector2(12f, -38f), new Color(0.15f, 0.65f, 1f, 1f));
        Image batteryFill = CreateHudBar(panelObject.transform, "Battery", "BAT", new Vector2(12f, -60f), new Color(0.4f, 1f, 0.4f, 1f));
        Image conditionFill = CreateHudBar(panelObject.transform, "Condition", "HP", new Vector2(12f, -82f), new Color(1f, 0.28f, 0.22f, 1f));
        Image coverageFill = CreateHudBar(panelObject.transform, "Coverage", "SIG", new Vector2(12f, -104f), new Color(1f, 0.82f, 0.22f, 1f));

        PlayerStatsUI ui = canvasObject.AddComponent<PlayerStatsUI>();
        ui.playerStats = stats;
        ui.waterFillImage = waterFill;
        ui.batteryFillImage = batteryFill;
        ui.conditionFillImage = conditionFill;
        ui.coverageFillImage = coverageFill;
        ui.stopwatchText = stopwatchText;
        ui.characterInfoText = characterText;
        ui.displayedWater = stats != null ? stats.water : 0f;
        ui.displayedBattery = stats != null ? stats.battery : 0f;
        ui.displayedCondition = stats != null ? stats.condition : 0f;
        ui.displayedCoverage = stats != null ? stats.coverage : 0f;
        ui.RefreshImmediate();

        return ui;
    }

    private static Image CreateHudBar(Transform parent, string objectName, string label, Vector2 anchoredPosition, Color fillColor)
    {
        GameObject rowObject = new GameObject(objectName + " Row");
        rowObject.transform.SetParent(parent, false);
        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(260f, 16f);

        GameObject labelObject = new GameObject(objectName + " Label");
        labelObject.transform.SetParent(rowObject.transform, false);
        Text labelText = labelObject.AddComponent<Text>();
        labelText.font = GetDefaultFont();
        labelText.fontSize = 11;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = new Color(0.82f, 0.86f, 0.84f, 1f);
        labelText.text = label;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(36f, 16f);

        GameObject backgroundObject = new GameObject(objectName + " Bar Background");
        backgroundObject.transform.SetParent(rowObject.transform, false);
        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.04f, 0.045f, 0.05f, 0.95f);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 1f);
        backgroundRect.offsetMin = new Vector2(42f, 2f);
        backgroundRect.offsetMax = new Vector2(0f, -2f);

        GameObject fillObject = new GameObject(objectName + " Bar Fill");
        fillObject.transform.SetParent(backgroundObject.transform, false);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        return fillImage;
    }

    private static Sprite CreateWaterDropSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;

                bool lowerDrop = Mathf.Pow(nx / 0.62f, 2f) + Mathf.Pow((ny + 0.28f) / 0.72f, 2f) <= 1f;
                bool upperDrop = Mathf.Abs(nx) <= (0.36f * (ny + 1f)) && ny > -0.12f && ny < 0.92f;
                bool highlight = Mathf.Pow((nx + 0.22f) / 0.12f, 2f) + Mathf.Pow((ny + 0.1f) / 0.28f, 2f) <= 1f;

                texture.SetPixel(x, y, lowerDrop || upperDrop ? (highlight ? new Color(1f, 1f, 1f, 0.75f) : white) : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Font GetDefaultFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Awake()
    {
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }

        displayedWater = playerStats != null ? playerStats.water : 0f;
        displayedBattery = playerStats != null ? playerStats.battery : 0f;
        displayedCondition = playerStats != null ? playerStats.condition : 0f;
        displayedCoverage = playerStats != null ? playerStats.coverage : 0f;
        RefreshImmediate();
    }

    void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged += HandleStatsChanged;
        }
    }

    void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= HandleStatsChanged;
        }
    }

    void Update()
    {
        if (playerStats == null)
        {
            return;
        }

        float targetWater = playerStats.water;
        displayedWater = animateChanges
            ? Mathf.Lerp(displayedWater, targetWater, animationSpeed * Time.deltaTime)
            : targetWater;
        displayedBattery = animateChanges ? Mathf.Lerp(displayedBattery, playerStats.battery, animationSpeed * Time.deltaTime) : playerStats.battery;
        displayedCondition = animateChanges ? Mathf.Lerp(displayedCondition, playerStats.condition, animationSpeed * Time.deltaTime) : playerStats.condition;
        displayedCoverage = animateChanges ? Mathf.Lerp(displayedCoverage, playerStats.coverage, animationSpeed * Time.deltaTime) : playerStats.coverage;

        ApplyValues();
        UpdateStopwatch();
    }

    private void HandleStatsChanged(PlayerStats stats)
    {
        if (!animateChanges)
        {
            displayedWater = stats.water;
            displayedBattery = stats.battery;
            displayedCondition = stats.condition;
            displayedCoverage = stats.coverage;
            ApplyValues();
        }
    }

    private void RefreshImmediate()
    {
        ApplyValues();
    }

    private void ApplyValues()
    {
        ApplyFill(waterFillImage, displayedWater);
        ApplyFill(batteryFillImage, displayedBattery);
        ApplyFill(conditionFillImage, displayedCondition);
        ApplyFill(coverageFillImage, displayedCoverage);

        if (characterInfoText != null)
        {
            characterInfoText.text = "Character";
        }
    }

    private void ApplyFill(Image image, float value)
    {
        if (image != null)
        {
            image.fillAmount = Mathf.Clamp01(value / 100f);
        }
    }

    private void UpdateStopwatch()
    {
        if (stopwatchText == null)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;
        int totalSeconds = Mathf.FloorToInt(elapsedSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        stopwatchText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}
