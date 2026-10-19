using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [Header("所有者（この判定を持っているプレイヤーなど）")]
    [SerializeField] private GameObject owner;

    public GameObject Owner => owner;

    // ダメージを受けた時の処理
    public void TakeDamage(int damage, Vector2 knockback)
    {
        Debug.Log($"{owner.name} が {damage} ダメージを受けた！");
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