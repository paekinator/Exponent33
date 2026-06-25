using UnityEngine;

/// <summary>
/// Tiny local result store for testing the end-to-end flow. Later, a proper
/// leaderboard UI can read LastShiftLeaderboard from PlayerPrefs and display
/// these rows or replace this with cloud-backed storage.
/// </summary>
public static class LastShiftLeaderboardStore
{
    public const string Key = "LastShiftLeaderboard";

    public static void Record(BossEndScreen.ResultType resultType, float survivedSeconds)
    {
        string playerName = string.IsNullOrWhiteSpace(PlayerProfile.PlayerName)
            ? GameSessionSettings.PlayerName
            : PlayerProfile.PlayerName;

        string row = System.DateTime.UtcNow.ToString("o")
            + "|" + playerName
            + "|" + GameSessionSettings.Difficulty
            + "|" + resultType
            + "|" + Mathf.Max(0f, survivedSeconds).ToString("0.00");

        string existing = PlayerPrefs.GetString(Key, "");
        PlayerPrefs.SetString(Key, string.IsNullOrEmpty(existing) ? row : existing + "\n" + row);
        PlayerPrefs.Save();
    }
}
