using System;
using System.Collections;
using UnityEngine;
using Fusion;

public class CameraController : NetworkBehaviour
{
    private Camera cam;
    public float yOffset = 1.5f;
    public float zOffset = -9f;
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

        cam.transform.position = temp;
        last = temp;
    }

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam) cam.enabled = false; // default off until authority confirmed
        Bounds = 10f;
    }

    public override void Spawned()
    {
        if (cam) cam.enabled = Object.HasInputAuthority;
        StartCoroutine(SetYLevel());
    }

    void Update()
    {
        if (Object != null && Object.HasInputAuthority && CamIsActive)
        {
            UpdatePosition();
        }
    }
}
