
using UnityEngine;
using UnityEngine.UI;

public class AttackCustomMenu : MonoBehaviour
{
    [Header("攻撃カスタム画面")]
    [SerializeField] private GameObject attackCustomPanel;

    [Header("ボタン")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }

    // 攻撃カスタム画面を開く
    public void OpenPanel()
    {
        if (attackCustomPanel != null)
            attackCustomPanel.SetActive(true);
    }

    // 攻撃カスタム画面を閉じる
    public void ClosePanel()
    {
        if (attackCustomPanel != null)
            attackCustomPanel.SetActive(false);
    }
}
