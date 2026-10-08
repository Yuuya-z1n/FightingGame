using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

public class SaveAttackCustum : MonoBehaviour
{

    StreamWriter sw;
    string filename =  "test.csv" ;
    public AttackCustum attackCustum;


    void Start()
    {
        sw = new StreamWriter("SaveData.csv", false, Encoding.GetEncoding("Shift_JIS"));

        string[] header = { "F", "J", "time" };
        sw.WriteLine(string.Join(",", header));

        attackCustum = GetComponent<AttackCustum>();
    }


    public void SaveData()
    {
        string folderpath = Path.Combine(
            Application.dataPath,
            "CustumData",
            "PlayerCustum");

        if (!Directory.Exists(folderpath))
        {
            Directory.CreateDirectory(folderpath);
        }

        string filePath = Path.Combine(folderpath, filename);

        StringBuilder csv = new StringBuilder();

        // ヘッダー
        csv.AppendLine("攻撃,ダメージ,発生,硬直");

        // 小攻撃
        csv.AppendLine(
            "小攻撃," +
            GetDamageName(attackCustum.GetValue(0, 0)) + "," +
            GetTriggerName(attackCustum.GetValue(0, 1)) + "," +
            GetStunName(attackCustum.GetValue(0, 2))
            );

        // 中攻撃
        csv.AppendLine(
            "中攻撃," +
            GetDamageName(attackCustum.GetValue(1,0)) + "," +
            GetTriggerName(attackCustum.GetValue(1, 1)) + "," +
            GetStunName(attackCustum.GetValue(1, 2))
            );

        // 大攻撃
        csv.AppendLine(
            "大攻撃," +
            GetDamageName(attackCustum.GetValue(2, 0)) + "," +
            GetTriggerName(attackCustum.GetValue(2, 1)) + "," +
            GetStunName(attackCustum.GetValue(2, 2))
            );

        // UTF-8 BOM付きで保存
        File.WriteAllText(
            filePath,
            csv.ToString(),
            new UTF8Encoding(true)
        );


        Debug.Log("CSVを保存しました : " + filePath);
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
        if (Input.GetKeyDown(KeyCode.W))
        {
            sw.Close();
        }
    }
}

