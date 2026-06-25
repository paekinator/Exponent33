using UnityEngine;

public static class GameAudioSettings
{
    public const string MusicSliderKey = "MusicVolume";
    public const string SfxSliderKey = "SFXVolume";
    const string AudioScaleVersionKey = "AudioScaleVersion";
    const int CurrentAudioScaleVersion = 3;
    public const float DefaultSliderValue = 0.5f;
    public const float MinMusicVolume = 0.01f;
    public const float MaxMusicVolume = 0.3f;
    public const float MinSfxVolume = 0f;
    public const float MaxSfxVolume = 2f;

    public static float MusicSlider
    {
        get => GetSlider(MusicSliderKey);
        set => SaveSlider(MusicSliderKey, value);
    }

    public static float SfxSlider
    {
        get => GetSlider(SfxSliderKey);
        set => SaveSlider(SfxSliderKey, value);
    }

    public static float MusicOutputVolume => MusicSliderToOutput(MusicSlider);
    public static float SfxOutputVolume => SfxSliderToOutput(SfxSlider);

    public static float MusicSliderToOutput(float sliderValue)
    {
        return Mathf.Lerp(MinMusicVolume, MaxMusicVolume, Mathf.Clamp01(sliderValue));
    }

    public static float SfxSliderToOutput(float sliderValue)
    {
        return Mathf.Lerp(MinSfxVolume, MaxSfxVolume, Mathf.Clamp01(sliderValue));
    }

    static float GetSlider(string key)
    {
        EnsureCurrentScale();

        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetFloat(key, DefaultSliderValue);
            PlayerPrefs.Save();
        }

        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultSliderValue));
    }

    static void SaveSlider(string key, float value)
    {
        EnsureCurrentScale();
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    static void EnsureCurrentScale()
    {
        if (PlayerPrefs.GetInt(AudioScaleVersionKey, 0) == CurrentAudioScaleVersion)
        {
            return;
        }

        PlayerPrefs.SetFloat(MusicSliderKey, DefaultSliderValue);
        PlayerPrefs.SetFloat(SfxSliderKey, DefaultSliderValue);
        PlayerPrefs.SetInt(AudioScaleVersionKey, CurrentAudioScaleVersion);
        PlayerPrefs.Save();
    }
}
