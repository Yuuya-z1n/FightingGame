using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PresetManager : MonoBehaviour
{
    [Header("Preset Settings")]
    [SerializeField] private Preset[] presets;

    [Header("Preset Panels")]
    //[SerializeField] private GameObject presetPanel;
    [SerializeField] private RectTransform[] presetCards;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference decideAction;

    [Header("Position Settings")]
    [SerializeField] private float sideDistance = 400f;
    [SerializeField] private float moveDuration = 0.25f;

    [Header("Scale Settings")]
    [SerializeField] private float centerScale = 1f;
    [SerializeField] private float sideScale = 0.7f;

    [Header("Display Settings")]
    [SerializeField] private float sideAlpha = 0.4f;

    private int selectedIndex = 0;
    private int lastX = 0;

    private Coroutine moveCoroutine;

    public TextAsset SelectedPreset { get; private set; }

    private void OnEnable()
    {
        //if (moveAction != null)
        //    moveAction.action.Enable();

        if (decideAction != null)
            decideAction.action.Enable();
    }

    //private void OnDisable()
    //{
    //    if (moveAction != null)
    //        moveAction.action.Disable();

    //    if (decideAction != null)
    //        decideAction.action.Disable();
    //}

    private void Start()
    {
        RefreshCards(true);
    }

    private void Update()
    {
        //if (presetPanel == null ||
        //    !presetPanel.activeInHierarchy)
        //    return;

        MovePreset();

        if (decideAction != null &&
            decideAction.action.WasPressedThisFrame())
        {
            DecidePreset();
        }
    }

    private void MovePreset()
    {
        if (presets == null || presets.Length == 0)
            return;

        Vector2 input = Vector2.zero;

        // ゲームパッドの十字キー
        if (Gamepad.current != null)
        {
            input = Gamepad.current.dpad.ReadValue();
        }

        // キーボードにも対応
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed ||
                Keyboard.current.aKey.isPressed)
            {
                input.x = -1;
            }
            else if (Keyboard.current.rightArrowKey.isPressed ||
                     Keyboard.current.dKey.isPressed)
            {
                input.x = 1;
            }
        }

        int x = input.x > 0.5f ? 1 :
                input.x < -0.5f ? -1 : 0;

        if (x != 0 && x != lastX)
        {
            selectedIndex =
                (selectedIndex + x + presets.Length)
                % presets.Length;

            Debug.Log("Preset Selected Index: " + selectedIndex);

            RefreshCards(false);
        }

        lastX = x;
    }
    private void RefreshCards(bool instant)
    {
        if (presetCards == null ||
            presets == null ||
            presetCards.Length != presets.Length)
        {
            Debug.LogWarning("プリセット数とパネル数を一致させてください");
            return;
        }

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(
            AnimateCards(instant)
        );
    }

    private IEnumerator AnimateCards(bool instant)
    {
        Vector2[] startPositions = new Vector2[presetCards.Length];
        Vector2[] targetPositions = new Vector2[presetCards.Length];

        Vector3[] startScales = new Vector3[presetCards.Length];
        Vector3[] targetScales = new Vector3[presetCards.Length];

        CanvasGroup[] groups = new CanvasGroup[presetCards.Length];

        for (int i = 0; i < presetCards.Length; i++)
        {
            int distance = i - selectedIndex;

            // 左右の最短距離を計算
            if (distance > presets.Length / 2)
                distance -= presets.Length;

            if (distance < -presets.Length / 2)
                distance += presets.Length;

            float x = distance * sideDistance;

            startPositions[i] = presetCards[i].anchoredPosition;
            startScales[i] = presetCards[i].localScale;

            targetPositions[i] = new Vector2(x, 0f);

            targetScales[i] =
                Vector3.one *
                (distance == 0 ? centerScale : sideScale);

            groups[i] = presetCards[i].GetComponent<CanvasGroup>();

            if (groups[i] == null)
                groups[i] = presetCards[i].gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;

        while (!instant && elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < presetCards.Length; i++)
            {
                presetCards[i].anchoredPosition =
                    Vector2.Lerp(startPositions[i], targetPositions[i], t);

                presetCards[i].localScale =
                    Vector3.Lerp(startScales[i], targetScales[i], t);

                int distance = i - selectedIndex;

                if (distance > presets.Length / 2)
                    distance -= presets.Length;

                if (distance < -presets.Length / 2)
                    distance += presets.Length;

                float alpha = distance == 0 ? 1f : sideAlpha;

                groups[i].alpha = alpha;
            }

            yield return null;
        }

        for (int i = 0; i < presetCards.Length; i++)
        {
            presetCards[i].anchoredPosition = targetPositions[i];
            presetCards[i].localScale = targetScales[i];

            int distance = i - selectedIndex;

            if (distance > presets.Length / 2)
                distance -= presets.Length;

            if (distance < -presets.Length / 2)
                distance += presets.Length;

            groups[i].alpha = distance == 0 ? 1f : sideAlpha;
        }

        moveCoroutine = null;
    }

    private void DecidePreset()
    {
        if (presets == null ||
            selectedIndex >= presets.Length)
            return;

        Preset selected = presets[selectedIndex];

        if (selected == null ||
            selected.presetSettings == null)
        {
            Debug.LogWarning("CSVファイルが設定されていません");
            return;
        }

        SelectedPreset = selected.presetSettings;

        Debug.Log("選択したプリセット：" + selected.presetName);
        Debug.Log("CSV：" + SelectedPreset.name);
    }
}