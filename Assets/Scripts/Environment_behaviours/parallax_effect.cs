using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // replace Photon.Pun

public class parallax_effect : MonoBehaviour
{
    private float Starting_pos;
    public float Parallax_factor;
    public Camera Camera;
    private Vector3 Pos;
    private float Distance;

    private Camera FindCam()
    {
        GameObject[] Players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in Players)
        {
            //PhotonView view = p.GetComponent<PhotonView>();
            NetworkObject netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority) // local player?
            {
                return p.GetComponentInChildren<Camera>();
            }
        }
        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        Starting_pos = transform.position.x;
        Camera = FindCam();
    }

    // Update is called once per frame
    void Update()
    {
        //unity is tweaking
        if (Camera == null)
        {
            return;
        }
        Pos = Camera.transform.position;
        Distance = Pos.x * Parallax_factor;

        transform.position = new Vector3(Starting_pos + Distance, transform.position.y, transform.position.z);
    }
}
