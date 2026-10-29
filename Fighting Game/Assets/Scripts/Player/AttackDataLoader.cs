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
                attackID = v[0].Trim(),                         // 攻撃
                startup = ParseInt(v, 1, row),                  // 発生
                active = ParseInt(v, 2, row),                   // 持続
                recovery = ParseInt(v, 3, row),                 // 硬直
                damage = ParseInt(v, 4, row),                   // ダメージ
                hitBoxName = v[5].Trim(),                       // ヒットボックスの名前(Attack_HitBoxなど)

                hitboxOffsetX = ParseFloat(v, 6, row),          // ヒットボックスのX座標オフセット
                hitboxOffsetY = ParseFloat(v, 7, row),          // ヒットボックスのY座標オフセット
                hitboxSizeX = ParseFloat(v, 8, row),            // ヒットボックスの幅
                hitboxSizeY = ParseFloat(v, 9, row),            // ヒットボックスの高さ

                knockbackX = ParseFloat(v, 10, row),            // ノックバックX
                knockbackY = ParseFloat(v, 11, row),            // ノックバックY
                hitStop = ParseInt(v, 12, row),                 // ヒットストップ
                hitStun = ParseInt(v, 13, row),                 // ヒットスタン

                guardType = v.Length > 14 ? v[14].Trim() : "",  // ガード種類
                blockStun = ParseInt(v, 15, row),               // ガード硬直
                blockKnockbackX = ParseFloat(v, 16, row),       // ガードの押し戻し
                chipDamage = ParseInt(v, 17, row)               // 削りダメ―ジ(必殺技のみ。通常技の場合は0)
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