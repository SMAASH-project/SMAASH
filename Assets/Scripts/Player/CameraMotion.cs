using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class CameraMotion : MonoBehaviour
{
    public Transform playerPos;
    private float hLimit = 2.7f;
    private float vLimit = 2.1f;
    private int playerLayerNumber = 7;
    public float yOffset = 0.2f;
    public float zOffset = 15f;
    PhotonView view;

    private GameObject[] FindPlayers()
    {
        var goArray = GameObject.FindObjectsOfType<GameObject>();
        var goList = new List<GameObject>();
        for (int i = 0; i < goArray.Length-1; i++)
        {
            if (goArray[i].layer == playerLayerNumber)
            {
                goList.Add(goArray[i]);
            }
        }

        if (goList.Count == 0)
        {
            return null;
        }
        return goList.ToArray();
    }

    private void MoveCamera()
    {
            transform.position = new Vector3(playerPos.position.x, playerPos.position.y - yOffset, 0 - zOffset);
        //if (math.abs(transform.position.x) < hLimit)
        //{
        //}
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }


    // Update is called once per frame
    void Update()
    {
        playerPos = FindPlayers()[0].transform;

        MoveCamera();
    }
}
