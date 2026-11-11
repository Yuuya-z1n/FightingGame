using System;
using UnityEditor;

namespace FightSim
{
    // 攻撃判定のチェック結果の記録（デバッグ表示用。ロールバックの状態には含めない）
    public struct HitCheckLog
    {
        public bool valid;
        public int frame;               // 判定を行ったティック
        public SimRect hit;             // 攻撃側の判定
        public SimRect hurt;            // 防御側の食らい判定
        public bool overlap;            // 重なったか
        public bool invulnerable;       // 防御側が無敵で、当たらなかったか
        public FighterStateId defenderState; // その時の防御側の状態
    }

    /// <summary>
    /// 対戦のシミュレーション本体。UnityEngine に依存しない。
    /// Tick(入力1P, 入力2P) を 1/60 秒ごとに1回呼ぶだけで、全てが1フレーム進む。
    ///   同じ初期状態 + 同じ入力列 → 必ず同じ結果（整数演算のみ）。
    /// 将来 Photon Fusion に載せる時は、この Tick を FixedUpdateNetwork から呼ぶ。
    /// </summary>
    public class MatchSim
    {
        private readonly FighterParams p;
        private readonly AttackFrameData[] attacks; // AttackType の値でインデックス
        private readonly FighterSimState[] f = new FighterSimState[2];
        private readonly HitLog[] lastHit = new HitLog[2]; // デバッグ用（防御側ごとの直近のヒット結果）
        private readonly HitCheckLog[] lastCheck = new HitCheckLog[2]; // デバッグ用（攻撃側ごとの直近の判定チェック）
        private int frame;

        public int Frame { get { return frame; } }
        public bool IsOver { get { return f[0].health <= 0 || f[1].health <= 0; } }

        public MatchSim(FighterParams parameters, AttackFrameData[] attackTable)
        {
            p = parameters;
            attacks = attackTable ?? new AttackFrameData[4];
            Reset();
        }

        public FighterSimState Get(int i)
        {
            return f[i];
        }

        public void Reset()
        {
            frame = 0;
            lastHit[0] = new HitLog();
            lastHit[1] = new HitLog();
            lastCheck[0] = new HitCheckLog();
            lastCheck[1] = new HitCheckLog();
            InitFighter(0, -p.startX, true);
            InitFighter(1, p.startX, false);
        }

        private void InitFighter(int i, int x, bool facingRight)
        {
            FighterSimState s = new FighterSimState();
            s.posX = x;
            s.facingRight = facingRight;
            s.stateId = FighterStateId.Idle;
            s.attackIndex = -1;
            s.health = p.maxHealth;
            f[i] = s;
        }

        // ---- ロールバック用 ----
        public MatchSnapshot Save()
        {
            MatchSnapshot s = new MatchSnapshot();
            s.frame = frame;
            s.p1 = f[0];
            s.p2 = f[1];
            return s;
        }

        public void Load(MatchSnapshot s)
        {
            frame = s.frame;
            f[0] = s.p1;
            f[1] = s.p2;
        }

        // ============================================================
        //  1ティック進める
        // ============================================================
        public void Tick(InputButtons in0, InputButtons in1)
        {
            // このティックの開始時点でヒットストップ中か
            bool frozen0 = f[0].hitStopFrames > 0;
            bool frozen1 = f[1].hitStopFrames > 0;
            int prevX0 = f[0].posX;
            int prevX1 = f[1].posX;

            // 入力バッファ（ヒットストップ中も入力は受け付ける）
            UpdateInput(ref f[0], in0, frozen0);
            UpdateInput(ref f[1], in1, frozen1);

            // 状態更新と移動（ヒットストップ中は進めない）
            if (frozen0) f[0].hitStopFrames--; else StepFighter(ref f[0], in0);
            if (frozen1) f[1].hitStopFrames--; else StepFighter(ref f[1], in1);

            ResolveThrowStates();
            ResolvePush();
            LimitSeparation(prevX0, prevX1);
            ClampStage();
            UpdateFacing();
            ResolveHits();

            frame++;
        }

        // ---- 入力 ----
        private void UpdateInput(ref FighterSimState s, InputButtons input, bool frozen)
        {
            InputButtons pressed = input & ~s.prevButtons;

            // 先行入力は、ヒットストップ中は減らさない
            if (!frozen)
            {
                if (s.bufLight > 0) s.bufLight--;
                if (s.bufMedium > 0) s.bufMedium--;
                if (s.bufHeavy > 0) s.bufHeavy--;
                if (s.bufThrow > 0) s.bufThrow--;
            }

            if (InputUtil.Has(pressed, InputButtons.Light)) s.bufLight = p.inputBufferFrames;
            if (InputUtil.Has(pressed, InputButtons.Medium)) s.bufMedium = p.inputBufferFrames;
            if (InputUtil.Has(pressed, InputButtons.Heavy)) s.bufHeavy = p.inputBufferFrames;
            if (InputUtil.Has(pressed, InputButtons.Throw)) s.bufThrow = p.inputBufferFrames;

            s.prevButtons = input;
            s.dirHistory = (s.dirHistory << 4) | (ulong)(uint)InputUtil.Numpad(input, s.facingRight);
        }

