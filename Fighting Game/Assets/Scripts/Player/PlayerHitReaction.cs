using UnityEngine;
using static MyFunctions.GlobalFunctions;

public class PlayerHitReaction : MonoBehaviour
{
    private PlayerAttackHandler attackHandler;
    private float stunEndTime;

    public bool IsInHitStun => Time.time < stunEndTime;

    private void Awake()
    {
        attackHandler = GetComponent<PlayerAttackHandler>();
    }

    // frames: ヒットスタンのフレーム数（CSV の hitStun）
    public void EnterHitStun(int frames)
    {
        if (frames <= 0) return;

        // 攻撃中に被弾したら攻撃をキャンセルする
        if (attackHandler != null) attackHandler.CancelAttack();

        // 連続ヒット時は、毎回ヒットスタンをやり直す
        stunEndTime = Time.time + ChangeFrameTime(frames);

        Debug.Log($"{name} ヒットスタン {frames}F");
    }
}
