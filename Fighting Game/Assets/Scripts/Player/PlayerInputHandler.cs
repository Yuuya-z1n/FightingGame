using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("プレイヤー設定")]
    [SerializeField] private int playerIndex = 0;

    private PlayerAction playerAction;

    public Vector2 moveInput { get; private set; }

    // 外部に攻撃発生の通知
    public event Action<AttackType> OnAttackPressed;

    private void Awake()
    {
        playerAction = new PlayerAction();

        playerAction.Player.Move.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
            Debug.Log($"入力: {moveInput}");
        };
        playerAction.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void Update()
    {
        Vector2 move = Vector2.zero;
        bool lightAttack = false;
        bool mediumAttack = false;
        bool heavyAttack = false;
        bool throwAttack = false;

        var gamepads = Gamepad.all;

        bool useGamepad = gamepads.Count > playerIndex;

        if (useGamepad)
        {
            var pad = gamepads[playerIndex];
            move = pad.leftStick.ReadValue();

            // デッドゾーン
            if (move.magnitude < 0.2f) move = Vector2.zero;

            // ボタン割り当て
            if (pad.buttonWest.wasPressedThisFrame) lightAttack = true;   
            if (pad.buttonSouth.wasPressedThisFrame) mediumAttack = true; 
            if (pad.buttonEast.wasPressedThisFrame) heavyAttack = true;   
            if (pad.buttonNorth.wasPressedThisFrame) throwAttack = true;  
        }
        else
        {
            // キーボード操作(コントローラー未接続時)
            if (playerIndex == 0)
            {
                // 1P の操作
                float x = 0f;
                if (Input.GetKey(KeyCode.D)) x += 1f;
                if (Input.GetKey(KeyCode.A)) x -= 1f;
                float y = 0f;
                if (Input.GetKey(KeyCode.W)) y += 1f;
                if (Input.GetKey(KeyCode.S)) y -= 1f;
                move = new Vector2(x, y);

                if (Input.GetKeyDown(KeyCode.J)) lightAttack = true;
                if (Input.GetKeyDown(KeyCode.K)) mediumAttack = true;
                if (Input.GetKeyDown(KeyCode.L)) heavyAttack = true;
                if (Input.GetKeyDown(KeyCode.M)) throwAttack = true;
            }
            else if (playerIndex == 1)
            {
                // 2P の操作
                float x = 0f;
                if (Input.GetKey(KeyCode.RightArrow)) x += 1f;
                if (Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                float y = 0f;
                if (Input.GetKey(KeyCode.UpArrow)) y += 1f;
                if (Input.GetKey(KeyCode.DownArrow)) y -= 1f;
                move = new Vector2(x, y);

                if (Input.GetKeyDown(KeyCode.Z)) lightAttack = true;
                if (Input.GetKeyDown(KeyCode.X)) mediumAttack = true;
                if (Input.GetKeyDown(KeyCode.C)) heavyAttack = true;
                if (Input.GetKeyDown(KeyCode.V)) throwAttack = true;
            }
        }

        moveInput = move;

        if (lightAttack) OnAttackPressed?.Invoke(AttackType.Light);
        if (mediumAttack) OnAttackPressed?.Invoke(AttackType.Medium);
        if (heavyAttack) OnAttackPressed?.Invoke(AttackType.Heavy);
        if (throwAttack) OnAttackPressed?.Invoke(AttackType.Throw);
    }

    private void OnEnable()
    {
        playerAction.Player.Enable();
    }

    private void OnDisable()
    {
        playerAction.Player.Disable();
    }


}
