using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// inspo https://www.youtube.com/watch?v=Q-bKHocRvE0

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField]
    private MusicLibrary musicLibrary;
    [SerializeField]
    private AudioSource musicSource;
    private float volume = GameAudioSettings.MusicOutputVolume;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            volume = GameAudioSettings.MusicOutputVolume;
            if (musicSource != null) musicSource.volume = volume;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnDestroy() {
        if (Instance == this) {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == "MainMenu") {
            PlayMusic("MainMenu", 0f);
        }
        else if (scene.name == "BackroomsLevel") {
            PlayMusic("Game", 0.35f);
        }
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f) {
        if (musicLibrary == null || musicSource == null) return;

        AudioClip nextTrack = musicLibrary.GetClipFromName(trackName);
        if (nextTrack == null) return;
        if (musicSource.clip == nextTrack && musicSource.isPlaying) return;

        if (fadeDuration <= 0f) {
            musicSource.clip = nextTrack;
            musicSource.volume = volume;
            musicSource.Play();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(AnimateMusicCrossfade(nextTrack, fadeDuration));
    }

    public void SetVolume(float sliderValue) {
        GameAudioSettings.MusicSlider = sliderValue;
        volume = GameAudioSettings.MusicSliderToOutput(sliderValue);
        if (musicSource != null) musicSource.volume = volume;
    }

    public void StopMusic() {
        StopAllCoroutines();
        if (musicSource != null) {
            musicSource.Stop();
            musicSource.clip = null;
        }
    }

    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f) {
        float percent = 0.1f;
        while (percent < 1) {
            percent += Time.deltaTime * 1 / fadeDuration;
            musicSource.volume = Mathf.Lerp(volume, 0, percent);
            yield return null;
        }

        musicSource.clip = nextTrack;
        musicSource.Play();

        percent = 0.1f;
        while (percent < 1) {
            percent += Time.deltaTime * 1 /fadeDuration;
            musicSource.volume = Mathf.Lerp(0, volume, percent);
            yield return null;
        }
    }
}
