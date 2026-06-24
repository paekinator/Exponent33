using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

// inspo: https://www.youtube.com/watch?v=B40xBPXK97A

public class MainMenu : MonoBehaviour
{
    public AudioMixer audioMixer;

    public Slider musicSlider;
    public Slider sfxSlider;

    public void Start() {
        LoadVolume();
        MusicManager.Instance.PlayMusic("MainMenu");
    }
    public void Play() {
        SceneManager.LoadScene("Game");
    }

    public void Quit() {
        Application.Quit();
    }

    public void UpdateMusicVolume(float volume) {
        audioMixer.SetFloat("MusicVolume", volume);
    }

    public void UpdateSoundVolume(float volume) {
        audioMixer.SetFloat("SFXVolume", volume);
    }

    public void SaveVolume() {
        audioMixer.GetFloat("MusicVolume", out float musicVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        audioMixer.GetFloat("SFXVolume", out float SFXVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
    }

    public void LoadVolume() {
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume");
    }
}
