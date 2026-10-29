using System;

// ============================================================
//  シミュレーション層の型定義
//  ・UnityEngine に依存しない純 C#（将来 Photon Fusion のティックから呼べる）
//  ・小数(float)を使わず、整数のみで計算する（決定論的に動かすため）
//  ・単位: 位置 = sim単位（1000 = 1ワールド単位）、速度 = sim単位/フレーム
//  ・1ティック = 1/60 秒
// ============================================================
namespace FightSim
{
    public static class SimConst
    {
        public const int UnitsPerWorld = 1000; // 1ワールド単位 = 1000 sim単位
        public const int Fps = 60;
    }

    // 1フレーム分の入力（方向は画面上の絶対方向。前/後ろは向きから算出する）
    [Flags]
    public enum InputButtons : ushort
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
        Light = 1 << 4,
        Medium = 1 << 5,
        Heavy = 1 << 6,
        Throw = 1 << 7
    }

    public static class InputUtil
    {
        public static bool Has(InputButtons b, InputButtons flag)
        {
            return (b & flag) != 0;
        }

        // 左右: -1 = 左, 0 = なし, +1 = 右（両方押しは 0）
        public static int Horizontal(InputButtons b)
        {
            int x = 0;
            if (Has(b, InputButtons.Right)) x += 1;
            if (Has(b, InputButtons.Left)) x -= 1;
            return x;
        }

        // 上下: +1 = 上, -1 = 下（両方押しは 0）
        public static int Vertical(InputButtons b)
        {
            int y = 0;
            if (Has(b, InputButtons.Up)) y += 1;
            if (Has(b, InputButtons.Down)) y -= 1;
            return y;
        }

        // テンキー表記（5 = ニュートラル、6 = 前、4 = 後ろ、8 = 上、2 = 下）
        public static int Numpad(InputButtons b, bool facingRight)
        {
            int x = Horizontal(b);
            if (!facingRight) x = -x;
            int y = Vertical(b);
            return 5 + x + 3 * y;
        }
    }

    // 攻撃のガード種別（簡略化: 上段と下段のみ）
    public enum GuardType : byte
    {
        High,        // 上段: 立ちガードでのみ防げる（ジャンプ攻撃も上段扱い）
        Low,         // 下段: しゃがみガードでのみ防げる
        Unblockable  // ガード不能（投げ）
    }

    public enum FighterStateId : byte
    {
        Idle,
        WalkForward,
        WalkBack,
        Crouch,
        Jump,
        Attack,     // 地上攻撃
        AirAttack,  // 空中攻撃
        HitStun,    // 食らい硬直
        BlockStun,  // ガード硬直
        KO
    }

    // 調整用パラメータ（Inspector から編集できる）
    [Serializable]
    public class FighterParams
    {
        // ---- 体力 ----
        public int maxHealth = 10000;
        public bool chipCanKill = true;      // 削りダメージで KO になるか

        // ---- 移動（sim単位/フレーム。83 ≒ 5.0 ワールド単位/秒）----
        public int walkForwardSpeed = 83;
        public int walkBackSpeed = 75;
        public int jumpSpeed = 150;          // ジャンプ初速（150 ≒ 9.0/秒）
        public int jumpForwardSpeed = 83;
        public int jumpBackSpeed = 75;
        public int gravity = 7;              // sim単位/フレーム²（7 ≒ 25/秒²）
        public int hitStunGroundFriction = 2; // 食らい・ガード硬直中、地上でのノックバックの減速

        // ---- 入力 ----
        public int inputBufferFrames = 6;    // 先行入力の受付フレーム数

        // ---- 判定ボックス（幅・高さ。原点 = 足元の中心）----
        public int pushWidth = 600;          // 押し合い判定
        public int standPushHeight = 1000;
        public int crouchPushHeight = 700;
        public int hurtWidth = 700;          // 食らい判定
        public int standHurtHeight = 1700;
        public int crouchHurtHeight = 1100;

        // ---- ステージ ----
        public int stageMinX = -10000;       // ステージ左端
        public int stageMaxX = 10000;        // ステージ右端
        public int maxSeparation = 9000;     // 2人の最大距離（カメラが引き切った時の画面幅に合わせる）
        public int startX = 3000;            // 初期位置（1Pは -startX、2Pは +startX）
    }

    // 技のフレームデータ（sim単位に変換済み）
    public struct AttackFrameData
    {
        public int startup;   // 判定が出るまでに経過するフレーム数
        public int active;    // 判定が出ているフレーム数
        public int recovery;  // 硬直フレーム数
        public int damage;
        public int hitboxOffsetX; // 足元の中心からの距離（右向き基準）
        public int hitboxOffsetY;
        public int hitboxWidth;
        public int hitboxHeight;
        public int knockbackX;    // sim単位/フレーム
        public int knockbackY;
        public int hitStop;       // ヒットストップのフレーム数（ヒット・ガード共通）
        public int hitStun;       // ヒットスタンのフレーム数

        public GuardType guardType; // 上段 / 下段 / ガード不能
        public int blockStun;       // ガード硬直のフレーム数
        public int blockKnockbackX; // ガード時の押し戻し（sim単位/フレーム）
        public int chipDamage;      // 削りダメージ（必殺技のみ値を入れる）

        public bool IsValid { get { return active > 0; } }
        public int TotalFrames { get { return startup + active + recovery; } }

        // CSV から読み込んだデータ（ワールド単位の小数）を sim 単位に変換する
        public static AttackFrameData FromCsv(AttackData d)
        {
            AttackFrameData r = new AttackFrameData();
            r.startup = d.startup;
            r.active = d.active;
            r.recovery = d.recovery;
            r.damage = d.damage;
            r.hitboxOffsetX = ToUnits(d.hitboxOffsetX);
            r.hitboxOffsetY = ToUnits(d.hitboxOffsetY);
            r.hitboxWidth = ToUnits(d.hitboxSizeX);
            r.hitboxHeight = ToUnits(d.hitboxSizeY);
            r.knockbackX = ToUnitsPerFrame(d.knockbackX);
            r.knockbackY = ToUnitsPerFrame(d.knockbackY);
            r.hitStop = d.hitStop;
            r.hitStun = d.hitStun;
            r.guardType = ParseGuardType(d.guardType);
            r.blockStun = d.blockStun;
            r.blockKnockbackX = ToUnitsPerFrame(d.blockKnockbackX);
            r.chipDamage = d.chipDamage;
            return r;
        }

        // CSV の文字列を GuardType に変換（空・不明は上段）
        private static GuardType ParseGuardType(string s)
        {
            if (string.IsNullOrEmpty(s)) return GuardType.High;
            string t = s.Trim().ToLowerInvariant();
            if (t == "low" || t == "下段" || t == "下") return GuardType.Low;
            if (t == "throw" || t == "unblockable" || t == "投げ" || t == "ガード不能") return GuardType.Unblockable;
            return GuardType.High;
        }

        private static int ToUnits(float v)
        {
            return (int)Math.Round(v * SimConst.UnitsPerWorld, MidpointRounding.AwayFromZero);
        }

        // 「ワールド単位/秒」を「sim単位/フレーム」へ
        private static int ToUnitsPerFrame(float v)
        {
            return (int)Math.Round(v * SimConst.UnitsPerWorld / SimConst.Fps, MidpointRounding.AwayFromZero);
        }
    }

    // 判定用の矩形（整数）
    public struct SimRect
    {
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;

        public bool Overlaps(SimRect o)
        {
            return minX < o.maxX && o.minX < maxX && minY < o.maxY && o.minY < maxY;
        }
    }

    // 1人分のシミュレーション状態。
    // 整数・bool・enum だけで構成する（参照型を含めない）ので、コピーするだけで保存・復元できる。
    public struct FighterSimState
    {
        public int posX;
        public int posY;          // 0 = 地面
        public int velX;
        public int velY;
        public bool facingRight;

        public FighterStateId stateId;
        public int stateFrame;    // 現在の状態になってからの経過フレーム
        public int attackIndex;   // 出している技（AttackType の値）。-1 = なし
        public bool attackHasHit; // この攻撃がすでにヒットしたか（多段ヒット防止）

        public int hitStopFrames; // 残りヒットストップ（この間は状態が進まない）
        public int hitStunFrames; // ヒットスタン / ガード硬直の長さ
        public bool guardCrouch;  // ガード硬直中の姿勢（true = しゃがみ）
        public int health;

        public InputButtons prevButtons; // 前フレームの入力（押した瞬間の検出用）
        public int bufLight;      // 先行入力の残りフレーム
        public int bufMedium;
        public int bufHeavy;
        public int bufThrow;
        public ulong dirHistory;  // 方向入力の履歴（4bit × 16フレーム、テンキー表記）。必殺技コマンド用

        public bool Grounded
        {
            get { return posY <= 0 && velY <= 0; }
        }
    }

    // 巻き戻し（ロールバック）用のスナップショット
    public struct MatchSnapshot
    {
        public int frame;
        public FighterSimState p1;
        public FighterSimState p2;
    }
}