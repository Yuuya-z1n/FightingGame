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

    private PlayerMovement plMove;
    private Rigidbody rb;

    private void Awake()
    {
        plMove = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (opponentTrans == null)
        {
            foreach (var other in FindObjectsOfType<PlayerFacingHandler>())
                if (other != this) { opponentTrans = other.transform; break; }
            if (opponentTrans == null) Debug.LogWarning($"{name}: opponent が未設定です");
        }
        ApplyRotation();
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateFacing();
        ApplyRotation();
    }

    private void UpdateFacing()
    {
        if (opponentTrans == null) return;
        if (plMove != null && !plMove.IsGrounded) return;
        if (!canTurn || opponentTrans == null) return;

        // 相手より右なら右向き(true)
        float dx = opponentTrans.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;

        isFacingRight = dx > 0f;
    }

    private void ApplyRotation()
    {
        float targetY = isFacingRight ? 0.0f : 180.0f;
        if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetY)) < 0.1f) return;

        Quaternion q = Quaternion.Euler(0.0f, targetY, 0.0f);
        transform.rotation = q;
        if (rb != null) rb.rotation = q;
    }

    // 外部から相手をセットするためのメソッド
    public void SetOpponent(Transform opponent)
    {
        opponentTrans = opponent;
    }
}
