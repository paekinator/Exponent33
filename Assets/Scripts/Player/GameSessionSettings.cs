using UnityEngine;

public enum GameDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public static class GameSessionSettings
{
    const string PlayerNameKey = "BackShift.PlayerName";
    const string DifficultyKey = "BackShift.Difficulty";

    public static string PlayerName { get; private set; } = "Player";
    public static GameDifficulty Difficulty { get; private set; } = GameDifficulty.Medium;

    public static void Load()
    {
        PlayerName = PlayerPrefs.GetString(PlayerNameKey, "Player");
        Difficulty = (GameDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)GameDifficulty.Medium);
        PlayerProfile.PlayerName = PlayerName;
    }

    public static void Save(string playerName, int difficulty)
    {
        PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
        Difficulty = ClampDifficulty(difficulty);

        PlayerPrefs.SetString(PlayerNameKey, PlayerName);
        PlayerPrefs.SetInt(DifficultyKey, (int)Difficulty);
        PlayerPrefs.Save();

        PlayerProfile.PlayerName = PlayerName;
    }

    static GameDifficulty ClampDifficulty(int difficulty)
    {
        if (difficulty <= (int)GameDifficulty.Easy) return GameDifficulty.Easy;
        if (difficulty >= (int)GameDifficulty.Hard) return GameDifficulty.Hard;
        return GameDifficulty.Medium;
    }
}
