using UnityEngine;

/// <summary>
/// プレイヤーの本体コライダー（Is Trigger がオフのもの）に、
/// 摩擦ゼロ・反発ゼロの物理マテリアルを適用する。
/// プレハブの設定が失われても、プレイヤー同士や壁・床で「くっつく」「跳ねる」ことを防ぐ。
/// プレイヤーのルートにアタッチするだけで動作する。
/// </summary>
public class PlayerPhysicsMaterial : MonoBehaviour
{
    private static PhysicMaterial sharedMaterial;

    private void Awake()
    {
        if (sharedMaterial == null)
        {
            // Minimum は、相手のコライダーの設定が Average などでも優先されて 0 になる
            sharedMaterial = new PhysicMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0.0f,
                staticFriction = 0.0f,
                bounciness = 0.0f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
        }

        foreach (var c in GetComponentsInChildren<Collider>(true))
        {
            if (c.isTrigger) continue;          // HitBox / HurtBox は対象外
            if (c is CharacterController) continue;

            // 元のマテリアルを記録（なし = 設定が失われている状態）
            PhysicMaterial before = c.sharedMaterial;
            Debug.Log($"[PhysicsMaterial] {name}/{c.name}: 元のマテリアル=" +
                      (before != null
                          ? $"{before.name} (摩擦={before.dynamicFriction}, 反発={before.bounciness}, 合成={before.frictionCombine})"
                          : "なし（デフォルト: 摩擦0.6）"));

            c.sharedMaterial = sharedMaterial;
        }
    }
}
