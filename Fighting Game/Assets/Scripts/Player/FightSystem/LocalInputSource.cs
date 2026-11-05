using UnityEngine;
using UnityEngine.InputSystem;
using FightSim;

/// <summary>
/// ローカル入力（キーボード / ゲームパッド）を、シミュレーション用の InputButtons に変換する。
/// ・Poll()    : Unity の Update ごとに呼ぶ（押している状態と、押した瞬間を記録）
/// ・Consume() : シミュレーションの1ティックごとに呼ぶ（そのティックの入力を返す）
/// 将来オンライン対戦にする時は、この入力を Fusion の INetworkInput に詰めて送る。
/// </summary>
public class LocalInputSource
{
    private const float StickThreshold = 0.5f;

    // ---- キーボード割り当て（ゲームパッドが未接続の時に使われる）----
    // 順番: 右, 左, 上, 下, 弱, 中, 強, 投げ, アシスト。キーを変えたい時は、ここだけ書き換える。
    private static readonly KeyCode[] Player1Keys =
    {
        KeyCode.D, KeyCode.A, KeyCode.W, KeyCode.S,
        KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.J
    };

    private static readonly KeyCode[] Player2Keys =
    {
        KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.UpArrow, KeyCode.DownArrow,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V,
        KeyCode.B
    };

    // 上のキー配列と同じ順番のボタン
    private static readonly InputButtons[] KeyButtons =
    {
        InputButtons.Right, InputButtons.Left, InputButtons.Up, InputButtons.Down,
        InputButtons.Light, InputButtons.Medium, InputButtons.Heavy, InputButtons.Throw,
        InputButtons.Assist
    };

    // 「押した瞬間」を保持するボタン（攻撃・アシスト・ジャンプ。方向の短い入力は保持しない）
    private const InputButtons LatchMask =
        InputButtons.Light | InputButtons.Medium | InputButtons.Heavy | InputButtons.Throw |
        InputButtons.Assist | InputButtons.Up;

    private readonly int playerIndex;
    private InputButtons held;     // 現在押されているボタン
    private InputButtons latched;  // 前回のティック以降に「押された瞬間」があったボタン

    public LocalInputSource(int playerIndex)
    {
        this.playerIndex = playerIndex;
    }

    public void Poll()
    {
        InputButtons now = InputButtons.None;
        InputButtons down = InputButtons.None;

        var pads = Gamepad.all;
        if (pads.Count > playerIndex)
        {
            // ---- ゲームパッド ----
            var pad = pads[playerIndex];
            Vector2 stick = pad.leftStick.ReadValue();

            if (stick.x > StickThreshold || pad.dpad.right.isPressed) now |= InputButtons.Right;
            if (stick.x < -StickThreshold || pad.dpad.left.isPressed) now |= InputButtons.Left;
            if (stick.y > StickThreshold || pad.dpad.up.isPressed) now |= InputButtons.Up;
            if (stick.y < -StickThreshold || pad.dpad.down.isPressed) now |= InputButtons.Down;

            if (pad.buttonWest.isPressed) now |= InputButtons.Light;
            if (pad.buttonSouth.isPressed) now |= InputButtons.Medium;
            if (pad.buttonEast.isPressed) now |= InputButtons.Heavy;
            if (pad.buttonNorth.isPressed) now |= InputButtons.Throw;
            if (pad.rightShoulder.isPressed) now |= InputButtons.Assist;

            if (pad.buttonWest.wasPressedThisFrame) down |= InputButtons.Light;
            if (pad.buttonSouth.wasPressedThisFrame) down |= InputButtons.Medium;
            if (pad.buttonEast.wasPressedThisFrame) down |= InputButtons.Heavy;
            if (pad.buttonNorth.wasPressedThisFrame) down |= InputButtons.Throw;
            if (pad.rightShoulder.wasPressedThisFrame) down |= InputButtons.Assist;
            if (pad.dpad.up.wasPressedThisFrame) down |= InputButtons.Up;
        }
        else if (playerIndex == 0)
        {
            PollKeyboard(Player1Keys, ref now, ref down);
        }
        else if (playerIndex == 1)
        {
            PollKeyboard(Player2Keys, ref now, ref down);
        }

        held = now;

        // ティックの間に押して離されたボタンも取りこぼさないよう、押した瞬間を保持する
        latched |= down & LatchMask;
    }

    private static void PollKeyboard(KeyCode[] keys, ref InputButtons now, ref InputButtons down)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKey(keys[i])) now |= KeyButtons[i];
            if (Input.GetKeyDown(keys[i])) down |= KeyButtons[i];
        }
    }

    public InputButtons Consume()
    {
        InputButtons result = held | latched;
        latched = InputButtons.None;
        return result;
    }
}