        // ============================================================
        //  ステートマシン
        // ============================================================
        private void StepFighter(ref FighterSimState s, InputButtons input)
        {
            s.stateFrame++;

            // 状態の終了判定
            switch (s.stateId)
            {
                case FighterStateId.LightAttack:
                case FighterStateId.MediumAttack:
                case FighterStateId.HeavyAttack:
                case FighterStateId.SpecialAttack1:
                case FighterStateId.AirAttack:
                    if (s.attackIndex < 0 || s.stateFrame >= attacks[s.attackIndex].TotalFrames)
                    {
                        EndToNeutral(ref s);
                    }
                    break;

                case FighterStateId.ThrowAttack:
                    if (s.attackHasHit) break;
                    if(s.attackIndex < 0 || s.stateFrame >= attacks[s.attackIndex].TotalFrames)
                    {
                        EndToNeutral(ref s);
                    }
                    else if (s.stateFrame >= attacks[s.attackIndex].startup + attacks[s.attackIndex].active)
                    {
                        // 投げが届かないまま判定が終わった → 投げスカリ
                        s.stateId = FighterStateId.ThrowMiss;
                    }
                    break;

                case FighterStateId.ThrowMiss:
                    if (s.attackIndex < 0 || s.stateFrame >= attacks[s.attackIndex].TotalFrames)
                    {
                        EndToNeutral(ref s);
                    }
                    break;

                case FighterStateId.ThrowEsc:
                    if (s.stateFrame >= p.throwEscFrames) EndToNeutral(ref s);
                    break;

                case FighterStateId.ForwardStep:
                    if (s.stateFrame >= p.forwardStepFrames) EndToNeutral(ref s);
                    break;

                case FighterStateId.BackStep:
                    if (s.stateFrame >= p.backStepFrames) EndToNeutral(ref s);
                    break;

                case FighterStateId.HitStun:
                case FighterStateId.BlockStun:
                    // ダウン確定の空中被弾は着地まで硬直が解けない
                    if (s.stateId == FighterStateId.HitStun && s.knockdown) break;
                    if (s.stateFrame >= s.hitStunFrames)
                    {
                        EndToNeutral(ref s);
                    }
                    break;

                case FighterStateId.Down:
                    if(s.stateFrame >= p.downFrame)
                    {
                        s.stateId = FighterStateId.WakeUp;
                        s.stateFrame = 0;
                    }
                    break;

                case FighterStateId.WakeUp:
                    if (s.stateFrame >= p.wakeUpFrame) EndToNeutral(ref s);
                    break;
            }

            // 状態ごとの処理
            switch (s.stateId)
            {
                case FighterStateId.Idle:
                case FighterStateId.WalkForward:
                case FighterStateId.WalkBack:
                case FighterStateId.Crouch:
                    GroundNeutral(ref s, input);
                    break;

                case FighterStateId.Jump:
                    TryStartAttack(ref s, true);
                    break;

                case FighterStateId.LightAttack:
                case FighterStateId.MediumAttack:
                case FighterStateId.HeavyAttack:
                    // 通常技がヒット/ガードされた後は、必殺技でキャンセルできる
                    TryCancelIntoSpecial(ref s);
                    break;

                case FighterStateId.BlockStun:
                    // ガード中は、下を押している間だけしゃがみ姿勢（ガード硬直は途切れない）
                    s.guardCrouch = InputUtil.Vertical(input) < 0;
                    if (s.Grounded) s.velX = MoveToward(s.velX, 0, p.hitStunGroundFriction);
                    break;

                case FighterStateId.HitStun:
                case FighterStateId.KO:
                case FighterStateId.Down:
                case FighterStateId.WakeUp:
                case FighterStateId.ThrowEsc:
                    // 地上ではノックバックの速度を徐々に減らす
                    if (s.Grounded) s.velX = MoveToward(s.velX, 0, p.hitStunGroundFriction);
                    break;

                    // Attack / AirAttack は、フレームが進むだけ（地上攻撃は速度 0 のまま）
            }

            Integrate(ref s);
        }

        private void GroundNeutral(ref FighterSimState s, InputButtons input)
        {
            int dx = InputUtil.Horizontal(input);
            int dy = InputUtil.Vertical(input);

            if (TryStartAttack(ref s, false)) return;

            if (dy > 0)
            {
                StartJump(ref s, dx);
                return;
            }

            if (dy < 0)
            {
                SetState(ref s, FighterStateId.Crouch);
                s.velX = 0;
                return;
            }

            if (s.stateId != FighterStateId.Crouch && TryStartStep(ref s)) return;

            if (dx == 0)
            {
                SetState(ref s, FighterStateId.Idle);
                s.velX = 0;
                return;
            }

            bool forward = (dx > 0) == s.facingRight;
            SetState(ref s, forward ? FighterStateId.WalkForward : FighterStateId.WalkBack);
            s.velX = dx * (forward ? p.walkForwardSpeed : p.walkBackSpeed);
        }

        // 前後どちらか2回の入力でステップ開始
        private bool TryStartStep(ref FighterSimState s)
        {
            bool forward;
            int forwardKey = 6, backKey = 4;
            if (IsDoubleTap(s.dirHistory, forwardKey, p.stepInputFrames)) forward = true;
            else if (IsDoubleTap(s.dirHistory, backKey, p.stepInputFrames)) forward = false;
            else return false;

            SetState(ref s, forward ? FighterStateId.ForwardStep : FighterStateId.BackStep);
            int dir = (forward == s.facingRight) ? 1 : -1;
            s.velX = dir * (forward ? p.forwardStepSpeed : p.backStepSpeed);
            return true;
        }

