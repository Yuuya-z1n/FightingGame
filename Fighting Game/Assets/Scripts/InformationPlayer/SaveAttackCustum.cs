using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

public class SaveAttackCustum : MonoBehaviour
{

    StreamWriter sw;
    string filename =  "test.csv" ;
    [SerializeField] public AttackCustum attackCustum;
    [SerializeField] public SpecialCustum specialMove;

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
        if (attackCustum == null && normalAttackPage != null)
            attackCustum = normalAttackPage.GetComponentInChildren<AttackCustum>(true);

        if (specialMove == null && specialAttackPage != null)
            specialMove = specialAttackPage.GetComponentInChildren<SpecialCustum>(true);
    }

    void Start()
    {
        sw = new StreamWriter("SaveData.csv", false, Encoding.GetEncoding("Shift_JIS"));

        string[] header = { "F", "J", "time" };
        sw.WriteLine(string.Join(",", header));

        if (attackCustum == null)
            Debug.LogError("AttackCustumが見つかりません。", this);

        if (specialMove == null)
            Debug.LogError("SpecialAttackCustumが見つかりません。", this);

    }



    public void SaveData()
    {
        if (attackCustum == null || specialMove == null)
        {
            Debug.LogError("通常技または必殺技の設定が取得できません。");
            return;
        }

        string folderpath = Path.Combine(
            Application.dataPath,
            "CustumData",
            "PlayerCustum"
        );

        if (!Directory.Exists(folderpath))
        {
            Directory.CreateDirectory(folderpath);
        }

        string filePath = Path.Combine(folderpath, filename);
        StringBuilder csv = new StringBuilder();

        csv.AppendLine("攻撃,ダメージ,発生,硬直");

        // 通常技：小・中・大
        string[] normalNames = { "小攻撃", "中攻撃", "大攻撃" };

        for (int i = 0; i < 3; i++)
        {
            csv.AppendLine(
                normalNames[i] + "," +
                attackCustum.GetValue(i, 0) + "," +
                attackCustum.GetValue(i, 1) + "," +
                attackCustum.GetValue(i, 2)
            );
        }

        csv.AppendLine(
            "必殺技1," +
            specialMove.GetValue(0, 0) + "," +
            specialMove.GetValue(0, 1) + "," +
            specialMove.GetValue(0, 2)
        );

        File.WriteAllText(
            filePath,
            csv.ToString(),
            new UTF8Encoding(true)
        );

        Debug.Log("CSVを保存しました: " + filePath);

        if(MessageWindow.instance != null)
        {
            MessageWindow.instance.SetMessage("あなたの設定が保存されました",2f);
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
        if(decidebuttonAction != null && decidebuttonAction.action.WasPressedThisFrame())
        {
            SaveData();
        }
    }
}

