using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("メニュー")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button[] menuButtons;

    [Header("子メニュー")]
    [SerializeField] private GameObject[] childMenuPanels;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference menuAction;    //Menuボタン
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference decideAction;
    [SerializeField] private InputActionReference backAction;  // Bボタン

    [Header("選択色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    public bool openMenu;

    private int selectedIndex = 0;
    private int lastY = 0;
    [SerializeField] private DummySettings dummySettings;   // Inspectorで登録

    private List<IChildMenu> childMenus = new List<IChildMenu>();


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

        if (dummySettings != null)
            childMenus.Add(dummySettings);


        RefreshSelection();
    }

    // メニューが開いていて、メニュー操作中か
    public bool IsMenuOpen
    {
        get
        {
            return menuPanel != null && menuPanel.activeSelf;
        }
    }
    void Update()
    {
        //if (decideAction == null || decideAction.action == null)
        //{
        //    Debug.LogError("Decide Action が設定されていません");
        //}
        //else if (!decideAction.action.enabled)
        //{
        //    Debug.LogWarning("Decide Action が無効です");
        //}

        // Menuボタンでメニューを開閉
        if (Pressed(menuAction))
        {
            //Debug.Log("Menuボタンが押されました");
            ToggleMenu();
        }

        // Bボタンで親Menuに戻る
        // Bボタンで子メニューを閉じる、または親Menuに戻る
        if (Pressed(backAction))
        {
            // 子メニューが開いていれば、最初の1つを閉じる
            if (CloseActiveChildMenu())
            {
                Debug.Log("Bボタン：子メニューを閉じました");
                return;
            }

            // 子メニューが開いていなければ、親Menuを表示する
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
                openMenu = true;

                RefreshSelection();

                Debug.Log("Bボタン：Menuに戻りました");
            }

            return;
        }

        if (IsChildMenuOpen()) return;

        // メニューが閉じている場合は操作しない
        if (menuPanel == null || !menuPanel.activeSelf)return;

        // ゲームパッドから十字キー入力を直接取得
        Vector2 input = Vector2.zero;

        if (Gamepad.current != null)
        {
            input = Gamepad.current.dpad.ReadValue();
        }

        //// 十字キーが押されている場合だけログを表示
        //if (input.sqrMagnitude > 0.25f)
        //{
        //    Debug.Log("十字キー入力: " + input);
        //}

        // 上下の入力を判定
        int y = input.y > 0.5f ? 1 :
                input.y < -0.5f ? -1 : 0;

        // 押した瞬間に1回だけ選択を移動
        if (y != 0 && y != lastY)
        {
            MoveSelection(-y);
            //Debug.Log("選択中の項目番号: " + selectedIndex);
        }

        lastY = y;

        // Aボタンで選択した項目を決定
        if (Pressed(decideAction))
        {
      //      Debug.Log($"Menu Active: {menuPanel.activeSelf}, " +$"Action Enabled: {decideAction.action.enabled}");
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
        if (menuPanel == null) return;

        bool isOpening = !menuPanel.activeSelf;

        if (isOpening && childMenuPanels != null)
        {
            foreach (GameObject panel in childMenuPanels)
            {
                if (panel != null)
                    panel.SetActive(false);
            }
        }

        menuPanel.SetActive(isOpening);
        openMenu = isOpening;

        if (isOpening)
        {
            if (decideAction != null && decideAction.action != null)
                decideAction.action.Enable();

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
        Debug.Log("Decide() が呼び出されました");

        if (menuButtons == null)
        {
            Debug.LogError("menuButtons が設定されていません");
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= menuButtons.Length)
        {
            Debug.LogError("選択番号が範囲外です: " + selectedIndex);
            return;
        }

        Button button = menuButtons[selectedIndex];

        if (button == null)
        {
            Debug.LogError("選択中のボタンが未設定です");
            return;
        }

        if (!button.interactable)
        {
            Debug.LogWarning("選択中のボタンが操作できません");
            return;
        }

        Debug.Log("ボタン処理を実行します: " + button.name);
        button.onClick.Invoke();
        menuPanel.SetActive(false);
    }

    bool IsChildMenuOpen()
    {
        if (childMenuPanels == null)
            return false;

        foreach (GameObject panel in childMenuPanels)
        {
            if (panel != null && panel.activeInHierarchy)
                return true;
        }

        return false;
    }

    bool CloseActiveChildMenu()
    {
        bool closedAny = false;

        // 独自の閉じる処理を持つメニュー
        foreach (IChildMenu menu in childMenus)
        {
            if (menu == null || !menu.IsOpen)
                continue;

            menu.CloseMenu();
            closedAny = true;
            Debug.Log("子メニューを閉じました");
        }

        // 独自処理のない従来のパネル
        if (childMenuPanels != null)
        {
            foreach (GameObject panel in childMenuPanels)
            {
                if (panel != null && panel.activeSelf)
                {
                    panel.SetActive(false);
                    closedAny = true;
                    Debug.Log(panel.name + " を閉じました");
                }
            }
        }

        return closedAny;
    }
}