        private static bool IsDoubleTap(ulong history, int direction, int window)
        {
            if (window > 15) window = 15;   // 履歴は 16 フレーム分
            if ((int)(history & 0xF) != direction) return false;

            int k = 1;
            while (k <= window && (int)((history >> (4 * k)) & 0xF) == 5) k++;
            if (k == 1) return false;       // 直前がニュートラルではない

            return k <= window && (int)((history >> (4 * k)) & 0xF) == direction;
        }

        // 先行入力された攻撃ボタンがあれば、攻撃を開始する
        //   ・地上では、アシスト + 割り当てのある方向 + 弱/中/強 なら、必殺技（通常技より優先）
        //   ・それ以外は通常技（投げボタン > 強 > 中 > 弱 の優先順）
        private bool TryStartAttack(ref FighterSimState s, bool air)
        {
            if (!air && s.bufThrow == 0)
            {
                int special = FindBufferedSpecial(ref s);
                if (special >= 0)
                {
                    StartAttack(ref s, special, false);
                    return true;
                }
            }

            // 空中攻撃はすべて同じ技
            if (air) return TryStartAirAttack(ref s);

            // 地上で下を押している間は、しゃがみ版の通常技（空中では立ち版のデータを使う）
            bool crouching = !air && InputUtil.Vertical(s.prevButtons) < 0;

            int idx = -1;
            if (s.bufThrow > 0) { idx = (int)AttackType.Throw; s.bufThrow = 0; }
            else if (s.bufHeavy > 0) { idx = NormalAttackIndex(2, crouching); s.bufHeavy = 0; }
            else if (s.bufMedium > 0) { idx = NormalAttackIndex(1, crouching); s.bufMedium = 0; }
            else if (s.bufLight > 0) { idx = NormalAttackIndex(0, crouching); s.bufLight = 0; }

            if (idx < 0 || idx >= attacks.Length) return false;
            if (!attacks[idx].IsValid) return false;

            StartAttack(ref s, idx, air);
            return true;
        }

        // 攻撃の開始（通常技・必殺技共通）
        private void StartAttack(ref FighterSimState s, int idx, bool air)
        {
            s.attackIndex = idx;
            s.attackHasHit = false;
            s.stateId = air ? FighterStateId.AirAttack : GroundAttackState(idx);
            s.stateFrame = 0;

            if (!air)
            {
                // 通常技は速度 0。昇竜などは、技のデータで決まる速度で飛び出す
                AttackFrameData atk = attacks[idx];
                int dir = s.facingRight ? 1 : -1;
                s.velX = dir * atk.startVelX;
                s.velY = atk.startVelY;
            }
        }

        // 空中攻撃の開始
        private bool TryStartAirAttack(ref FighterSimState s)
        {
            int strength;
            if (s.bufHeavy > 0) strength = 2;
            else if (s.bufMedium > 0) strength = 1;
            else if (s.bufLight > 0) strength = 0;
            else return false;  // 投げはしない

            s.bufLight = 0;
            s.bufMedium = 0;
            s.bufHeavy = 0;

            int idx = (int)AttackType.Air_Attack;

            if (idx >= attacks.Length || !attacks[idx].IsValid) idx = NormalAttackIndex(strength, false);
            if (idx >= attacks.Length || !attacks[idx].IsValid) return false;

            StartAttack(ref s, idx, true);
            return true;
        }


        private static bool IsSpecialAttack(int attackIndex)
        {
            return attackIndex >= (int)AttackType.Special1_L;
        }

        // しゃがみ通常技か
        private static bool IsCrouchAttack(int attackIndex)
        {
            return attackIndex >= (int)AttackType.Crouch_Light && attackIndex <= (int)AttackType.Crouch_Heavy;
        }

        // 地上攻撃か
        private static bool IsGroundAttackState(FighterStateId id)
        {
            return id == FighterStateId.LightAttack ||
                   id == FighterStateId.MediumAttack ||
                   id == FighterStateId.HeavyAttack ||
                   id == FighterStateId.SpecialAttack1 ||
                   id == FighterStateId.ThrowAttack;
        }

        // 攻撃中か
        private static bool IsAttackState(FighterStateId id)
        {
            return IsGroundAttackState(id) || id == FighterStateId.AirAttack;
        }

        // 地上技の番号から攻撃の状態を決める
        // 必殺技を増やす場合はここにSpecialAttack2などの分岐を足す。
        private static FighterStateId GroundAttackState(int attackIndex)
        {
            if (IsThrowAttack(attackIndex)) return FighterStateId.ThrowAttack;
            if (IsSpecialAttack(attackIndex)) return FighterStateId.SpecialAttack1;

            switch (attackIndex % 3)
            {
                case 1: return FighterStateId.MediumAttack;
                case 2: return FighterStateId.HeavyAttack;
                default: return FighterStateId.LightAttack;
            }

        }

        // 投げか
        private static bool IsThrowAttack(int attackIndex)
        {
            return attackIndex == (int)AttackType.Throw;
        }

