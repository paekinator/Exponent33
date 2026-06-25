using UnityEngine;

/// <summary>
/// Survival stopwatch. Sits at 0:00 until IntroDialogueSequencer calls
/// StartTimer() once the player has clicked through the intro — it does NOT
/// start counting at scene load.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public bool IsRunning { get; private set; }
    float elapsed;

    public float ElapsedSeconds => elapsed;

    public string FormattedTime
    {
        get
        {
            int totalSeconds = Mathf.FloorToInt(elapsed);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:00}";
        }
    }

    void Update()
    {
        if (IsRunning)
        {
            elapsed += Time.deltaTime;
        }
    }

    public void StartTimer()
    {
        elapsed = 0f;
        IsRunning = true;
    }

    /// <summary>Continues counting from wherever it was paused — unlike
    /// StartTimer(), this does not reset elapsed back to 0.</summary>
    public void ResumeTimer()
    {
        IsRunning = true;
    }

    public void StopTimer()
    {
        IsRunning = false;
    }
}
