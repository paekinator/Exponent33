using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    const string MainMenuScene = "MainMenu";
    const string GameScene = "BackroomsLevel";

    public AudioMixer audioMixer;

    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_InputField playerNameInput;

    Canvas menuCanvas;
    GraphicRaycaster menuRaycaster;
    GameObject nameDifficultyPanel;
    GameObject storyPanel;
    TextMeshProUGUI storyText;
    TextMeshProUGUI storyHint;
    TextMeshProUGUI loadingSideText;
    TMP_FontAsset menuFont;
    bool transitionRunning;

    readonly string[] storyLines =
    {
        "You're in an office working a 9-5 but its more like an 8-9. Its almost clock out time today, but things are busy, and you know that if your manager sees you, he'll ask you to work overtime. You just had a daughter two weeks ago, but your manager and boss don't care.",
        "All you need to do, is hide until 9 o'clock, and sneak out without making eye contact with your manager. You haven't seen your boss all day, and you're bored, so you open his door, and its empty. There is another door connected to his room. Curiosity takes the better of you, so you look inside, and open the door that your boss has never let anyone else open..."
    };

    public void Start()
    {
        menuCanvas = GetComponent<Canvas>();
        menuRaycaster = GetComponent<GraphicRaycaster>();
        menuFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/VCR_OSD_MONO_1");

        GameSessionSettings.Load();
        if (playerNameInput == null) playerNameInput = FindAnyObjectByType<TMP_InputField>();
        if (playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            playerNameInput.text = GameSessionSettings.PlayerName;
        }

        BindVolumeSliders();
        LoadVolume();
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("MainMenu", 0f);
        }
    }

    public void Play()
    {
        ShowNameDifficultyPanel();
    }

    public void Play(int difficulty)
    {
        if (transitionRunning) return;

        string playerName = playerNameInput != null ? playerNameInput.text : "";
        GameSessionSettings.Save(playerName, difficulty);

        StartCoroutine(StoryThenLoadGame());
    }

    public void ReturnToMenu()
    {
        if (SceneManager.GetActiveScene().name == MainMenuScene)
        {
            ShowMainMenuPanel();
            return;
        }

        SceneManager.LoadScene(MainMenuScene);
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("MainMenu", 0.5f);
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void UpdateMusicVolume(float volume)
    {
        float outputVolume = GameAudioSettings.MusicSliderToOutput(volume);
        GameAudioSettings.MusicSlider = volume;
        if (MusicManager.Instance != null) MusicManager.Instance.SetVolume(volume);
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(outputVolume, 0.0001f)) * 20);
        }
    }

    public void UpdateSoundVolume(float volume)
    {
        float outputVolume = GameAudioSettings.SfxSliderToOutput(volume);
        GameAudioSettings.SfxSlider = volume;
        if (SoundManager.Instance != null) SoundManager.Instance.SetVolume(volume);
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(outputVolume, 0.0001f)) * 20);
        }
    }

    public void SaveVolume()
    {
        if (musicSlider != null) GameAudioSettings.MusicSlider = musicSlider.value;
        if (sfxSlider != null) GameAudioSettings.SfxSlider = sfxSlider.value;
        PlayerPrefs.Save();
    }

    public void LoadVolume()
    {
        float musicVolume = GameAudioSettings.MusicSlider;
        float sfxVolume = GameAudioSettings.SfxSlider;

        if (musicSlider != null) musicSlider.value = musicVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
        UpdateMusicVolume(musicVolume);
        UpdateSoundVolume(sfxVolume);
    }

    void BindVolumeSliders()
    {
        Slider[] sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include);
        foreach (Slider slider in sliders)
        {
            if (slider == null) continue;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            SliderDragInput.Attach(slider);

            string parentName = slider.transform.parent != null ? slider.transform.parent.name : "";
            if (musicSlider == null && parentName.Contains("Music"))
            {
                musicSlider = slider;
            }
            else if (sfxSlider == null && (parentName.Contains("SoundEffects") || parentName.Contains("SFX")))
            {
                sfxSlider = slider;
            }
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(UpdateMusicVolume);
            musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(UpdateSoundVolume);
            sfxSlider.onValueChanged.AddListener(UpdateSoundVolume);
        }
    }

    void ShowNameDifficultyPanel()
    {
        SetMainMenuVisible(false);

        if (nameDifficultyPanel == null)
        {
            nameDifficultyPanel = BuildNameDifficultyPanel();
        }

        nameDifficultyPanel.SetActive(true);
        if (playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            playerNameInput.text = GameSessionSettings.PlayerName;
        }
    }

    void ShowMainMenuPanel()
    {
        if (nameDifficultyPanel != null)
        {
            nameDifficultyPanel.SetActive(false);
        }

        SetMainMenuVisible(true);
    }

    void SetMainMenuVisible(bool isVisible)
    {
        if (menuCanvas != null) menuCanvas.enabled = isVisible;
        if (menuRaycaster != null) menuRaycaster.enabled = isVisible;
    }

    GameObject BuildNameDifficultyPanel()
    {
        GameObject panel = new GameObject("NameDifficultyPanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform root = panel.GetComponent<RectTransform>();
        Stretch(root);

        AddText(root, "The BackShift", new Vector2(0f, 258f), new Vector2(1600f, 120f), 150f);
        AddText(root, "Enter Your Name", new Vector2(0f, 82f), new Vector2(900f, 70f), 54f);

        playerNameInput = AddInputField(root, new Vector2(0f, 8f), new Vector2(620f, 72f));
        playerNameInput.text = GameSessionSettings.PlayerName;

        AddText(root, "Select a Difficulty Level", new Vector2(0f, -100f), new Vector2(1100f, 70f), 54f);

        AddButton(root, "Easy", new Vector2(-360f, -195f), () => Play((int)GameDifficulty.Easy));
        AddButton(root, "Medium", new Vector2(0f, -195f), () => Play((int)GameDifficulty.Medium));
        AddButton(root, "Hard", new Vector2(360f, -195f), () => Play((int)GameDifficulty.Hard));
        AddButton(root, "Back", new Vector2(0f, -315f), ReturnToMenu, new Vector2(260f, 60f), 38f);

        return panel;
    }

    IEnumerator StoryThenLoadGame()
    {
        transitionRunning = true;

        if (nameDifficultyPanel != null) nameDifficultyPanel.SetActive(false);
        SetMainMenuVisible(false);

        EnsureStoryPanel();
        storyPanel.SetActive(true);

        for (int i = 0; i < storyLines.Length; i++)
        {
            yield return TypeLine(storyLines[i], 4f, i == storyLines.Length - 1 ? "[ E ]  Press E to continue" : "[ E ]  Press E to continue");
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic();
        }

        storyText.text = "... loading";
        storyText.alignment = TextAlignmentOptions.Center;
        storyText.fontSize = 58f;
        storyHint.text = "";
        if (loadingSideText != null) loadingSideText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(4f);
        if (loadingSideText != null) loadingSideText.gameObject.SetActive(false);

        SceneManager.LoadScene(GameScene);
    }

    IEnumerator TypeLine(string line, float seconds, string hint)
    {
        storyText.text = "";
        storyText.alignment = TextAlignmentOptions.MidlineLeft;
        storyText.fontSize = 36f;
        storyHint.text = "";

        float elapsed = 0f;
        int lastCount = -1;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt((elapsed / seconds) * line.Length), 0, line.Length);
            if (count != lastCount)
            {
                storyText.text = line.Substring(0, count);
                lastCount = count;
            }

            yield return null;
        }

        storyText.text = line;
        storyHint.text = hint;

        while (!Input.GetKeyDown(KeyCode.E) && !Input.GetMouseButtonDown(0))
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            Color c = Color.white;
            c.a = 0.45f + 0.45f * pulse;
            storyHint.color = c;
            yield return null;
        }
    }

    void EnsureStoryPanel()
    {
        if (storyPanel != null)
        {
            return;
        }

        GameObject panel = new GameObject("StoryTransitionPanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.94f);
        Stretch(background.rectTransform);

        storyText = AddText(panel.transform, "", new Vector2(0f, 30f), new Vector2(1220f, 420f), 36f);
        storyText.alignment = TextAlignmentOptions.MidlineLeft;
        storyText.textWrappingMode = TextWrappingModes.Normal;
        storyText.lineSpacing = 18f;
        storyText.color = new Color(0.92f, 0.88f, 0.82f, 1f);

        storyHint = AddText(panel.transform, "", new Vector2(0f, -280f), new Vector2(760f, 70f), 30f);
        storyHint.color = new Color(1f, 1f, 1f, 0.85f);

        loadingSideText = AddText(panel.transform, "Headphones recommended", new Vector2(0f, -140f), new Vector2(1000f, 100f), 54f);
        loadingSideText.alignment = TextAlignmentOptions.Center;
        loadingSideText.color = new Color(0.92f, 0.88f, 0.82f, 0.9f);
        loadingSideText.gameObject.SetActive(false);

        storyPanel = panel;
        storyPanel.SetActive(false);
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

    TMP_InputField AddInputField(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject inputObject = new GameObject("Username", typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(parent, false);

        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject viewportObject = new GameObject("Text Area", typeof(RectMask2D));
        viewportObject.transform.SetParent(inputObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport, 22f, 8f, 22f, 8f);

        TextMeshProUGUI text = AddText(viewport, "", Vector2.zero, Vector2.zero, 42f);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(6f, 0f, 6f, 0f);

        TextMeshProUGUI placeholder = AddText(viewport, "Name", Vector2.zero, Vector2.zero, 42f);
        Stretch(placeholder.rectTransform);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.margin = new Vector4(6f, 0f, 6f, 0f);
        placeholder.color = new Color(1f, 1f, 1f, 0.38f);

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 24;
        input.caretColor = Color.white;
        input.selectionColor = new Color(1f, 1f, 1f, 0.25f);
        input.targetGraphic = background;

        return input;
    }

    Button AddButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        return AddButton(parent, text, position, action, new Vector2(300f, 72f), 44f);
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
        colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f, 0.88f);
        colors.pressedColor = new Color(0.55f, 0.55f, 0.55f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        AddText(buttonObject.transform, text, Vector2.zero, size, fontSize);

        return button;
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