        // 投げができる状態かどうか
        private static bool CanBeThrown(FighterSimState s)
        {
            if (!s.Grounded) return false;

            switch (s.stateId)
            {
                case FighterStateId.Idle:
                case FighterStateId.WalkForward:
                case FighterStateId.WalkBack:
                case FighterStateId.Crouch:
                case FighterStateId.ForwardStep:
                case FighterStateId.BlockStun:
                case FighterStateId.LightAttack:
                case FighterStateId.MediumAttack:
                case FighterStateId.HeavyAttack:
                case FighterStateId.SpecialAttack1:
                case FighterStateId.ThrowAttack:
                case FighterStateId.ThrowMiss:
                    return true;

                default:
                    return false;
            }

        }

        // 通常技の番号（strength: 0 = 弱, 1 = 中, 2 = 強）。
        // AttackType は Stand_Light, Stand_Medium, Stand_Heavy / Crouch_Light, ... の連番
        private int NormalAttackIndex(int strength, bool crouching)
        {
            int stand = (int)AttackType.Stand_Light + strength;
            if (!crouching) return stand;

            int crouch = (int)AttackType.Crouch_Light + strength;
            // しゃがみ版のデータが無ければ、立ち版を使う（CSV の移行途中でも動くように）
            if (crouch < attacks.Length && attacks[crouch].IsValid) return crouch;
            return stand;
        }

        // アシストが押されていて、先行入力された 弱/中/強 と現在の方向に、必殺技が割り当てられていれば、
        // その技の番号を返す（先行入力を消費する）。無ければ -1（通常技にフォールバックする）
        private int FindBufferedSpecial(ref FighterSimState s)
        {
            if (p.specialBindings == null) return -1;
            if (!InputUtil.Has(s.prevButtons, InputButtons.Assist)) return -1;

            // 弱・中・強のどれが押されたか（強い方を優先）
            int strength;
            if (s.bufHeavy > 0) strength = 2;
            else if (s.bufMedium > 0) strength = 1;
            else if (s.bufLight > 0) strength = 0;
            else return -1;

            int dir = InputUtil.Numpad(s.prevButtons, s.facingRight);
            for (int i = 0; i < p.specialBindings.Length; i++)
            {
                if (p.specialBindings[i].direction != dir) continue;

                int idx = (int)p.specialBindings[i].firstAttack + strength;
                if (idx < 0 || idx >= attacks.Length || !attacks[idx].IsValid) return -1; // データが無ければ通常技

                if (strength == 2) s.bufHeavy = 0;
                else if (strength == 1) s.bufMedium = 0;
                else s.bufLight = 0;
                return idx;
            }
            return -1;
        }

        // 通常技がヒット/ガードされた後、アシスト + 方向 + 攻撃ボタンの入力があれば、必殺技でキャンセルする
        private void TryCancelIntoSpecial(ref FighterSimState s)
        {
            if (!p.allowSpecialCancel) return;
            if (s.attackIndex < 0 || IsSpecialAttack(s.attackIndex)) return; // 通常技からのみ
            if (!s.attackHasHit || !s.Grounded) return;

            int special = FindBufferedSpecial(ref s);
            if (special >= 0) StartAttack(ref s, special, false);
        }

        // 技の出始めの無敵（昇竜など）。CSV の invulnFrames の間は、攻撃を受けない
        private bool IsInvulnerable(FighterSimState s)
        {
            // ダウン～起き上がり中は常に無敵
            if (s.stateId == FighterStateId.Down || s.stateId == FighterStateId.WakeUp) return true;
            if (!IsAttackState(s.stateId)) return false;
            if (s.attackIndex < 0 || s.attackIndex >= attacks.Length) return false;

            return s.stateFrame < attacks[s.attackIndex].invulnFrames;
        }

        private void StartJump(ref FighterSimState s, int dx)
        {
            s.stateId = FighterStateId.Jump;
            s.stateFrame = 0;
            s.velY = p.jumpSpeed;

            if (dx == 0)
            {
                s.velX = 0;
            }
            else
            {
                bool forward = (dx > 0) == s.facingRight;
                s.velX = dx * (forward ? p.jumpForwardSpeed : p.jumpBackSpeed);
            }
        }

        private void SetState(ref FighterSimState s, FighterStateId id)
        {
            if (s.stateId != id)
            {
                s.stateId = id;
                s.stateFrame = 0;
            }
        }

        private void EndToNeutral(ref FighterSimState s)
        {
            s.attackIndex = -1;
            s.attackHasHit = false;
            s.stateFrame = 0;
            s.knockdown = false;
            s.comboCount = 0;
            s.stateId = s.Grounded ? FighterStateId.Idle : FighterStateId.Jump;
        }

        // 重力・位置の更新・着地
        private void Integrate(ref FighterSimState s)
        {
            bool wasAirborne = !s.Grounded;
            if (wasAirborne) s.velY -= p.gravity;

            // 技のデータで指定されていれば、最高到達点で横移動を止めて、垂直に落ちる（昇竜など）
            if (wasAirborne && s.velY <= 0 &&
                IsGroundAttackState(s.stateId) &&
                s.attackIndex >= 0 && s.attackIndex < attacks.Length &&
                attacks[s.attackIndex].stopXAtApex)
            {
                s.velX = 0;
            }

            s.posX += s.velX;
            s.posY += s.velY;

            if (wasAirborne && s.posY <= 0)
            {
                s.posY = 0;
                if (s.velY < 0) s.velY = 0;
                OnLand(ref s);
            }
        }

