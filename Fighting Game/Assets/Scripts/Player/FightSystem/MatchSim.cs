using System;

namespace FightSim
{
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
            s.dirHistory = (s.dirHistory << 4) | (ulong)InputUtil.Numpad(input, s.facingRight);
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
                case FighterStateId.Attack:
                case FighterStateId.AirAttack:
                    if (s.attackIndex < 0 || s.stateFrame >= attacks[s.attackIndex].TotalFrames)
                    {
                        EndToNeutral(ref s);
                    }
                    break;
                case FighterStateId.HitStun:
                case FighterStateId.BlockStun:
                    if (s.stateFrame >= s.hitStunFrames)
                    {
                        EndToNeutral(ref s);
                    }
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

                case FighterStateId.BlockStun:
                    // ガード中は、下を押している間だけしゃがみ姿勢（ガード硬直は途切れない）
                    s.guardCrouch = InputUtil.Vertical(input) < 0;
                    if (s.Grounded) s.velX = MoveToward(s.velX, 0, p.hitStunGroundFriction);
                    break;

                case FighterStateId.HitStun:
                case FighterStateId.KO:
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

        // 先行入力された攻撃ボタンがあれば、攻撃を開始する
        private bool TryStartAttack(ref FighterSimState s, bool air)
        {
            int idx = -1;
            if (s.bufThrow > 0) { idx = (int)AttackType.Throw; s.bufThrow = 0; }
            else if (s.bufHeavy > 0) { idx = (int)AttackType.Heavy; s.bufHeavy = 0; }
            else if (s.bufMedium > 0) { idx = (int)AttackType.Medium; s.bufMedium = 0; }
            else if (s.bufLight > 0) { idx = (int)AttackType.Light; s.bufLight = 0; }

            if (idx < 0 || idx >= attacks.Length) return false;
            if (!attacks[idx].IsValid) return false;

            s.attackIndex = idx;
            s.attackHasHit = false;
            s.stateId = air ? FighterStateId.AirAttack : FighterStateId.Attack;
            s.stateFrame = 0;
            if (!air) s.velX = 0;
            return true;
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
            s.stateId = s.Grounded ? FighterStateId.Idle : FighterStateId.Jump;
        }

        // 重力・位置の更新・着地
        private void Integrate(ref FighterSimState s)
        {
            bool wasAirborne = !s.Grounded;
            if (wasAirborne) s.velY -= p.gravity;

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
                    // HitStun / KO は着地しても状態を維持（速度は地上摩擦で減る）
            }
        }

        // ============================================================
        //  押し合い・位置の制限
        // ============================================================
        // しゃがみ姿勢か（しゃがみ状態、またはしゃがみガード中）
        private static bool IsCrouchPose(FighterSimState s)
        {
            return s.stateId == FighterStateId.Crouch ||
                   (s.stateId == FighterStateId.BlockStun && s.guardCrouch);
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

            if (s.stateId != FighterStateId.Attack && s.stateId != FighterStateId.AirAttack) return false;
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
            return hit.Overlaps(GetHurtRect(defender));
        }

        private void ResolveHits()
        {
            // 両者の判定を、同じ時点の状態から先に判定する（相打ちを公平に扱う）
            bool hit0 = CheckHit(0, 1);
            bool hit1 = CheckHit(1, 0);
            if (!hit0 && !hit1) return;

            AttackFrameData atk0 = hit0 ? attacks[f[0].attackIndex] : new AttackFrameData();
            AttackFrameData atk1 = hit1 ? attacks[f[1].attackIndex] : new AttackFrameData();
            bool face0 = f[0].facingRight;
            bool face1 = f[1].facingRight;

            // ガードされるかどうかも、適用前の状態で決める
            bool blocked0 = hit0 && IsBlocked(0, 1, atk0);
            bool blocked1 = hit1 && IsBlocked(1, 0, atk1);
            bool crouchGuard0 = InputUtil.Vertical(f[1].prevButtons) < 0; // 1P の攻撃を 2P がガードする時の姿勢
            bool crouchGuard1 = InputUtil.Vertical(f[0].prevButtons) < 0;

            // 攻撃側: ヒットでもガードでも、当たった扱い（多段防止・ヒットストップ）
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

        // defender が、attacker の攻撃 atk をガードできるか
        //   ・ガードできるのは、地上の動ける状態（またはガード硬直中）で、相手から離れる方向を入力している時
        //   ・上段は立ちガード、下段はしゃがみガードでのみ防げる（ジャンプ攻撃は上段）
        //   ・投げ（ガード不能）は防げない
        private bool IsBlocked(int attacker, int defender, AttackFrameData atk)
        {
            if (atk.guardType == GuardType.Unblockable) return false;

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
                    return false;
            }
            if (!d.Grounded) return false; // 空中ガードはない

            // ガード方向 = 相手から離れる方向（めくりでも、向きではなく位置関係で判定する）
            int away;
            if (d.posX > f[attacker].posX) away = 1;
            else if (d.posX < f[attacker].posX) away = -1;
            else away = d.facingRight ? -1 : 1;
            if (InputUtil.Horizontal(d.prevButtons) != away) return false;

            bool crouching = InputUtil.Vertical(d.prevButtons) < 0;
            GuardType level = (f[attacker].stateId == FighterStateId.AirAttack) ? GuardType.High : atk.guardType;
            return (level == GuardType.High) ? !crouching : crouching;
        }

        private void ApplyHitToDefender(ref FighterSimState d, AttackFrameData atk, bool attackerFacingRight)
        {
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
            d.stateId = (d.health <= 0) ? FighterStateId.KO : FighterStateId.HitStun;
            d.stateFrame = 0;
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