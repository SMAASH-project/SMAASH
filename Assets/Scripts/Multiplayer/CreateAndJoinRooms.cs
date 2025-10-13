using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public TMP_InputField createInput;
    public TMP_InputField joinInput;

    
    //Szoba letrehozasa (Create custom room)
    public void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // Maximum 2 players for a match
        roomOptions.IsVisible = true; // Visible in lobby
        roomOptions.IsOpen = true; // Open for joining
        PhotonNetwork.CreateRoom(createInput.text, roomOptions);
    }

    //Szobahoz csatlakozas (Join custom room)
    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(joinInput.text);
    }

    //Quick Match - csatlakozas random szobahoz vagy uj letrehozasa
    //Quick Match - join random room or create new one
    public void QuickMatch()
    {
        Debug.Log("Quick Match initiated");
        PhotonNetwork.JoinRandomRoom();
    }

    //Miutan csatlakozott a szobahoz, belep a varoterbe
    //After joining room, enter the waiting room
    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name} with {PhotonNetwork.CurrentRoom.PlayerCount} player(s)");
        PhotonNetwork.LoadLevel("sc_waiting_room");
    }

    //Ha nincs elerheto szoba, letrehoz egyet a Quick Match-hez
    //If no room available, create one for Quick Match
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No room available, creating a new one for Quick Match");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.IsVisible = true; // Allow others to find this room
        roomOptions.IsOpen = true;
        PhotonNetwork.CreateRoom(null, roomOptions); // null = random room name
    }
}