        private void OnLand(ref FighterSimState s)
        {
            switch (s.stateId)
            {
                case FighterStateId.Jump:
                    s.velX = 0;
                    s.stateId = FighterStateId.Idle;
                    s.stateFrame = 0;
                    break;

                case FighterStateId.AirAttack:
                    s.velX = 0;
                    s.attackIndex = -1;
                    s.attackHasHit = false;
                    s.stateId = FighterStateId.Idle;
                    s.stateFrame = 0;
                    break;

                case FighterStateId.LightAttack:
                case FighterStateId.MediumAttack:
                case FighterStateId.HeavyAttack:
                case FighterStateId.SpecialAttack1:
                case FighterStateId.ThrowAttack:
                    // 昇竜など、地上技で空中に出た技。着地で横移動は止まるが、技のフレームは続く
                    s.velX = 0;
                    break;
            
                case FighterStateId.HitStun:
                    // ダウン確定であれば着地でダウン移項
                    if (s.knockdown)
                    {
                        s.velX = 0;
                        s.knockdown = false;
                        s.stateId = FighterStateId.Down;
                        s.stateFrame = 0;
                    }
                    break;
            }
        }

        // ============================================================
        //  押し合い・位置の制限
        // ============================================================
        // しゃがみ姿勢か（しゃがみ状態、またはしゃがみガード中）
        private static bool IsCrouchPose(FighterSimState s)
        {
            return s.stateId == FighterStateId.Crouch ||
                   s.stateId == FighterStateId.Down ||
                   s.stateId == FighterStateId.WakeUp ||
                   (s.stateId == FighterStateId.BlockStun && s.guardCrouch) ||
                   (IsGroundAttackState(s.stateId) && IsCrouchAttack(s.attackIndex));
        }

        private static bool IsStunned(FighterSimState s)
        {
            return s.stateId == FighterStateId.HitStun || s.stateId == FighterStateId.BlockStun;
        }

        private int PushHeight(FighterSimState s)
        {
            return IsCrouchPose(s) ? p.crouchPushHeight : p.standPushHeight;
        }

        private void ResolvePush()
        {
            int topA = f[0].posY + PushHeight(f[0]);
            int topB = f[1].posY + PushHeight(f[1]);

            // 縦方向に重なっていなければ押し合わない（飛び越えられる）
            if (f[0].posY >= topB || f[1].posY >= topA) return;

            int dist = Math.Abs(f[0].posX - f[1].posX);
            int overlap = p.pushWidth - dist;
            if (overlap <= 0) return;

            int li = (f[0].posX <= f[1].posX) ? 0 : 1; // 左側のファイター（同位置は 1P を左とする）
            int ri = 1 - li;

            bool leftAir = f[li].posY > 0;
            bool rightAir = f[ri].posY > 0;

            int moveL;
            int moveR;
            if (leftAir && !rightAir) { moveL = overlap; moveR = 0; }      // 空中側だけが押し出される
            else if (rightAir && !leftAir) { moveL = 0; moveR = overlap; }
            else { moveL = overlap / 2; moveR = overlap - moveL; }         // 両方で半分ずつ

            // 壁際で動けない分は、相手が余計に動く
            int halfW = p.pushWidth / 2;
            int minPos = p.stageMinX + halfW;
            int maxPos = p.stageMaxX - halfW;
            int roomL = Math.Max(0, f[li].posX - minPos);
            int roomR = Math.Max(0, maxPos - f[ri].posX);

            if (moveL > roomL) { moveR += moveL - roomL; moveL = roomL; }
            if (moveR > roomR) { moveL += moveR - roomR; moveR = roomR; }

            f[li].posX -= moveL;
            f[ri].posX += moveR;
        }

        // 2人の距離が maxSeparation を超えたら、離れる方向に動いた側を引き戻す
        // （カメラが引き切った状態で、それ以上下がれない仕組み）
        private void LimitSeparation(int prevX0, int prevX1)
        {
            int excess = Math.Abs(f[0].posX - f[1].posX) - p.maxSeparation;
            if (excess <= 0) return;

            int li = (f[0].posX <= f[1].posX) ? 0 : 1;
            int ri = 1 - li;
            int prevL = (li == 0) ? prevX0 : prevX1;
            int prevR = (ri == 0) ? prevX0 : prevX1;

            // 画面端に向かって動いた量
            int awayL = Math.Max(0, prevL - f[li].posX);
            int awayR = Math.Max(0, f[ri].posX - prevR);
            int total = awayL + awayR;

            int takeL = 0;
            int takeR = 0;
            if (total > 0)
            {
                takeL = Math.Min(awayL, excess * awayL / total);
                takeR = Math.Min(awayR, excess - takeL);
            }

            int rest = excess - takeL - takeR;
            f[li].posX += takeL;
            f[ri].posX -= takeR;

            // 動いていない側の要因（ノックバックなど）で超えた分は、2人で歩み寄る
            if (rest > 0)
            {
                int halfRest = rest / 2;
                f[li].posX += halfRest;
                f[ri].posX -= rest - halfRest;
            }
        }

