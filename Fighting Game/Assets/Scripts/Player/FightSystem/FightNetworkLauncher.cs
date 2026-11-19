using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using FightSim;

public class FightNetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private FightManager manager;
    [SerializeField] private NetworkPrefabRef simPrefab;

    private const int playerCount = 2; 
    
    // 診断用
    private FightNetworkSim diagSim;

    private NetworkRunner activeRunner;
    private GameObject runnerObject;
    private readonly LocalInputSource localInput = new LocalInputSource(0);
    private readonly List<PlayerRef> joined = new List<PlayerRef>();
    private bool simSpawned;
    private string status = "";

    private void Awake()
    {
        Application.runInBackground = true;
    }

    private void Update()
    {
        localInput.Poll();
    }

    public async void StartOnline()
    {
        if (activeRunner != null) return;

        // 接続中にローカルのティックが進まないようにする
        if (manager != null) manager.SetNetworkDriven(true);

        runnerObject = new GameObject("NetworkRunner");
        //runnerObject.transform.SetParent(transform);
        activeRunner = runnerObject.AddComponent<NetworkRunner>();
        activeRunner.AddCallbacks(this);
        activeRunner.ProvideInput = true;
        NetworkSceneManagerDefault sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();

        status = "Connecting...";

        StartGameResult result = await activeRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = null,          // 名前なし = ランダムマッチ
            PlayerCount = playerCount,
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError("オンラインの開始に失敗しました: " + result.ShutdownReason);
            status = "Failed: " + result.ShutdownReason;
            Cleanup();
            return;
        }

        Debug.Log("Session : " + activeRunner.SessionInfo);
        status = activeRunner.IsServer ? "Waiting for opponent... (Host)" : "Connected (Client)";
    }

    // 診断用
    private string RegionTag()
    {
        if (activeRunner == null) return "";
        return " [" + activeRunner.SessionInfo.Region + "]";
    }

    private string RttText()
    {
        if (activeRunner == null) return "";

        double rtt = 0;
        if(activeRunner.IsServer)
        {
            if (joined.Count >= 2) rtt = activeRunner.GetPlayerRtt(joined[1]);
        }
        else
        {
            rtt = activeRunner.GetPlayerRtt(PlayerRef.None);
        }
        return "RTT : " + (rtt * 1000.0).ToString("F0") + " ms";
    }

    // 接続前の状態に戻す
    private void Cleanup()
    {
        if (runnerObject != null) Destroy(runnerObject);
        runnerObject = null;
        activeRunner = null;
        joined.Clear();
        simSpawned = false;

        if (manager != null)
        {
            manager.SetNetworkDriven(false);
            manager.ResetMatch();
        }
    }

    private void OnGUI()
    {
        float cx = Screen.width * 0.5f;

        if (activeRunner == null)
        {
            if (GUI.Button(new Rect(cx - 100, Screen.height * 0.5f - 20, 200, 40), "Online Match (Random)"))
            {
                StartOnline();
            }
            if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(cx - 150, Screen.height * 0.5f + 30, 300, 24), status);
            return;
        }

        GUI.Label(new Rect(cx - 150, 60, 300, 24), status);
        GUI.Label(new Rect(cx - 150, 108, 300, 24), RttText());

        // 診断用 : ホストの画面にクライアントの入力が間に合わなかった回数を出す
        if (diagSim == null) diagSim = FindFirstObjectByType<FightNetworkSim>();
        if(diagSim != null && activeRunner != null && activeRunner.IsServer)
        {
            int f = Mathf.Max(1, diagSim.FightTicks);

            GUI.Label(new Rect(cx - 150, 84, 360, 24), "Client input missing : " + diagSim.MissingInputTicks
                + " / " + f + " (" + (100f * diagSim.MissingInputTicks / f). ToString("F1") + "%) max streak : " + diagSim.MaxMissingStreak);
        }
    }

    // ----------------------------------
    // INetworkRunnerCallBacks
    // ----------------------------------

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return; // 生成できるのはホストだけ

        joined.Add(player);
        status = "Players: " + joined.Count + "/" + playerCount + " (Host)" + RegionTag();

        if (joined.Count == playerCount && !simSpawned)
        {
            simSpawned = true;
            PlayerRef p0 = joined[0];
            PlayerRef p1 = joined[1];

            runner.Spawn(simPrefab, Vector3.zero, Quaternion.identity, null,
                (r, obj) => obj.GetComponent<FightNetworkSim>().Init(p0, p1));

            status = "Fight! (Host = P1)" + RegionTag();
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // 1 対 1 なので、途中で誰かが抜けたらホストは試合を終了する
        if (runner.IsServer && simSpawned)
        {
            Debug.LogWarning("対戦相手が切断したため、試合を終了します。");
            runner.Shutdown();
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetInput data = new NetInput();
        data.buttons = (ushort)localInput.Consume();
        input.Set(data);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Fusion が終了しました: " + shutdownReason);
        status = "Disconnected: " + shutdownReason;
        Cleanup();
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log("ホストから切断されました: " + reason);
    }

    // 使わないけど必要な奴
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
#pragma warning disable CS0618
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
#pragma warning restore CS0618
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
