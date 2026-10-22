using static MyFunctions.GlobalFunctions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHandler : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerMovement movement;

    [Header("CSV設定")]
    [SerializeField] private string csvResourceName = "test"; // あとで Attack_Data に変更

    [Header("攻撃判定（HitBox）")]
    [SerializeField] private HitBox attackHitBox; // 打撃全般用
    [SerializeField] private HitBox throwHitBox;  // 投げ用

    private Dictionary<string, AttackData> attackDataBase;
    private bool isAttacking = false;
    private Coroutine attackCoroutine;
    private PlayerFacingHandler facingHandler;

    private void Awake()
    {
        if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (facingHandler == null) facingHandler = GetComponent<PlayerFacingHandler>();

        attackDataBase = AttackDataLoader.LoadAttackDataFromCSV(csvResourceName);

        DisableAllHitBoxes();
    }

    private void OnEnable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnAttackPressed += HandleAttack;
        }
    }

    private void OnDisable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnAttackPressed -= HandleAttack;
        }
    }

    private void HandleAttack(AttackType strength)
    {
        if (isAttacking || (movement != null && !movement.isCanMove)) return;

        AttackData crntAttack = GetAttackData(strength);

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine = StartCoroutine(AttackSequence(crntAttack));

        Vector2 dir = inputHandler.moveInput;
        Debug.Log($"【{strength}攻撃】レバー方向: {dir} | 発生:{crntAttack.startup}F");
    }

    private IEnumerator AttackSequence(AttackData data)
    {
        facingHandler.canTurn = false;
        isAttacking = true;
        if (movement != null) movement.isCanMove = false;

        // CSVの hitBoxName に応じて attackHitBox か throwHitBox を選択
        HitBox targetHitBox = GetHitBoxByName(data.hitBoxName);
        Collider targetCollider = (targetHitBox != null) ? targetHitBox.GetComponent<Collider>() : null;

        // 発生
        yield return new WaitForSeconds(ChangeFrameTime(data.startup));

        // 持続
        if (targetCollider != null)
        {
            targetHitBox.ApplyAttackData(data);
            targetCollider.enabled = true;
        }
        yield return new WaitForSeconds(ChangeFrameTime(data.active));

        // HitBox無効化
        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }

        // 硬直
        yield return new WaitForSeconds(ChangeFrameTime(data.recovery));

        if (movement != null) movement.isCanMove = true;
        isAttacking = false;
        attackCoroutine = null;
        facingHandler.canTurn = true;
    }

    // CSVからデータを取り出す
    private AttackData GetAttackData(AttackType type)
    {
        string key = type.ToString();

        if (attackDataBase != null && attackDataBase.TryGetValue(key, out AttackData data))
        {
            return data;
        }

        Debug.LogWarning($"CSV内部に {key} のデータが見つかりません。");
        return new AttackData { startup = 4, active = 2, recovery = 6, damage = 10, hitBoxName = "Attack_HitBox" };
    }

    // CSVの hitBoxName に基づいてコンポーネントを返す
    private HitBox GetHitBoxByName(string name)
    {
        if (throwHitBox != null && throwHitBox.gameObject.name == name) return throwHitBox;
        return attackHitBox; // デフォルトはすべて打撃用 HitBox
    }

    private void SetHitBoxEnabled(HitBox hitBox, bool state)
    {
        if (hitBox != null)
        {
            Collider col = hitBox.GetComponent<Collider>();
            if (col != null) col.enabled = state;
        }
    }

    private void DisableAllHitBoxes()
    {
        SetHitBoxEnabled(attackHitBox, false);
        SetHitBoxEnabled(throwHitBox, false);
    }

}