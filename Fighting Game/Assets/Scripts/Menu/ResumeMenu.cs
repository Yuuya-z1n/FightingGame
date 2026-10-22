using UnityEngine;
using UnityEngine.UI;

public class ResumeMenu : MonoBehaviour
{

    [Header("ゲームに戻るボタン")]
    [SerializeField] private Button resumeButton;

    [Header("時間停止を使用するか")]
    [SerializeField] private bool resumeTimeOnClose = true;

    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }
    }

    /// <summary>
    /// メニューを閉じてゲームに戻る
    /// </summary>
    public void ResumeGame()
    {
        if (resumeTimeOnClose)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    /// <summary>
    /// メニューを開いたときに、再開前の時間スケールを記録する
    /// </summary>
    public void SaveTimeScale()
    {
        previousTimeScale = Time.timeScale;
    }
}
