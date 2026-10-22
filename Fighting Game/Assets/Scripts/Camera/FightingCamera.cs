using UnityEngine;

/// <summary>
/// ・2人の中間点を横方向に追従
/// ・2人の距離に応じて 寄り / 引き を切り替え（常に2人が画面内に収まる）
/// ・ジャンプ中はズームせず、カメラを少し上に移動
/// Orthographic / Perspective どちらでも動作する。
/// </summary>
[RequireComponent(typeof(Camera))]
public class FightingCamera : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("ズーム")]
    [Tooltip("2人の外側に確保する余白。大きいほど引き気味になる")]
    [SerializeField] private float screenMargin = 2.0f;
    [SerializeField] private float zoomSmoothTime = 0.15f;

    //[Header("Orthographic の場合")]
    //[Tooltip("最も寄った時の Size")]
    //[SerializeField] private float minOrthoSize = 3.0f;
    //[Tooltip("最も引いた時の Size")]
    //[SerializeField] private float maxOrthoSize = 5.5f;

    [Header("カメラの寄り引きの設定")]
    [Tooltip("最も寄った時のプレイヤーまでの距離")]
    [SerializeField] private float minDistance = 6.0f;
    [Tooltip("最も引いた時のプレイヤーまでの距離")]
    [SerializeField] private float maxDistance = 12.0f;

    [Header("縦方向（ジャンプ時に少し上へ）")]
    [Tooltip("ジャンプの高さのうち、カメラが追従する割合（0〜1）")]
    [SerializeField] private float jumpFollowRate = 0.4f;
    [Tooltip("カメラが上に動く最大量")]
    [SerializeField] private float maxJumpOffset = 1.5f;
    [SerializeField] private float verticalSmoothTime = 0.15f;

    [Header("横方向")]
    [SerializeField] private float horizontalSmoothTime = 0.1f;

    [Header("ステージ範囲")]
    [SerializeField] private bool useStageBounds = false;
    [SerializeField] private Transform minusWall;
    [SerializeField] private Transform plusWall;

    public bool IsReady => initialized;
    public float VisibleMinX => transform.position.x - VisibleHalfWidth();
    public float VisibleMaxX => transform.position.x + VisibleHalfWidth();

    private class Tracked
    {
        public Transform trans;
        public PlayerMovement move;
        public float groundY; // 最後に接地していた時の Y
    }

    private Camera cam;
    private Tracked a;
    private Tracked b;

    private float baseY;        // カメラの基準の高さ
    private float zoom;         // Ortho: Size / Perspective: プレイヤーまでの距離
    private float xVel, yVel, zoomVel;
    private bool initialized;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogWarning("FightingCamera: player1 / player2 が未設定です");
            enabled = false;
            return;
        }

        a = CreateTracked(player1);
        b = CreateTracked(player2);
        baseY = transform.position.y;
    }

    private Tracked CreateTracked(Transform t)
    {
        return new Tracked
        {
            trans = t,
            move = t.GetComponent<PlayerMovement>(),
            groundY = t.position.y
        };
    }

    private void LateUpdate()
    {
        UpdateGroundY(a);
        UpdateGroundY(b);

        Vector3 pa = a.trans.position;
        Vector3 pb = b.trans.position;

        // ---- 目標値の計算 ----
        float targetX = (pa.x + pb.x) * 0.5f;
        float playerZ = (pa.z + pb.z) * 0.5f;

        // ズーム: 2人＋余白が画面幅に収まる大きさ
        float halfSpan = Mathf.Abs(pa.x - pb.x) * 0.5f + screenMargin;
        float targetZoom = CalcTargetZoom(halfSpan);

        // ジャンプ: 高く跳んでいる方の高さに応じて少し上へ
        float jumpHeight = Mathf.Max(JumpHeight(a), JumpHeight(b));
        float targetY = baseY + Mathf.Min(jumpHeight * jumpFollowRate, maxJumpOffset);

        // 適用
        if (!initialized)
        {
            zoom = targetZoom;
            initialized = true;
            ApplyPosition(ClampX(targetX), targetY, playerZ);
            ApplyZoom(playerZ);
            return;
        }

        zoom = Mathf.SmoothDamp(zoom, targetZoom, ref zoomVel, zoomSmoothTime);

        float clampedTargetX = ClampX(targetX);
        float newX = Mathf.SmoothDamp(transform.position.x, clampedTargetX, ref xVel, horizontalSmoothTime);
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref yVel, verticalSmoothTime);

        ApplyPosition(ClampX(newX), newY, playerZ);
        ApplyZoom(playerZ);
    }

    // 接地中だけ「地面の高さ」を更新する。ジャンプの高さ = 現在Y - 地面Y
    private void UpdateGroundY(Tracked t)
    {
        if (t.move != null && t.move.IsGrounded)
        {
            t.groundY = t.trans.position.y;
        }
    }

    private float JumpHeight(Tracked t)
    {
        return Mathf.Max(0.0f, t.trans.position.y - t.groundY);
    }

    private float CalcTargetZoom(float halfSpan)
    {
        //if (cam.orthographic)
        //{
        //    float needed = halfSpan / cam.aspect;
        //    return Mathf.Clamp(needed, minOrthoSize, maxOrthoSize);
        //}
        //else
        //{
            float tan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float needed = halfSpan / (tan * cam.aspect);
            return Mathf.Clamp(needed, minDistance, maxDistance);
        //}
    }

    // 現在のズームで画面に映る横幅の半分（プレイヤーの位置での値）
    private float VisibleHalfWidth()
    {
        if (cam.orthographic) return zoom * cam.aspect;
        return zoom * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;
    }

    private float ClampX(float x)
    {
        if (!useStageBounds || minusWall == null || plusWall == null) return x;

        float minEdge = GetInnerEdge(minusWall, true);
        float maxEdge = GetInnerEdge(plusWall, false);

        float half = VisibleHalfWidth();
        float min = minEdge + half;
        float max = maxEdge - half;

        if (min > max) return (minEdge + maxEdge) * 0.5f;
        return Mathf.Clamp(x, min, max);
    }

    // マイナス側の壁は右端、プラス側の壁は左端がステージの内側
    private static float GetInnerEdge(Transform wall, bool isMinusSide)
    {
        var col = wall.GetComponentInChildren<Collider>();
        if (col == null) return wall.position.x;
        return isMinusSide ? col.bounds.max.x : col.bounds.min.x;
    }

    private void ApplyPosition(float x, float y, float playerZ)
    {
        float z = cam.orthographic ? transform.position.z : playerZ - zoom;
        transform.position = new Vector3(x, y, z);
    }

    private void ApplyZoom(float playerZ)
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = zoom;
        }
    }
}