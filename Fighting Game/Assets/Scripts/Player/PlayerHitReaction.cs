using UnityEngine;
using static MyFunctions.GlobalFunctions;

/// <summary>
/// 被弾時のヒットスタン（食らい硬直）を管理する。
/// ・ヒットスタン中は IsInHitStun が true になる（移動・攻撃・ジャンプ入力を受け付けない）
/// ・被弾した瞬間に、出していた攻撃を中断する
/// 時間は Time.time（timeScale の影響を受ける）で数えるので、ヒットストップ中は進まない。
/// ステートマシンを導入したら、このクラスの中身は「食らい状態」に置き換える想定。
/// </summary>
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
