using FightSim;
using Fusion;
using UnityEngine;

/// <summary>
/// 画面にステート・フレーム数・体力・ガード状況などを表示するデバッグ用オーバーレイ。
/// （IMGUI の標準フォントは日本語が表示できないため、英語表記にしている）
/// </summary>
public class FightDebugOverlay : MonoBehaviour
{
    [SerializeField] private FightManager manager;
    [SerializeField] private bool visible = true;

    [Header("ガード表示")]
    [Tooltip("直近のヒット結果を表示しておくフレーム数（色付きで強調）")]
    [SerializeField] private int hitResultHighlightFrames = 90;

    private static readonly Color BlockingColor = new Color(0.5f, 1f, 0.6f);
    private static readonly Color BlockedHitColor = new Color(0.6f, 0.9f, 1f);
    private static readonly Color FailedGuardColor = new Color(1f, 0.55f, 0.5f);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) visible = !visible; // F1 で表示切り替え
    }

    private void OnGUI()
    {
        if (!visible || manager == null || manager.Sim == null) return;

        DrawFighter(0, new Rect(10, 10, 320, 400));
        DrawFighter(1, new Rect(Screen.width - 330, 10, 320, 400));

        GUI.Label(new Rect(Screen.width * 0.5f - 90, 10, 180, 40),
            "Tick " + manager.Sim.Frame + (manager.Sim.IsOver ? "  [KO]" : "") +
            "\nR: reset   F1: overlay");
    }

    private void DrawFighter(int i, Rect rect)
    {
        MatchSim sim = manager.Sim;
        FighterSimState s = sim.Get(i);
        int numpad = (int)(s.dirHistory & 0xF);
        bool blocking = s.stateId == FighterStateId.BlockStun;

        string text =
            "State : " + s.stateId + "\n" +
            "Frame : " + s.stateFrame + "\n" +
            "Attack: " + (s.attackIndex < 0 ? "-" : ((AttackType)s.attackIndex).ToString()) +
                (s.attackHasHit ? " (hit)" : "") + "\n" +
            "HitStop: " + s.hitStopFrames + "   Stun: " + s.hitStunFrames + "\n" +
            "HP    : " + s.health + "\n" +
            "Pos   : " + s.posX + ", " + s.posY + "\n" +
            "Vel   : " + s.velX + ", " + s.velY + "\n" +
            "Facing: " + (s.facingRight ? "Right" : "Left") + "   Grounded: " + s.Grounded + "\n" +
            "Input : " + numpad + "  " + s.prevButtons + "\n" +
            "Buffer: L" + s.bufLight + " M" + s.bufMedium + " H" + s.bufHeavy + " T" + s.bufThrow;

        // ---- ガードの状況 ----
        string guardLine = blocking
            ? "BLOCKING (" + (s.guardCrouch ? "crouch" : "stand") + ")  " +
              (s.stateFrame) + "/" + s.hitStunFrames
            : "-";

        // 今この入力で、上段・下段の攻撃を受けたらガードできるか
        string preview =
            "High: " + ShortGuard(sim.PreviewGuard(i, GuardType.High)) +
            "   Low: " + ShortGuard(sim.PreviewGuard(i, GuardType.Low));

        // 直近のヒット結果
        HitLog log = sim.GetLastHit(i);
        string lastHit = "-";
        Color lastColor = Color.white;
        if (log.valid)
        {
            string atk = ((AttackType)log.attackIndex).ToString();
            int age = sim.Frame - log.frame;
            lastHit = (log.blocked ? "BLOCKED " : "HIT ") + atk +
                      (log.blocked ? "" : "  (" + log.guard + ")") +
                      "  " + age + "F ago";
            if (age <= hitResultHighlightFrames)
                lastColor = log.blocked ? BlockedHitColor : FailedGuardColor;
        }

        // 枠はガード中だけ緑色にする
        Color prev = GUI.color;
        GUI.color = blocking ? BlockingColor : prev;
        GUI.Box(rect, "P" + (i + 1) + (blocking ? "  [GUARD]" : ""));
        GUI.color = prev;

        float x = rect.x + 8;
        float w = rect.width - 16;
        GUI.Label(new Rect(x, rect.y + 22, w, 190), text);

        GUI.color = blocking ? BlockingColor : prev;
        GUI.Label(new Rect(x, rect.y + 214, w, 20), "Guard : " + guardLine);
        GUI.color = prev;

        GUI.Label(new Rect(x, rect.y + 234, w, 20), "Guard now> " + preview);

        GUI.color = lastColor;
        GUI.Label(new Rect(x, rect.y + 254, w, 40), "Last hit: " + lastHit);
        GUI.color = prev;

        // ---- この選手が攻撃した時の判定チェック ----
        // 攻撃判定が出ていたフレームの、判定の矩形と、相手の食らい判定の矩形（sim単位）
        HitCheckLog chk = sim.GetLastCheck(i);
        string checkText = "Atk check: -";
        Color checkColor = Color.white;
        if (chk.valid)
        {
            int age = sim.Frame - chk.frame;
            checkText =
                "Atk check: " + (chk.invulnerable ? "INVULN" : (chk.overlap ? "OVERLAP" : "MISS")) + "  " + age + "F ago\n" +
                "  hitbox  x " + chk.hit.minX + ".." + chk.hit.maxX + "  y " + chk.hit.minY + ".." + chk.hit.maxY + "\n" +
                "  hurtbox x " + chk.hurt.minX + ".." + chk.hurt.maxX + "  y " + chk.hurt.minY + ".." + chk.hurt.maxY + "\n" +
                "  target state: " + chk.defenderState + (chk.invulnerable ? "  [INVULN]" : "");
            checkColor = (chk.overlap && !chk.invulnerable) ? BlockedHitColor : FailedGuardColor;
        }
        GUI.color = checkColor;
        GUI.Label(new Rect(x, rect.y + 296, w, 100), checkText);
        GUI.color = prev;
    }

    // ガード判定の結果を短く表示する（OK = 今ガードできる）
    private static string ShortGuard(GuardResult r)
    {
        switch (r)
        {
            case GuardResult.Blocked: return "OK";
            case GuardResult.FailDirection: return "NG(no back)";
            case GuardResult.FailState: return "NG(state)";
            case GuardResult.FailStandVsLow: return "NG(stand)";
            case GuardResult.FailCrouchVsHigh: return "NG(crouch)";
            case GuardResult.FailUnblockable: return "NG(throw)";
            default: return r.ToString();
        }
    }
}