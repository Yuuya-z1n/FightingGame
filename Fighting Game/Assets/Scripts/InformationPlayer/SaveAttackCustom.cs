using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SaveAttackCustom : MonoBehaviour
{
    [Header("メニュー管理")]
    [SerializeField] private MenuManager menuManager;

    [SerializeField] public AttackCustom attackCustom;
    [SerializeField] public SpecialCustom specialMove;

    [SerializeField] GameObject normalAttackPage;
    [SerializeField] GameObject specialAttackPage;

    [Header("Input Actions")]
    [SerializeField] InputActionReference decidebuttonAction;

    private Dictionary<string, AttackData> attackDataDictionary;

    private const string ProjectFolderName = "Fighting Game";
    private const string CsvFolderName = "Assets/Resources/Player/";
    private const string CsvFileName = "test.csv";

    void OnEnable()
    {
        Enable(decidebuttonAction);
    }

    static void Enable(InputActionReference action)
    {
        if (action != null)
        {
            action.action.Enable();
        }
    }

    void Awake()
    {
        if (attackCustom == null && normalAttackPage != null)
            attackCustom = normalAttackPage.GetComponentInChildren<AttackCustom>(true);

        if (specialMove == null && specialAttackPage != null)
            specialMove = specialAttackPage.GetComponentInChildren<SpecialCustom>(true);
    }

    void Start()
    {
        if (attackCustom == null)
            Debug.LogError("AttackCustomが見つかりません。", this);

        if (specialMove == null)
            Debug.LogError("SpecialCustomが見つかりません。", this);

        attackDataDictionary =
            AttackDataLoader.LoadAttackDataFromCSV("test");

        if (attackDataDictionary == null)
        {
            Debug.LogError("攻撃データの読み込みに失敗しました。");
            return;
        }

        foreach (string key in attackDataDictionary.Keys)
        {
            Debug.Log($"辞書に登録されている攻撃名: [{key}]");
        }
    }

    private static string GetCsvFilePath()
    {
        string desktopPath = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.DesktopDirectory
        );

        return Path.Combine(
            desktopPath,
            "卒業研究",
            ProjectFolderName,
            CsvFolderName,
            CsvFileName
        );
    }

    public void SaveData()
    {
        if (attackCustom == null || specialMove == null)
        {
            Debug.LogError("通常技または必殺技の設定が取得できません。");
            return;
        }

        if (attackDataDictionary == null)
        {
            Debug.LogError("攻撃データ辞書が読み込まれていません。");
            return;
        }

        string filePath = GetCsvFilePath();

        string folderPath = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(folderPath);

        StringBuilder csv = new StringBuilder();

        csv.AppendLine(
            "攻撃,発生,持続,硬直,ダメージ,ヒットボックスの名前,判定X,判定Y,判定幅,判定高,ノックバックX,ノックバックY,ヒットストップ,ヒット時の相手の硬直,ガード種別,ガード硬直,ガード押し戻し,削り,技開始時の横速度,技開始時の上速度,無敵フレーム数,横移動を止める(昇竜用),コンボのダウン閾値(0はダウンしない)"
        );

        // 通常技：立ち・しゃがみ
        string[] strengths = { "Light", "Medium", "Heavy" };
        string[] stances = { "Stand_", "Crouch_" };

        int attackCount = attackCustom.GetAttackCount();

        for (int i = 0; i < attackCount; i++)
        {
            foreach (string stance in stances)
            {
                string attackName = stance + strengths[i];

                if (attackDataDictionary.TryGetValue(attackName, out AttackData data))
                {
                    Debug.Log($"攻撃データ取得成功: {attackName}");

                    csv.AppendLine(
                        attackName + "," +
                        attackCustom.GetValue(i, 1) + "," +
                        attackCustom.GetValue(i, 3) + "," +
                        attackCustom.GetValue(i, 2) + "," +
                        attackCustom.GetValue(i, 0) + "," +
                        "Attack_HitBox" + "," +
                        data.hitboxOffsetX + "," +
                        data.hitboxOffsetY + "," +
                        data.hitboxSizeX + "," +
                        data.hitboxSizeY + "," +

                        data.knockbackX + "," +
                        data.knockbackY + "," +
                        data.hitStop + "," +
                        data.hitStun + "," +

                        data.guardType + "," +
                        data.blockStun + "," +
                        data.blockKnockbackX + "," +
                        data.chipDamage + "," +

                        data.startVelX + "," +
                        data.startVelY + "," +
                        data.invulnFrames + "," +
                        data.stopXAtApex + "," +
                        data.knockDownCombo
                    );
                    Debug.Log(data.guardType);
                }
                else
                {
                    Debug.LogError($"攻撃データ取得失敗: {attackName}");
                }   
            }
        }

        // 空中攻撃
        if (attackDataDictionary.TryGetValue("Air_Attack", out AttackData airData))
        {
            csv.AppendLine(
                "Air_Attack," +
                attackCustom.GetValue(0, 1) + "," +
                attackCustom.GetValue(0, 3) + "," +
                attackCustom.GetValue(0, 2) + "," +
                attackCustom.GetValue(0, 0) + "," +
                "Attack_HitBox" + "," +
                airData.hitboxOffsetX + "," +
                airData.hitboxOffsetY + "," +
                airData.hitboxSizeX + "," +
                airData.hitboxSizeY + "," +

                airData.knockbackX + "," +
                airData.knockbackY + "," +
                airData.hitStop + "," +
                airData.hitStun + "," +

                airData.guardType + "," +
                airData.blockStun + "," +
                airData.blockKnockbackX + "," +
                airData.chipDamage + "," +

                airData.startVelX + "," +
                airData.startVelY + "," +
                airData.invulnFrames + "," +
                airData.stopXAtApex + "," +
                airData.knockDownCombo
            );
        }
        else
        {
            Debug.LogError("攻撃データ取得失敗: Air_Attack");
        }

        if (attackDataDictionary.TryGetValue("Throw", out AttackData ThrowData))
        {
            // 投げ技
            csv.AppendLine("Throw,5,2,23,1200,Throw_HitBox" +"," +
                ThrowData.hitboxOffsetX + "," +
                ThrowData.hitboxOffsetY + "," +
                ThrowData.hitboxSizeX + "," +
                ThrowData.hitboxSizeY + "," +

                ThrowData.knockbackX + "," +
                ThrowData.knockbackY + "," +
                ThrowData.hitStop + "," +
                ThrowData.hitStun + "," +

                ThrowData.guardType + "," +
                ThrowData.blockStun + "," +
                ThrowData.blockKnockbackX + "," +
                ThrowData.chipDamage + "," +

                ThrowData.startVelX + "," +
                ThrowData.startVelY + "," +
                ThrowData.invulnFrames + "," +
                ThrowData.stopXAtApex + "," +
                ThrowData.knockDownCombo
                );

        }

        // 必殺技（弱・中・強）

        string[] specialNames = { "Special1_L", "Special1_M", "Special1_H" };

        // Lの値を基準として取得
        int baseStartup = specialMove.GetValue(0, 1);
        int baseActive = specialMove.GetValue(0, 3);
        int baseRecovery = specialMove.GetValue(0, 2);
        int baseDamage = specialMove.GetValue(0, 0);

        for (int i = 0; i < specialNames.Length; i++)
        {
            string attackName = specialNames[i];

            if (attackDataDictionary.TryGetValue(attackName, out AttackData specialMoveData))
            {
                // Lからの段階的な補正
                int level = i;

                csv.AppendLine(attackName + "," +
                    (baseStartup + level) + "," +
                    (baseActive + level) + "," +
                    (baseRecovery + level * 6) + "," +
                    (baseDamage + level * 100) + "," +
                    attackName + "_HitBox" + "," +
                    specialMoveData.hitboxOffsetX + "," +
                    specialMoveData.hitboxOffsetY + "," +
                    specialMoveData.hitboxSizeX + "," +
                    specialMoveData.hitboxSizeY + "," +
                    specialMoveData.knockbackX + "," +
                    specialMoveData.knockbackY + "," +
                    specialMoveData.hitStop + "," +
                    specialMoveData.hitStun + "," +
                    specialMoveData.guardType + "," +
                    specialMoveData.blockStun + "," +
                    specialMoveData.blockKnockbackX + "," +
                    specialMoveData.chipDamage + "," +
                    specialMoveData.startVelX + "," +
                    specialMoveData.startVelY + "," +
                    specialMoveData.invulnFrames + "," +
                    specialMoveData.stopXAtApex + "," +
                    specialMoveData.knockDownCombo
                );
            }
            else
            {
                Debug.LogError($"必殺技データ取得失敗: {attackName}");
            }
        }

        File.WriteAllText(
            filePath,
            csv.ToString(),
            new UTF8Encoding(true)
        );

        Debug.Log("CSVを保存しました: " + filePath);

        if (MessageWindow.instance != null)
        {
            MessageWindow.instance.ShowSaveMessage();
        }
    }

    void Update()
    {
        if (menuManager != null && menuManager.IsMenuOpen)
        {
            return;
        }

        if (decidebuttonAction != null &&
            decidebuttonAction.action.WasPressedThisFrame())
        {
            SaveData();
        }
    }
}
