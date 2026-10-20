using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpecialCustom : MonoBehaviour
{
    [Serializable]
    public class ButtonRow
    {
        public Button[] buttons;
        public int[] optionValues;
    }

    [Serializable]
    public class SpecialAttackBlock
    {
        public string label;

        public ButtonRow damage;
        public ButtonRow startup;
        public ButtonRow recovery;

        [Header("持続フレーム数")]
        public int specialAttacsDuration = 8;

        public ButtonRow GetRow(int index)
        {
            if (index == 0) return damage;
            if (index == 1) return startup;
            if (index == 2) return recovery;

            return null;
        }
    }

    [Header("必殺技の設定")]
    [SerializeField]
    SpecialAttackBlock[] specialAttacks =
        new SpecialAttackBlock[3];

    [Header("Input Actions")]
    [SerializeField] InputActionReference prevAttackAction;
    [SerializeField] InputActionReference nextAttackAction;
    [SerializeField] InputActionReference navigateAction;
    [SerializeField] InputActionReference decideAction;

    [Header("キーボード")]
    [SerializeField] bool enableKeyboard = true;

    InputAction kbPrev;
    InputAction kbNext;
    InputAction kbNavigate;
    InputAction kbDecide;

    [Header("色")]
    [SerializeField] Color selectedColor = Color.red;
    [SerializeField] Color normalColor = Color.white;

    public int attackIndex = 0;
    public int rowIndex = 0;

    // 各必殺技の各項目の初期選択位置
    readonly int[,] valueIndex = new int[3, 3]
    {
        { 1, 1, 1 },
        { 1, 1, 1 },
        { 1, 1, 1 }
    };

    int lastX;
    int lastY;

    void Awake()
    {
        if (!enableKeyboard) return;

        kbPrev = new InputAction(
            "KbPrevAttack",
            InputActionType.Button,
            "<Keyboard>/q");

        kbNext = new InputAction(
            "KbNextAttack",
            InputActionType.Button,
            "<Keyboard>/e");

        kbDecide = new InputAction(
            "KbDecide",
            InputActionType.Button,
            "<Keyboard>/space");

        kbNavigate = new InputAction(
            "KbNavigate",
            InputActionType.Value);

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
        Disable(prevAttackAction);
        Disable(nextAttackAction);
        Disable(navigateAction);
        Disable(decideAction);

        kbPrev?.Disable();
        kbNext?.Disable();
        kbNavigate?.Disable();
        kbDecide?.Disable();
    }

    void OnDestroy()
    {
        kbPrev?.Dispose();
        kbNext?.Dispose();
        kbNavigate?.Dispose();
        kbDecide?.Dispose();
    }

    static void Enable(InputActionReference action)
    {
        if (action != null)
            action.action.Enable();
    }

    static void Disable(InputActionReference action)
    {
        if (action != null)
            action.action.Disable();
    }

    void Start()
    {
        attackIndex = 0;
        rowIndex = 0;

        RefreshColors();
        SelectCurrentButton();
    }

    void Update()
    {
        if (Pressed(prevAttackAction, kbPrev))
            ChangeAttack(-1);
        else if (Pressed(nextAttackAction, kbNext))
            ChangeAttack(1);

        Vector2 input = Vector2.zero;

        if (navigateAction != null)
            input += navigateAction.action.ReadValue<Vector2>();

        if (kbNavigate != null)
            input += kbNavigate.ReadValue<Vector2>();

        input = Vector2.ClampMagnitude(input, 1f);

        int x = Step(input.x);
        int y = Step(input.y);

        if (y != 0 && y != lastY)
            MoveRow(-y);

        if (x != 0 && x != lastX)
            ChangeValue(x);

        lastX = x;
        lastY = y;

        if (Pressed(decideAction, kbDecide))
            Decide();
    }

    static bool Pressed(
        InputActionReference action,
        InputAction keyboardAction)
    {
        return
            (action != null &&
             action.action.WasPressedThisFrame()) ||
            (keyboardAction != null &&
             keyboardAction.WasPressedThisFrame());
    }

    static int Step(float value)
    {
        return value > 0.5f ? 1 :
               value < -0.5f ? -1 : 0;
    }

    void ChangeAttack(int direction)
    {
        if (specialAttacks == null ||
            specialAttacks.Length == 0)
            return;

        attackIndex =
            (attackIndex + direction + specialAttacks.Length)
            % specialAttacks.Length;

        RefreshColors();
        SelectCurrentButton();
    }

    void MoveRow(int direction)
    {
        rowIndex = (rowIndex + direction + 3) % 3;

        RefreshColors();
        SelectCurrentButton();
    }

    void ChangeValue(int direction)
    {
        ButtonRow row = CurrentRow;

        if (row == null ||
            row.buttons == null ||
            row.buttons.Length == 0)
            return;

        int count = row.buttons.Length;

        valueIndex[attackIndex, rowIndex] =
            (valueIndex[attackIndex, rowIndex] + direction + count)
            % count;

        RefreshColors();
        SelectCurrentButton();
    }

    ButtonRow CurrentRow
    {
        get
        {
            if (specialAttacks == null ||
                attackIndex < 0 ||
                attackIndex >= specialAttacks.Length ||
                specialAttacks[attackIndex] == null)
                return null;

            return specialAttacks[attackIndex].GetRow(rowIndex);
        }
    }

    void SelectCurrentButton()
    {
        ButtonRow row = CurrentRow;

        if (row == null ||
            row.buttons == null ||
            row.buttons.Length == 0)
            return;

        int index = Mathf.Clamp(
            valueIndex[attackIndex, rowIndex],
            0,
            row.buttons.Length - 1);

        if (row.buttons[index] != null)
            row.buttons[index].Select();
    }

    void RefreshColors()
    {
        if (specialAttacks == null)
            return;

        for (int a = 0; a < specialAttacks.Length; a++)
        {
            if (specialAttacks[a] == null)
                continue;

            for (int r = 0; r < 3; r++)
            {
                ButtonRow row = specialAttacks[a].GetRow(r);

                if (row == null || row.buttons == null)
                    continue;

                for (int i = 0; i < row.buttons.Length; i++)
                {
                    Button button = row.buttons[i];

                    if (button == null || button.image == null)
                        continue;

                    button.image.color =
                        i == valueIndex[a, r]
                        ? selectedColor
                        : normalColor;
                }
            }
        }
    }

    /// <summary>
    /// row:
    /// 0 = ダメージ
    /// 1 = 発生
    /// 2 = 硬直
    /// 3 = 持続
    /// </summary>
    public int GetValue(int attack, int row)
    {
        if (specialAttacks == null ||
            attack < 0 ||
            attack >= specialAttacks.Length ||
            specialAttacks[attack] == null)
        {
            return 0;
        }

        // 持続フレーム数
        if (row == 3)
        {
            return specialAttacks[attack].specialAttacsDuration;
        }

        // ダメージ・発生・硬直
        if (row < 0 || row > 2)
            return 0;

        ButtonRow setting =
            specialAttacks[attack].GetRow(row);

        int index = valueIndex[attack, row];

        if (setting != null &&
            setting.optionValues != null &&
            index >= 0 &&
            index < setting.optionValues.Length)
        {
            return setting.optionValues[index];
        }

        return index;
    }

    void Decide()
    {
        string[] rowNames =
        {
            "ダメージ",
            "発生",
            "硬直"
        };

        Debug.Log(
            $"必殺技：{specialAttacks[attackIndex].label} / " +
            $"項目：{rowNames[rowIndex]} / " +
            $"値：{GetValue(attackIndex, rowIndex)}"
        );
    }
}
