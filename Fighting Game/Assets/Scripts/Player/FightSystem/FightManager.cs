using System;
using UnityEngine;
using FightSim;

/// <summary>
/// 対戦の進行役。
///  ・Unity のフレームレートに関係なく、シミュレーションを 60Hz の固定ティックで進める
///  ・入力を集めて MatchSim.Tick に渡し、結果を FighterView に反映する
/// 将来オンライン対戦にする時は、このクラスの「入力を集めて Tick を呼ぶ」部分を
/// Photon Fusion の FixedUpdateNetwork に置き換える（MatchSim 自体は変更しない）。
/// </summary>
public class FightManager : MonoBehaviour
{
    [Header("キャラクター")]
    [SerializeField] private FighterView view1;
    [SerializeField] private FighterView view2;

    [Header("攻撃データ（Resources 内の CSV 名）")]
    [SerializeField] private string csvResourceName = "test";

    [Header("シミュレーション設定（位置の単位: 1000 = 1ワールド単位）")]
    [SerializeField] private FighterParams fighterParams = new FighterParams();

    [Header("デバッグ")]
    [SerializeField] private bool showBoxes = true;
    [SerializeField] private bool enableResetKey = true; // R キーで試合をやり直す

    private const float TickDelta = 1f / SimConst.Fps;
    private const int MaxTicksPerFrame = 4; // 処理落ち時に溜まった分を一気に消化しすぎない

    private MatchSim sim;
    private LocalInputSource input1;
    private LocalInputSource input2;
    private float accumulator;

    public MatchSim Sim { get { return sim; } }

    private void Awake()
    {
        AttackFrameData[] attacks = LoadAttackTable();
        sim = new MatchSim(fighterParams, attacks);
        input1 = new LocalInputSource(0);
        input2 = new LocalInputSource(1);
    }

    private void Start()
    {
        SnapViews();
    }

    // CSV から、AttackType の値をインデックスにした技データの配列を作る
    private AttackFrameData[] LoadAttackTable()
    {
        var table = new AttackFrameData[Enum.GetValues(typeof(AttackType)).Length];
        var db = AttackDataLoader.LoadAttackDataFromCSV(csvResourceName);

        foreach (AttackType type in Enum.GetValues(typeof(AttackType)))
        {
            AttackData data;
            if (db.TryGetValue(type.ToString(), out data))
            {
                table[(int)type] = AttackFrameData.FromCsv(data);
            }
            else
            {
                Debug.LogWarning($"CSV に {type} のデータがありません。この技は出ません。");
            }
        }
        return table;
    }

    private void Update()
    {
        if (sim == null) return;

        if (enableResetKey && Input.GetKeyDown(KeyCode.R)) ResetMatch();

        input1.Poll();
        input2.Poll();

        // 経過時間を溜めて、60Hz で何回ティックを進めるかを決める
        accumulator += Time.deltaTime;
        int ticks = 0;
        while (accumulator >= TickDelta && ticks < MaxTicksPerFrame)
        {
            StepOneTick();
            accumulator -= TickDelta;
            ticks++;
        }
        if (accumulator > TickDelta) accumulator = 0f; // 大幅な遅れは捨てる

        // ティックの間を補間して表示する
        float alpha = Mathf.Clamp01(accumulator / TickDelta);
        if (view1 != null) view1.Render(alpha);
        if (view2 != null) view2.Render(alpha);
    }

    private void StepOneTick()
    {
        sim.Tick(input1.Consume(), input2.Consume());
        if (view1 != null) view1.PushState(sim.Get(0));
        if (view2 != null) view2.PushState(sim.Get(1));
    }

    public void ResetMatch()
    {
        sim.Reset();
        accumulator = 0f;
        SnapViews();
    }

    private void SnapViews()
    {
        if (view1 != null) view1.Snap(sim.Get(0));
        if (view2 != null) view2.Snap(sim.Get(1));
    }

    // ---- 判定ボックスの可視化 ----
    private void OnDrawGizmos()
    {
        if (!showBoxes || sim == null || view1 == null || view2 == null) return;

        for (int i = 0; i < 2; i++)
        {
            FighterView view = (i == 0) ? view1 : view2;

            DrawRect(view, sim.GetPushRect(i), new Color(0.2f, 0.4f, 1f, 0.9f));   // 押し合い: 青
            DrawRect(view, sim.GetHurtRect(i), new Color(0f, 1f, 0f, 0.9f));       // 食らい: 緑

            SimRect hit;
            if (sim.TryGetHitRect(i, out hit))
            {
                DrawRect(view, hit, new Color(1f, 0f, 0f, 1f));                    // 攻撃: 赤
            }
        }
    }

    private static void DrawRect(FighterView view, SimRect r, Color color)
    {
        Vector3 min = view.ToWorld(r.minX, r.minY);
        Vector3 max = view.ToWorld(r.maxX, r.maxY);
        Gizmos.color = color;
        Gizmos.DrawWireCube((min + max) * 0.5f, new Vector3(max.x - min.x, max.y - min.y, 0.05f));
    }
}
