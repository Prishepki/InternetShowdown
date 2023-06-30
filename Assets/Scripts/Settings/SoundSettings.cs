using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    private const string MASTER_NAME = "Master";
    private const string SFX_NAME = "SFX";
    private const string UI_NAME = "UI";
    private const string MUSIC_NAME = "Music";

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
        _masterSlider.value = LoadMixerGroup(MASTER_NAME);
    }

    public void LoadSFX()
    {
        _sfxSlider.value = LoadMixerGroup(SFX_NAME);
    }

    public void LoadUI()
    {
        _uiSlider.value = LoadMixerGroup(UI_NAME);
    }

    public void LoadMusic()
    {
        _musicSlider.value = LoadMixerGroup(MUSIC_NAME);
    }

    public void SetMaster(float value)
    {
        SetMixerGroup(MASTER_NAME, value);
    }

    public void SetSFX(float value)
    {
        SetMixerGroup(SFX_NAME, value);
    }

    public void SetUI(float value)
    {
        SetMixerGroup(UI_NAME, value);
    }

    public void SetMusic(float value)
    {
        SetMixerGroup(MUSIC_NAME, value);
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
