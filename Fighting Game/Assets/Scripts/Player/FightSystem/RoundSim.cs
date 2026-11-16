using System;

namespace FightSim
{
    public enum RoundPhase : byte
    {
        Intro,      // ラウンド開始(SF6の Round1 Fight みたいな始まりの合図)
        Fight,      // 戦闘中
        RoundEnd,   // KO(次のラウンドに進む前の勝利演出)
        MatchEnd    // 試合終了(再選するかどうかを選べる)
    }

    // ラウンド終了の要因
    public enum RoundEndReason : byte
    {
        None,
        KO,
        DoubleKO,
        TimeUp
    }

    [Serializable]
    public class RoundParams 
    {
        public int roundsToWin = 2;             // 何ラウンド先取か
        public int roundTimeSeconds = 99;       // 1ラウンドの制限時間
        public int introFrames = 180;           // ラウンド開始の長さ
        public int roundEndFrames = 180;        // ラウンド終了の長さ
        public int winPoseDelayFrames = 180;    // KOから勝利ポーズまでの時間
        public int winPoseFrames = 150;         // 勝利ポーズの時間
    }

    public struct RoundSnapshot
    {
        public MatchSnapshot match;
        public RoundPhase phase;
        public int phaseFrame;
        public int timerFrames;
        public int roundNumber;
        public int wins0;
        public int wins1;
        public RoundEndReason endReason;
        public int roundWinner;
        public int matchWinner;
    }

    public class RoundSim
    {
        private readonly MatchSim sim;
        private readonly RoundParams rp;

        private RoundPhase phase;
        private int phaseFrame;     // 今のフェーズになってからの経過ティック
        private int timerFrames;    // ラウンドの残りティック（無制限の時は使わない）
        private int roundNumber;    // 1 から数える
        private int wins0;
        private int wins1;
        private RoundEndReason endReason;
        private int roundWinner;    // 0 = 1P, 1 = 2P, -1 = 引き分け（RoundEnd / MatchEnd の時だけ意味がある）
        private int matchWinner;
        private bool simWasReset;

        public MatchSim Match { get { return sim; } }
        public RoundParams Params { get { return rp; } }
        public RoundPhase Phase { get { return phase; } }
        public int PhaseFrame { get { return phaseFrame; } }
        public int RoundNumber { get { return roundNumber; } }
        public RoundEndReason EndReason { get { return endReason; } }
        public int RoundWinner { get { return roundWinner; } }
        public int MatchWinner { get { return matchWinner; } }
        public int RoundEndLength
        { 
            get
            {
                if (endReason == RoundEndReason.KO) return rp.winPoseDelayFrames + rp.winPoseFrames;
                else return rp.roundEndFrames;
            } 
        }
        public int WinnerShowFrame
        { 
            get
            {
                if (endReason == RoundEndReason.KO) return rp.winPoseDelayFrames;
                else return rp.roundEndFrames / 2;
            }
        }
        
        public bool SimWasReset { get { return simWasReset; } }
        public bool TimerEnabled { get { return rp.roundTimeSeconds > 0; } }

        // 残り秒数
        public int TimerSeconds
        {
            get
            {
                if (!TimerEnabled) return -1;
                return (timerFrames + SimConst.fps - 1) / SimConst.fps;
            }
        }

        public int Wins(int player)
        {
            return player == 0 ? wins0 : wins1;
        }

        public RoundSim(MatchSim match, RoundParams parameters)
        {
            sim = match;
            rp = parameters;
            wins0 = 0;
            wins1 = 0;
            roundNumber = 1;
            matchWinner = -1;
            StartRound();
        }

        public void ResetMatch()
        {
            wins0 = 0;
            wins1 = 0;
            roundNumber = 1;
            matchWinner = -1;
            StartRound();
        }

