using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Click-through dialogue at the bottom of the screen, built once and reused
/// for every "chapter" (the opening intro, and later interrupts like the boss
/// laugh). While a chapter is active, the survival systems are frozen (no
/// water/phone drain, no noise, no world interaction, no movement, phone
/// hidden) — they resume when the player presses E through the final line.
/// Music is NOT gated by this — it starts immediately on spawn (GameMusicPlayer).
///
/// Monologue lines are prefixed with the player's name (PlayerProfile.PlayerName)
/// and quoted, e.g. "Joe:\n'Wow, this is...'" — PlayerProfile is a placeholder
/// until the main menu's name-select screen sets it for real.
/// </summary>
public class IntroDialogueSequencer : MonoBehaviour
{
    [System.Serializable]
    public class IntroLine
    {
        public enum Kind { Monologue, Tip, Mission }
        public Kind kind = Kind.Monologue;
        [TextArea(2, 5)] public string text;
    }

    public List<IntroLine> lines = new List<IntroLine>
    {
        new IntroLine { kind = IntroLine.Kind.Monologue, text = "Wow, so this is Paul's secret room. No wonder he hid it from everyone... What is this? Why is it so hot? I am dehydrating so fast..." },
        new IntroLine { kind = IntroLine.Kind.Tip, text = "Make sure you keep hydrated! This room is hot, and if you faint, you might not wake up..." },
        new IntroLine { kind = IntroLine.Kind.Monologue, text = "My phone battery is draining so fast, visibility is bad but I can use my torch to see if needed. I need to keep my phone charged, otherwise it will send the distress signal and get me caught..." },
        new IntroLine { kind = IntroLine.Kind.Tip, text = "Your company tracks your phone, and when dead, sends distress signals and your boss will immediately know your location. If he knows you are in the backrooms... You will be found and dealt with..." },
        new IntroLine { kind = IntroLine.Kind.Monologue, text = "Ahh, 8:55, shift ends in 5 minutes, I can't go back to the office until shift ends, or I will be found. I have to sneak back right after shift ends in the night meeting, when everyone goes to the other side of the office." },
        new IntroLine { kind = IntroLine.Kind.Mission, text = "Survive for 5 minutes, this is the only way you can make it out alive." },
        new IntroLine { kind = IntroLine.Kind.Tip, text = "Press T while on your phone to turn on your torch; it drains battery faster!" },
    };

    [Header("References (auto-found if left empty)")]
    public PlayerStats stats;
    public PlayerNoiseMeter noiseMeter;
    public PlayerInteractor interactor;
    public FirstPersonController controller;
    public PhoneViewmodel phoneViewmodel;
    public MilkItem milkItem;
    public PlayerWalkAudio walkAudio;
    public Rigidbody playerRigidbody;
    public GameTimer gameTimer;

