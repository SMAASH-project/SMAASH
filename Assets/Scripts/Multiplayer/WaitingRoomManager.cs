using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WaitingRoomManager : MonoBehaviour
{
    private void Start()
    {
        // Use a coroutine to wait for the scene to fully load and elements to be initialized
        StartCoroutine(SetupDisconnectButtonDelayed());
    }

    private IEnumerator SetupDisconnectButtonDelayed()
    {
        // Wait a frame to ensure all objects are initialized
        yield return null;

        var networkHandler = NetworkHandler.Instance;
        if (networkHandler == null)
        {
            Debug.LogError("[WaitingRoomManager] NetworkHandler instance not found in DontDestroyOnLoad!");
            yield break;
        }

        Button disconnectButton = null;
        int attempts = 0;
        int maxAttempts = 10;

        // Try to find the button with multiple attempts
        while (disconnectButton == null && attempts < maxAttempts)
        {
            disconnectButton = GameObject.Find("DisconnectButton")?.GetComponent<Button>();
            
            if (disconnectButton == null)
            {
                disconnectButton = GameObject.Find("Disconnect")?.GetComponent<Button>();
            }
            if (disconnectButton == null)
            {
                disconnectButton = GameObject.Find("CancelButton")?.GetComponent<Button>();
            }
            if (disconnectButton == null)
            {
                disconnectButton = GameObject.Find("Cancel")?.GetComponent<Button>();
            }

            if (disconnectButton == null)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
        }

        if (disconnectButton != null)
        {
            disconnectButton.onClick.RemoveAllListeners();
            disconnectButton.onClick.AddListener(() => 
            {
                Debug.Log("[WaitingRoomManager] Disconnect button clicked - cancelling matchmaking");
                networkHandler.CancelMatchmaking();
            });
            Debug.Log("[WaitingRoomManager] Disconnect button successfully registered after " + attempts + " attempts");
        }
        else
        {
            Debug.LogWarning("[WaitingRoomManager] DisconnectButton not found after " + maxAttempts + " attempts. Checked for: DisconnectButton, Disconnect, CancelButton, Cancel");
        }
    }
}