        // ステージ端を超えた分を押し戻す。
        // ノックバック・ガード硬直中のファイターが壁で止められた分は、相手が後ろに下がる（壁際の押し戻し）
        private void ClampStage()
        {
            int halfW = p.pushWidth / 2;
            int minPos = p.stageMinX + halfW;
            int maxPos = p.stageMaxX - halfW;

            for (int i = 0; i < 2; i++)
            {
                int lost = 0; // 壁に止められて動けなかった量（右向きが正）
                if (f[i].posX < minPos) { lost = f[i].posX - minPos; f[i].posX = minPos; }
                else if (f[i].posX > maxPos) { lost = f[i].posX - maxPos; f[i].posX = maxPos; }

                if (lost != 0 && IsStunned(f[i]))
                {
                    int o = 1 - i;
                    f[o].posX -= lost; // 壁と逆方向へ下がる
                    if (f[o].posX < minPos) f[o].posX = minPos;
                    if (f[o].posX > maxPos) f[o].posX = maxPos;
                }
            }
        }

        // 地上の動ける状態の時だけ、相手の方を向く
        private void UpdateFacing()
        {
            for (int i = 0; i < 2; i++)
            {
                FighterStateId id = f[i].stateId;
                bool canTurn = f[i].Grounded &&
                    (id == FighterStateId.Idle || id == FighterStateId.WalkForward ||
                     id == FighterStateId.WalkBack || id == FighterStateId.Crouch);
                if (!canTurn) continue;

                int dx = f[1 - i].posX - f[i].posX;
                if (dx > 0) f[i].facingRight = true;
                else if (dx < 0) f[i].facingRight = false;
            }
        }

        // ============================================================
        //  判定
        // ============================================================
        public SimRect GetPushRect(int i)
        {
            FighterSimState s = f[i];
            SimRect r = new SimRect();
            r.minX = s.posX - p.pushWidth / 2;
            r.maxX = s.posX + p.pushWidth / 2;
            r.minY = s.posY;
            r.maxY = s.posY + PushHeight(s);
            return r;
        }

        public SimRect GetHurtRect(int i)
        {
            FighterSimState s = f[i];
            int h = IsCrouchPose(s) ? p.crouchHurtHeight : p.standHurtHeight;
            SimRect r = new SimRect();
            r.minX = s.posX - p.hurtWidth / 2;
            r.maxX = s.posX + p.hurtWidth / 2;
            r.minY = s.posY;
            r.maxY = s.posY + h;
            return r;
        }

        // 攻撃判定が出ているフレームなら true を返し、その矩形を返す
        public bool TryGetHitRect(int i, out SimRect rect)
        {
            rect = new SimRect();
            FighterSimState s = f[i];

            if (!IsAttackState(s.stateId)) return false;
            if (s.attackIndex < 0 || s.attackHasHit) return false;

            AttackFrameData atk = attacks[s.attackIndex];
            if (s.stateFrame < atk.startup || s.stateFrame >= atk.startup + atk.active) return false;

            int cx = s.posX + (s.facingRight ? atk.hitboxOffsetX : -atk.hitboxOffsetX);
            int cy = s.posY + atk.hitboxOffsetY;
            rect.minX = cx - atk.hitboxWidth / 2;
            rect.maxX = cx + atk.hitboxWidth / 2;
            rect.minY = cy - atk.hitboxHeight / 2;
            rect.maxY = cy + atk.hitboxHeight / 2;
            return true;
        }

        private bool CheckHit(int attacker, int defender)
        {
            if (f[defender].stateId == FighterStateId.KO) return false;

            SimRect hit;
            if (!TryGetHitRect(attacker, out hit)) return false;

            SimRect hurt = GetHurtRect(defender);
            bool overlap = hit.Overlaps(hurt);
            bool invulnerable = overlap && IsInvulnerable(f[defender]);

            // デバッグ用に、判定が出ているフレームごとの結果を記録しておく
            HitCheckLog log = new HitCheckLog();
            log.valid = true;
            log.frame = frame;
            log.hit = hit;
            log.hurt = hurt;
            log.overlap = overlap;
            log.invulnerable = invulnerable;
            log.defenderState = f[defender].stateId;
            lastCheck[attacker] = log;

            bool throwable = !IsThrowAttack(f[attacker].attackIndex) || CanBeThrown(f[defender]);

            return overlap && !invulnerable && throwable;
        }

