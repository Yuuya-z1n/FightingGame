using UnityEngine;
using FightSim;

/// <summary>
/// 画面にステート・フレーム数・体力などを表示するデバッグ用オーバーレイ。
/// （IMGUI の標準フォントは日本語が表示できないため、英語表記にしている）
/// </summary>
public class FightDebugOverlay : MonoBehaviour
{
    [SerializeField] private FightManager manager;
    [SerializeField] private bool visible = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) visible = !visible; // F1 で表示切り替え
    }

    private void OnGUI()
    {
        if (!visible || manager == null || manager.Sim == null) return;

        DrawFighter(0, new Rect(10, 10, 280, 230));
        DrawFighter(1, new Rect(Screen.width - 290, 10, 280, 230));

        GUI.Label(new Rect(Screen.width * 0.5f - 90, 10, 180, 40),
            "Tick " + manager.Sim.Frame + (manager.Sim.IsOver ? "  [KO]" : "") +
            "\nR: reset   F1: overlay");
    }

    private void DrawFighter(int i, Rect rect)
    {
        FighterSimState s = manager.Sim.Get(i);
        int numpad = (int)(s.dirHistory & 0xF);

        string text =
            "State : " + s.stateId + "\n" +
            "Frame : " + s.stateFrame + "\n" +
            "Attack: " + (s.attackIndex < 0 ? "-" : ((AttackType)s.attackIndex).ToString()) +
                (s.attackHasHit ? " (hit)" : "") + "\n" +
            "HitStop: " + s.hitStopFrames + "   HitStun: " + s.hitStunFrames + "\n" +
            "HP    : " + s.health + "\n" +
            "Pos   : " + s.posX + ", " + s.posY + "\n" +
            "Vel   : " + s.velX + ", " + s.velY + "\n" +
            "Facing: " + (s.facingRight ? "Right" : "Left") + "   Grounded: " + s.Grounded + "\n" +
            "Input : " + numpad + "  " + s.prevButtons + "\n" +
            "Buffer: L" + s.bufLight + " M" + s.bufMedium + " H" + s.bufHeavy + " T" + s.bufThrow;

        GUI.Box(rect, "P" + (i + 1));
        GUI.Label(new Rect(rect.x + 8, rect.y + 22, rect.width - 16, rect.height - 26), text);
    }
}
