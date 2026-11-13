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
        public int roundsToWin = 2;         // 何ラウンド先取か
        public int roundTimeSeconds = 99;   // 1ラウンドの制限時間
        public int introFrames = 180;       // ラウンド開始の長さ
        public int roundEndFrames = 180;    // ラウンド終了の長さ
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
        private int matchWinner;    // 同上（MatchEnd の時だけ意味がある）
        private bool simWasReset;

        public MatchSim Match { get { return sim; } }
        public RoundParams Params { get { return rp; } }
        public RoundPhase Phase { get { return phase; } }
        public int PhaseFrame { get { return phaseFrame; } }
        public int RoundNumber { get { return roundNumber; } }
        public RoundEndReason EndReason { get { return endReason; } }
        public int RoundWinner { get { return roundWinner; } }
        public int MatchWinner { get { return matchWinner; } }

        
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
            //StartRound();
        }

        public void ResetMatch()
        {
            wins0 = 0;
            wins1 = 0;
            roundNumber = 1;
        }
    }
}
