using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AttackDataLoader
{
    public static Dictionary<string, AttackData> LoadAttackDataFromCSV(string resourcePath)
    {
        var dictionary = new Dictionary<string, AttackData>();
        TextAsset csvFile = Resources.Load<TextAsset>(resourcePath);

        if (csvFile == null)
        {
            Debug.LogError($"CSVファイルが見つかりません: Resources/{resourcePath}");
            return dictionary;
        }

        string[] lines = csvFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 1行目はヘッダーなので i = 1 からスタート
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 6) continue;

            AttackData data = new AttackData
            {
                attackID = values[0].Trim(),
                startup = int.Parse(values[1].Trim()),
                active = int.Parse(values[2].Trim()),
                recovery = int.Parse(values[3].Trim()),
                damage = int.Parse(values[4].Trim()),
                hitBoxName = values[5].Trim()
            };

            dictionary[data.attackID] = data;
        }

        return dictionary;
    }
}
