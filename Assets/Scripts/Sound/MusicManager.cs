using System.Collections;
using UnityEngine;

// inspo https://www.youtube.com/watch?v=Q-bKHocRvE0

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField]
    private MusicLibrary musicLibrary;
    [SerializeField]
    private AudioSource musicSource;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f) {
        if (musicLibrary == null || musicSource == null) return;

        AudioClip nextTrack = musicLibrary.GetClipFromName(trackName);
        if (nextTrack == null) return;

        if (fadeDuration <= 0f) {
            musicSource.clip = nextTrack;
            musicSource.volume = 1f;
            musicSource.Play();
            return;
        }

        StartCoroutine(AnimateMusicCrossfade(nextTrack, fadeDuration));
    }

    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f) {
        float percent = 0.1f;
        while (percent < 1) {
            percent += Time.deltaTime * 1 / fadeDuration;
            musicSource.volume = Mathf.Lerp(1f, 0, percent);
            yield return null;
        }

        musicSource.clip = nextTrack;
        musicSource.Play();

        percent = 0.1f;
        while (percent < 1) {
            percent += Time.deltaTime * 1 /fadeDuration;
            musicSource.volume = Mathf.Lerp(0, 1f, percent);
            yield return null;
        }
    }
}
