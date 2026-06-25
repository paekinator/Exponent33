using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using TMPro;
using System.Threading.Tasks;


public class LeaderBoard : MonoBehaviour
{
    [SerializeField] public float playerScore;

    [SerializeField] public GameObject leaderBoardParent;
    [SerializeField] public Transform leaderBoardContentParent;
    [SerializeField] public Transform leaderItemPrefab;

    [SerializeField] private TMP_InputField usernameInput;


    private string leaderboardID = "LeaderboardGameJam2026";

    private async void Start(){
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
        LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardID, playerScore);
        UpdateLeaderBoard();
        }

    public async void UpdateLeaderBoard() {
        
        LeaderboardScoresPage leaderboardScoresPage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardID);
        foreach (Transform t in leaderBoardContentParent) {
            Destroy(t.gameObject);

        }

        foreach (LeaderboardEntry entry in leaderboardScoresPage.Results) {
            Transform leaderboardItem = Instantiate(leaderItemPrefab, leaderBoardContentParent);
            leaderboardItem.GetChild(1).GetComponent<TextMeshProUGUI>().text = entry.PlayerName;
            leaderboardItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = entry.Score.ToString();            
        }
    }

    public async void CreateProfile() {
        if (!string.IsNullOrEmpty(usernameInput.text)) {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(usernameInput.text);
        }
    }

}

