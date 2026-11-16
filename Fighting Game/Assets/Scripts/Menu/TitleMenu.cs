using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [Header("タイトルシーン名")]
    [SerializeField] private string titleSceneName = "Title";

    // タイトルシーンへ戻る
    public void GoToTitle()
    {
        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogError("タイトルシーン名が設定されていません。");
            return;
        }

        Debug.Log("タイトルシーンへ戻ります。");

        // ポーズ中でもシーンを切り替えられるようにする
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }
}
