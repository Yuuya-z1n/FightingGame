using UnityEngine;
using FightSim;
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
    }

    private float CalcGroundTopY()
    {
        if (groundObj == null)
        {
            Debug.LogError($"{name}: FighterView の groundObj が未設定です。");
            return 0f;
        }

        // Collider があれば、その上面を使う
        Collider col = groundObj.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.max.y;

        // Collider がない場合は、オブジェクトの原点を床の上面とみなす
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
    }

    private void ApplyAnimation(FighterSimState s)
    {
        // 直前が同じであれば早期リターン
        if (s.stateId == StateId) return;

        Debug.Log($"アニメーションが変わりました。{StateId} -> {s.stateId}");

        int crossFrame = 2;

        switch (s.stateId)
        {
            case FighterStateId.Idle:
                anim.CrossFade("Idle", ChangeFrameTime(crossFrame));
                break;

            case FighterStateId.WalkForward:
                anim.CrossFade("WalkForward", ChangeFrameTime(crossFrame));
                break;

            case FighterStateId.WalkBack:
                anim.CrossFade("WalkBack", ChangeFrameTime(crossFrame));
                break;

            case FighterStateId.Jump:
                anim.CrossFade("Jump", ChangeFrameTime(crossFrame));
                break;

            case FighterStateId.SpecialAttack1:
                anim.CrossFade("Test", ChangeFrameTime(crossFrame));
                Debug.Log("昇竜拳！");
                break;

            case FighterStateId.Down:
            case FighterStateId.KO:
                anim.CrossFade("Down", ChangeFrameTime(crossFrame));
                break;

            case FighterStateId.WakeUp:
                anim.CrossFade("WakeUp", ChangeFrameTime(crossFrame));
                break;
        }

    }
}