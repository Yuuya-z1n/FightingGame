using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ゲームパッドで「小/中/大」×「ダメージ/発生/硬直」を選び、
/// 各項目の選択肢ボタンから値を決める画面。
///   LB/RB   : 小・中・大の切り替え
///   D-pad↑↓ : 項目（ダメージ/発生/硬直）の切り替え
///   D-pad←→ : 選択肢の変更
///   A       : 決定（AttackPresetに反映して保存）
/// 注意: 対象ボタンの Navigation は None にしてください（UIの自動ナビゲーションと二重に動くため）。
/// </summary>
public class AttackCustom : MonoBehaviour
{ 
    [Serializable]
    public class ButtonRow
    {
        public Button[] buttons;     // 選択肢のボタン（左→右）
        public int[] optionValues;   // 各ボタンが表す実際の値（    ）
    }

    [Serializable]
    public class AttackBlock
    {
        public string label;         // 小攻撃 / 中攻撃 / 大攻撃
        public ButtonRow damage;     // 行0
        public ButtonRow startup;    // 行1（発生）
        public ButtonRow recovery;   // 行2（硬直）

        public ButtonRow GetRow(int i) => i == 0 ? damage : i == 1 ? startup : recovery;
    }
    
    static readonly string[] RowNames = { "ダメージ", "発生", "硬直" };

    [Header("0=小 / 1=中 / 2=大")]
    [SerializeField] AttackBlock[] attacks = new AttackBlock[3];
    [Header("通常技の持続フレーム数")]
    [SerializeField] public int smallAttackDuration;
    [SerializeField] public int mediumAttackDuration;
    [SerializeField] public int largeAttackDuration;

    [Header("Input Actions")]
    [SerializeField] InputActionReference prevAttackAction; // LB (Button)
    [SerializeField] InputActionReference nextAttackAction; // RB (Button)
    [SerializeField] InputActionReference navigateAction;   // D-pad (Vector2)
    [SerializeField] InputActionReference decideAction;     // A  (Button)

    [Header("キーボード")]
    [SerializeField] bool enableKeyboard = true;
    InputAction kbPrev, kbNext, kbNavigate, kbDecide;

    [Header("色")]
    [SerializeField] Color normalColor = Color.white;

    public int attackIndex = 0;  // 0=小 1=中 2=大
    public int rowIndex = 0;     // 0=ダメージ 1=発生 2=硬直

    // 各攻撃×各項目で選ばれているボタン番号
    readonly int[,] valueIndex = new int[3, 3]
    {
    { 1, 1, 1 }, // 小攻撃：ダメージ・発生・硬直
    { 1, 1, 1 }, // 中攻撃：ダメージ・発生・硬直
    { 1, 1, 1 }  // 大攻撃：ダメージ・発生・硬直
    };

    int lastX, lastY; // D-padのエッジ検出用

    [SerializeField] GameObject[] hideObjects;
    
    [SerializeField] private Color selectedColor = Color.red;

void Awake()
    {
        if (!enableKeyboard) return;
        
        kbPrev = new InputAction("KbPrevAttack", InputActionType.Button, "<Keyboard>/q");
        kbNext = new InputAction("KbNextAttack", InputActionType.Button, "<Keyboard>/e");
        kbDecide = new InputAction("KbDecide", InputActionType.Button, "<Keyboard>/space");
        kbNavigate = new InputAction("KbNavigate", InputActionType.Value);
        kbNavigate.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
    }

    void OnEnable()
    {
        Enable(prevAttackAction);
        Enable(nextAttackAction);
        Enable(navigateAction);
        Enable(decideAction);

        kbPrev?.Enable();
        kbNext?.Enable();
        kbNavigate?.Enable();
        kbDecide?.Enable();

    }

    void OnDisable()
    {
        kbPrev?.Disable();
        kbNext?.Disable();
        kbNavigate?.Disable();
        kbDecide?.Disable();
    }

    void OnDestory()
    {
        kbPrev?.Dispose();
        kbNext?.Dispose();
        kbNavigate?.Dispose();
        kbDecide?.Dispose();
    }

    static void Enable(InputActionReference r) { if (r != null) r.action.Enable(); }

    void Start()
    {
        // 最初は小攻撃を選択
        attackIndex = 0;

        // 最初はダメージ項目を選択
        rowIndex = 0;

        RefreshColors();
        SelectCurrentButton();
    }