        private void ResolveHits()
        {
            // 両者の判定を、同じ時点の状態から先に判定する（相打ちを公平に扱う）
            bool hit0 = CheckHit(0, 1);
            bool hit1 = CheckHit(1, 0);
            if (!hit0 && !hit1) return;

            bool throw0 = hit0 && IsThrowAttack(f[0].attackIndex);
            bool throw1 = hit1 && IsThrowAttack(f[1].attackIndex);
            if(throw0 && throw1)
            {
                // 両者が投げた場合投げ抜け成立
                BeginThrowEsc();
                return;
            }

            if( throw0 && hit1) { hit0 = false; throw0 = false; }
            if( throw1 && hit0) { hit1 = false; throw1 = false; }

            if (throw0) { BeginGrab(0, 1); return; }
            if (throw1) { BeginGrab(1, 0); return; }

            AttackFrameData atk0 = hit0 ? attacks[f[0].attackIndex] : new AttackFrameData();
            AttackFrameData atk1 = hit1 ? attacks[f[1].attackIndex] : new AttackFrameData();
            bool face0 = f[0].facingRight;
            bool face1 = f[1].facingRight;

            // ガードされるかどうかも、適用前の状態で決める
            GuardResult guard0 = hit0 ? EvaluateGuard(0, 1, EffectiveLevel(0, atk0)) : GuardResult.FailState;
            GuardResult guard1 = hit1 ? EvaluateGuard(1, 0, EffectiveLevel(1, atk1)) : GuardResult.FailState;
            bool blocked0 = hit0 && guard0 == GuardResult.Blocked;
            bool blocked1 = hit1 && guard1 == GuardResult.Blocked;
            bool crouchGuard0 = InputUtil.Vertical(f[1].prevButtons) < 0; // 1P の攻撃を 2P がガードする時の姿勢
            bool crouchGuard1 = InputUtil.Vertical(f[0].prevButtons) < 0;

            // ヒットでもガードでも当たった扱い
            if (hit0)
            {
                f[0].attackHasHit = true;
                f[0].hitStopFrames = Math.Max(f[0].hitStopFrames, atk0.hitStop);
            }
            if (hit1)
            {
                f[1].attackHasHit = true;
                f[1].hitStopFrames = Math.Max(f[1].hitStopFrames, atk1.hitStop);
            }

            // デバッグ用ヒット結果
            if (hit0) RecordHit(1, blocked0, guard0, f[0].attackIndex);
            if (hit1) RecordHit(0, blocked1, guard1, f[1].attackIndex);

            // 防御側
            if (hit0)
            {
                if (blocked0) ApplyBlockToDefender(ref f[1], atk0, face0, crouchGuard0);
                else ApplyHitToDefender(ref f[1], atk0, face0);
            }
            if (hit1)
            {
                if (blocked1) ApplyBlockToDefender(ref f[0], atk1, face1, crouchGuard1);
                else ApplyHitToDefender(ref f[0], atk1, face1);
            }
        }

        private void RecordHit(int defender, bool blocked, GuardResult guard, int attackIndex)
        {
            HitLog log = new HitLog();
            log.valid = true;
            log.frame = frame;
            log.blocked = blocked;
            log.guard = guard;
            log.attackIndex = attackIndex;
            lastHit[defender] = log;
        }

        // ジャンプ攻撃はCSVの指定に関係なく上段扱い
        private GuardType EffectiveLevel(int attacker, AttackFrameData atk)
        {
            if (atk.guardType == GuardType.Unblockable) return GuardType.Unblockable;
            return (f[attacker].stateId == FighterStateId.AirAttack) ? GuardType.High : atk.guardType;
        }

        // defender が、attacker から level の攻撃を受けた時にガードできるか。失敗の場合は理由を返す
        //   地上の動ける状態で、相手から離れる方向を入力している時のみ
        //   上段は立ちガード、下段はしゃがみガードでのみ防げる
        //   投げは防げない
        private GuardResult EvaluateGuard(int attacker, int defender, GuardType level)
        {
            if (level == GuardType.Unblockable) return GuardResult.FailUnblockable;

            FighterSimState d = f[defender];
            switch (d.stateId)
            {
                case FighterStateId.Idle:
                case FighterStateId.WalkForward:
                case FighterStateId.WalkBack:
                case FighterStateId.Crouch:
                case FighterStateId.BlockStun:
                    break;
                default:
                    return GuardResult.FailState;
            }
            if (!d.Grounded) return GuardResult.FailState; // 空中ガードはない

            // ガード方向 = 相手から離れる方向（めくりでも、向きではなく位置関係で判定する）
            int away;
            if (d.posX > f[attacker].posX) away = 1;
            else if (d.posX < f[attacker].posX) away = -1;
            else away = d.facingRight ? -1 : 1;
            if (InputUtil.Horizontal(d.prevButtons) != away) return GuardResult.FailDirection;

            bool crouching = InputUtil.Vertical(d.prevButtons) < 0;
            if (level == GuardType.High)
            {
                return crouching ? GuardResult.FailCrouchVsHigh : GuardResult.Blocked;
            }
            return crouching ? GuardResult.Blocked : GuardResult.FailStandVsLow;
        }

        // ---- デバッグ表示用 ----

        public GuardResult PreviewGuard(int defender, GuardType level)
        {
            return EvaluateGuard(1 - defender, defender, level);
        }

        // 防御側の直近のヒット結果
        public HitLog GetLastHit(int defender)
        {
            return lastHit[defender];
        }

        // 攻撃側の直近の判定チェック
        public HitCheckLog GetLastCheck(int attacker)
        {
            return lastCheck[attacker];
        }

        // 投げはじめ
        private void BeginGrab(int attacker, int defender)
        {
            if (f[defender].stateId == FighterStateId.ThrowAttack)
            {
                BeginThrowEsc();
                return;
            }

            f[attacker].attackHasHit = true;
            f[attacker].stateId = FighterStateId.ThrowAttack;
            f[attacker].stateFrame = 0;
            f[attacker].hitStopFrames = 0;
            f[attacker].velX = 0;
            f[attacker].velY = 0;

            f[defender].attackIndex = -1;
            f[defender].attackHasHit = false;
            f[defender].guardCrouch = false;
            f[defender].stateId = FighterStateId.Thrown;
            f[defender].stateFrame = 0;
            f[defender].hitStopFrames = 0;
            f[defender].velX = 0;
            f[defender].velY = 0;
            f[defender].bufThrow = 0;
        }

