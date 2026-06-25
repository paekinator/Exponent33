using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(900)]
public class GamePauseMenu : MonoBehaviour
{
    const string BackroomsSceneName = "BackroomsLevel";
    const string MainMenuSceneName = "MainMenu";
    GameObject root;
    GameObject mainPanel;
    GameObject optionsPanel;
    TMP_FontAsset menuFont;
    readonly List<Behaviour> disabledWhilePaused = new List<Behaviour>();
    bool paused;
    CursorLockMode previousCursorLock;
    bool previousCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureForScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    static void EnsureForScene(Scene scene)
    {
        if (scene.name != BackroomsSceneName) return;
        if (Object.FindAnyObjectByType<GamePauseMenu>() != null) return;

        GameObject pauseObject = new GameObject("Game_Pause_Menu");
        pauseObject.AddComponent<GamePauseMenu>();
    }

    void Awake()
    {
        menuFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/VCR_OSD_MONO_1");
        BuildUi();
        SetVisible(false);
        ApplySavedVolumes();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (paused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (paused) return;
        paused = true;

        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisableGameplayScripts();
        Time.timeScale = 0f;
        ShowMainPanel();
        SetVisible(true);
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;

        Time.timeScale = 1f;
        RestoreGameplayScripts();
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        SetVisible(false);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        RestoreGameplayScripts();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("MainMenu", 0f);
        }

        SceneManager.LoadScene(MainMenuSceneName);
    }

    void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    void ShowOptionsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }

    void DisableGameplayScripts()
    {
        disabledWhilePaused.Clear();
        DisableIfEnabled(Object.FindAnyObjectByType<FirstPersonController>());
        DisableIfEnabled(Object.FindAnyObjectByType<PlayerInteractor>());
        DisableIfEnabled(Object.FindAnyObjectByType<PhoneViewmodel>());
        DisableIfEnabled(Object.FindAnyObjectByType<MilkItem>());
        DisableIfEnabled(Object.FindAnyObjectByType<PlayerWalkAudio>());
        DisableIfEnabled(Object.FindAnyObjectByType<PlayerDrinkAudio>());
        DisableIfEnabled(Object.FindAnyObjectByType<PlayerChargeAudio>());
        DisableIfEnabled(Object.FindAnyObjectByType<PlayerLowWaterPanting>());
    }

    void DisableIfEnabled(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled || behaviour == this) return;
        disabledWhilePaused.Add(behaviour);
        behaviour.enabled = false;
    }

    void RestoreGameplayScripts()
    {
        foreach (Behaviour behaviour in disabledWhilePaused)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        disabledWhilePaused.Clear();
    }

    void ApplySavedVolumes()
    {
        SetMusicVolume(GameAudioSettings.MusicSlider);
        SetSfxVolume(GameAudioSettings.SfxSlider);
    }

    void SetMusicVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        if (MusicManager.Instance != null) MusicManager.Instance.SetVolume(volume);
    }

    void SetSfxVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        if (SoundManager.Instance != null) SoundManager.Instance.SetVolume(volume);
    }

    void BuildUi()
    {
        EnsureEventSystem();

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

        root = new GameObject("Pause_Menu_Root");
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = false;
        Stretch(dim.rectTransform);

        mainPanel = CreatePanel("Pause_Main_Panel");
        AddText(mainPanel.transform, "PAUSED", new Vector2(0f, 190f), new Vector2(900f, 110f), 92f);
        AddButton(mainPanel.transform, "Resume", new Vector2(0f, 50f), Resume);
        AddButton(mainPanel.transform, "Options", new Vector2(0f, -50f), ShowOptionsPanel);
        AddButton(mainPanel.transform, "Back To Main Menu", new Vector2(0f, -150f), BackToMainMenu, new Vector2(480f, 72f), 38f);

        optionsPanel = CreatePanel("Pause_Options_Panel");
        AddText(optionsPanel.transform, "OPTIONS", new Vector2(0f, 190f), new Vector2(900f, 110f), 92f);
        AddSlider(optionsPanel.transform, "Sound Effects", new Vector2(0f, 35f), GameAudioSettings.SfxSlider, SetSfxVolume);
        AddSlider(optionsPanel.transform, "Music", new Vector2(0f, -98f), GameAudioSettings.MusicSlider, SetMusicVolume);
        AddButton(optionsPanel.transform, "Back", new Vector2(0f, -175f), ShowMainPanel, new Vector2(260f, 62f), 38f);
    }

    void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    GameObject CreatePanel(string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(root.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(900f, 560f);

        return panel;
    }

    TextMeshProUGUI AddText(Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(text, typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = Color.white;
        if (menuFont != null) label.font = menuFont;

        return label;
    }

    Button AddButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        return AddButton(parent, text, position, action, new Vector2(360f, 72f), 44f);
    }

    Button AddButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action, Vector2 size, float fontSize)
    {
        GameObject buttonObject = new GameObject(text + "Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.03f, 0.03f, 0.03f, 0.72f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.5764706f, 0.3882353f, 0.3882353f, 1f);
        colors.pressedColor = new Color(0.78f, 0.18f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        PauseMenuButtonHover hover = buttonObject.AddComponent<PauseMenuButtonHover>();
        hover.Configure(image, rect);
        AddMenuSounds(buttonObject);
        AddText(buttonObject.transform, text, Vector2.zero, size, fontSize);

        return button;
    }

    void AddSlider(Transform parent, string label, Vector2 position, float value, UnityEngine.Events.UnityAction<float> action)
    {
        GameObject group = new GameObject(label + "_Vol");
        group.transform.SetParent(parent, false);
        RectTransform groupRect = group.AddComponent<RectTransform>();
        groupRect.anchorMin = groupRect.anchorMax = new Vector2(0.5f, 0.5f);
        groupRect.anchoredPosition = position;
        groupRect.sizeDelta = new Vector2(480f, 60f);

        TextMeshProUGUI title = AddText(group.transform, label, new Vector2(0f, 28f), new Vector2(480f, 34f), 30f);
        title.color = new Color(1f, 1f, 1f, 0.92f);

        GameObject sliderObject = new GameObject(label + "_Slider", typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(group.transform, false);
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(480f, 60f);
        Image sliderHitbox = sliderObject.GetComponent<Image>();
        sliderHitbox.color = new Color(1f, 1f, 1f, 0f);
        sliderHitbox.raycastTarget = true;

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.03f, 0.03f, 0.03f, 0.72f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(480f, 22f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillAreaRect.anchoredPosition = Vector2.zero;
        fillAreaRect.sizeDelta = new Vector2(456f, 18f);

        Image fill = CreateImage(fillArea.transform, "Fill", new Color(0.42f, 0.42f, 0.42f, 0.95f));
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleAreaRect.anchoredPosition = Vector2.zero;
        handleAreaRect.sizeDelta = new Vector2(456f, 42f);

        Image handle = CreateImage(handleArea.transform, "Handle", new Color(0.9f, 0.9f, 0.9f, 1f));
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(24f, 42f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
        slider.targetGraphic = handle;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        ColorBlock colors = slider.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
        colors.selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        colors.fadeDuration = 0.1f;
        slider.colors = colors;
        slider.onValueChanged.AddListener(action);
        SliderDragInput.Attach(slider);
    }

    Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    void AddMenuSounds(GameObject buttonObject)
    {
        EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();
        EventTrigger.Entry hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hover.callback.AddListener(_ =>
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound2D("Hover");
            }
        });
        trigger.triggers.Add(hover);

        EventTrigger.Entry click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(_ =>
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound2D("Click");
            }
        });
        trigger.triggers.Add(click);
    }

    static void Stretch(RectTransform rect)
    {
        Stretch(rect, 0f, 0f, 0f, 0f);
    }

    static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}

class PauseMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    static readonly Color NormalColor = new Color(0.03f, 0.03f, 0.03f, 0.72f);
    static readonly Color HoverColor = new Color(0.5764706f, 0.3882353f, 0.3882353f, 1f);
    static readonly Color PressedColor = new Color(0.78f, 0.18f, 0.18f, 1f);

    Image targetImage;
    RectTransform targetRect;
    Vector3 normalScale = Vector3.one;
    Color targetColor = NormalColor;
    Vector3 targetScale = Vector3.one;
    bool hovered;

    public void Configure(Image image, RectTransform rect)
    {
        targetImage = image;
        targetRect = rect;
        if (targetRect != null) normalScale = targetRect.localScale;
        targetColor = NormalColor;
        targetScale = normalScale;
    }

    void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetRect == null) targetRect = GetComponent<RectTransform>();
        if (targetRect != null) normalScale = targetRect.localScale;
    }

    void Update()
    {
        float blend = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
        if (targetImage != null)
        {
            targetImage.color = Color.Lerp(targetImage.color, targetColor, blend);
        }

        if (targetRect != null)
        {
            targetRect.localScale = Vector3.Lerp(targetRect.localScale, targetScale, blend);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        targetColor = HoverColor;
        targetScale = normalScale * 1.035f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        targetColor = NormalColor;
        targetScale = normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetColor = PressedColor;
        targetScale = normalScale * 0.985f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetColor = hovered ? HoverColor : NormalColor;
        targetScale = hovered ? normalScale * 1.035f : normalScale;
    }
}
