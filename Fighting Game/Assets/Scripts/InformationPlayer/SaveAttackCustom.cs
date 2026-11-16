using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SaveAttackCustom : MonoBehaviour
{

    [Header("メニュー管理")]
    [SerializeField] private MenuManager menuManager;
    StreamWriter sw;
    string filename =  "test.csv" ;
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
        if(action != null)
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

        attackDataDictionary = AttackDataLoader.LoadAttackDataFromCSV(GetCsvFilePath());

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

        string filePath = GetCsvFilePath();

        string folderPath = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(folderPath);

        // CSVデータを格納する変数
        StringBuilder csv = new StringBuilder();



        //var dictionary = new Dictionary<string, AttackData>();
        //TextAsset csvFile = Resources.Load<TextAsset>(filePath);
        //AttackDataLoader.LoadAttackDataFromCSV(csvFile);

        //if(csvFile == null)
        //{
        //    Debug.Log("CSVが見当たらない");
        //    return dictionary;
        //}


        csv.AppendLine("攻撃,発生,持続,硬直,ダメージ,ヒットボックスの名前,判定X,判定Y,判定幅,判定高,ノックバックX,ノックバックY,ヒットストップ,ヒットスタン");
        // 通常技：小・中・大
        string[] normalNames = { "Light", "Medium", "Heavy" };

        int attackCount = attackCustom.GetAttackCount();

        for (int i = 0; i < attackCount; i++)
        {
            //Debug.Log(i);
            if (attackDataDictionary.TryGetValue(normalNames[i], out AttackData data))
            {
                Debug.Log($"攻撃データ取得成功: {normalNames[i]}");

                csv.AppendLine(
                    normalNames[i] + "," +
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
                    data.hitStun
                );
            }
            else
            {
                Debug.LogError($"攻撃データ取得失敗: {normalNames[i]}");
            }
        }
        // 投げ技の項目
        csv.AppendLine("Throw," +
            "5,"+ "2,"+"23,"+"1200,"+ "Throw_HitBox");

        // 必殺技の項目
        csv.AppendLine(
            "SpecialMove1," +
            specialMove.GetValue(0, 1) + "," +
            specialMove.GetValue(0, 3) + "," +
            specialMove.GetValue(0, 2) + "," +
            specialMove.GetValue(0, 0) + "," +
            "SpecialMove1_HitBox"
        );

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

    string GetDamageName(int index)
    {
        switch (index)
        {
            case 0:
                return "低い";
            case 1:
                return "普通";
            case 2:
                return "高い";
        }
        return "";
    }

    string GetTriggerName(int index)
    {
        switch (index)
        {
            case 0:
                return "遅い";
            case 1:
                return "普通";
            case 2:
                return "速い";
        }
        return "";
    }

    string GetStunName(int index)
    {
        switch (index)
        {
            case 0:
                return "遅い";
            case 1:
                return "普通";
            case 2:
                return "速い";
        }
        return "";
    }


    void Update()
    {
        // Menuが開いている間は保存入力を受け付けない
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

