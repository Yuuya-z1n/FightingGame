using UnityEngine;
using UnityEngine.UI;

public class ExitGameMenu : MonoBehaviour
{
    [Header("ゲーム終了ボタン")]
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void OnDestroy()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
        }
    }

    public void ExitGame()
    {
        Debug.Log("ゲームを終了します。");

#if UNITY_EDITOR
        // Unityエディターで実行中の場合
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ビルドしたゲームの場合
        Application.Quit();
#endif
    }
}
