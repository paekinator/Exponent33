using UnityEngine;

// inspo https://www.youtube.com/watch?v=Q-bKHocRvE0

[System.Serializable]
public struct MusicTrack {
    public string trackName;
    public AudioClip clip;
}

public class MusicLibrary : MonoBehaviour
{
    public MusicTrack[] tracks;

    public AudioClip GetClipFromName(string trackName) {
        foreach (var track in tracks) {
            if (track.trackName == trackName) {
                return track.clip;
            }
        }
        return null;
    }
}
