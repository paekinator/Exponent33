using UnityEngine;
using UnityEngine.SceneManagement;

// inspo: https://www.youtube.com/watch?v=B40xBPXK97A

public class MainMenu : MonoBehaviour
{
    public void Play() {
        SceneManager.LoadScene("Game");
    }

    public void Quit() {
        Application.Quit();
    }
}
