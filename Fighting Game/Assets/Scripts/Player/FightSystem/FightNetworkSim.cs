using Fusion;
using FightSim;
using UnityEngine;

public class FightNetworkSim : NetworkBehaviour
{
    [Networked] public NetRound State { get; set; }
    [Networked] public PlayerRef Player0 { get; set; }
    [Networked] public PlayerRef Player1 { get; set; }

    // 入力が届かなかったティックに使う直前の入力
    [Networked] private ushort LastInput0 { get; set; }
    [Networked] private ushort LastInput1 { get; set; }

    private FightManager manager;
    private int lastFrame = -1;
    private float lastPushTime;
    private bool resetPending;

    public void Init(PlayerRef player0, PlayerRef player1)
    {
        Player0 = player0;
        Player1 = player1;
    }

    public override void Spawned()
    {
        manager = FindFirstObjectByType<FightManager>();
        if (manager == null)
        {
            Debug.LogError("FightNetworkSim: シーンに FightManager が見つかりません。");
            return;
        }

        manager.SetNetworkDriven(true);

        // クライアントでも FixedUpdateNetwork を動かす（予測のため）
        Runner.SetIsSimulated(Object, true);

        if (Object.HasStateAuthority)
        {
            manager.Round.ResetMatch();
            State = NetSimConvert.ToNet(manager.Round.Save());
        }
        else
        {
            manager.Round.Load(NetSimConvert.FromNet(State));
        }

        manager.SnapViews();
        lastFrame = manager.Sim.Frame;
        lastPushTime = Time.unscaledTime;
    }

    public override void FixedUpdateNetwork()
    {
        if (manager == null) return;
        RoundSim round = manager.Round;

        // Fusion が保持している状態から始める
        round.Load(NetSimConvert.FromNet(State));

        ushort b0 = ReadButtons(Player0, LastInput0);
        ushort b1 = ReadButtons(Player1, LastInput1);
        LastInput0 = b0;
        LastInput1 = b1;

        round.Tick((InputButtons)b0, (InputButtons)b1);

        State = NetSimConvert.ToNet(round.Save());

        if (round.SimWasReset) resetPending = true;
    }

    // そのティックの入力を読む。無ければ直前の入力を使う
    // クライアントは相手の入力を受け取れないので予測の間は相手の直前の入力を使い続ける
    private ushort ReadButtons(PlayerRef player, ushort last)
    {
        if (player == PlayerRef.None) return last;

        NetInput input;
        if (Runner.TryGetInputForPlayer<NetInput>(player, out input)) return input.buttons;
        return last;
    }

    public override void Render()
    {
        if (manager == null) return;
        MatchSim sim = manager.Sim;

        if (resetPending)
        {
            // ラウンド切り替え・試合リセット: 補間せず合わせる
            resetPending = false;
            manager.SnapViews();
            lastFrame = sim.Frame;
            lastPushTime = Time.unscaledTime;
        }
        else if (sim.Frame != lastFrame)
        {
            // sim が進んだ（または巻き戻って補正された）
            manager.PushViews();
            lastFrame = sim.Frame;
            lastPushTime = Time.unscaledTime;
        }

        // 最後に sim が進んでからの経過時間で、60Hz のティックの間を補間する
        float alpha = Mathf.Clamp01((Time.unscaledTime - lastPushTime) * SimConst.fps);
        manager.RenderViews(alpha);
    }
}
