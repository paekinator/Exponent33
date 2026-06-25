using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the BackroomsLevel win/loss conditions:
/// boss caught, water reaches zero, or the survival timer reaches the target.
/// It auto-creates itself for BackroomsLevel so the scene can be tested without
/// manually wiring a new object every time the level changes.
/// </summary>
[DefaultExecutionOrder(500)]
public class GameEndManager : MonoBehaviour
{
    const string BackroomsSceneName = "BackroomsLevel";

    [Tooltip("How long the player has to survive after the intro dialogue starts the timer.")]
    public float survivalSeconds = 300f;

    public GameTimer timer;
    public PlayerStats stats;
    public BossAI boss;
    public BossEndScreen endScreen;

    bool ended;

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
        if (Object.FindAnyObjectByType<GameEndManager>() != null) return;

        GameObject managerObject = new GameObject("Game_End_Manager");
        managerObject.AddComponent<GameEndManager>();
    }

    void Awake()
    {
        Time.timeScale = 1f;

        if (timer == null) timer = Object.FindAnyObjectByType<GameTimer>();
        if (stats == null) stats = Object.FindAnyObjectByType<PlayerStats>();
        if (boss == null) boss = Object.FindAnyObjectByType<BossAI>();
        if (endScreen == null) endScreen = Object.FindAnyObjectByType<BossEndScreen>();
        if (endScreen == null) endScreen = gameObject.AddComponent<BossEndScreen>();

        if (boss != null)
        {
            boss.endScreen = endScreen;
        }
    }

    void Update()
    {
        if (ended || endScreen == null || endScreen.IsShown)
        {
            return;
        }

        if (stats != null && stats.water <= 0f)
        {
            End(BossEndScreen.ResultType.Dehydrated);
            return;
        }

        if (timer != null && timer.ElapsedSeconds >= survivalSeconds)
        {
            End(BossEndScreen.ResultType.Survived);
        }
    }

    void End(BossEndScreen.ResultType resultType)
    {
        ended = true;
        float survivedSeconds = timer != null ? timer.ElapsedSeconds : 0f;
        endScreen.ShowResult(resultType, survivedSeconds);
    }
}
