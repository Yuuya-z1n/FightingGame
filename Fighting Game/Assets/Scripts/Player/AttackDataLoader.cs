using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class AttackDataLoader
{
    private const int RequiredColumns = 6;

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
            string[] v = lines[i].Split(',');
            if (v.Length < RequiredColumns) continue;

            int row = i + 1; // 警告表示用（ヘッダーを1行目として数える）

            AttackData data = new AttackData
            {
                attackID = v[0].Trim(),
                startup = ParseInt(v, 1, row),
                active = ParseInt(v, 2, row),
                recovery = ParseInt(v, 3, row),
                damage = ParseInt(v, 4, row),
                hitBoxName = v[5].Trim(),

                hitboxOffsetX = ParseFloat(v, 6, row),
                hitboxOffsetY = ParseFloat(v, 7, row),
                hitboxSizeX = ParseFloat(v, 8, row),
                hitboxSizeY = ParseFloat(v, 9, row),

                knockbackX = ParseFloat(v, 10, row),
                knockbackY = ParseFloat(v, 11, row),
                hitStop = ParseInt(v, 12, row),
                hitStun = ParseInt(v, 13, row)
            };

            dictionary[data.attackID] = data;
        }

        return dictionary;
    }

    // 列が無い場合は 0、数値として読めない場合は警告を出して 0 を返す
    private static int ParseInt(string[] v, int index, int row)
    {
        if (index >= v.Length) return 0;
        string s = v[index].Trim();
        if (s.Length == 0) return 0;

        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;

        Debug.LogWarning($"CSV {row}行目 {index + 1}列目 '{s}' を整数として読めません。0 として扱います");
        return 0;
    }

    private static float ParseFloat(string[] v, int index, int row)
    {
        if (index >= v.Length) return 0f;
        string s = v[index].Trim();
        if (s.Length == 0) return 0f;

        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;

        Debug.LogWarning($"CSV {row}行目 {index + 1}列目 '{s}' を小数として読めません。0 として扱います");
        return 0f;
    }
}