using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移動関連のパラメータ")]
    [SerializeField] public float moveSpeed = 5.0f;
    [SerializeField] public float backMoveSpeed = 4.5f;
    [SerializeField] private float jumpForce = 9.0f;
    [SerializeField] private float jumpForwardSpeed = 5.0f;
    [SerializeField] private float jumpBackSpeed = 4.5f;
    [SerializeField] private float gravityScale = 25.0f;

    [Header("地上での減速（入力なし・攻撃中・被押し時）")]
    [Tooltip("大きいほど押された後にすぐ止まる。小さいと滑る")]
    [SerializeField] private float groundDeceleration = 30.0f;

    [Header("接地判定")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer; // Player 自身のレイヤーは含めないこと

    private PlayerInputHandler inputHandler;
    private PlayerFacingHandler facingHandler;
    private Rigidbody rb;

    private Animator anim;

    private bool isGrounded;
    private bool wasGrounded;
    private float jumpCurrentXVelocity = 0f; // 空中で維持する横速度

    public bool IsGrounded => isGrounded;
    public bool isCanMove = true;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        facingHandler = GetComponent<PlayerFacingHandler>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        if (inputHandler == null) Debug.LogError("PlayerInputHandler が見つかりません");
        if (facingHandler == null) Debug.LogError("PlayerFacingHandler が見つかりません");
        if (rb == null) { Debug.LogError("Rigidbodyが見つかりません"); return; }
        if (groundCheck == null) Debug.LogError($"{name}: groundCheck が未設定です");
        if (groundLayer.value == 0) Debug.LogError($"{name}: groundLayer が未設定です");
        if (anim == null) Debug.Log("Animator が見つかりません");

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 重力は自前で適用する
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (rb == null || inputHandler == null) return;

        UpdateGrounded();

        Vector2 input = isCanMove ? inputHandler.moveInput : Vector2.zero;
        Vector3 vel = rb.velocity;
        float dt = Time.fixedDeltaTime;

        if (isGrounded)
        {
            if (input.y > 0.5f)
            {
                Jump(input.x, ref vel);
            }
            else if (input.y >= -0.5f && Mathf.Abs(input.x) > 0.2f)
            {
                vel.x = input.x * moveSpeed;
            }
            else
            {
                vel.x = Mathf.MoveTowards(vel.x, 0f, groundDeceleration * dt);
            }
        }
        else
        {
            // 空中: 攻撃中でも横速度と重力は維持する
            vel.x = jumpCurrentXVelocity;
        }

        // 重力
        vel.y -= gravityScale * dt;
        vel.z = 0f;

        rb.velocity = vel;
    }

    private void Jump(float inputX, ref Vector3 vel)
    {
        vel.y = jumpForce;

        isGrounded = false;
        wasGrounded = false;

        if (Mathf.Abs(inputX) > 0.2f)
        {
            bool facingRight = facingHandler == null || facingHandler.isFacingRight;
            bool isMovingForward = (facingRight && inputX > 0) || (!facingRight && inputX < 0);

            // 前飛び / 後ろ飛び
            float speed = isMovingForward ? jumpForwardSpeed : jumpBackSpeed;
            jumpCurrentXVelocity = Mathf.Sign(inputX) * speed;
        }
        else
        {
            // 垂直
            jumpCurrentXVelocity = 0.0f;
        }

        vel.x = jumpCurrentXVelocity;
    }

    private void UpdateGrounded()
    {
        wasGrounded = isGrounded;

        if (groundCheck == null) return;

        // 上昇中は接地扱いにしない
        bool rising = rb.velocity.y > 0.1f;

        isGrounded = !rising && Physics.CheckSphere(
            groundCheck.position, groundRadius, groundLayer,
            QueryTriggerInteraction.Ignore);

        if (isGrounded && !wasGrounded)
        {
            OnLanding();
        }
    }

    private void OnLanding()
    {
        jumpCurrentXVelocity = 0.0f;
        Debug.Log("着地した");
    }

    public string DebugGroundInfo()
    {
        if (groundCheck == null) return "groundCheck=null";
        var sb = new System.Text.StringBuilder();
        sb.Append($"groundCheck='{groundCheck.name}' pos={groundCheck.position} radius={groundRadius} mask={groundLayer.value} ");
        var hits = Physics.OverlapSphere(groundCheck.position, groundRadius, ~0, QueryTriggerInteraction.Collide);
        sb.Append($"重なっているコライダー({hits.Length}): ");
        foreach (var h in hits)
        {
            bool match = (groundLayer.value & (1 << h.gameObject.layer)) != 0;
            sb.Append($"[{h.name} layer={LayerMask.LayerToName(h.gameObject.layer)} trigger={h.isTrigger} 判定対象={match}] ");
        }
        return sb.ToString();
    }
}