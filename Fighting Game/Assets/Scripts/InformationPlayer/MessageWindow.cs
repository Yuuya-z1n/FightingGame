
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageWindow : MonoBehaviour
{
    public static MessageWindow instance;

    [Header("薄くしたいUIの親GameObject")]
    [SerializeField] private GameObject targetUIObject;

    [Header("画面全体を暗くするImage（任意）")]
    [SerializeField] private Image dimPanelImage;

    private CanvasGroup targetCanvasGroup;
    private Coroutine coroutine;

    private TextMeshProUGUI text;
    private Image windowImage;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // メッセージウィンドウの背景Imageを取得
        windowImage = GetComponent<Image>();

        // 子オブジェクトからTextMeshProUGUIを取得
        Transform textTransform =
            transform.Find("MessageWindowText");

        if (textTransform != null)
        {
            text = textTransform.GetComponent<TextMeshProUGUI>();
        }
        else if (transform.childCount > 0)
        {
            text = transform.GetChild(0)
                .GetComponent<TextMeshProUGUI>();
        }

        if (windowImage == null)
        {
            Debug.LogError(
                "MessageWindowImageにImageがありません。",
                this
            );
        }

        if (text == null)
        {
            Debug.LogError(
                "MessageWindowTextが見つかりません。",
                this
            );
        }

        // 指定されたGameObjectからCanvasGroupを取得
        if (targetUIObject != null)
        {
            targetCanvasGroup =
                targetUIObject.GetComponent<CanvasGroup>();

            // CanvasGroupがなければ自動追加
            if (targetCanvasGroup == null)
            {
                targetCanvasGroup =
                    targetUIObject.AddComponent<CanvasGroup>();
            }
        }
        else
        {
            Debug.LogError(
                "Target UI ObjectにGameObjectを設定してください。",
                this
            );
        }

        // メッセージを初期状態で非表示にする
        SetAlpha(windowImage, 0f);
        SetAlpha(text, 0f);       

        // UIを元の状態にする
        RestoreTargetUI();
    }

    // 外部からメッセージを表示する
    public void SetMessage(string str, float sec)
    {
        // 自分自身と親オブジェクトが有効か確認
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "MessageWindowImageまたは親オブジェクトが非アクティブです。",
                this
            );
            return;
        }

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        coroutine = StartCoroutine(
            UpdateMessageWindow(str, sec)
        );
    }
    // メッセージウィンドウを更新する
    private IEnumerator UpdateMessageWindow(
        string str,
        float sec)
    {
        if (windowImage == null || text == null)
        {
            coroutine = null;
            yield break;
        }

        // 対象のUIを薄くして操作を無効化
        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = 0.35f;
            targetCanvasGroup.interactable = false;
            targetCanvasGroup.blocksRaycasts = false;
        }

        // 暗転パネルを表示
        if (dimPanelImage != null)
        {
            dimPanelImage.gameObject.SetActive(true);
            SetAlpha(dimPanelImage, 0.65f);
        }

        // メッセージを表示
        text.text = str;

        SetAlpha(windowImage, 1f);
        SetAlpha(text, 1f);

        // 指定秒数待機
        yield return new WaitForSecondsRealtime(sec);

        // メッセージを非表示
        SetAlpha(windowImage, 0f);
        SetAlpha(text, 0f);

        // 暗転パネルを非表示
        if (dimPanelImage != null)
        {
            SetAlpha(dimPanelImage, 0f);
            dimPanelImage.gameObject.SetActive(false);
        }

        // 対象UIを元に戻す
        RestoreTargetUI();

        coroutine = null;
    }

    // 対象のUIを通常状態に戻す
    private void RestoreTargetUI()
    {
        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = 1f;
            targetCanvasGroup.interactable = true;
            targetCanvasGroup.blocksRaycasts = true;
        }
    }

    // ImageやTextの透明度を変更する
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
