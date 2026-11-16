using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

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
    private const string BGMKey = "BgmVolume";
    private const string SEKey = "SeVolume";
    [Header("入力設定")]
    [SerializeField] private InputActionReference navigateAction;

    [Header("スライダー操作")]
    [SerializeField] private float volumeStep = 0.05f;

    private int selectedIndex = 0;

    private Slider[] sliders;

    private void Awake()
    {
        if (volumePanel != null)
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

        sliders = new Slider[]
        {
            masterSlider,
            bgmSlider,
            seSlider
        };

        RefreshSliderSelection();
    }
    private void OnEnable()
    {
        if (navigateAction != null)
        {
            navigateAction.action.Enable();
            navigateAction.action.performed += OnNavigate;
        }
    }

    private void OnDisable()
    {
        if (navigateAction != null)
        {
            navigateAction.action.performed -= OnNavigate;
            navigateAction.action.Disable();
        }
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

        // Unity標準の左右ナビゲーションを無効化
        Navigation navigation = slider.navigation;
        navigation.mode = Navigation.Mode.None;
        slider.navigation = navigation;
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
    private void OnNavigate(InputAction.CallbackContext context)
    {
        // パネルが開いていない場合は操作しない
        if (volumePanel == null || !volumePanel.activeInHierarchy)
            return;

        if (sliders == null || sliders.Length == 0)
            return;

        Vector2 input = context.ReadValue<Vector2>();

        // 上下入力：スライダーの選択切り替え
        if (input.y > 0.5f)
        {
            selectedIndex--;
            RefreshSliderSelection();
            return;
        }
        else if (input.y < -0.5f)
        {
            selectedIndex++;
            RefreshSliderSelection();
            return;
        }

        // 左右入力：選択中のスライダーだけ変更
        if (input.x > 0.5f)
        {
            ChangeVolume(volumeStep);
        }
        else if (input.x < -0.5f)
        {
            ChangeVolume(-volumeStep);
        }
    }

    private void ChangeVolume(float amount)
    {
        if (sliders == null || sliders.Length == 0)
            return;

        Slider slider = sliders[selectedIndex];

        if (slider == null)
            return;

        slider.value = Mathf.Clamp(
            slider.value + amount,
            slider.minValue,
            slider.maxValue
        );
    }

    private void RefreshSliderSelection()
    {
        if (sliders == null || sliders.Length == 0)
            return;

        // 選択番号を0～2の範囲に収める
        selectedIndex = (selectedIndex + sliders.Length) % sliders.Length;

        // nullでないスライダーを選択する
        if (sliders[selectedIndex] != null)
        {
            sliders[selectedIndex].Select();
        }
    }

    private void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public void OpenVolumePanel()
    {
        if (volumePanel == null)
            return;

        volumePanel.SetActive(true);

        // パネルを開いたらMasterスライダーを選択
        selectedIndex = 0;
        RefreshSliderSelection();
    }

    public void CloseVolumePanel()
    {
        if (volumePanel != null)
            volumePanel.SetActive(false);
    }
}