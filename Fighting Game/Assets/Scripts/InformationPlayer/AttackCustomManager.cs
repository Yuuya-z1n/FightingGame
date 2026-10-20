
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackCustomManager : MonoBehaviour
{
    [Header("ページ設定")]
    [SerializeField] private GameObject attackPage;
    [SerializeField] private GameObject specialMovePage;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference prevPageAction; // LT
    [SerializeField] private InputActionReference nextPageAction; // RT

    [Header("キーボード")]
    [SerializeField] private bool enableKeyboard = true;

    private InputAction kbNextPage;
    private InputAction kbPrevPage;

    // 0 = 通常技設定ページ
    // 1 = 必殺技設定ページ
    public int CurrentPage { get; private set; } = 0;

    private void Awake()
    {
        if (!enableKeyboard) return;

        kbPrevPage = new InputAction(
            "KbPrevPage",
            InputActionType.Button,
            "<Keyboard>/z"
        );

        kbNextPage = new InputAction(
            "KbNextPage",
            InputActionType.Button,
            "<Keyboard>/c"
        );
    }

    private void OnEnable()
    {
        EnableAction(prevPageAction);
        EnableAction(nextPageAction);

        kbPrevPage?.Enable();
        kbNextPage?.Enable();
    }

    private void OnDisable()
    {
        DisableAction(prevPageAction);
        DisableAction(nextPageAction);

        kbPrevPage?.Disable();
        kbNextPage?.Disable();
    }

    private void OnDestroy()
    {
        kbPrevPage?.Dispose();
        kbNextPage?.Dispose();
    }

    private void Start()
    {
        ShowPage(0);
    }



    private void Update()
    {
        // キーボード入力
        if (kbPrevPage != null && kbPrevPage.WasPressedThisFrame())
            PreviousPage();

        if (kbNextPage != null && kbNextPage.WasPressedThisFrame())
            NextPage();

        // Xbox系コントローラーの入力を直接確認
        if (Gamepad.current == null)
            return;

        if (Gamepad.current.leftTrigger.wasPressedThisFrame)
        {
            Debug.Log("LT入力を検出");
            PreviousPage();
        }

        if (Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            Debug.Log("RT入力を検出");
            NextPage();
        }
    }

    private static void EnableAction(InputActionReference reference)
    {
        if (reference != null && reference.action != null)
            reference.action.Enable();
    }

    private static void DisableAction(InputActionReference reference)
    {
        if (reference != null && reference.action != null)
            reference.action.Disable();
    }

    private static bool WasPressed(InputActionReference reference)
    {
        return reference != null &&
               reference.action != null &&
               reference.action.WasPressedThisFrame();
    }

    // 指定したページを表示する
    public void ShowPage(int pageIndex)
    {
        if (attackPage == null || specialMovePage == null)
        {
            Debug.LogError(
                "AttackCustomManager：ページをInspectorで設定してください。",
                this
            );
            return;
        }

        if (pageIndex < 0 || pageIndex > 1)
            return;

        CurrentPage = pageIndex;

        attackPage.SetActive(CurrentPage == 0);
        specialMovePage.SetActive(CurrentPage == 1);

        Debug.Log(
            "現在のページ：" +
            (CurrentPage == 0 ? "通常技設定" : "必殺技設定")
        );
    }

    // 次のページ
    public void NextPage()
    {
        ShowPage((CurrentPage + 1) % 2);
    }

    // 前のページ
    public void PreviousPage()
    {
        ShowPage((CurrentPage - 1 + 2) % 2);
    }

    // 現在のページ番号を取得
    public int GetCurrentPage()
    {
        return CurrentPage;
    }
}
