public enum AttackType
{
    Light,
    Medium,
    Heavy,
    Throw
}

[System.Serializable]
public struct AttackData
{
    public string attackID; // 弱・中・強
    public int startup;           // 発生フレーム
    public int active;            // 持続フレーム
    public int recovery;          // 硬直フレーム
    public int damage;            // ダメージ
    public string hitBoxName;
}