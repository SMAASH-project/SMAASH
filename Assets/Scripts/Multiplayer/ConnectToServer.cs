using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Fusion;
using TMPro;
using Fusion.Sockets;
using System;

// Implement INetworkRunnerCallbacks
public class ConnectToServer : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    public TMP_Text LogsText;

    private async void Start()
    {
        await ConnectAndJoinLobby();
    }

    private async Task ConnectAndJoinLobby()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        // Add THIS script as a callback handler for diagnostics
        _runner.AddCallbacks(this); 
        gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.JoinSessionLobby(SessionLobby.Shared);
        // ... rest of join lobby code (handled in callbacks now) ...

        if (result.Ok)
        {
            SceneManager.LoadScene("sc_lobby");
            //LogsText.text += "Joined lobby successfully.\n";
        }
        else
        {
            Debug.LogError($"Fusion: failed to join lobby. Reason: {result.ErrorMessage}");
            //LogsText.text += $"Runner State: {result.ShutdownReason}\n";
        }
    }

    // --- Add this essential diagnostic callback ---
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        // This provides much more specific errors than ShutdownReason.Error
        LogsText.text += $"Connection Failed Specific Reason: {reason}\n";
        Debug.LogError($"Fusion connection failed: {reason}");
    }
    
    // You must implement all other callbacks, even if empty, for the interface to work correctly.
    #region Other Required INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, SimulationInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        LogsText.text += $"Runner Shutdown: {shutdownReason}\n";
    }
    public void OnConnectedToServer(NetworkRunner runner, NetAddress peers) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }


    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }
    #endregion
}
