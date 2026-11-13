using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSettingsManager : MonoBehaviour
{
    [Header("音量設定パネル")]
    [SerializeField] private GameObject volumePanel;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("音量スライダー")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    private const string MasterKey = "MasterVolume";
    private const string BGMKey = "BGMVolume";
    private const string SEKey = "SEVolume";

    private void Awake()
    {
        volumePanel.SetActive(false);
        SetupSlider(masterSlider);
        SetupSlider(bgmSlider);
        SetupSlider(seSlider);

        float master = PlayerPrefs.GetFloat(MasterKey, 1f);
        float bgm = PlayerPrefs.GetFloat(BGMKey, 1f);
        float se = PlayerPrefs.GetFloat(SEKey, 1f);

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(master);

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(bgm);

        if (seSlider != null)
            seSlider.SetValueWithoutNotify(se);

        ApplyMasterVolume(master);
        ApplyBGMVolume(bgm);
        ApplySEVolume(se);

        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(ApplyMasterVolume);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(ApplyBGMVolume);

        if (seSlider != null)
            seSlider.onValueChanged.AddListener(ApplySEVolume);
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(ApplyMasterVolume);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(ApplyBGMVolume);

        if (seSlider != null)
            seSlider.onValueChanged.RemoveListener(ApplySEVolume);
    }

    private void SetupSlider(Slider slider)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void ApplyMasterVolume(float value)
    {
        SetMixerVolume("MasterVolume", value);
        SaveVolume(MasterKey, value);
    }

    private void ApplyBGMVolume(float value)
    {
        SetMixerVolume("BGMVolume", value);
        SaveVolume(BGMKey, value);
    }

    private void ApplySEVolume(float value)
    {
        SetMixerVolume("SEVolume", value);
        SaveVolume(SEKey, value);
    }

    private void SetMixerVolume(string parameter, float value)
    {
        if (audioMixer == null) return;

        float decibels = value <= 0.0001f
            ? -80f
            : Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(parameter, decibels);
    }

    private void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public void OpenVolumePanel()
    {
        volumePanel.SetActive(true);
    }

    public void CloseVolumePanel()
    {
        if (volumePanel != null)
            volumePanel.SetActive(false);
    }

    //public void OpenVolumePanel()
    //{
    //    if (volumePanel != null)
    //    {
    //        volumePanel.SetActive(true);
    //        Debug.Log("音量設定UIを開きました");
    //    }
    //    else
    //    {
    //        Debug.LogError("VolumePanelが設定されていません");
    //    }
    //}
}
