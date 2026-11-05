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
    Stand_Light,
    Stand_Medium,
    Stand_Heavy,
    Crouch_Light,
    Crouch_Medium,
    Crouch_Heavy,
    Throw,
    Special1_L,   // 必殺技1（昇竜）弱
    Special1_M,   // 必殺技1（昇竜）中
    Special1_H,   // 必殺技1（昇竜）強
    Air_Attack,
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

    public float startVelX;     // 技の開始時の横速度
    public float startVelY;     // 技の開始時の上方向の速度（昇竜のジャンプなど）
    public int invulnFrames;    // 技の開始から無敵になるフレーム数
    public int stopXAtApex;
    public int knockDownCombo;  // 相手へのヒット数カウント。この値以上になるとダウン(0はダウンしない)
}