using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class NetworkHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Setup")]
    public Character_Database characterDatabase;
    [SerializeField] private string _gameSceneName = "sc_main";

    [Header("Spawn Points")]
    [SerializeField] private string player1SpawnPointName = "Player1_SpawnPoint";
    [SerializeField] private string player2SpawnPointName = "Player2_SpawnPoint";

    private static NetworkHandler _instance;
    private NetworkRunner _runner;
    private TMP_InputField createInput;
    private TMP_InputField joinInput;

    private Dictionary<PlayerRef, int> _playerSelections = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private GameMode _lastGameMode;
    private string _lastRoomName;
    private bool _isConnecting = false;
    private bool _sceneLoadRequested = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        createInput = GameObject.Find("Create_input")?.GetComponent<TMP_InputField>();
        joinInput = GameObject.Find("Join_input")?.GetComponent<TMP_InputField>();
    }

    public void CreateRoom() => StartGame(GameMode.Host, createInput.text);
    public void JoinRoom() => StartGame(GameMode.Client, joinInput.text);

    async void StartGame(GameMode mode, string roomName)
    {
        if (_isConnecting) return;

        _isConnecting = true;
        _lastGameMode = mode;
        _lastRoomName = roomName;

        try
        {
            if (_runner != null)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }

            // IMPORTANT: keep runner as ROOT object (no parent), so it survives scene loads
            GameObject runnerObj = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerObj);

            _runner = runnerObj.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            runnerObj.AddComponent<NetworkSceneManagerDefault>();

            await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = roomName,
                PlayerCount = 2,
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start game: {ex.Message}");
            _isConnecting = false;
        }
    }

    // --- INPUT BRIDGE ---
    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        var data = new NetworkInputData();

        // Use TryGetPlayerObject to find specific character
        if (runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObj))
        {
            var handler = playerObj.GetComponent<LocalInputHandler>(); 
            if (handler != null)
            {
                data = handler.GetNetworkInput();
            }
        }
        input.Set(data);
    }

    // --- SPAWNING LOGIC ---
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        Debug.Log("[NetworkHandler] Scene load done, spawning players...");
        Debug.Log($"Active Players: {runner.ActivePlayers}, Player Selections: {_playerSelections.Count}");

        foreach (var player in runner.ActivePlayers)
        {
            if (_spawnedCharacters.ContainsKey(player)) continue;

            int index = _playerSelections.TryGetValue(player, out int selection) ? selection : 0;
            Character characterData = characterDatabase.GetCharacter(index);

            if (characterData != null && characterData.playerPrefab.IsValid)
            {
                // Get spawn position from spawn point objects in the current scene
                bool isLeftSide = (player == runner.LocalPlayer);
                string spawnPointName = isLeftSide ? player1SpawnPointName : player2SpawnPointName;
                
                GameObject spawnPointObj = GameObject.Find(spawnPointName);
                if (spawnPointObj == null)
                {
                    Debug.LogError($"Spawn point '{spawnPointName}' not found in scene!");
                    continue;
                }
                
                Vector3 spawnPos = spawnPointObj.transform.position;
                Quaternion spawnRot = spawnPointObj.transform.rotation;
                
                NetworkObject obj = runner.Spawn(characterData.playerPrefab, spawnPos, spawnRot, player);
                Debug.Log($"Spawned player {player} with character index {index} at {spawnPos}");
                
                // SetPlayerObject is required for OnInput to work!
                runner.SetPlayerObject(player, obj);
                _spawnedCharacters.Add(player, obj);

                // Face each other
                if (!isLeftSide) RPC_InitialFlip(obj);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InitialFlip(NetworkObject playerObj)
    {
        var sr = playerObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = true;
    }

    #region Character Selection Logic
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            int mySelection = PlayerPrefs.GetInt("selectedOption", 0);
            if (runner.IsServer) {
                _playerSelections.Add(player, mySelection);
                CheckStartCondition(runner);
            } else {
                runner.SendReliableDataToServer(default, BitConverter.GetBytes(mySelection));
            }
        }
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (runner.IsServer) {
            int selection = BitConverter.ToInt32(data.Array, data.Offset);
            _playerSelections[player] = selection;
            CheckStartCondition(runner);
        }
    }

    private void CheckStartCondition(NetworkRunner runner)
    {
        if (!runner.IsServer || _sceneLoadRequested) return;

        if (runner.ActivePlayers.Count() >= 2 && _playerSelections.Count >= 2)
        {
            _sceneLoadRequested = true;
            runner.LoadScene(_gameSceneName);
        }
    }
    #endregion

    #region Required Interface Callbacks
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { /* Despawn logic */ }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[NetworkHandler] Shutdown - Reason: {shutdownReason}");
        _isConnecting = false;
        _sceneLoadRequested = false;
        if (_runner == runner) _runner = null;
    }
    
    public void OnConnectedToServer(NetworkRunner runner) 
    { 
        Debug.Log("[NetworkHandler] Connected to server");
        _isConnecting = false;
    }
    
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[NetworkHandler] Disconnected from server: {reason}");
        _isConnecting = false;
    }
    
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { request.Accept(); }
    
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[NetworkHandler] Connection failed: {reason} to {remoteAddress}");
        _isConnecting = false;
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    #endregion
}