using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioControl : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider soundsSlider;
    [SerializeField] private Slider dialogueSlider;

    private const string SOUNDS = "Sounds";
    private const string DIALOGUE = "Dialogues";

    private void Start()
    {
        LoadSlider(soundsSlider, SOUNDS);
        LoadSlider(dialogueSlider, DIALOGUE);

        soundsSlider.onValueChanged.AddListener(v => SetVolume(SOUNDS, v));
        dialogueSlider.onValueChanged.AddListener(v => SetVolume(DIALOGUE, v));
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