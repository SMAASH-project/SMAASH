using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateAndJoinRooms : MonoBehaviour, INetworkRunnerCallbacks
{
    #region  INetworkRunnerCallbacks
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
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

    

    [Header("Setup")]
    public Character_Database characterDatabase;
    [SerializeField] private string _gameSceneName = "sc_main"; // Ensure this matches your Build Settings

    private NetworkRunner _runner;
    private TMP_InputField createInput;
    private TMP_InputField joinInput;

    // Stores the character index for each player
    private Dictionary<PlayerRef, int> _playerSelections = new Dictionary<PlayerRef, int>();
    // Prevents spawning duplicates
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

        // IMPORTANT: Keep this object alive to handle OnSceneLoadDone in the next scene
        DontDestroyOnLoad(gameObject);

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = roomName,
            PlayerCount = 2,
            SceneManager = sceneManager
        });
    }

    // 1. When a player joins (Host or Client)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // If I am the one who just joined (Client or Host), send/store my selection
        if (player == runner.LocalPlayer)
        {
            int mySelection = PlayerPrefs.GetInt("selectedOption", 0);

            if (runner.IsServer)
            {
                // Host stores their own selection directly
                if (!_playerSelections.ContainsKey(player))
                {
                    _playerSelections.Add(player, mySelection);
                    Debug.Log($"Host (Player {player}) selected character index: {mySelection}");
                }
                CheckStartCondition(runner);
            }
            else
            {
                // Client sends selection to Server via Reliable Data
                // FIX: Removed 'new ReliableKey(1)' argument. The system generates the key.
                byte[] data = BitConverter.GetBytes(mySelection);
                runner.SendReliableDataToServer(default, data);
                Debug.Log($"Client (Player {player}) sent selection index: {mySelection}");
            }
        }
    }

    // 2. Host receives Client's selection
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // FIX: Removed 'key.Int == 1' check. We assume all data is character selection.
        if (runner.IsServer) 
        {
            int selection = BitConverter.ToInt32(data.Array, data.Offset);
            
            if (!_playerSelections.ContainsKey(player))
            {
                _playerSelections.Add(player, selection);
                Debug.Log($"Server received selection from Player {player}: {selection}");
            }
            else
            {
                _playerSelections[player] = selection;
            }

            CheckStartCondition(runner);
        }
    }

    // 3. Check if both players are present AND we know their characters
    private void CheckStartCondition(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        // We need 2 players in the session
        if (runner.SessionInfo.PlayerCount < 2) 
        {
            Debug.Log($"Waiting for players... ({runner.SessionInfo.PlayerCount}/2)");
            return;
        }

        // We need 2 selections in the dictionary (Host + Client)
        if (_playerSelections.Count < 2)
        {
            Debug.Log($"Waiting for character selections... ({_playerSelections.Count}/2)");
            return;
        }

        Debug.Log("All players ready and selections received. Loading Game Scene.");
        runner.LoadScene(_gameSceneName);
    }

    // 4. Scene Loaded -> Spawn Players
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        Debug.Log("Scene Load Done. Spawning Players...");

        foreach (var player in runner.ActivePlayers)
        {
            if (_spawnedCharacters.ContainsKey(player)) continue;

            // Get the selection we stored earlier
            int index = 0;
            if (_playerSelections.TryGetValue(player, out int selection))
            {
                index = selection;
            }
            else
            {
                Debug.LogWarning($"No selection found for player {player}. Defaulting to 0.");
            }
            
            Character characterData = characterDatabase.GetCharacter(index);

            // Check if prefab is valid to prevent ArgumentException
            if (characterData != null && characterData.playerPrefab.IsValid)
            {
                // Calculate spawn position (Player 0 at 0, Player 1 at 3)
                Vector3 spawnPos = new Vector3(player.RawEncoded * 3f, 1f, 0f);
                
                NetworkObject obj = runner.Spawn(characterData.playerPrefab, spawnPos, Quaternion.identity, player);
                _spawnedCharacters.Add(player, obj);
                
                Debug.Log($"Spawned {characterData.character_name} for {player}");
            }
            else
            {
                Debug.LogError($"Invalid prefab for player {player} (Index: {index}). Check CharacterDatabase Inspector!");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out var obj))
        {
            runner.Despawn(obj);
            _spawnedCharacters.Remove(player);
        }
        _playerSelections.Remove(player);
    }
}