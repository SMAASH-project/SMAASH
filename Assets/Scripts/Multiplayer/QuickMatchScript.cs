using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class QuickMatchScript : MonoBehaviourPunCallbacks
{

    [SerializeField]
    private int maxPlayers = 2;

    private void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        PhotonNetwork.CreateRoom(null, roomOptions, null);
    }

    public void QuickMatch()
    {
        PhotonNetwork.JoinRandomRoom();
        
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom();
    }  

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("sc_main");
    } 
}
