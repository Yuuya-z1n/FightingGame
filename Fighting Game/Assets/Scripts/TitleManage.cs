using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TitleManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneButton
    {
        public Button button;
        public string sceneName;
    }

    [Header("Input Actions")]
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference decideAction;

    [Header("シーン移動ボタン")]
    [SerializeField] private SceneButton[] sceneButtons;

    [Header("選択色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private int selectedIndex = 0;
    private int lastY = 0;

    void OnEnable()
    {
        Enable(selectAction);
        Enable(decideAction);
    }

    void OnDisable()
    {
        Disable(selectAction);
        Disable(decideAction);
    }

    static void Enable(InputActionReference r)
    {
        if (r != null && r.action != null)
            r.action.Enable();
    }

    static void Disable(InputActionReference r)
    {
        if (r != null && r.action != null)
            r.action.Disable();
    }

    private void Start()
    {
        // 各ボタンにシーン移動処理を登録
        foreach (SceneButton item in sceneButtons)
        {
            if (item == null || item.button == null)
                continue;

            string targetScene = item.sceneName;

            item.button.onClick.AddListener(() =>
            {
                ChangeScene(targetScene);
            });
        }

        RefreshSelection();
    }

    private void Update()
    {
        if (sceneButtons == null || sceneButtons.Length == 0)
            return;

        // 十字キー入力
        Vector2 input = Vector2.zero;

        if (Gamepad.current != null)
        {
            input = Gamepad.current.dpad.ReadValue();
        }

        int y = input.y > 0.5f ? 1 :
                input.y < -0.5f ? -1 : 0;

        // 選択移動
        if (y != 0 && y != lastY)
        {
            MoveSelection(-y);
        }

        lastY = y;

        // Aボタンを押した瞬間だけ決定
        if (decideAction != null &&
            decideAction.action != null &&
            decideAction.action.WasPressedThisFrame())
        {
            Debug.Log("決定ボタンが押されました");
            Decide();
        }
    }
    // 選択位置を移動
    private void MoveSelection(int direction)
    {
        selectedIndex =
            (selectedIndex + direction + sceneButtons.Length)
            % sceneButtons.Length;

        RefreshSelection();
    }

    // 選択色を更新
    private void RefreshSelection()
    {
        if (sceneButtons == null)
            return;

        for (int i = 0; i < sceneButtons.Length; i++)
        {
            if (sceneButtons[i] == null ||
                sceneButtons[i].button == null)
                continue;

            Image image =
                sceneButtons[i].button.GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    i == selectedIndex ? selectedColor : normalColor;
            }
        }
    }

    // 選択したボタンを決定
    private void Decide()
    {
        if (selectedIndex < 0 ||
            selectedIndex >= sceneButtons.Length)
            return;

        SceneButton item = sceneButtons[selectedIndex];

        if (item == null || item.button == null)
            return;

        if (!item.button.interactable)
            return;

        Debug.Log("Aボタンで決定：" + item.button.name);

        item.button.onClick.Invoke();
    }
    // シーンを切り替える
    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("移動先のシーン名が設定されていません");
            return;
        }

        Debug.Log("シーンを移動します：" + sceneName);

        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}