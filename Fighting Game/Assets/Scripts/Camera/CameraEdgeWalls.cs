using UnityEngine;
 
/// <summary>
/// カメラの画面の左右端に「見えない壁」を置き、プレイヤーが画面外へ出られないようにする。
/// 壁は物理コライダー（Kinematic）なので、押し合い・ノックバックとも自然に連動する。
/// カメラが限界まで引いている時だけ実質的に効く（それ以外は余白があるため触れない）。
/// 空の GameObject などに付けて、fightingCamera を設定するだけで動作する。
/// </summary>
public class CameraEdgeWalls : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private FightingCamera fightingCamera;

    [Header("壁の設定")]
    [SerializeField] private string wallLayerName = "Default";
    [SerializeField] private float wallThickness = 2.0f;
    [SerializeField] private float wallHeight = 40.0f;
    [SerializeField] private float wallDepth = 10.0f;
    [Tooltip("プレイヤーの Z 座標（Z 固定の値）")]
    [SerializeField] private float wallCenterZ = 0.0f;
    [Tooltip("画面端より内側に壁を寄せる量。見た目のはみ出しを防ぐ")]
    [SerializeField] private float edgeInset = 0.3f;

    private Rigidbody leftRb;
    private Rigidbody rightRb;
    private bool activated;

    private void Awake()
    {
        int layer = LayerMask.NameToLayer(wallLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"CameraEdgeWalls: レイヤー '{wallLayerName}' が見つからないため Default を使います");
            layer = 0;
        }

        // 摩擦ゼロ・反発ゼロ
        var mat = new PhysicMaterial("CameraWall")
        {
            dynamicFriction = 0.0f,
            staticFriction = 0.0f,
            bounciness = 0.0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine = PhysicMaterialCombine.Minimum
        };

        leftRb = CreateWall("CameraWall_Left", layer, mat);
        rightRb = CreateWall("CameraWall_Right", layer, mat);
    }

    private Rigidbody CreateWall(string wallName, int layer, PhysicMaterial mat)
    {
        var go = new GameObject(wallName);
        go.layer = layer;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(wallThickness, wallHeight, wallDepth);
        col.sharedMaterial = mat;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        go.SetActive(false); // カメラの準備ができるまで無効
        return rb;
    }

    private void FixedUpdate()
    {
        if (fightingCamera == null || !fightingCamera.IsReady) return;

        float y = fightingCamera.transform.position.y;
        Vector3 leftPos = new Vector3(fightingCamera.VisibleMinX + edgeInset - wallThickness * 0.5f, y, wallCenterZ);
        Vector3 rightPos = new Vector3(fightingCamera.VisibleMaxX - edgeInset + wallThickness * 0.5f, y, wallCenterZ);

        if (!activated)
        {
            // 初回は直接配置してから有効化
            leftRb.transform.position = leftPos;
            rightRb.transform.position = rightPos;
            leftRb.gameObject.SetActive(true);
            rightRb.gameObject.SetActive(true);
            activated = true;
            return;
        }

        leftRb.MovePosition(leftPos);
        rightRb.MovePosition(rightPos);
    }

    private void OnDestroy()
    {
        if (leftRb != null) Destroy(leftRb.gameObject);
        if (rightRb != null) Destroy(rightRb.gameObject);
    }
}