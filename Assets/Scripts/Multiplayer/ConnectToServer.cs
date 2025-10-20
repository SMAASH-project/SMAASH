using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Fusion;

public class ConnectToServer : MonoBehaviour
{
    private NetworkRunner _runner;

    private async void Start()
    {
        await ConnectAndJoinLobby();
    }

    private async Task ConnectAndJoinLobby()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.JoinSessionLobby(SessionLobby.Shared);
        if (result.Ok)
        {
            SceneManager.LoadScene("sc_lobby");
        }
        else
        {
            Debug.LogError($"Fusion: failed to join lobby. Reason: {result.ShutdownReason}");
        }
    }
}
