namespace MyFunctions
{
    public static class GlobalFunctions
    {
        public static float ChangeFrameTime(int frame)
        {
            return (float)frame / 60f;
        }

    }
}
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
    public string attackID;
    public int startup;
    public int active;
    public int recovery;
    public int damage;
    public string hitBoxName;

    // 判定の位置・大きさ
    public float hitboxOffsetX;
    public float hitboxOffsetY;
    public float hitboxSizeX;
    public float hitboxSizeY;

    public float knockbackX;
    public float knockbackY;
    public int hitStop;   // ヒットストップ
    public int hitStun;   // ヒットスタン

    public string guardType;       // High / Low / Throw 上段 / 下段 / ガード不能
    public int blockStun;          // ガード硬直
    public float blockKnockbackX;  // ガード時の押し戻し
    public int chipDamage;         // 削りダメージ
}