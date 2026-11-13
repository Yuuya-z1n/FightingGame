using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MyFunctions.GlobalFunctions;

public class HitBox : MonoBehaviour
{
    private int crntDamage = 10;
    private Vector2 crntKnockback = new Vector2(2f, 1f);
    private int crntHitStop = 3;

    [Header("所有者情報")]
    [SerializeField] private GameObject owner;

    [Header("自身のHurtBox(自傷防止)")]
    [SerializeField] private HurtBox myHurtBox;

    private Collider col;

    private void OnTriggerEnter(Collider other)
    {
        // 接触相手の HurtBox を取得
        if (other.TryGetComponent<HurtBox>(out var targetHurtBox))
        {
            // 自分自身の食らい判定でないか確認
            if (targetHurtBox.Owner == this.owner) return;

            // 相手にダメージを与える
            targetHurtBox.TakeDamage(owner, crntDamage, crntKnockback, ChangeFrameTime(crntHitStop));
        }
    }

    private void Awake()
    {
        col = GetComponent<Collider>();

        if (owner == null) owner = transform.root.gameObject;
    }

    public void SetAttackData(int damage, Vector2 knockback, int hitStopDuration = 3)
    {
        crntDamage = damage;
        crntKnockback = knockback;
        crntHitStop = hitStopDuration;
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

    public void ApplyAttackData(AttackData data)
    {
        crntDamage = data.damage;
        crntKnockback = new Vector2(data.knockbackX, data.knockbackY);
        crntHitStop = data.hitStop;

        if (col == null) col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            box.center = new Vector3(data.hitboxOffsetX, data.hitboxOffsetY, 0f);
            box.size = new Vector3(data.hitboxSizeX, data.hitboxSizeY, box.size.z);
        }
    }
}