        public void Tick(InputButtons input0, InputButtons input1)
        {
            simWasReset = false;

            switch (phase)
            {
                case RoundPhase.Intro:
                    phaseFrame++;
                    if(phaseFrame >= rp.introFrames)
                    {
                        phase = RoundPhase.Fight;
                        phaseFrame = 0;
                    }
                    break;

                case RoundPhase.Fight:
                    phaseFrame++;
                    sim.Tick(input0, input1);
                    if (TimerEnabled && timerFrames > 0) timerFrames--;
                    CheckRoundEnd();
                    break;

                case RoundPhase.RoundEnd:
                    phaseFrame++;
                    // KO時、倒れる動きが終わるまで進める。タイムアップの場合は止める
                    if (endReason != RoundEndReason.TimeUp) sim.Tick(InputButtons.None, InputButtons.None);
                    if (endReason == RoundEndReason.KO && phaseFrame >= rp.winPoseDelayFrames) sim.TryEnterWin(roundWinner);
                    if (phaseFrame >= RoundEndLength) FinishRound();
                    break;

                case RoundPhase.MatchEnd:
                    phaseFrame++;
                    if (endReason != RoundEndReason.TimeUp) sim.Tick(InputButtons.None, InputButtons.None);
                    break;
            }
        }

        private void StartRound()
        {
            sim.Reset();
            timerFrames = TimerEnabled ? rp.roundTimeSeconds * SimConst.fps : 0;
            phase = RoundPhase.Intro;
            phaseFrame = 0;
            endReason = RoundEndReason.None;
            roundWinner = -1;
            simWasReset = true;
        }

        private void CheckRoundEnd()
        {
            int h0 = sim.Get(0).health;
            int h1 = sim.Get(1).health;

            if(h0 <= 0 || h1 <= 0)
            {
                if(h0 <= 0 && h1 <= 0)
                {
                    endReason = RoundEndReason.DoubleKO;
                    roundWinner = -1;
                }
                else
                {
                    endReason = RoundEndReason.KO;
                    roundWinner = (h0 <= 0) ? 1 : 0;
                }
            }
            else if(TimerEnabled && timerFrames <= 0)
            {
                endReason = RoundEndReason.TimeUp;
                // 最終的に体力が高かった方が勝ち、同じなら引き分け
                if (h0 > h1) roundWinner = 0;
                else if (h1 > h0) roundWinner = 1;
                else roundWinner = -1;
            }
            else
            {
                return;
            }

            phase = RoundPhase.RoundEnd;
            phaseFrame = 0;

            // 引き分けならば両者に1勝
            if (roundWinner == 0 || roundWinner == -1) wins0++;
            if (roundWinner == 1 || roundWinner == -1) wins1++;
        }

        private void FinishRound()
        {
            int need = Math.Max(1, rp.roundsToWin);
            bool win0 = wins0 >= need;
            bool win1 = wins1 >= need;

            if(win0 || win1)
            {
                phase = RoundPhase.MatchEnd;
                phaseFrame = 0;
                if (win0 && win1) matchWinner = -1;
                else if (win0) matchWinner = 0;
                else matchWinner = 1;
            }
            else
            {
                roundNumber++;
                StartRound();
            }
        }

        public RoundSnapshot Save()
        {
            RoundSnapshot s = new RoundSnapshot();
            s.match = sim.Save();
            s.phase = phase;
            s.phaseFrame = phaseFrame;
            s.timerFrames = timerFrames;
            s.roundNumber = roundNumber;
            s.wins0 = wins0;
            s.wins1 = wins1;
            s.endReason = endReason;
            s.roundWinner = roundWinner;
            s.matchWinner = matchWinner;
            return s;
        }

        public void Load(RoundSnapshot s)
        {
            sim.Load(s.match);
            phase = s.phase;
            phaseFrame = s.phaseFrame;
            timerFrames = s.timerFrames;
            roundNumber = s.roundNumber;
            wins0 = s.wins0;
            wins1 = s.wins1;
            endReason = s.endReason;
            roundWinner = s.roundWinner;
            matchWinner = s.matchWinner;
        }
    }
}
