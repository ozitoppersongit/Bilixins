using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioControl : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundsSlider;

    private const string MUSIC = "Músicas";
    private const string SOUNDS = "SFX";

    private void Start()
    {
        LoadSlider(musicSlider, MUSIC);
        LoadSlider(soundsSlider, SOUNDS);

        musicSlider.onValueChanged.AddListener(v => SetVolume(MUSIC, v));
        soundsSlider.onValueChanged.AddListener(v => SetVolume(SOUNDS, v));
    }

    private void LoadSlider(Slider slider, string key)
    {
        float value = PlayerPrefs.GetFloat(key, 0.75f);
        slider.minValue = 0.0001f;   // importante: nunca 0
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        SetVolume(key, value);
    }

    private void SetVolume(string key, float value)
    {
        mixer.SetFloat(key, Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(key, value);
    }
}