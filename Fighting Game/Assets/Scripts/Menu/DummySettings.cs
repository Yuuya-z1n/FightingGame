using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.IO;
using System.Text;

public class DummySettings : MonoBehaviour , IChildMenu
{
    public enum GuardType
    {
        NoGuard,
        Guard,
        GuardAfterFirstHit
    }

    public enum HitType
    {
        Counter,
        PunishCounter,
        Normal
    }
    public enum ThrowEscape
    {
        On,
        Off
    }

    [SerializeField] private GameObject dummyPanel;

    [Header("メニュー操作")]
    [SerializeField] private InputActionReference menuAction;
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference decideAction;
    [SerializeField] private InputActionReference backAction;


    private Selectable[] settingsItems;
    private int selectedIndex;
    private int lastY;


    [Header("ガード設定")]
    [SerializeField] private TMP_Dropdown guardDropdown;

    [Header("ヒット設定")]
    [SerializeField] private TMP_Dropdown hitDropdown;

    [Header("投げ抜け設定")]
    [SerializeField] private Toggle throwEscapeToggle;

    [Header("設定結果の表示")]
    [SerializeField] private TMP_Text guardResultText;
    [SerializeField] private TMP_Text hitResultText;
   // [SerializeField] private TMP_Text throwEscapeResultText;

    public GuardType CurrentGuardType { get; private set; }
    public HitType CurrentHitType { get; private set; }
    public bool ThrowEscapeEnabled { get; private set; }

    public static bool IsDropdownOpen { get; private set; }

    private int lastToggleChangeFrame = -1;

    [Header("選択色")]
    [SerializeField] private Color NormalColor = Color.white;
    [SerializeField] private Color SelectedColor = Color.yellow;


    private const string ProjectFolderName = "Fighting Game";
    private const string CsvFolderName = "Assets/Resources/DummySystem";
    private const string CsvFileName = "DummySettings.csv";

    public bool IsOpen => dummyPanel != null && dummyPanel.activeSelf;

    void OnEnable()
    {
        Enable(menuAction);
        Enable(selectAction);
        Enable(decideAction);
        Enable(backAction);
    }

    void OnDisable()
    {
        Disable(menuAction);
        Disable(selectAction);
        Disable(decideAction);
        Disable(backAction);
        IsDropdownOpen = false;
    }

    static void Enable(InputActionReference r)
    {
        if (r != null) r.action.Enable();
    }

    static void Disable(InputActionReference r)
    {
        if (r != null) r.action.Disable();
    }

    private void Start()
    {

        dummyPanel.SetActive(false);
        if (guardDropdown == null ||
            hitDropdown == null ||
            throwEscapeToggle == null)
        {
            enabled = false;
            return;
        }

        guardDropdown.ClearOptions();
        guardDropdown.AddOptions(new List<string>
        {
            "ガードなし",
            "ガード",
            "初弾ヒット後はガード"
        });

        hitDropdown.ClearOptions();
        hitDropdown.AddOptions(new List<string>
        {
            "カウンター",
            "パニッシュカウンター",
            "通常ヒット"
        });

        guardDropdown.onValueChanged.AddListener(OnGuardChanged);
        hitDropdown.onValueChanged.AddListener(OnHitChanged);
        throwEscapeToggle.onValueChanged.AddListener(OnThrowEscapeChanged);

        OnGuardChanged(guardDropdown.value);
        OnHitChanged(hitDropdown.value);
        OnThrowEscapeChanged(throwEscapeToggle.isOn);

        settingsItems = new Selectable[]
        {
            guardDropdown,
            hitDropdown,
            throwEscapeToggle
        };

        selectedIndex = 0;
        RefreshSelection();
    }

    private void Update()
    {
        if (dummyPanel == null || !dummyPanel.activeSelf)
        {
            IsDropdownOpen = false;
            return;
        }

        IsDropdownOpen =
            (guardDropdown != null && guardDropdown.IsExpanded) ||
            (hitDropdown != null && hitDropdown.IsExpanded);

        // Dropdownを開いている間は、UnityのUI入力に操作を任せる
        if (IsDropdownOpen)
        {
            lastY = 0;
            return;
        }

        // ここから下は既存のUpdate()の処理を残す
        if (dummyPanel == null || !dummyPanel.activeSelf)
            return;

        Vector2 input = UnityEngine.InputSystem.Gamepad.current != null
            ? UnityEngine.InputSystem.Gamepad.current.dpad.ReadValue()
            : Vector2.zero;

        int y = input.y > 0.5f ? 1 :
                input.y < -0.5f ? -1 : 0;

        if (y != 0 && y != lastY)
        {
            selectedIndex = (selectedIndex - y + settingsItems.Length)
                % settingsItems.Length;

            RefreshSelection();
        }

        lastY = y;

        if (decideAction != null &&
            decideAction.action.WasPressedThisFrame() &&
            lastToggleChangeFrame != Time.frameCount)
        {
            SelectCurrentItem();
        }
    }

