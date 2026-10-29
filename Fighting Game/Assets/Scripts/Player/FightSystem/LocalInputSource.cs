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

            if (pad.buttonWest.wasPressedThisFrame) down |= InputButtons.Light;
            if (pad.buttonSouth.wasPressedThisFrame) down |= InputButtons.Medium;
            if (pad.buttonEast.wasPressedThisFrame) down |= InputButtons.Heavy;
            if (pad.buttonNorth.wasPressedThisFrame) down |= InputButtons.Throw;
            if (pad.dpad.up.wasPressedThisFrame) down |= InputButtons.Up;
        }
        else if (playerIndex == 0)
        {
            // ---- キーボード 1P: WASD + J K L M ----
            if (Input.GetKey(KeyCode.D)) now |= InputButtons.Right;
            if (Input.GetKey(KeyCode.A)) now |= InputButtons.Left;
            if (Input.GetKey(KeyCode.W)) now |= InputButtons.Up;
            if (Input.GetKey(KeyCode.S)) now |= InputButtons.Down;

            if (Input.GetKey(KeyCode.J)) now |= InputButtons.Light;
            if (Input.GetKey(KeyCode.K)) now |= InputButtons.Medium;
            if (Input.GetKey(KeyCode.L)) now |= InputButtons.Heavy;
            if (Input.GetKey(KeyCode.M)) now |= InputButtons.Throw;

            if (Input.GetKeyDown(KeyCode.J)) down |= InputButtons.Light;
            if (Input.GetKeyDown(KeyCode.K)) down |= InputButtons.Medium;
            if (Input.GetKeyDown(KeyCode.L)) down |= InputButtons.Heavy;
            if (Input.GetKeyDown(KeyCode.M)) down |= InputButtons.Throw;
            if (Input.GetKeyDown(KeyCode.W)) down |= InputButtons.Up;
        }
        else if (playerIndex == 1)
        {
            // ---- キーボード 2P: 矢印キー + Z X C V ----
            if (Input.GetKey(KeyCode.RightArrow)) now |= InputButtons.Right;
            if (Input.GetKey(KeyCode.LeftArrow)) now |= InputButtons.Left;
            if (Input.GetKey(KeyCode.UpArrow)) now |= InputButtons.Up;
            if (Input.GetKey(KeyCode.DownArrow)) now |= InputButtons.Down;

            if (Input.GetKey(KeyCode.Z)) now |= InputButtons.Light;
            if (Input.GetKey(KeyCode.X)) now |= InputButtons.Medium;
            if (Input.GetKey(KeyCode.C)) now |= InputButtons.Heavy;
            if (Input.GetKey(KeyCode.V)) now |= InputButtons.Throw;

            if (Input.GetKeyDown(KeyCode.Z)) down |= InputButtons.Light;
            if (Input.GetKeyDown(KeyCode.X)) down |= InputButtons.Medium;
            if (Input.GetKeyDown(KeyCode.C)) down |= InputButtons.Heavy;
            if (Input.GetKeyDown(KeyCode.V)) down |= InputButtons.Throw;
            if (Input.GetKeyDown(KeyCode.UpArrow)) down |= InputButtons.Up;
        }

        held = now;

        // ティックの間に押して離されたボタンも取りこぼさないよう、押した瞬間を保持する
        latched |= down;
    }

    public InputButtons Consume()
    {
        InputButtons result = held | latched;
        latched = InputButtons.None;
        return result;
    }
}
