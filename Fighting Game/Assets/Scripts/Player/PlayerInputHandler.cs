using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
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

        playerAction.Player.LightAttack.started += ctx => OnAttackPressed?.Invoke(AttackType.Light);
        playerAction.Player.MiddleAttack.started += ctx => OnAttackPressed?.Invoke(AttackType.Medium);
        playerAction.Player.HeavyAttack.started += ctx => OnAttackPressed?.Invoke(AttackType.Heavy);
        playerAction.Player.Throw.started += ctx => OnAttackPressed?.Invoke(AttackType.Throw);
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
