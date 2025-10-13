using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

/// <summary>
/// Manages the waiting room functionality.
/// Waits for a second player before starting the game.
/// </summary>
public class WaitingRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public TextMeshProUGUI waitingText;
    public TextMeshProUGUI playerCountText;
    
    [Header("Settings")]
    public int requiredPlayers = 2;
    public float startGameDelay = 2f; // Delay before starting game after all players join
    
    private bool isStartingGame = false;

    void Start()
    {
        // Check if room already has enough players when we join
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= requiredPlayers)
        {
            Debug.Log($"Room already has {PhotonNetwork.CurrentRoom.PlayerCount} players - starting game immediately");
            if (waitingText != null) waitingText.text = "Player connected! Starting game...";
            if (playerCountText != null) playerCountText.text = $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{requiredPlayers}";
            StartCoroutine(StartGameWithDelay());
        }
        else
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// Updates the UI to show current room status
    /// </summary>
    private void UpdateUI()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            if (waitingText != null) waitingText.text = "Error: Not in a room";
            if (playerCountText != null) playerCountText.text = "";
            return;
        }

        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {currentPlayers}/{requiredPlayers}";
        }

        if (currentPlayers < requiredPlayers)
        {
            if (waitingText != null) waitingText.text = "Waiting for another player...";
        }
        else if (!isStartingGame)
        {
            if (waitingText != null) waitingText.text = "Player connected! Starting game...";
            StartCoroutine(StartGameWithDelay());
        }
    }

    /// <summary>
    /// Called when a player enters the room
    /// </summary>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.NickName} entered the room. Total players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        UpdateUI();
    }

    /// <summary>
    /// Called when a player leaves the room
    /// </summary>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.NickName} left the room. Total players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        // Cancel game start if a player leaves
        if (isStartingGame)
        {
            StopAllCoroutines();
            isStartingGame = false;
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Starts the game after a short delay
    /// </summary>
    private IEnumerator StartGameWithDelay()
    {
        isStartingGame = true;
        
        // Wait for the specified delay
        yield return new WaitForSeconds(startGameDelay);
        
        // Double-check we still have enough players
        if (PhotonNetwork.CurrentRoom.PlayerCount >= requiredPlayers)
        {
            // Only the Master Client loads the level (Photon will sync it)
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Starting game...");
                PhotonNetwork.LoadLevel("sc_main");
            }
        }
        else
        {
            isStartingGame = false;
            UpdateUI();
        }
    }

    /// <summary>
    /// Called when the local player leaves the room
    /// </summary>
    public override void OnLeftRoom()
    {
        Debug.Log("Left the waiting room");
        // Return to lobby
        UnityEngine.SceneManagement.SceneManager.LoadScene("sc_lobby");
    }

    /// <summary>
    /// Allows player to leave the waiting room and return to lobby
    /// </summary>
    public void LeaveWaitingRoom()
    {
        Debug.Log("Player chose to leave the waiting room");
        PhotonNetwork.LeaveRoom();
    }
}
