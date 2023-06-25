using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer _targetMixer;

    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _uiSlider;
    [SerializeField] private Slider _musicSlider;

    private void Start()
    {
        LoadMaster();
        LoadSFX();
        LoadUI();
        LoadMusic();
    }

    public void LoadMaster()
    {
        _masterSlider.value = LoadMixerGroup("Master");
    }

    public void LoadSFX()
    {
        _sfxSlider.value = LoadMixerGroup("SFX");
    }

    public void LoadUI()
    {
        _uiSlider.value = LoadMixerGroup("UI");
    }

    public void LoadMusic()
    {
        _musicSlider.value = LoadMixerGroup("Music");
    }

    public void SetMaster(float value)
    {
        SetMixerGroup("Master", value);
    }

    public void SetSFX(float value)
    {
        SetMixerGroup("SFX", value);
    }

    public void SetUI(float value)
    {
        SetMixerGroup("UI", value);
    }

    public void SetMusic(float value)
    {
        SetMixerGroup("Music", value);
    }

    private void SetMixerGroup(string name, float value)
    {
        _targetMixer.SetFloat(name, value);
        PlayerPrefs.SetFloat(name, value);
    }

    private float LoadMixerGroup(string name)
    {
        float value = PlayerPrefs.GetFloat(name, 0);

        _targetMixer.SetFloat(name, value);
        return value;
    }
}
