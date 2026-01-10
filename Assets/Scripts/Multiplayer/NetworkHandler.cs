using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
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

    private NetworkRunner _runner;
    private TMP_InputField createInput;
    private TMP_InputField joinInput;

    private Dictionary<PlayerRef, int> _playerSelections = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    void Start()
    {
        createInput = GameObject.Find("Create_input")?.GetComponent<TMP_InputField>();
        joinInput = GameObject.Find("Join_input")?.GetComponent<TMP_InputField>();
    }

    public void CreateRoom() => StartGame(GameMode.Host, createInput.text);
    public void JoinRoom() => StartGame(GameMode.Client, joinInput.text);

    async void StartGame(GameMode mode, string roomName)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);
        DontDestroyOnLoad(gameObject);

        await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = roomName,
            PlayerCount = 2,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    // --- INPUT BRIDGE ---
    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        // Links the Local Player's device to the spawned Character's Handler
        if (runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObj))
        {
            var handler = playerObj.GetComponent<LocalInputHandler>(); 
            if (handler != null) input.Set(handler.GetNetworkInput());
            if (handler == null)
            {
                Debug.LogError("LocalInputHandler component not found on player object!");            
            }
    
        }
    }

    // --- SPAWNING LOGIC ---
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

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

    #region Character Selection Logic (Unchanged)
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
        if (runner.IsServer && runner.SessionInfo.PlayerCount >= 2 && _playerSelections.Count >= 2)
            runner.LoadScene(_gameSceneName);
    }
    #endregion

    #region Required Interface Callbacks
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { /* Despawn logic */ }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { request.Accept(); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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