        // 投げ抜け成立
        private void BeginThrowEsc()
        {
            for (int i = 0; i < 2; i++)
            {
                int other = 1 - i;
                int away;
                if (f[i].posX > f[other].posX) away = 1;
                else if (f[i].posX < f[other].posX) away = -1;
                else away = f[i].facingRight ? -1 : 1;

                f[i].attackIndex = -1;
                f[i].attackHasHit = false;
                f[i].guardCrouch = false;
                f[i].stateId = FighterStateId.ThrowEsc;
                f[i].stateFrame = 0;
                f[i].hitStopFrames = 0;
                f[i].velX = away * p.throwEscPushSpeed;
                f[i].velY = 0;
                f[i].bufThrow = 0;
            }
        }

        // 投げ中
        private void ResolveThrowStates()
        {
            for (int d = 0; d < 2; d++)
            {
                if (f[d].stateId != FighterStateId.Thrown) continue;

                int a = 1 - d;

                if (f[a].stateId != FighterStateId.ThrowAttack || !f[a].attackHasHit) continue;

                if (f[d].bufThrow > 0) BeginThrowEsc();
                else if (f[d].stateFrame >= p.throwTechFrames) FinishThrow(a, d);

                return;
            }
        }

        // 投げ成立
        private void FinishThrow(int attacker, int defender)
        {
            AttackFrameData atk = attacks[f[attacker].attackIndex];
            bool knockBackToRight = f[attacker].facingRight;

            RecordHit(defender, false, GuardResult.FailUnblockable, f[attacker].attackIndex);

            // 成立の瞬間に後ろが入っていれば位置入れ替え
            if (IsBackInput(f[attacker]))
            {
                int tmp = f[attacker].posX;
                f[attacker].posX = f[defender].posX;
                f[defender].posX = tmp;
                knockBackToRight = !knockBackToRight;
            }

            EndToNeutral(ref f[attacker]);
            f[attacker].hitStopFrames = Math.Max(f[attacker].hitStopFrames, atk.hitStop);

            ApplyHitToDefender(ref f[defender], atk, knockBackToRight);
        }

        // 後ろ入力が入っているかどうか
        private static bool IsBackInput(FighterSimState s)
        {
            int numpad = (int)(s.dirHistory & 0xF);
            return numpad == 1 || numpad == 4 || numpad == 7;
        }

        private void ApplyHitToDefender(ref FighterSimState d, AttackFrameData atk, bool attackerFacingRight)
        {
            // 被弾時、空中にいたか
            bool hitInAir = !d.Grounded;

            d.health -= atk.damage;
            if (d.health < 0) d.health = 0;

            d.attackIndex = -1;
            d.attackHasHit = false;
            d.guardCrouch = false;

            int dir = attackerFacingRight ? 1 : -1; // ノックバックは攻撃側の向きで決まる
            d.velX = dir * atk.knockbackX;
            d.velY = atk.knockbackY;

            d.hitStopFrames = Math.Max(d.hitStopFrames, atk.hitStop);
            d.hitStunFrames = atk.hitStun;

            // コンボ数を数え、技のしきい値に達していればダウン確定
            d.comboCount++;
            if (atk.knockDownCombo > 0 && d.comboCount >= atk.knockDownCombo) d.knockdown = true;

            if (hitInAir) d.knockdown = true;

            d.stateId = (d.health <= 0) ? FighterStateId.KO : FighterStateId.HitStun;
            d.stateFrame = 0;

            if (d.health <= 0)
            {
                d.knockdown = false;           // KO はダウンより優先
            }
            else if (d.knockdown && d.Grounded)
            {
                d.knockdown = false;
                d.stateId = FighterStateId.Down;
            }
        }

        private void ApplyBlockToDefender(ref FighterSimState d, AttackFrameData atk, bool attackerFacingRight, bool crouching)
        {
            // 削りダメージ
            int chip = atk.chipDamage;
            if (chip < 0) chip = 0;
            if (!p.chipCanKill && chip >= d.health) chip = d.health - 1;
            if (chip < 0) chip = 0;
            d.health -= chip;
            if (d.health < 0) d.health = 0;

            d.attackIndex = -1;
            d.attackHasHit = false;

            int dir = attackerFacingRight ? 1 : -1;
            d.velX = dir * atk.blockKnockbackX; // ガードの押し戻し
            d.velY = 0;

            d.hitStopFrames = Math.Max(d.hitStopFrames, atk.hitStop);
            d.hitStunFrames = atk.blockStun;
            d.guardCrouch = crouching;
            d.stateId = (d.health <= 0) ? FighterStateId.KO : FighterStateId.BlockStun;
            d.stateFrame = 0;
        }

        // ---- ユーティリティ ----
        private static int MoveToward(int value, int target, int maxDelta)
        {
            if (value < target) return Math.Min(value + maxDelta, target);
            return Math.Max(value - maxDelta, target);
        }
    }
}