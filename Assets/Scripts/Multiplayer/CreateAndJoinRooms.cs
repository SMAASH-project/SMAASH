/*
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Fusion;
using Fusion.Sockets;

public class CreateAndJoinRooms : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private SceneRef mainScene; // assign sc_main in Inspector

    private TMP_InputField createInput;
    private TMP_InputField joinInput;

    private bool isQuickMatch = false;

    private NetworkRunner runner;
    private INetworkSceneManager sceneManager;
    private List<SessionInfo> sessions = new List<SessionInfo>();

    private async void Start()
    {
        createInput = GameObject.Find("Create_input")?.GetComponent<TMP_InputField>();
        joinInput   = GameObject.Find("Join_input")?.GetComponent<TMP_InputField>();

        // Setup NetworkRunner
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        
        sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        
        runner.AddCallbacks(this);

        // Join the shared lobby to receive session list updates (for QuickMatch)
        var result = await runner.JoinSessionLobby(SessionLobby.Shared);
        if (!result.Ok)
        {
            Debug.LogError($"Failed to join session lobby: {result.ShutdownReason}");
        }
    }

    public async void CreateRoom()
    {
        isQuickMatch = false;
        string name = string.IsNullOrWhiteSpace(createInput?.text) ? Guid.NewGuid().ToString("N") : createInput.text;

        var props = new Dictionary<string, SessionProperty> {
            { "MaxPlayers", maxPlayers }
        };

        var result = await runner.StartGame(new StartGameArgs {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = name,
            SceneManager = sceneManager,
            SessionProperties = props
        });

        if (!result.Ok)
        {
            Debug.LogError($"Failed to start game: {result.ShutdownReason}");
        }
    }

    public async void JoinRoom()
    {
        if (string.IsNullOrWhiteSpace(joinInput?.text))
        {
            Debug.LogWarning("Fusion: JoinRoom called without a room name.");
            return;
        }

        var result = await runner.StartGame(new StartGameArgs {
            GameMode = GameMode.Client,
            SessionName = joinInput.text,
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError($"Failed to join room: {result.ShutdownReason}");
        }
    }

   public async void QuickMatch()
{
    isQuickMatch = true;

    // Ha van már futó NetworkRunner, leállítjuk és újraindítjuk
    if (runner != null && runner.IsRunning)
    {
        await runner.Shutdown();
        Destroy(runner);
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        runner.AddCallbacks(this);
    }

    // Frissítjük a session listát
    sessions = sessions ?? new List<SessionInfo>();
    var target = sessions.FirstOrDefault(s => s.PlayerCount < s.MaxPlayers && s.IsValid);

    if (target != null)
    {
        // Kiírjuk a szoba állapotát
        Debug.Log($"Szoba: {target.Name}, Játékosok száma: {target.PlayerCount}/{target.MaxPlayers}");

        // Ha a szobában már legalább 2 játékos van, csatlakozunk
        if (target.PlayerCount >= 2)
        {
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = target.Name,
                SceneManager = sceneManager,
                SessionProperties = target.Properties.ToDictionary(kv => kv.Key, kv => kv.Value)
            });

            if (!result.Ok)
                Debug.LogError($"Hibás csatlakozás: {result.ShutdownReason}");
            else
                Debug.Log($"Sikeres csatlakozás a szobához: {target.Name}");
        }
        else
        {
            Debug.Log("Még nincs elég játékos a szobában.");
        }
    }
    else
    {
        // Ha nincs elérhető szoba, új szobát hozunk létre
        var props = new Dictionary<string, SessionProperty>
        {
            { "MaxPlayers", maxPlayers }
        };

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = $"qm-{Guid.NewGuid():N}",
            SceneManager = sceneManager,
            SessionProperties = props
        });

        if (!result.Ok)
            Debug.LogError($"Hiba új szoba létrehozásakor: {result.ShutdownReason}");
        else
            Debug.Log("Új szoba sikeresen létrehozva.");
    }
}

    // INetworkRunnerCallbacks

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        sessions = sessionList ?? new List<SessionInfo>();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer || runner.IsSharedModeMasterClient)
        {
            int count = runner.ActivePlayers.Count();
            if (count == maxPlayers && mainScene.IsValid)
            {
                Debug.Log($"Room full: request networked scene change to {mainScene} (use sceneManager to load the scene).");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionOpen(NetworkRunner runner) { }
    public void OnSessionClose(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectDestroyed(NetworkRunner runner, NetworkObject obj) { }
    public void OnObjectSpawned(NetworkRunner runner, NetworkObject obj) { }
}

*/


using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateAndJoinRooms : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Create a unique position for the player
            Vector2 spawnPosition = new Vector2((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    private NetworkRunner _runner;

    private TMP_InputField createInput;
    private TMP_InputField joinInput;

    void Start()
    {
        createInput = GameObject.Find("Create_input")?.GetComponent<TMP_InputField>();
        joinInput   = GameObject.Find("Join_input")?.GetComponent<TMP_InputField>();
    }
    async void StartGame(GameMode mode, string roomName)
    {
        Debug.Log($"Starting game in mode: {mode} with room name: {roomName}");
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(1); // sc_main
        Debug.Log(scene);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = sceneInfo,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
    public void CreateRoom()
    {
        StartGame(GameMode.Host, createInput.text.ToString());
    }
    public void JoinRoom()
    {
        StartGame(GameMode.Client, joinInput.text.ToString());
    }
}