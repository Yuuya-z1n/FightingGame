using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

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
    }

    public void SaveData()
    {
        if (attackCustom == null || specialMove == null)
        {
            Debug.LogError("通常技または必殺技の設定が取得できません。");
            return;
        }

        // 保存先フォルダを固定
        string folderpath = Path.Combine(
            Application.persistentDataPath,
            "CustomData",
            "PlayerCustom"
        );

        Directory.CreateDirectory(folderpath);

        string filePath = Path.Combine(folderpath, filename);
        StringBuilder csv = new StringBuilder();

        csv.AppendLine("攻撃,発生,持続,硬直,ダメージ,ヒットボックスの名前");
        // 通常技：小・中・大
        string[] normalNames = { "Light", "Medium", "Heavey"};

        for (int i = 0; i < 3; i++)
        {
            csv.AppendLine(
                normalNames[i] + "," +
                attackCustom.GetValue(i, 1) + "," +
                attackCustom.GetValue(i, 3) + "," +
                attackCustom.GetValue(i, 2) + "," +
                attackCustom.GetValue(i, 0) + "," +
                "Attack_HitBox"
            );
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

