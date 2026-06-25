using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen "CAUGHT" overlay shown when the Boss catches the player.
/// Builds its own UI at runtime if no references are assigned — nothing to
/// set up by hand in the Editor, same as the rest of the HUD.
///
/// SETUP: put this on the Player (or anywhere), then drag it into
/// BossAI's onCatchPlayer event -> ShowEndScreen(). If you'd rather hand-build
/// the UI yourself, assign 'panel'/'catchText'/'subText' and this skips the
/// runtime build entirely.
/// </summary>
public class BossEndScreen : MonoBehaviour
{
    public enum ResultType
    {
        Caught,
        Dehydrated,
        Survived
    }

    [Header("UI References (built at runtime if left empty)")]
    public GameObject panel;
    public TextMeshProUGUI catchText;
    public TextMeshProUGUI subText;

    [Header("Text Content")]
    public string catchMessage = "CAUGHT";
    public string subtitleMessage = "Press R to Restart";

    [Header("Behaviour")]
    [Tooltip("Freeze game time when the end screen appears.")]
    public bool freezeTimeOnShow = true;
    [Tooltip("Unlock and show the cursor on the end screen.")]
    public bool unlockCursorOnShow = true;
    [Tooltip("Switch back to menu music when this result screen appears.")]
    public bool playMenuMusicOnShow = true;
    [Tooltip("Track name in MusicLibrary used for menu and result-screen music.")]
    public string menuMusicTrackName = "MainMenu";
    [Tooltip("Scene name to load on restart. Leave empty to reload the current scene.")]
    public string restartSceneName = "";

    GameTimer timer;
    bool shown;

    public bool IsShown => shown;

    void Awake()
    {
        if (panel == null || catchText == null)
        {
            BuildRuntimeUi();
        }

        if (timer == null) timer = Object.FindAnyObjectByType<GameTimer>();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
    }

    /// <summary>Wire this to BossAI.onCatchPlayer in the Inspector.</summary>
    public void ShowEndScreen()
    {
        ShowResult(ResultType.Caught);
    }

    public void ShowResult(ResultType resultType)
    {
        float survivedSeconds = timer != null ? timer.ElapsedSeconds : 0f;
        ShowResult(resultType, survivedSeconds);
    }

    public void ShowResult(ResultType resultType, float survivedSeconds)
    {
        if (shown) return;
        shown = true;

        BackShiftLeaderboardStore.Record(resultType, survivedSeconds);

        if (catchText != null)
        {
            catchText.text = GetTitle(resultType);
            catchText.color = GetTitleColor(resultType);
        }

        if (subText != null)
        {
            subText.text = GetSubtitle(resultType, survivedSeconds);
        }

        PlayEndScreenMusic();

        if (panel != null) panel.SetActive(true);

        if (freezeTimeOnShow)
        {
            Time.timeScale = 0f;
        }

        if (unlockCursorOnShow)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void PlayEndScreenMusic()
    {
        foreach (GameMusicPlayer gameMusic in Object.FindObjectsByType<GameMusicPlayer>())
        {
            gameMusic.StopMusic();
        }

        if (!playMenuMusicOnShow || MusicManager.Instance == null)
        {
            return;
        }

        // The screen freezes time immediately after this, so do not use a
        // time-based crossfade here.
        MusicManager.Instance.PlayMusic(menuMusicTrackName, 0f);
    }

    string GetTitle(ResultType resultType)
    {
        switch (resultType)
        {
            case ResultType.Survived:
                return "PASSED THE LEVEL!!!";
            case ResultType.Dehydrated:
                return "OUT OF WATER";
            default:
                return catchMessage;
        }
    }

    string GetSubtitle(ResultType resultType, float survivedSeconds)
    {
        if (resultType == ResultType.Survived)
        {
            return "You survived the full shift.\nPress R to Restart";
        }

        return "You survived " + FormatTime(survivedSeconds) + ".\nPress R to Restart";
    }

    Color GetTitleColor(ResultType resultType)
    {
        switch (resultType)
        {
            case ResultType.Survived:
                return new Color(0.45f, 0.95f, 0.55f, 1f);
            case ResultType.Dehydrated:
                return new Color(0.28f, 0.72f, 1f, 1f);
            default:
                return new Color(0.9f, 0.15f, 0.12f, 1f);
        }
    }

    string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainder = totalSeconds % 60;
        return minutes + ":" + remainder.ToString("00");
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        shown = false;

        if (string.IsNullOrEmpty(restartSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(restartSceneName);
        }
    }

    void BuildRuntimeUi()
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

        GameObject panelObject = new GameObject("Boss_End_Screen");
        panelObject.transform.SetParent(canvas.transform, false);
        // Renders on top of every other HUD element.
        panelObject.transform.SetAsLastSibling();

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.88f);
        RectTransform bgRect = background.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        TMP_FontAsset horrorFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");

        catchText = CreateText(panelObject.transform, "Caught_Title", catchMessage, 90, new Vector2(0f, 30f), horrorFont);
        catchText.color = new Color(0.9f, 0.15f, 0.12f, 1f);

        subText = CreateText(panelObject.transform, "Caught_Subtitle", subtitleMessage, 28, new Vector2(0f, -60f), horrorFont);
        subText.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);

        panel = panelObject;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Vector2 anchoredPosition, TMP_FontAsset font)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(1200f, 140f);

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(3f, -3f);

        return tmp;
    }
}
