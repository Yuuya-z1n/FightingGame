using FightSim;
using UnityEngine;
using static MyFunctions.GlobalFunctions;

/// <summary>
/// キャラクターの「見た目」担当。シミュレーションの状態を Transform に反映するだけで、
/// 移動や判定のロジックは持たない。Rigidbody / Collider は不要
/// 将来アニメーションを付ける時は、PushState で受け取る stateId を Animator に渡す。
/// </summary>
public class FighterView : MonoBehaviour
{
    [Header("座標の対応")]
    [Tooltip("床のオブジェクト。Collider の上面を、sim の地面(0)として使う")]
    [SerializeField] private Transform groundObj;

    [Tooltip("キャラの原点が足元より上にある場合の補正（ワールド単位）。" +
             "原点が足元なら 0、体の中心なら足元から原点までの高さ")]
    [SerializeField] private float pivotOffsetY = 0f;

    [Header("アニメーター")]
    [SerializeField] private Animator anim;

    // 奥行き（Z）はシーン上の初期値で固定
    private float fixedZ;

    // 床の上面のワールド Y（起動時に1回だけ求める）
    private float groundTopY;

    private Vector3 prevPos;
    private Vector3 currPos;

    // プレイヤーが2P側にいるかどうかでアニメーションを反転させる
    private static readonly int mirrorHash = Animator.StringToHash("Mirror");
    private bool hasMirrorParam;

    // FightingCamera などから参照される
    public bool IsGrounded { get; private set; }
    public bool FacingRight { get; private set; }
    public FighterStateId StateId { get; private set; }

    private void Awake()
    {
        fixedZ = transform.position.z;
        groundTopY = CalcGroundTopY();
        anim = GetComponentInChildren<Animator>();
        if (anim == null) { Debug.LogError("FighterView の Animator が見当たりません。"); return; }

        hasMirrorParam = HasBoolParam(anim, mirrorHash);
        if(!hasMirrorParam) Debug.LogWarning($"{name}: Animator に Bool パラメータ \"Mirror\" がありません。");
    }

    private static bool HasBoolParam(Animator a, int hash)
    {
        foreach(AnimatorControllerParameter p in a.parameters)
        {
            if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Bool) return true;
        }
        return false;
    }

    private float CalcGroundTopY()
    {
        if (groundObj == null)
        {
            Debug.LogError($"{name}: FighterView の groundObj が未設定です。");
            return 0f;
        }

        // Collider があれば上面を使う
        Collider col = groundObj.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.max.y;

        // Collider がない場合はオブジェクトの原点を床の上面とみなす
        Debug.LogWarning($"{name}: groundObj に Collider がないため、原点の Y を地面として使います。");
        return groundObj.position.y;
    }

    // sim 座標をワールド座標に変換する
    public Vector3 ToWorld(float simX, float simY)
    {
        return new Vector3(
            simX / SimConst.unitsPerWorld,
            groundTopY + pivotOffsetY + simY / SimConst.unitsPerWorld,
            fixedZ);
    }

    // 位置を即座に合わせる（開始時・リセット時）
    public void Snap(FighterSimState s)
    {
        currPos = ToWorld(s.posX, s.posY);
        prevPos = currPos;
        ApplyDiscrete(s);
        transform.position = currPos;
        AnimationChanger("Idle");
    }

    // シミュレーションが1ティック進むたびに呼ぶ
    public void PushState(FighterSimState s)
    {
        prevPos = currPos;
        currPos = ToWorld(s.posX, s.posY);
        ApplyAnimation(s);
        ApplyDiscrete(s);
    }

    // 毎フレーム呼ぶ。60Hz のティックの間を補間して滑らかに表示する
    public void Render(float alpha)
    {
        transform.position = Vector3.Lerp(prevPos, currPos, alpha);
    }

    private void ApplyDiscrete(FighterSimState s)
    {
        IsGrounded = s.Grounded;
        FacingRight = s.facingRight;
        StateId = s.stateId;
        transform.rotation = Quaternion.Euler(0f, s.facingRight ? 0f : 180f, 0f);
        if (hasMirrorParam) anim.SetBool(mirrorHash, !s.facingRight);
    }

    private void ApplyAnimation(FighterSimState s)
    {
        // 直前が同じであれば早期リターン
        if (s.stateId == StateId) return;

        Debug.Log($"アニメーションが変わりました。{StateId} -> {s.stateId}");
        

        switch (s.stateId)
        {
            // 立ち
            case FighterStateId.Idle:
                AnimationChanger("Idle");
                break;

            // 前歩き
            case FighterStateId.WalkForward:
                AnimationChanger("WalkForward");
                break;

            // 後ろ歩き
            case FighterStateId.WalkBack:
                AnimationChanger("WalkBack");
                break;

            // 垂直飛び
            case FighterStateId.Jump:
                AnimationChanger("Jump");
                break;

            // 前飛び
            case FighterStateId.ForwardJump:
                AnimationChanger("ForwardJump");
                break;

            // 後ろ飛び
            case FighterStateId.BackJump:
                AnimationChanger("BackJump");
                break;

            // 昇竜拳
            case FighterStateId.SpecialAttack1:
                AnimationChanger("SpecialAttack1");
                Debug.Log("昇竜拳！");
                break;

            // しゃがむ
            case FighterStateId.Crouch:
                AnimationChanger("Crouch");
                break;

            // 倒れる
            case FighterStateId.Down:
            case FighterStateId.KO:
                AnimationChanger("Down");
                break;

            // 起き上がる
            case FighterStateId.WakeUp:
                AnimationChanger("WakeUp");
                break;

            // 投げ
            case FighterStateId.ThrowAttack:
                AnimationChanger("Throw");
                break;

            // 投げられ
            case FighterStateId.Thrown:
                AnimationChanger("Throw_Uke");
                break;

            // 前ステップ
            case FighterStateId.ForwardStep:
                AnimationChanger("ForwardStep");
                break;

            // 後ろステップ
            case FighterStateId.BackStep:
                AnimationChanger("BackStep");
                break;

            // 勝利ポーズ
            case FighterStateId.Win:
                AnimationChanger("Win");
                break;

        }

    }

    private void AnimationChanger(string name)
    {
        int stateHash = Animator.StringToHash(name);
        int layerIndex = 0;
        if (anim.HasState(layerIndex, stateHash))
        {
            int crossFrame = 3;
            anim.CrossFade(name, ChangeFrameTime(crossFrame));
        }
        else
        {
            Debug.LogError($"指定されたアニメーションが見つかりません {name}");
        }
    }
}