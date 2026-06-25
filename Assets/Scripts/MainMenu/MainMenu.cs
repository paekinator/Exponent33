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
    TMP_FontAsset menuFont;

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
        string playerName = playerNameInput != null ? playerNameInput.text : "";
        GameSessionSettings.Save(playerName, difficulty);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("Game", 0.5f);
        }

        SceneManager.LoadScene(GameScene);
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
        if (audioMixer == null) return;
        float logAdjustedVol = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        audioMixer.SetFloat("MusicVolume", logAdjustedVol);
    }

    public void UpdateSoundVolume(float volume)
    {
        if (audioMixer == null) return;
        float logAdjustedVol = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        audioMixer.SetFloat("SFXVolume", logAdjustedVol);
    }

    public void SaveVolume()
    {
        if (audioMixer == null) return;

        audioMixer.GetFloat("MusicVolume", out float musicVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        audioMixer.GetFloat("SFXVolume", out float SFXVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.Save();
    }

    public void LoadVolume()
    {
        float defaultLinearVolume = 0.75f;
        if (!PlayerPrefs.HasKey("MusicVolume")) PlayerPrefs.SetFloat("MusicVolume", defaultLinearVolume);
        if (!PlayerPrefs.HasKey("SFXVolume")) PlayerPrefs.SetFloat("SFXVolume", defaultLinearVolume);

        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultLinearVolume);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", defaultLinearVolume);

        if (musicSlider != null) musicSlider.value = musicVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
        UpdateMusicVolume(musicVolume);
        UpdateSoundVolume(sfxVolume);
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
