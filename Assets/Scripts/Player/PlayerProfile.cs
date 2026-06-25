/// <summary>
/// Placeholder for the player's chosen display name. A future main-menu
/// name-select screen should set PlayerProfile.PlayerName before this scene
/// loads. Until that exists, dialogue just shows this default.
/// </summary>
public static class PlayerProfile
{
    static string playerName = "Player";

    public static string PlayerName
    {
        get
        {
            if (playerName == "Player")
            {
                GameSessionSettings.Load();
            }

            return playerName;
        }
        set => playerName = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
    }
}
