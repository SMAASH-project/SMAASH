using System;
using System.Collections;
using UnityEngine;
using Fusion;
using TMPro;

public class CameraController : NetworkBehaviour
{
    private Camera cam;
    public float yOffset = 3.8f;
    public float zOffset = -2f;
    private Vector3 temp;
    private Vector3 last;
    private float yLevel;
    public bool CamIsActive = false;
    private float Bounds;
    public CanvasGroup blackScreenCanvasGroup; // Assign in Inspector
    public TextMeshProUGUI loadingText; 

    //Wait for spawned player to fall down to the ground before setting camera Y level
    private IEnumerator SetYLevel()
    {

        yield return new WaitForSeconds(1f);
        Debug.LogWarning("Setting camera Y level");
        yLevel = transform.position.y;
        yield return new WaitForSeconds(1f); // Extra wait to reset the text
        loadingText.text = "";
        CamIsActive = true;

        // Fade out the black screen and loading text
        if (blackScreenCanvasGroup != null)
        {
            float fadeDuration = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                blackScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                yield return null;
            }
            blackScreenCanvasGroup.alpha = 0f;
        }
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
        blackScreenCanvasGroup = FindObjectOfType<CanvasGroup>();
        
        //loadingText = FindObjectTag<TextMeshProUGUI>();
        loadingText = GameObject.FindWithTag("LoadingText").GetComponent<TextMeshProUGUI>();
        loadingText.text = "Loading...";
        Debug.Log("text component found: " + loadingText.tag);
        
        if (cam) cam.enabled = Object.HasInputAuthority; 
        StartCoroutine(SetYLevel());
    }

    public bool IsCamActive()
    {
        return CamIsActive;
    }

    void LateUpdate()
    {
        if (Object != null && Object.HasInputAuthority && CamIsActive)
        {
            UpdatePosition();
        }
    }
}
