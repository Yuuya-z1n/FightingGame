using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class AttackDataLoader
{
    private const int RequiredColumns = 6;

    public static Dictionary<string, AttackData> LoadAttackDataFromCSV(string resourcePath)
    {
        var dictionary = new Dictionary<string, AttackData>();
        TextAsset csvFile = Resources.Load<TextAsset>("Player/" + resourcePath);

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
                attackID = v[0].Trim(),                              // 攻撃
                startup = ParseInt(v, 1, row),                       // 発生
                active = ParseInt(v, 2, row),                        // 持続
                recovery = ParseInt(v, 3, row),                      // 硬直
                damage = ParseInt(v, 4, row),                        // ダメージ
                hitBoxName = v[5].Trim(),                            // ヒットボックスの名前

                hitboxOffsetX = ParseFloat(v, 6, row),               // 判定X
                hitboxOffsetY = ParseFloat(v, 7, row),               // 判定Y
                hitboxSizeX = ParseFloat(v, 8, row),                 // 判定幅
                hitboxSizeY = ParseFloat(v, 9, row),                 // 判定高

                knockbackX = ParseFloat(v, 10, row),                 // ノックバックX
                knockbackY = ParseFloat(v, 11, row),                 // ノックバックY
                hitStop = ParseInt(v, 12, row),                      // ヒットストップ（フレーム。ヒット・ガード共通）
                hitStun = ParseInt(v, 13, row),                      // ヒットスタン（フレーム）

                guardType = v.Length > 14 ? v[14].Trim() : "",       // ガード種別（High=上段 / Low=下段 / Throw=ガード不能）
                blockStun = ParseInt(v, 15, row),                    // ガード硬直（フレーム）
                blockKnockbackX = ParseFloat(v, 16, row),            // ガード時の押し戻し（ノックバックと同じ単位）
                chipDamage = ParseInt(v, 17, row),                   // 削りダメージ（必殺技のみ値を入れる。通常技は 0）

                startVelX = ParseFloat(v, 18, row),                  // 技の開始時の横速度
                startVelY = ParseFloat(v, 19, row),                  // 技の開始時の上方向の速度
                invulnFrames = ParseInt(v, 20, row),                 // 技の開始から無敵になるフレーム数
                stopXAtApex = ParseInt(v, 21, row),                  // 昇竜など、最高到達点に至った時に垂直落下するかどうか
                knockDownCombo = ParseInt(v, 22, row)                // 相手へのヒット数カウント。この値以上になるとダウン(0はダウンしない)   
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