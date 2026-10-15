using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;

public class PlayerMovement : MonoBehaviour
{
    [Header("移動関連のパラメータ")]
    [SerializeField] public float moveSpeed = 5.0f;
    [SerializeField] public float jumpForce = 12.0f;
    [SerializeField] public float jumpForwardSpeed = 5.0f;
    [SerializeField] public float jumpBackSpeed = 4.5f;
    [SerializeField] private float gravityScale = 25.0f;

    [Header("接地判定")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private PlayerInputHandler inputHandler;
    private PlayerFacingHandler facingHandler;

    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;
    private float jumpCurrentXVelocity = 0f; // 空中で維持する横速度
    
    public bool isCanMove = true;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        facingHandler = GetComponent<PlayerFacingHandler>();
        if (inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler が見つかりません");
        }
        if (facingHandler == null)
        {
            Debug.LogError("PlayerFacingHandler が見つかりません");
        }
    }


    private void Update()
    {
        MoveUpdate();
        GroundCheck();
   }

    // 移動処理
    private void MoveUpdate()
    {
        if(!isCanMove) return;

        Vector2 input = inputHandler.moveInput;

        if(isGrounded)
        {
            if(input.y > 0.5f)
            {
                Jump(input.x);
                return;
            }

            // しゃがみ状態なら動かない
            if (input.y < -0.5f) return;


            if (Mathf.Abs(input.x) > 0.2f)
            {
                //Debug.Log($"移動中");
                transform.Translate(Vector3.right * input.x * moveSpeed * Time.deltaTime, Space.World);
            }
        }
        else
        {
            transform.Translate(Vector3.right * jumpCurrentXVelocity * Time.deltaTime, Space.World);

            // 重力適用
            velocity.y -= gravityScale * Time.deltaTime;
            transform.Translate(Vector3.up * velocity.y * Time.deltaTime, Space.World);
        }
    }

    private void Jump(float inputX)
    {
        // 上方向初速
        velocity.y = jumpForce;

        isGrounded = false;
        wasGrounded = false;

        if(Mathf.Abs(inputX) > 0.2f)
        {
            bool isMovingForward = (facingHandler.isFacingRight && inputX > 0) || (!facingHandler.isFacingRight && inputX < 0);
            
            if(isMovingForward)
            {
                // 前飛び
                jumpCurrentXVelocity = Mathf.Sign(inputX) * jumpForwardSpeed;
            }
            else
            {
                // 後ろ飛び
                jumpCurrentXVelocity = Mathf.Sign(inputX) * jumpBackSpeed;
            }
        
        }
        else
        {
            // 垂直
            jumpCurrentXVelocity = 0.0f;
        }

        // ジャンプ中は振り返りをロック
        facingHandler.canTurn = false;
    }

    private void GroundCheck()
    {
        if (groundCheck == null) return;

        if(velocity.y > 0)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            OnLanding();
        }

        // 接地している場合、Y方向の落下速度を一定にリセット
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 次フレームのために現在の情報を保持
        wasGrounded = isGrounded;
    }

    private void OnLanding()
    {
        jumpCurrentXVelocity = 0.0f;

        facingHandler.canTurn = true;

        Debug.Log("着地した");
    }
}
