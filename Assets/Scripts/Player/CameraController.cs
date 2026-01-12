using System;
using System.Collections;
using UnityEngine;
using Fusion;

public class CameraController : NetworkBehaviour
{
    private Camera cam;
    public float yOffset = 3.8f;
    public float zOffset = -2f;
    private Vector3 temp;
    private Vector3 last;
    private float yLevel;
    private bool CamIsActive = false;
    private float Bounds;


    //Wait for spawned player to fall down to the ground before setting camera Y level
    private IEnumerator SetYLevel()
    {
        yield return new WaitForSeconds(1f);
        yLevel = transform.position.y;
        CamIsActive = true;
    }

    private void UpdatePosition()
    {
        if (Math.Abs(transform.position.x) > Bounds)
        {
            cam.transform.position = last;
            return;
        }

        temp.x = transform.position.x;
        temp.y = yLevel + yOffset; 
        temp.z = transform.position.z + zOffset;

        Debug.Log($"Temp y: {temp.y}");
        Debug.Log($"Y Level: {cam.transform.position.y}");

        cam.transform.position = temp;
        last = temp;
    }

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam)
        {
            cam.enabled = false; // default off until authority confirmed
            cam.transform.SetParent(null, true);
        }
        Bounds = 10f;
    }

    public override void Spawned()
    {
        if (cam) cam.enabled = Object.HasInputAuthority;
        StartCoroutine(SetYLevel());
    }

    void LateUpdate()
    {
        if (Object != null && Object.HasInputAuthority && CamIsActive)
        {
            UpdatePosition();
        }
    }
}
