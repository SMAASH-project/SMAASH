using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{

    [SerializeField]
    private int maxPlayers = 2;
    private TMP_InputField createInput;
    private TMP_InputField joinInput;

    private bool isQuickMatch = false;

    private void Start()
    {
        createInput = GameObject.Find("Create_input").GetComponent<TMP_InputField>();
        joinInput = GameObject.Find("Join_input").GetComponent<TMP_InputField>();

        // ensure the master client's LoadLevel call is propagated to all clients
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    
    //Szoba letrehozasa
    public void CreateRoom()
    {
        if(isQuickMatch)
        {
            Debug.Log("Creating Quick Match Room");
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = (byte)maxPlayers;
            PhotonNetwork.CreateRoom(null, roomOptions, null);
        }
        else
        {
            Debug.Log("Creating Room: " + createInput.text);
            // ensure named rooms also have proper max players
            RoomOptions ro = new RoomOptions { MaxPlayers = (byte)maxPlayers };
            PhotonNetwork.CreateRoom(createInput.text, ro, null);
        }
    }

    //Szobahoz csatlakozas
    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(joinInput.text);
    }

    public void QuickMatch()
    {
        //SceneManager.LoadScene("sc_loading", LoadSceneMode.Single);
        isQuickMatch = true; // set before join so CreateRoom knows
        PhotonNetwork.JoinRandomRoom();
        //SceneManager.LoadScene("sc_loading");

    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom();
    }

    //Miutan csatlakozott a szobahoz, belep a palyara
    public override void OnJoinedRoom()
    {
        Debug.Log("Players in room: " + PhotonNetwork.CurrentRoom.PlayerCount);
        if(PhotonNetwork.CurrentRoom.PlayerCount == maxPlayers)
        {
            Debug.Log("Loading Level");
            PhotonNetwork.LoadLevel("sc_main");
            
        }
    }

    // ensure the room creator (master client) loads the scene when the second player joins
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.LogFormat("Player entered: {0}. Now {1}/{2}", newPlayer.NickName ?? newPlayer.UserId, PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.CurrentRoom.MaxPlayers);
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == maxPlayers)
        {
            Debug.Log("Master detected required players -> loading scene for all clients");
            PhotonNetwork.LoadLevel("sc_main");
        }
    }
}
