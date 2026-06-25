using UnityEngine;

// inspo from https://www.youtube.com/watch?v=jEoobucfoL4

[System.Serializable]
public struct SoundEffect {
    public string groupID;
    public AudioClip[] clips;
}

public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] SoundEffects;

    public AudioClip GetClipFromName(string name) {
        foreach (var soundEffect in SoundEffects) {
            if (soundEffect.groupID == name) {
                return soundEffect.clips[Random.Range(0, soundEffect.clips.Length)];
            }
        }

        return null;
    }
}
