using ExitGames.Client.Photon.StructWrapping;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.VFX;
using Photon;
using Photon.Pun;

public class CameraBehaviour : MonoBehaviour
{
    public float yOffset;
    public float zOffset;
    private Camera[] cams;
    private Vector3 temp;
    PhotonView view;

    private void SetCameras()
    {
        cams = GameObject.FindObjectsOfType<Camera>();
        foreach (Camera c in cams)
        {
            if (c != gameObject.GetComponent<Camera>())
            {
                c.enabled = false;
            }
        }
    }

    IEnumerator waitForSomeTime()
    {
        yield return new WaitForSeconds(2);
    }

    // Start is called before the first frame update
    void Start()
    {
        view = GetComponent<PhotonView>();
        //nem mukodik ha mashol initializeolom oket, dont ask me
        yOffset = 1.5f;
        zOffset = -9f;
        transform.position = Vector3.zero;
        StartCoroutine(waitForSomeTime());
        SetCameras();
    }

    // Update is called once per frame
    void Update()
    {
        temp.x = transform.parent.gameObject.transform.position.x;
        temp.y = transform.parent.gameObject.transform.position.y + yOffset;
        temp.z = transform.parent.gameObject.transform.position.z + zOffset;

        transform.position = temp;
    }
}
