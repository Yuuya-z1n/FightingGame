using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFacingHandler : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform opponentTrans;

    [Header("状態")]
    public bool isFacingRight = true;
    public bool canTurn = true;

    // Update is called once per frame
    private void Update()
    {
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if(!canTurn || opponentTrans == null) return;

        // 相手より右なら右向き(true)
        bool shouldFaceRight = opponentTrans.position.x > transform.position.x;

        // 現在の向きと違う場合だけ反転する
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            // 向きを反転する処理
            OnDirectionChanged();
        }

    }

    private void OnDirectionChanged()
    {
        float yAngle = isFacingRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, yAngle, 0f);
        
        Debug.Log($"現在の向き: {(isFacingRight ? "右" : "左")}");
    }

    // 外部から相手をセットするためのメソッド
    public void SetOpponent(Transform opponent)
    {
        opponentTrans = opponent;
    }
}
