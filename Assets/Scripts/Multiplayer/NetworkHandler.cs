using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class MatchParticipationDto
{
    public int player_id;
    public int character_id;
    public string result;
    public string network_status;
}

[Serializable]
public class MatchResultDto
{
    public string session_id;
    public string started_at;
    public string ended_at;
    public int level_id;
    public MatchParticipationDto participation;
}

public class NetworkHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private const string ApiDateTimeFormat = "dd MMM yy HH:mm 'UTC'";

    [Header("Setup")]
    public Character_Database characterDatabase;
    [SerializeField] private string _gameSceneName = "sc_main";
    [SerializeField] private string _characterSelectSceneName = "sc_champ_select";
    [SerializeField] private string _waitingRoomSceneName = "sc_waiting_room";
    [SerializeField] private string _lobbySceneName = "sc_lobby";
    [SerializeField] private string _loginSceneName = "sc_login";
    [Header("Match Result API")]
    [SerializeField] private AuthClient authClient;
    [SerializeField] private string matchResultEndpoint = "/api/matches";
    [SerializeField] private int levelId = 1;

    [Header("Spawn Points")]
    [SerializeField] private string player1SpawnPointName = "Player1_SpawnPoint";
    [SerializeField] private string player2SpawnPointName = "Player2_SpawnPoint";

    private static NetworkHandler _instance;
    public static NetworkHandler Instance => _instance;
    
    private NetworkRunner _runner;
    private TMP_InputField createInput;
    private TMP_InputField joinInput;

    private Dictionary<PlayerRef, int> _playerSelections = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private GameMode _lastGameMode;
    private string _lastRoomName;
    private GameMode _pendingCharacterSelectMode = GameMode.Host;
    private string _pendingRoomName = "DefaultRoom";
    private bool _isConnecting = false;
    private bool _sceneLoadRequested = false;
    private bool _isCancellingMatchmaking = false;
    private bool _isDisposing = false;
    private bool _isEndingMatch = false;
    private string _matchStartedAt = string.Empty;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;

            if (activeSceneName == _lobbySceneName)
            {
                Debug.Log("[NetworkHandler] Replacing previous singleton with lobby scene instance.");
                _instance.DisposeForLogout();
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == _loginSceneName && _instance == this)
        {
            _ = ShutdownAndDestroyAsync();
        }
    }

    void Start()
    {
        RefreshRoomInputReferences();
    }

    public void SelectCharacterAsCreator()
    {
        _pendingCharacterSelectMode = GameMode.Host;
        RefreshRoomInputReferences();
        _pendingRoomName = createInput != null && !string.IsNullOrWhiteSpace(createInput.text)
            ? createInput.text.Trim()
            : "DefaultRoom";
        SceneManager.LoadScene(_characterSelectSceneName);
    }

    public void SelectCharacterAsJoiner()
    {
        _pendingCharacterSelectMode = GameMode.Client;
        RefreshRoomInputReferences();
        _pendingRoomName = joinInput != null ? joinInput.text.Trim() : string.Empty;
        SceneManager.LoadScene(_characterSelectSceneName);
    }

    public void RoomCreateAndJoin()
    {
        if (_pendingCharacterSelectMode == GameMode.Single)
        {
            StartGame(GameMode.Single, "LocalTestRoom");
            return;
        }
        
        SceneManager.LoadScene(_waitingRoomSceneName);
        
        // Use a coroutine to create/join room after scene loads
        StartCoroutine(CreateOrJoinRoomAfterSceneLoad());
    }

    private IEnumerator CreateOrJoinRoomAfterSceneLoad()
    {
        // Wait for the scene to load
        yield return null;
        yield return null;
        
        string roomName = string.IsNullOrWhiteSpace(_pendingRoomName) ? "DefaultRoom" : _pendingRoomName;

        if (_pendingCharacterSelectMode == GameMode.Host)
        {
            Debug.Log("[NetworkHandler] Creating room: " + roomName);
            StartGame(GameMode.Host, roomName);
        }
        else
        {
            Debug.Log("[NetworkHandler] Joining room: " + roomName);
            StartGame(GameMode.Client, roomName);
        }
    }

    public void CancelMatchmaking()
    {
        if (_isDisposing)
        {
            return;
        }

        Debug.Log("[NetworkHandler] Player cancelled matchmaking");
        _ = CancelMatchmakingAsync();
    }

    private async Task CancelMatchmakingAsync()
    {
        if (_isCancellingMatchmaking)
        {
            return;
        }

        _isCancellingMatchmaking = true;
        _isConnecting = false;
        _sceneLoadRequested = false;

        try
        {
            if (_runner != null)
            {
                Debug.Log("[NetworkHandler] Shutting down network runner...");

                var runnerToShutdown = _runner;
                _runner = null;

                await runnerToShutdown.Shutdown(destroyGameObject: true, shutdownReason: ShutdownReason.Ok);
            }

            _playerSelections.Clear();
            _spawnedCharacters.Clear();

            // Load back to lobby
            if (!string.IsNullOrEmpty(_lobbySceneName))
            {
                Debug.Log("[NetworkHandler] Loading lobby scene: " + _lobbySceneName);
                var asyncLoad = SceneManager.LoadSceneAsync(_lobbySceneName);

                while (asyncLoad != null && !asyncLoad.isDone)
                {
                    await Task.Yield();
                }

                Debug.Log("[NetworkHandler] Lobby scene loaded successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkHandler] Cancel matchmaking failed: {ex.Message}");
        }
        finally
        {
            _isConnecting = false;
            _isCancellingMatchmaking = false;
        }
    }

    public void CreateRoom()
    {
        RefreshRoomInputReferences();
        string roomName = createInput != null ? createInput.text : "DefaultRoom";
        StartGame(GameMode.Host, roomName);
    }

    public void JoinRoom()
    {
        RefreshRoomInputReferences();
        string roomName = joinInput != null ? joinInput.text : string.Empty;
        StartGame(GameMode.Client, roomName);
    }

    public void StartSinglePlayerTest()
    {
        if (_isDisposing) return;

        Debug.Log("[NetworkHandler] Starting single-player test mode via character select");
        _pendingCharacterSelectMode = GameMode.Single;
        _pendingRoomName = "LocalTestRoom";
        SceneManager.LoadScene(_characterSelectSceneName);
    }

    async void StartGame(GameMode mode, string roomName)
    {
        if (_isConnecting || _isCancellingMatchmaking || _isDisposing) return;

        _isConnecting = true;
        _lastGameMode = mode;
        _lastRoomName = string.IsNullOrWhiteSpace(roomName) ? "DefaultRoom" : roomName.Trim();

        try
        {
            if (_runner != null)
            {
                await _runner.Shutdown(destroyGameObject: true, shutdownReason: ShutdownReason.Ok);
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
                SessionName = _lastRoomName,
                PlayerCount = mode == GameMode.Single ? 1 : 2,
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
            });

            _matchStartedAt = FormatApiUtcNow();
            _isEndingMatch = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start game: {ex.Message}");
            _isConnecting = false;
        }
    }

    public void DisposeForLogout()
    {
        _ = ShutdownAndDestroyAsync();
    }

    public void HandleMatchEnded(int deadPlayerId)
    {
        if (_isEndingMatch || _isDisposing)
            return;

        _isEndingMatch = true;
        StartCoroutine(PostMatchResultAndReturnToLobby(deadPlayerId));
    }

    private IEnumerator PostMatchResultAndReturnToLobby(int deadPlayerId)
    {
        if (authClient == null)
            authClient = FindObjectOfType<AuthClient>();

        string startedAt = string.IsNullOrWhiteSpace(_matchStartedAt)
            ? FormatApiUtcNow()
            : _matchStartedAt;
        string endedAt = FormatApiUtcNow();

        int localPhotonPlayerId = _runner != null ? _runner.LocalPlayer.PlayerId : -1;
        string localResult = localPhotonPlayerId == deadPlayerId ? "lose" : "win";
        string networkStatus = _lastGameMode == GameMode.Single ? "offline" : "online";

        var payload = new MatchResultDto
        {
            session_id = ResolveSessionId(),
            started_at = startedAt,
            ended_at = endedAt,
            level_id = levelId,
            participation = new MatchParticipationDto
            {
                player_id = PlayerPrefs.GetInt("selected_profile_id", -1),
                character_id = ResolveLocalCharacterId(),
                result = localResult,
                network_status = networkStatus
            }
        };

        Debug.Log($"[MATCH POST] Match ended. Timestamp format={ApiDateTimeFormat}, started_at={startedAt}, ended_at={endedAt}");
        Debug.Log($"[MATCH POST] Payload: {JsonUtility.ToJson(payload, true)}");

        if (payload.participation.player_id <= 0)
            Debug.LogWarning("[MATCH POST] selected_profile_id is missing or invalid. Match post is likely to fail.");

        if (authClient != null)
        {
            bool done = false;
            bool success = false;
            string response = string.Empty;

            yield return StartCoroutine(authClient.PostAuthorizedJson(matchResultEndpoint, payload, (ok, body) =>
            {
                success = ok;
                response = body;
                done = true;
            }));

            if (!done || !success)
                Debug.LogWarning($"[MATCH POST] Failed. Endpoint={matchResultEndpoint}, Response={response}");
            else
                Debug.Log($"[MATCH POST] Success. Endpoint={matchResultEndpoint}, Response={response}");
        }
        else
        {
            Debug.LogWarning("[MATCH POST] AuthClient not found. Skipping match result post.");
        }

        CancelMatchmaking();
    }

    private string ResolveSessionId()
    {
        string photonSessionName = string.Empty;

        if (_runner != null && _runner.SessionInfo.IsValid)
            photonSessionName = _runner.SessionInfo.Name;

        if (string.IsNullOrWhiteSpace(photonSessionName))
            photonSessionName = _lastRoomName;

        return ToDeterministicGuid(photonSessionName);
    }

    private static string FormatApiUtcNow()
    {
        // Backend date parsing expects an RFC822-style timestamp.
        return DateTime.UtcNow.ToString(ApiDateTimeFormat, CultureInfo.InvariantCulture);
    }

    private static string ToDeterministicGuid(string value)
    {
        if (Guid.TryParse(value, out var parsed))
            return parsed.ToString();

        string normalized = string.IsNullOrWhiteSpace(value) ? "smaash-session" : value.Trim();

        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return new Guid(hash).ToString();
    }

    private int ResolveLocalCharacterId()
    {
        int selectedIndex = PlayerPrefs.GetInt("selectedOption", 0);

        if (characterDatabase != null &&
            characterDatabase.character != null &&
            selectedIndex >= 0 &&
            selectedIndex < characterDatabase.character.Length)
        {
            var selectedCharacter = characterDatabase.GetCharacter(selectedIndex);
            if (selectedCharacter != null && selectedCharacter.character_id > 0)
                return selectedCharacter.character_id;
        }

        return selectedIndex + 1;
    }

    private async Task ShutdownAndDestroyAsync()
    {
        if (_isDisposing)
        {
            return;
        }

        _isDisposing = true;
        _isConnecting = false;
        _sceneLoadRequested = false;
        _isCancellingMatchmaking = false;

        try
        {
            if (_runner != null)
            {
                var runnerToShutdown = _runner;
                _runner = null;
                await runnerToShutdown.Shutdown(destroyGameObject: true, shutdownReason: ShutdownReason.Ok);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NetworkHandler] Shutdown during dispose failed: {ex.Message}");
        }

        _playerSelections.Clear();
        _spawnedCharacters.Clear();

        if (_instance == this)
        {
            _instance = null;
        }

        Destroy(gameObject);
    }

    private void RefreshRoomInputReferences()
    {
        if (createInput == null)
        {
            createInput = GameObject.Find("Create_input")?.GetComponent<TMP_InputField>();
        }

        if (joinInput == null)
        {
            joinInput = GameObject.Find("Join_input")?.GetComponent<TMP_InputField>();
        }
    }

    // --- INPUT BRIDGE ---
    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        var data = new NetworkInputData();

        LocalInputHandler handler = null;

        if (runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObj))
            handler = playerObj.GetComponent<LocalInputHandler>();

        if (handler == null)
        {
            var allHandlers = FindObjectsOfType<LocalInputHandler>();
            for (int i = 0; i < allHandlers.Length; i++)
            {
                var networkObject = allHandlers[i].GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.HasInputAuthority)
                {
                    handler = allHandlers[i];
                    break;
                }
            }
        }

        if (handler != null)
            data = handler.GetNetworkInput();

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
        int playerCount = runner.ActivePlayers.Count();
        UpdateWaitingRoomStatus(playerCount);
        if (player == runner.LocalPlayer)
        {
            int mySelection = PlayerPrefs.GetInt("selectedOption", 0);
            if (runner.IsServer) {
                _playerSelections.Add(player, mySelection);

                if (runner.GameMode == GameMode.Single && !_sceneLoadRequested)
                {
                    _sceneLoadRequested = true;
                    runner.LoadScene(_gameSceneName);
                    return;
                }

                CheckStartCondition(runner);
            } else {
                runner.SendReliableDataToServer(default, BitConverter.GetBytes(mySelection));
            }
        }
    }

    private void UpdateWaitingRoomStatus(int playerCount)
    {
        TMP_Text waitingRoomStatusText = GameObject.Find("WaitingRoomStatusText")?.GetComponent<TMP_Text>();
        if (waitingRoomStatusText != null)
        {    
            waitingRoomStatusText.text = "Waiting for other player to join... (" + playerCount + "/2)";
            Debug.Log($"[NetworkHandler] Updated waiting room status: {playerCount}/2 players joined.");
        }
        else
        {
            Debug.LogWarning("WaitingRoomStatusText not found in scene!");
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
        _isEndingMatch = false;
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