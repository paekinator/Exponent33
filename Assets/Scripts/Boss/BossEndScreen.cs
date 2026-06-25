using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
    public TextMeshProUGUI bodyText;
    public Button continueButton;

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
    Camera mainCamera;
    BossAI boss;
    bool shown;
    Coroutine endingRoutine;

    public bool IsShown => shown;

    void Awake()
    {
        if (panel == null || catchText == null)
        {
            BuildRuntimeUi();
        }

        if (timer == null) timer = Object.FindAnyObjectByType<GameTimer>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (boss == null) boss = Object.FindAnyObjectByType<BossAI>();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            ContinueToMainMenu();
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
        EndGameplayImmediately();
        PlayEndScreenMusic();

        if (catchText != null)
        {
            catchText.text = GetTitle(resultType);
            catchText.color = GetTitleColor(resultType);
        }

        if (subText != null)
        {
            subText.text = GetStatsLine(resultType, survivedSeconds);
        }

        if (bodyText != null) bodyText.text = "";
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        if (endingRoutine != null) StopCoroutine(endingRoutine);
        endingRoutine = StartCoroutine(EndingSequence(resultType, survivedSeconds));
    }

    void EndGameplayImmediately()
    {
        if (timer != null) timer.StopTimer();

        foreach (GameMusicPlayer gameMusic in Object.FindObjectsByType<GameMusicPlayer>())
        {
            gameMusic.StopMusic();
        }

        foreach (FirstPersonController controller in Object.FindObjectsByType<FirstPersonController>())
        {
            controller.playerCanMove = false;
            controller.enabled = false;
        }

        foreach (PlayerInteractor interactor in Object.FindObjectsByType<PlayerInteractor>())
        {
            interactor.enabled = false;
        }

        foreach (PlayerStats stats in Object.FindObjectsByType<PlayerStats>())
        {
            stats.enabled = false;
        }

        foreach (PlayerNoiseMeter noise in Object.FindObjectsByType<PlayerNoiseMeter>())
        {
            noise.enabled = false;
        }

        foreach (PhoneViewmodel phone in Object.FindObjectsByType<PhoneViewmodel>())
        {
            phone.enabled = false;
        }

        foreach (MilkItem milk in Object.FindObjectsByType<MilkItem>())
        {
            milk.enabled = false;
        }

        foreach (PlayerWalkAudio walkAudio in Object.FindObjectsByType<PlayerWalkAudio>())
        {
            walkAudio.enabled = false;
        }

        foreach (PlayerDrinkAudio drinkAudio in Object.FindObjectsByType<PlayerDrinkAudio>())
        {
            drinkAudio.enabled = false;
        }

        foreach (PlayerChargeAudio chargeAudio in Object.FindObjectsByType<PlayerChargeAudio>())
        {
            chargeAudio.enabled = false;
        }

        foreach (PlayerLowWaterPanting panting in Object.FindObjectsByType<PlayerLowWaterPanting>())
        {
            panting.enabled = false;
        }

        foreach (GamePauseMenu pauseMenu in Object.FindObjectsByType<GamePauseMenu>())
        {
            pauseMenu.enabled = false;
        }

        foreach (BossAI bossAi in Object.FindObjectsByType<BossAI>())
        {
            bossAi.enabled = false;
        }

        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>())
        {
            source.Stop();
        }

        foreach (Rigidbody rb in Object.FindObjectsByType<Rigidbody>())
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

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

    IEnumerator EndingSequence(ResultType resultType, float survivedSeconds)
    {
        if (resultType == ResultType.Caught)
        {
            yield return JumpscareCamera();
        }

        if (panel != null) panel.SetActive(true);
        yield return TypeBody(GetEndingMessage(resultType, survivedSeconds), 5.5f);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    IEnumerator JumpscareCamera()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (boss == null) boss = Object.FindAnyObjectByType<BossAI>();
        if (mainCamera == null || boss == null) yield break;

        Transform bossTransform = boss.transform;
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        Vector3 lookTarget = bossTransform.position + Vector3.up * 1.65f;
        Vector3 directionFromBoss = (startPosition - lookTarget).normalized;
        if (directionFromBoss.sqrMagnitude < 0.01f)
        {
            directionFromBoss = bossTransform.forward;
        }

        Vector3 targetPosition = lookTarget + directionFromBoss * 1.15f;
        targetPosition.y = lookTarget.y;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - targetPosition, Vector3.up);

        float duration = 0.42f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            mainCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t));
            yield return null;
        }

        mainCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    IEnumerator TypeBody(string message, float seconds)
    {
        if (bodyText == null) yield break;

        bodyText.text = "";
        float elapsed = 0f;
        int lastCount = -1;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt((elapsed / seconds) * message.Length), 0, message.Length);
            if (count != lastCount)
            {
                bodyText.text = message.Substring(0, count);
                lastCount = count;
            }

            yield return null;
        }

        bodyText.text = message;
    }

    void PlayEndScreenMusic()
    {
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
                return "AVOIDED OVERTIME!";
            case ResultType.Dehydrated:
                return "OUT OF WATER";
            default:
                return catchMessage;
        }
    }

    string GetStatsLine(ResultType resultType, float survivedSeconds)
    {
        string result = resultType == ResultType.Survived ? "Success" : resultType == ResultType.Dehydrated ? "Thirst" : "Caught";
        return "Result: " + result
               + "   |   Time survived: " + FormatTime(survivedSeconds)
               + "   |   Difficulty: " + GameSessionSettings.Difficulty;
    }

    string GetEndingMessage(ResultType resultType, float survivedSeconds)
    {
        switch (resultType)
        {
            case ResultType.Survived:
                return "SUCCESS. Your boss is suspicious, and probably knows someone is here, but he needs to go back upstairs and address the company. He has to leave... Did you discover his secrets?... What is he doing in here...? How was this place built? Who is he calling on the phone.. So many questions.......";
            case ResultType.Dehydrated:
                return "You've fainted. You lived for " + FormatTimeWords(survivedSeconds) + ". You wake up the next day... And the boss has you tied up in an interrogation chair...";
            default:
                return "You have been caught. You lived for " + FormatTimeWords(survivedSeconds) + ". Your boss ties you up and puts you into another area; a dungeon. You hope that people will come looking out for you. But can anyone find you, in here?";
        }
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

    string FormatTimeWords(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainder = totalSeconds % 60;

        if (minutes <= 0)
        {
            return remainder == 1 ? "1 second" : remainder + " seconds";
        }

        string minuteText = minutes == 1 ? "1 minute" : minutes + " minutes";
        if (remainder <= 0)
        {
            return minuteText;
        }

        string secondText = remainder == 1 ? "1 second" : remainder + " seconds";
        return minuteText + " and " + secondText;
    }

    void ContinueToMainMenu()
    {
        Time.timeScale = 1f;
        shown = false;
        SceneManager.LoadScene(string.IsNullOrEmpty(restartSceneName) ? "MainMenu" : restartSceneName);
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

        EnsureEventSystem();

        GameObject panelObject = new GameObject("Complete_End_Screen");
        panelObject.transform.SetParent(canvas.transform, false);
        // Renders on top of every other HUD element.
        panelObject.transform.SetAsLastSibling();

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.92f);
        RectTransform bgRect = background.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        TMP_FontAsset horrorFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");

        catchText = CreateText(panelObject.transform, "End_Title", catchMessage, 96, new Vector2(0f, 220f), horrorFont);
        catchText.color = new Color(0.9f, 0.15f, 0.12f, 1f);

        subText = CreateText(panelObject.transform, "End_Stats", subtitleMessage, 28, new Vector2(0f, 120f), horrorFont);
        subText.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);

        bodyText = CreateText(panelObject.transform, "End_Body", "", 36, new Vector2(0f, -30f), horrorFont);
        bodyText.color = new Color(0.92f, 0.88f, 0.82f, 1f);
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.rectTransform.sizeDelta = new Vector2(1240f, 260f);

        continueButton = CreateButton(panelObject.transform, "Continue", new Vector2(0f, -270f), ContinueToMainMenu, horrorFont);
        continueButton.gameObject.SetActive(false);

        panel = panelObject;
    }

    void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, TMP_FontAsset font)
    {
        GameObject buttonObject = new GameObject(label + "_Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.03f, 0.03f, 0.03f, 0.78f);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(340f, 74f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.5764706f, 0.3882353f, 0.3882353f, 1f);
        colors.pressedColor = new Color(0.78f, 0.18f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI buttonText = CreateText(buttonObject.transform, label, label, 38, Vector2.zero, font);
        buttonText.rectTransform.sizeDelta = rect.sizeDelta;
        buttonText.color = Color.white;

        return button;
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