    private void RefreshSelection()
    {
        if (settingsItems == null)
            return;

        for (int i = 0; i < settingsItems.Length; i++)
        {
            Selectable item = settingsItems[i];

            if (item == null)
                continue;

            bool isSelected = i == selectedIndex;

            // 項目の背景色を変更する
            Image image = item.GetComponent<Image>();

            if (image != null)
            {
                image.color = isSelected
                    ? SelectedColor
                    : NormalColor;
            }

            // 項目内の文字色も変更する
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.color = Color.white;
            }
        }
    }

    private void SelectCurrentItem()
    {

        if (settingsItems == null || settingsItems.Length == 0)
            return;

        if (selectedIndex < 0 || selectedIndex >= settingsItems.Length)
            return;

        Selectable item = settingsItems[selectedIndex];

        if (item is TMP_Dropdown dropdown)
        {
            if (!dropdown.interactable)
                return;

            dropdown.Show();
        }
        else if (item is Toggle toggle)
        {
            if (!toggle.interactable)
                return;

            bool newValue = !toggle.isOn;

            toggle.SetIsOnWithoutNotify(newValue);
            OnThrowEscapeChanged(newValue);

        }
    }


    private void OnDestroy()
    {
        if (guardDropdown != null)
            guardDropdown.onValueChanged.RemoveListener(OnGuardChanged);

        if (hitDropdown != null)
            hitDropdown.onValueChanged.RemoveListener(OnHitChanged);

        if (throwEscapeToggle != null)
            throwEscapeToggle.onValueChanged.RemoveListener(OnThrowEscapeChanged);
    }


    private void OnGuardChanged(int index)
    {
        if (!System.Enum.IsDefined(typeof(GuardType), index))
            return;

        CurrentGuardType = (GuardType)index;

        if (guardResultText != null)
            guardResultText.text = guardDropdown.options[index].text;
    }

    private void OnHitChanged(int index)
    {
        if (!System.Enum.IsDefined(typeof(HitType), index))
            return;

        CurrentHitType = (HitType)index;

        if (hitResultText != null)
            hitResultText.text = hitDropdown.options[index].text;
    }


    private void OnThrowEscapeChanged(bool isOn)
    {
        ThrowEscapeEnabled = isOn;
        lastToggleChangeFrame = Time.frameCount;

    }
    public void OpendummyPanel()
    {
        if (dummyPanel == null)
            return;

        dummyPanel.SetActive(true);

        if (selectAction != null && selectAction.action != null)
            selectAction.action.Enable();

        if (decideAction != null && decideAction.action != null)
            decideAction.action.Enable();

        selectedIndex = 0;
        RefreshSelection();
    }

    public void CloseMenu()
    {
        ClosedummyPanel();
    }

    public void ClosedummyPanel()
    {
        if (dummyPanel == null)
            return;

        ExportSettingsToCsv();
        dummyPanel.SetActive(false);
    }

    private static string GetCsvFilePath()
    {
        string desktopPath = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.DesktopDirectory
        );

        return Path.Combine(
            desktopPath,
            "卒業研究",
            ProjectFolderName,
            CsvFolderName,
            CsvFileName
        );
    }
    private void ExportSettingsToCsv()
    {
        string filePath = GetCsvFilePath();

        string folderPath = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(folderPath);

        StringBuilder csv = new StringBuilder();

        csv.AppendLine("Setting,Value");
        csv.AppendLine($"HitType,{CurrentHitType}");
        csv.AppendLine($"GuardType,{CurrentGuardType}");
        csv.AppendLine($"ThrowEscapeEnabled,{ThrowEscapeEnabled.ToString().ToLowerInvariant()}");

        File.WriteAllText(
            filePath,
            csv.ToString(),
            new UTF8Encoding(true)
        );

        Debug.Log("ダミー設定をCSVに保存しました: " + filePath);
    }
}