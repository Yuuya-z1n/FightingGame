using FightSim;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FightHUD : MonoBehaviour
{
    [SerializeField] private FightManager manager;

    [Header("体力バー 上がすぐに減る 下が遅れて減る")]
    [SerializeField] private Image p1Fill;
    [SerializeField] private Image p1Delay;
    [SerializeField] private Image p2Fill;
    [SerializeField] private Image p2Delay;

    [Header("パラメータ")]
    [SerializeField] private float delayHoldSeconds = 0.5f;
    [SerializeField] private float decreaseDelayPerSecond = 0.5f;

    [Header("タイマー")]
    [SerializeField] private TMP_Text timerText;

    [Header("ラウンド勝利マーク")]
    [SerializeField] private Image[] p1WinMarks;
    [SerializeField] private Image[] p2WinMarks;
    [SerializeField] private Color winOnColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color winOffColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("バナー")]
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private int fightBannerFrames = 40;

    private class Gauge
    {
        public Image fill;
        public Image delay;
        public float delayValue = 1.0f;
        public float lastRatio = 1.0f;
        public float holdTime;
    }

    private Gauge gauge1;
    private Gauge gauge2;

    private void Awake()
    {
        gauge1 = new Gauge { fill = p1Fill, delay = p1Delay };
        gauge2 = new Gauge { fill = p2Fill, delay = p2Delay };
    }

    private void LateUpdate()
    {
        if (manager == null || manager.Round == null) return;
        RoundSim r = manager.Round;

        float max = Mathf.Max(1, manager.MaxHealth);

        UpdateGauge(gauge1, r.Match.Get(0).health / max);
        UpdateGauge(gauge2, r.Match.Get(1).health / max);

        UpdateTimer(r);
        UpdateWinMarks(p1WinMarks, r.Wins(0));
        UpdateWinMarks(p2WinMarks, r.Wins(1));
        UpdateBanner(r);

    }

    private void UpdateGauge(Gauge g, float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        if(ratio >= g.delayValue)
        {
            g.delayValue = ratio;
            g.holdTime = 0.0f;
        }
        else
        {
            // 新しくダメージを受けたら待ち時間を直す
            if (ratio < g.lastRatio) g.holdTime = delayHoldSeconds;

            if(g.holdTime > 0.0f)
            {
                g.holdTime -= Time.deltaTime;
            }
            else
            {
                g.delayValue = Mathf.Max(ratio, g.delayValue - decreaseDelayPerSecond * Time.deltaTime);
            }
        }
        g.lastRatio = ratio;

        if (g.fill != null) g.fill.fillAmount = ratio;
        if (g.delay != null) g.delay.fillAmount = g.delayValue;
    }

    private void UpdateTimer(RoundSim r)
    {
        int sec = r.TimerSeconds;
        if(sec < 0)
        {
            SetText(timerText, "--");
        }
        else
        {
            SetText(timerText, sec.ToString());
        }
    }

    private void UpdateWinMarks(Image[] marks, int wins)
    {
        if (marks == null) return;
        for(int i = 0; i < marks.Length; i++)
        {
            if (marks[i] != null)
            {
                if (i < wins) marks[i].color = winOnColor;
                else marks[i].color = winOffColor;
            }
        }
    }

    private void UpdateBanner(RoundSim r)
    {
        string main = "";
        string sub = "";

        switch (r.Phase)
        {
            case RoundPhase.Intro:
                main = "ROUND " + r.RoundNumber;
                break;

            case RoundPhase.Fight:
                if (r.PhaseFrame < fightBannerFrames) main = "FIGHT!";
                break;

            case RoundPhase.RoundEnd:
                if (r.PhaseFrame < r.WinnerShowFrame) EndReasonText(r.EndReason);
                else WinnerText(r.RoundWinner);
                break;

            case RoundPhase.MatchEnd:
                main = WinnerText(r.MatchWinner);
                sub = "Press R to Restart";
                break;
        }

        SetText(bannerText, main);
        SetText(subText, sub);
    }

    private static string EndReasonText(RoundEndReason reason)
    {
        switch (reason)
        {
            case RoundEndReason.KO: return "KO";
            case RoundEndReason.DoubleKO: return "DOUBLE KO";
            case RoundEndReason.TimeUp: return "TIME UP";
            default: return "";
        }
    }

    private static string WinnerText(int winner)
    {
        if (winner == 0) return "P1 WIN";
        if (winner == 1) return "P2 WIN";
        return "DRAW";
    }

    private static void SetText(TMP_Text t, string s)
    {
        if (t != null && t.text != s) t.text = s;
    }
}
