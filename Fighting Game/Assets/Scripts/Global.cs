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

    // 判定の位置・大きさ（右向き基準、HitBox の原点からの相対）
    public float hitboxOffsetX;
    public float hitboxOffsetY;
    public float hitboxSizeX;
    public float hitboxSizeY;

    public float knockbackX;
    public float knockbackY;
    public int hitStop;   // ヒットストップ（フレーム）
    public int hitStun;   // ヒットスタン（フレーム。次のステップで使う）
}