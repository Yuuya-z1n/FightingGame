using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [Header("所有者")]
    [SerializeField] private GameObject owner;
    [SerializeField] private Rigidbody rb;

    [Header("ステータス")]
    [SerializeField] private int maxHealth = 10000;
    private int crntHealth;

    public GameObject Owner => owner;

    private void Awake()
    {
        if (owner == null) owner = gameObject;
        if (rb == null) rb = owner.GetComponent<Rigidbody>();
        crntHealth = maxHealth;
    }

    // ダメージを受けた時の処理
    public void TakeDamage(GameObject attackOwner, int damage, Vector2 knockback, float hitStopDuration)
    {
        crntHealth -= damage;
        Debug.Log($"{owner.name} が {damage} ダメージを受けた！");
    
        if(hitStopDuration > 0.0f && HitStopManager.Instance != null)
        {
            HitStopManager.Instance.TriggerHitStop(hitStopDuration);
        }

        ApplyKnockback(attackOwner, knockback);

        if(crntHealth <= 0)
        {
            Dead();
        }
    }

    private void ApplyKnockback(GameObject attackOwner, Vector2 knockback)
    {
        if (rb == null) return;

        rb.velocity = Vector3.zero;

        float direction = Mathf.Sign(transform.position.x - attackOwner.transform.position.x);
        if (direction == 0) direction = attackOwner.transform.forward.z >= 0 ? 1.0f : -1.0f;

        Vector3 force = new Vector3(knockback.x * direction, knockback.y, 0.0f);

        rb.AddForce(force, ForceMode.Impulse);
    }

    private void Dead()
    {
        // KO時のアニメーション

        Debug.Log($"{owner.name} は倒れた！");
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);

            // 枠線をくっきり表示する
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}