    void Update()
    {
        //if (Gamepad.current != null) return;   
        if (Pressed(prevAttackAction,kbPrev)) ChangeAttack(-1);
        if (Pressed(nextAttackAction,kbNext)) ChangeAttack(1);

        // D-pad（Vector2）を「押した瞬間」だけ処理する
        Vector2 v = Vector2.zero;
        if (navigateAction != null) v += navigateAction.action.ReadValue<Vector2>();
        if (kbNavigate != null) v += kbNavigate.ReadValue<Vector2>();
        v = Vector2.ClampMagnitude(v, 1f); // 斜めの入力を正規化

        int x = Step(v.x);
        int y = Step(v.y);
        if (y != 0 && y != lastY) MoveRow(-y); // 上でrowIndexが減る
        if (x != 0 && x != lastX) ChangeValue(x);
        lastX = x;
        lastY = y;

        if (Pressed(decideAction,kbDecide)) Decide();
    }

    static bool Pressed(InputActionReference r,InputAction kb)
    {
        return (r != null && r.action.WasPressedThisFrame()) || (kb != null && kb.WasPressedThisFrame());
    }

    static bool IsTypeing()
    {
        var es = EventSystem.current;

        if (es == null || es.currentSelectedGameObject == null)
            return false;

        var input = es.currentSelectedGameObject.GetComponent<TMP_InputField>();

        return input != null && input.isFocused;
    }
    static int Step(float f) => f > 0.5f ? 1 : f < -0.5f ? -1 : 0;

    // ---------- 選択の移動 ----------

    // 選択している攻撃(小/中/大)の切り替え
    void ChangeAttack(int dir)
    {
        attackIndex = (attackIndex + dir + 3) % 3;
        SelectCurrentButton();
    }

    // 選択している項目(ダメージ/発生/硬直)の切り替え
    void MoveRow(int dir)
    {
        rowIndex = (rowIndex + dir + 3) % 3;
        SelectCurrentButton();
    }

    // 選択している項目の切り替え
    void ChangeValue(int dir)
    {
        var row = CurrentRow;
        if (row == null || row.buttons == null || row.buttons.Length == 0) return;

        int n = row.buttons.Length;
        valueIndex[attackIndex, rowIndex] = (valueIndex[attackIndex, rowIndex] + dir + n) % n;

        RefreshColors();
        SelectCurrentButton();
    }

    ButtonRow CurrentRow => attacks[attackIndex]?.GetRow(rowIndex);

    void SelectCurrentButton()
    {
        var row = CurrentRow;
        if (row == null || row.buttons == null || row.buttons.Length == 0) return;

        int i = Mathf.Clamp(valueIndex[attackIndex, rowIndex], 0, row.buttons.Length - 1);
        if (row.buttons[i] != null) row.buttons[i].Select();
    }

    /// <summary>選ばれている値のボタンだけ赤、他は通常色にする</summary>
    // 選択状態の見た目を更新する

    void RefreshColors()
    {
        if (attacks == null) return;

        for (int a = 0; a < attacks.Length; a++)
        {
            if (attacks[a] == null) continue;

            for (int r = 0; r < 3; r++)
            {
                ButtonRow row = attacks[a].GetRow(r);

                if (row == null || row.buttons == null) continue;

                for (int i = 0; i < row.buttons.Length; i++)
                {
                    Button button = row.buttons[i];
                    if (button == null) continue;

                    Image image = button.GetComponent<Image>();
                    if (image == null) continue;

                    // 画像全体の色は変更しない
                    image.color = Color.white;

                    // 選択中のボタンだけ中央を赤くする
                    Material material = image.material;

                    if (material != null &&
                        material.HasProperty("_Selected"))
                    {
                        material.SetFloat(
                            "_Selected",
                            a == attackIndex &&
                            r == rowIndex &&
                            i == valueIndex[a, r] ? 1f : 0f
                        );
                    }
                }
            }
        }
    }

    // ---------- 値の取得・反映 ----------


    public int GetValue(int a, int r)
    {
        // 攻撃番号の範囲チェック
        if (a < 0 || a >= 3)
            return 0;

        // 持続フレーム数を返す
        if (r == 3)
        {
            switch (a)
            {
                case 0:
                    return smallAttackDuration;

                case 1:
                    return mediumAttackDuration;

                case 2:
                    return largeAttackDuration;
            }
        }

        // 既存のダメージ・発生・硬直
        if (attacks == null || a >= attacks.Length ||
            attacks[a] == null || r < 0 || r > 2)
        {
            return 0;
        }

        var row = attacks[a].GetRow(r);

        if (row != null &&
            row.optionValues != null &&
            valueIndex[a, r] >= 0 &&
            valueIndex[a, r] < row.optionValues.Length)
        {
            return row.optionValues[valueIndex[a, r]];
        }

        return valueIndex[a, r];
    }

    void Decide()
    {
        Debug.Log(
            $"攻撃：{attacks[attackIndex].label} / " +
            $"項目：{RowNames[rowIndex]} / " +
            $"値：{GetValue(attackIndex, rowIndex)}"
        );

    }

    void HideUI()
    {
        foreach (GameObject obj in hideObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

}
