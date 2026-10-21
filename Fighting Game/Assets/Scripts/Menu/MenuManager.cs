
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("メニュー")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button[] menuButtons;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference menuAction;
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference decideAction;

    [Header("選択色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    public bool openMenu;

    private int selectedIndex = 0;
    private int lastY = 0;

    void OnEnable()
    {
        Enable(menuAction);
        Enable(selectAction);
        Enable(decideAction);
    }

    void OnDisable()
    {
        Disable(menuAction);
        Disable(selectAction);
        Disable(decideAction);
    }

    static void Enable(InputActionReference r)
    {
        if (r != null) r.action.Enable();
    }

    static void Disable(InputActionReference r)
    {
        if (r != null) r.action.Disable();
    }

    void Start()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        RefreshSelection();
    }


    void Update()
    {
        // Menuボタンでメニューを開閉
        if (Pressed(menuAction))
        {
            Debug.Log("Menuボタンが押されました");
            ToggleMenu();
        }

        // メニューが閉じている場合は操作しない
        if (menuPanel == null || !menuPanel.activeSelf)
            return;

        // ゲームパッドから十字キー入力を直接取得
        Vector2 input = Vector2.zero;

        if (Gamepad.current != null)
        {
            input = Gamepad.current.dpad.ReadValue();
        }

        // 十字キーが押されている場合だけログを表示
        if (input.sqrMagnitude > 0.25f)
        {
            Debug.Log("十字キー入力: " + input);
        }

        // 上下の入力を判定
        int y = input.y > 0.5f ? 1 :
                input.y < -0.5f ? -1 : 0;

        // 押した瞬間に1回だけ選択を移動
        if (y != 0 && y != lastY)
        {
            MoveSelection(-y);
            Debug.Log("選択中の項目番号: " + selectedIndex);
        }

        lastY = y;

        // Aボタンで選択した項目を決定
        if (Pressed(decideAction))
        {
            Debug.Log("決定ボタンが押されました");
            Decide();
        }
    }

    static bool Pressed(InputActionReference r)
    {
        return r != null &&
               r.action != null &&
               r.action.WasPressedThisFrame();
    }

    void ToggleMenu()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);
        openMenu = !openMenu;
        if (menuPanel.activeSelf)
        {
            selectedIndex = 0;
            RefreshSelection();
        }
    }

    void MoveSelection(int direction)
    {
        if (menuButtons == null || menuButtons.Length == 0)
            return;

        selectedIndex =
            (selectedIndex + direction + menuButtons.Length)
            % menuButtons.Length;

        RefreshSelection();
    }

    void RefreshSelection()
    {
        if (menuButtons == null) return;

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;

            Image image = menuButtons[i].GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    i == selectedIndex ? selectedColor : normalColor;
            }
        }
    }

    void Decide()
    {
        if (menuButtons == null ||
            selectedIndex < 0 ||
            selectedIndex >= menuButtons.Length)
            return;

        Button button = menuButtons[selectedIndex];

        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
    }
}
