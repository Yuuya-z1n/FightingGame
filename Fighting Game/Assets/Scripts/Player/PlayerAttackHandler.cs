using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHandler : MonoBehaviour
{
    private PlayerInputHandler inputHandler;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        // 入力クラスのイベントを監視する
        if (inputHandler != null)
        {
            inputHandler.OnAttackPressed += HandleAttack;
        }
    }

    private void OnDisable()
    {
        // オブジェクトが無効化されたら解除する
        if (inputHandler != null)
        {
            inputHandler.OnAttackPressed -= HandleAttack;
        }
    }

    // 実際の攻撃処理
    private void HandleAttack(AttackType strength)
    {
        Vector2 dir = inputHandler.moveInput;

        // ここでアシスト判定や方向キー判定、アニメーション呼び出しを行う
        Debug.Log($"【{strength}攻撃】レバー方向: {dir}");
    }
}
