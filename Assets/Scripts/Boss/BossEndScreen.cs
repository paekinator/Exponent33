using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BossEndScreen — shows a full-screen "caught" overlay when the Boss catches the player.
///
/// SETUP IN UNITY:
///   1. In Hierarchy: right-click → UI → Canvas. Name it "EndScreenCanvas".
///      - Canvas Scaler: Scale With Screen Size, 1920x1080.
///      - Canvas: Render Mode = Screen Space - Overlay.
///   2. Add a child Panel (black background):
///      - right-click Canvas → UI → Panel
///      - Image color: (0, 0, 0, 1) — solid black.
///   3. Add a child TextMeshPro text:
///      - right-click Canvas → UI → Text - TextMeshPro
///      - Set text: "CAUGHT"  Font size: 80  Alignment: Center+Middle
///   4. Optionally add a subtext for "Press R to Restart" below.
///   5. Add BossEndScreen component to the EndScreenCanvas GameObject.
///   6. Drag Panel → backgroundPanel, TextMeshPro → catchText, subText.
///   7. Disable the Canvas in the Inspector (it starts hidden).
///   8. In BossAI Inspector → onCatchPlayer → + → drag EndScreenCanvas →
///      BossEndScreen.ShowEndScreen().
/// </summary>
public class BossEndScreen : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The root Canvas or Panel to show/hide.")]
    public GameObject panel;
    [Tooltip("Main 'CAUGHT' title text.")]
    public TextMeshProUGUI catchText;
    [Tooltip("Optional subtitle, e.g. 'Press R to Restart'.")]
    public TextMeshProUGUI subText;

    [Header("Text Content")]
    public string catchMessage    = "CAUGHT";
    public string subtitleMessage = "Press R to Restart";

    [Header("Behaviour")]
    [Tooltip("Freeze game time when the end screen appears.")]
    public bool freezeTimeOnShow  = true;
    [Tooltip("Unlock and show the cursor on the end screen.")]
    public bool unlockCursorOnShow = true;
    [Tooltip("Scene name or index to load on restart. Leave empty to reload current scene.")]
    public string restartSceneName = "";

    void Start()
    {
        // Make sure the screen is hidden at start
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        // Restart on R key while end screen is visible
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.R))
            RestartScene();
    }

    /// <summary>
    /// Wire this to BossAI.onCatchPlayer in the Inspector.
    /// </summary>
    public void ShowEndScreen()
    {
        if (catchText != null) catchText.text  = catchMessage;
        if (subText   != null) subText.text    = subtitleMessage;

        if (panel != null) panel.SetActive(true);

        if (freezeTimeOnShow)
            Time.timeScale = 0f;

        if (unlockCursorOnShow)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        Debug.Log("[BossEndScreen] End screen shown.");
    }

    void RestartScene()
    {
        Time.timeScale = 1f; // always restore before loading

        if (string.IsNullOrEmpty(restartSceneName))
        {
            // Reload the currently active scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(restartSceneName);
        }
    }
}