    [Header("Style")]
    public TMP_FontAsset horrorFont;
    public Color monologueColor = new Color(0.95f, 0.78f, 0.68f, 1f);
    public Color tipColor = new Color(1f, 0.78f, 0.25f, 1f);
    public Color missionColor = new Color(0.95f, 0.2f, 0.18f, 1f);
    public Color hintColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);

    GameObject panelRoot;
    TextMeshProUGUI labelText;
    TextMeshProUGUI bodyText;
    TextMeshProUGUI hintText;

    List<IntroLine> activeLines;
    System.Action onChapterComplete;
    int index;
    bool active;

    public bool IsActive => active;

    void Awake()
    {
        if (stats == null) stats = Object.FindAnyObjectByType<PlayerStats>();
        if (noiseMeter == null) noiseMeter = Object.FindAnyObjectByType<PlayerNoiseMeter>();
        if (interactor == null) interactor = Object.FindAnyObjectByType<PlayerInteractor>();
        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (phoneViewmodel == null) phoneViewmodel = Object.FindAnyObjectByType<PhoneViewmodel>();
        if (milkItem == null) milkItem = Object.FindAnyObjectByType<MilkItem>();
        if (walkAudio == null) walkAudio = Object.FindAnyObjectByType<PlayerWalkAudio>();
        if (playerRigidbody == null && controller != null) playerRigidbody = controller.GetComponent<Rigidbody>();
        if (gameTimer == null) gameTimer = Object.FindAnyObjectByType<GameTimer>();

        BuildUi();
        BeginChapter(lines, OnIntroComplete);
    }

    void Update()
    {
        if (!active)
        {
            return;
        }

        // Pulse the hint so it draws the eye without shouting over the dialogue.
        if (hintText != null)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 4f);
            Color c = hintColor;
            c.a = hintColor.a * pulse;
            hintText.color = c;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Advance();
        }
    }

    /// <summary>Freezes gameplay and shows the given lines from the start.
    /// Reuses the same panel for any chapter — the intro, the boss laugh
    /// interrupt, or anything added later. onComplete fires once the player
    /// clicks through the last line, after gameplay is already unfrozen.</summary>
    public void BeginChapter(List<IntroLine> chapterLines, System.Action onComplete)
    {
        activeLines = chapterLines;
        onChapterComplete = onComplete;
        index = 0;
        active = true;

        Freeze();
        panelRoot.SetActive(true);
        ShowLine();
    }

    void Advance()
    {
        index++;
        if (index >= activeLines.Count)
        {
            EndChapter();
        }
        else
        {
            ShowLine();
        }
    }

    void ShowLine()
    {
        IntroLine line = activeLines[index];
        bool isLast = index == activeLines.Count - 1;

        switch (line.kind)
        {
            case IntroLine.Kind.Tip:
                labelText.text = "GAME TIP";
                labelText.color = tipColor;
                labelText.gameObject.SetActive(true);
                bodyText.color = tipColor;
                bodyText.text = line.text;
                break;
            case IntroLine.Kind.Mission:
                labelText.text = "GAME MISSION";
                labelText.color = missionColor;
                labelText.gameObject.SetActive(true);
                bodyText.color = missionColor;
                bodyText.text = line.text;
                break;
            default:
                // Placeholder until the main menu's name-select screen sets
                // PlayerProfile.PlayerName for real.
                labelText.text = PlayerProfile.PlayerName + ":";
                labelText.color = monologueColor;
                labelText.gameObject.SetActive(true);
                bodyText.color = monologueColor;
                bodyText.text = "'" + line.text + "'";
                break;
        }

        hintText.text = isLast ? "[ E ]  Press E to begin" : "[ E ]  Press E to continue";
    }

    void Freeze()
    {
        if (stats != null) stats.enabled = false;
        if (noiseMeter != null) noiseMeter.enabled = false;
        if (interactor != null) interactor.enabled = false;
        if (phoneViewmodel != null) phoneViewmodel.enabled = false;
        if (milkItem != null) milkItem.enabled = false;
        if (walkAudio != null) walkAudio.enabled = false;
        if (controller != null)
        {
            controller.playerCanMove = false;
            controller.cameraCanMove = false;
        }

        // playerCanMove=false only stops NEW movement forces — with friction
        // set to 0 on the player's collider, any momentum already in flight
        // (e.g. mid-sprint when this fires) would otherwise just keep
        // coasting. Zero it out so the player actually goes still.
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (gameTimer != null) gameTimer.StopTimer();
    }

    void EndChapter()
    {
        active = false;
        panelRoot.SetActive(false);

        if (stats != null) stats.enabled = true;
        if (noiseMeter != null) noiseMeter.enabled = true;
        if (interactor != null) interactor.enabled = true;
        if (phoneViewmodel != null) phoneViewmodel.enabled = true;
        if (milkItem != null) milkItem.enabled = true;
        if (walkAudio != null) walkAudio.enabled = true;
        if (controller != null)
        {
            controller.playerCanMove = true;
            controller.cameraCanMove = true;
        }

        onChapterComplete?.Invoke();
    }

    void OnIntroComplete()
    {
        if (gameTimer != null) gameTimer.StartTimer();
    }

    void BuildUi()
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

        if (horrorFont == null)
        {
            horrorFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");
        }

        panelRoot = new GameObject("Intro_Dialogue_Panel");
        panelRoot.transform.SetParent(canvas.transform, false);

        Image panelBackground = panelRoot.AddComponent<Image>();
        panelBackground.color = new Color(0.03f, 0.02f, 0.02f, 0.88f);
        RectTransform panelRect = panelBackground.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 36f);
        panelRect.sizeDelta = new Vector2(1000f, 230f);

        Outline panelOutline = panelRoot.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.55f, 0.06f, 0.04f, 0.9f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        labelText = CreateLabel("Intro_Label", 22);
        labelText.transform.SetParent(panelRoot.transform, false);
        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 2f;
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(24f, -44f);
        labelRect.offsetMax = new Vector2(-24f, -16f);

        bodyText = CreateLabel("Intro_Body", 22);
        bodyText.transform.SetParent(panelRoot.transform, false);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 15f;
        bodyText.fontSizeMax = 24f;
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(24f, 46f);
        bodyRect.offsetMax = new Vector2(-24f, -50f);

        hintText = CreateLabel("Intro_Hint", 16);
        hintText.transform.SetParent(panelRoot.transform, false);
        hintText.alignment = TextAlignmentOptions.BottomRight;
        hintText.color = hintColor;
        RectTransform hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-20f, 14f);
        hintRect.sizeDelta = new Vector2(360f, 24f);
    }

    TextMeshProUGUI CreateLabel(string name, int fontSize)
    {
        GameObject obj = new GameObject(name);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        if (horrorFont != null)
        {
            tmp.font = horrorFont;
        }

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return tmp;
    }
}
