using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 診断用。プレイヤーのルートに付けて実行すると、1秒ごとに移動に関わる状態をログに出す。
/// 原因の特定が終わったら削除してよい。
/// </summary>
public class PlayerDebugProbe : MonoBehaviour
{
    private PlayerMovement movement;
    private PlayerInputHandler input;
    private PlayerHitReaction hitReaction;
    private Rigidbody rb;

    private int fixedCalls;
    private float nextLogTime;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputHandler>();
        hitReaction = GetComponent<PlayerHitReaction>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Debug.Log($"[Probe {name}] 起動時 timeScale={Time.timeScale} fixedDeltaTime={Time.fixedDeltaTime} " +
                  $"movement={(movement != null)} input={(input != null)} rb={(rb != null)} hitReaction={(hitReaction != null)}");
    }

    // timeScale が 0 だと呼ばれない。呼び出し回数が 0 なら時間が止まっている
    private void FixedUpdate()
    {
        fixedCalls++;
    }

    // timeScale に関係なく毎フレーム呼ばれる
    private void Update()
    {
        if (Time.unscaledTime < nextLogTime) return;
        nextLogTime = Time.unscaledTime + 1.0f;

        string moveInput = input != null ? input.moveInput.ToString() : "null";
        string canMove = movement != null ? movement.isCanMove.ToString() : "null";
        string grounded = movement != null ? movement.IsGrounded.ToString() : "null";
        string moveSpeed = movement != null ? movement.moveSpeed.ToString("F1") : "null";
        string stun = hitReaction != null ? hitReaction.IsInHitStun.ToString() : "なし";
        string vel = rb != null ? rb.velocity.ToString("F2") : "null";
        string kin = rb != null ? rb.isKinematic.ToString() : "null";
        string cons = rb != null ? rb.constraints.ToString() : "null";

        Debug.Log($"[Probe {name}] timeScale={Time.timeScale} FixedUpdate回数/秒={fixedCalls} " +
                  $"入力={moveInput} isCanMove={canMove} grounded={grounded} moveSpeed={moveSpeed} " +
                  $"hitStun={stun} vel={vel} kinematic={kin} constraints={cons} " +
                  $"ゲームパッド数={Gamepad.all.Count} enabled={enabled}");

        // 接地していない時だけ、接地判定の詳細を出す
        if (movement != null && !movement.IsGrounded)
        {
            Debug.Log($"[Probe {name}] 接地判定: {movement.DebugGroundInfo()}");
        }

        fixedCalls = 0;
    }
}