using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [Header("攻撃パラメータ")]
    [SerializeField] private int damage = 10;
    [SerializeField] private Vector2 knockback = new Vector2(2f, 1f);

    [Header("自身のHurtBox")]
    [SerializeField] private HurtBox myHurtBox;

    private Collider col;

    private void OnTriggerEnter(Collider other)
    {
        // 接触相手の HurtBox を取得
        if (other.TryGetComponent<HurtBox>(out var targetHurtBox))
        {
            // 自分自身の食らい判定でないか確認
            if (targetHurtBox == myHurtBox) return;

            // 相手にダメージを与える
            targetHurtBox.TakeDamage(damage, knockback);
        }
    }

    private void OnDrawGizmos()
    {
        if (col == null) col = GetComponent<Collider>();

        if (col == null || !col.enabled) return;

        // ON(有効) の時だけ赤色半透明で描画する
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);

            // 枠線をくっきり表示する
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}