
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MessageWindow : MonoBehaviour
{
    public static MessageWindow instance;

    [Header("薄くするUIの親")]
    [SerializeField] private GameObject targetUIObject;

    [Header("暗転用Image（任意）")]
    [SerializeField] private Image dimPanelImage;

    [Header("メッセージを閉じた後の移動先")]
    [SerializeField] private string nextSceneName = "TrainingScene";

    [SerializeField] private GameObject nextButton;

    private CanvasGroup targetCanvasGroup;
    private TextMeshProUGUI messageText;
    private Image windowImage;
    private bool waitingForClick;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        windowImage = GetComponent<Image>();

        Transform textTransform =
            transform.Find("MessageWindowText");

        if (textTransform != null)
        {
            messageText =
                textTransform.GetComponent<TextMeshProUGUI>();
        }

        if (targetUIObject != null)
        {
            targetCanvasGroup =
                targetUIObject.GetComponent<CanvasGroup>();

            if (targetCanvasGroup == null)
            {
                targetCanvasGroup =
                    targetUIObject.AddComponent<CanvasGroup>();
            }
        }

        HideMessage();
        RestoreTargetUI();
    }

    // 保存成功時に呼び出す
    public void ShowSaveMessage()
    {
        waitingForClick = true;

        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = 0.35f;
            targetCanvasGroup.interactable = false;
            targetCanvasGroup.blocksRaycasts = false;
        }

        if (dimPanelImage != null)
        {
            dimPanelImage.gameObject.SetActive(true);

            Color color = dimPanelImage.color;
            color.a = 0.65f;
            dimPanelImage.color = color;
        }

        if (messageText != null)
        {
            messageText.text =
                "保存しました！\nクリックして次へ";
            SetAlpha(messageText, 1f);
        }

        if (windowImage != null)
        {
            SetAlpha(windowImage, 1f);
        }
        if (nextButton != null)
        {
            nextButton.SetActive(true);
        }
    }

    // メッセージウィンドウのButtonから呼び出す
    public void CloseMessageAndMove()
    {
        if (!waitingForClick)
        {
            return;
        }

        waitingForClick = false;

        HideMessage();
        RestoreTargetUI();

        SceneManager.LoadScene(nextSceneName);
    }

    private void HideMessage()
    {
        SetAlpha(windowImage, 0f);
        SetAlpha(messageText, 0f);

        if (dimPanelImage != null)
        {
            Color color = dimPanelImage.color;
            color.a = 0f;
            dimPanelImage.color = color;
            dimPanelImage.gameObject.SetActive(false);
        }
        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }
    }

    private void RestoreTargetUI()
    {
        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = 1f;
            targetCanvasGroup.interactable = true;
            targetCanvasGroup.blocksRaycasts = true;
        }
    }

    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
