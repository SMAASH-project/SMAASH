using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;

public class CountdownScript : MonoBehaviour
{
    public TMP_Text countdown_text;
    private int expectedPlayerCount = 2; // Set this to the number of players you expect
    private float maxWaitTime = 10f; // Maximum time to wait for players to spawn
    private bool camIsActive = false;
    private ButtonCooldowns Attack_btn;

    [Header("Hide During Countdown")]
    [SerializeField] private GameObject joystickRoot;
    [SerializeField] private GameObject jumpButtonRoot;
    [SerializeField] private GameObject attackButtonRoot;
    [SerializeField] private HealthBar healthBar1;
    [SerializeField] private HealthBar healthBar2;

    void Start()
    {
        GameObject attackObject = GameObject.Find("Attack");
        if (attackObject != null)
            Attack_btn = attackObject.GetComponent<ButtonCooldowns>();

        ResolveGameplayUiReferences();
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        SetGameplayUiVisible(false);

        bool isSinglePlayer = IsSinglePlayerMode();
        int requiredPlayerCount = isSinglePlayer ? 1 : expectedPlayerCount;

        // Wait for players to spawn
        if (Attack_btn != null)
        {
            Attack_btn.SetCountdownState(true);
        }
        float waitTime = 0f;
        GameObject[] players = new GameObject[0];
        
        while (players.Length < requiredPlayerCount && waitTime < maxWaitTime)
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            Debug.Log("Waiting for players... Found: " + players.Length + "/" + requiredPlayerCount);
            
            if (players.Length < requiredPlayerCount)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
            }
        }

        Debug.Log("Players found for countdown: " + players.Length);
        
        // Wait for camera setup and black-screen fade-out to complete.
        // This runs in both multiplayer and singleplayer.
        {
            waitTime = 0f;
            bool allCamerasActive = false;

            while (!allCamerasActive && waitTime < maxWaitTime)
            {
                allCamerasActive = true;
                int readyCamCount = 0;

                foreach (GameObject player in players)
                {
                    CameraController camController = player.GetComponent<CameraController>();
                    bool isCameraReady = camController != null && camController.IsCamActive() && !IsBlackScreenVisible(camController);
                    if (!isCameraReady)
                    {
                        allCamerasActive = false;
                    }
                    else
                    {
                        readyCamCount++;
                    }
                }

                Debug.Log("Cameras ready (including fade): " + readyCamCount + "/" + players.Length);

                if (!allCamerasActive)
                {
                    yield return new WaitForSeconds(0.5f);
                    waitTime += 0.5f;
                }
            }
        }
        
        Debug.Log("All cameras ready!");

        // Freeze both players before countdown
        foreach (GameObject player in players)
        {
            player.GetComponent<PlayerMovement>().isCountingDown = true;
            
            if(player.GetComponent<MeleeAttack>() != null)
            {
                player.GetComponent<MeleeAttack>().isCountingDown = true; 
            }
            else if(player.GetComponent<ShootingAttack>() != null)
            {
                player.GetComponent<ShootingAttack>().isCountingDown = true; 
            }

           
        }
        
        // Countdown sequence
        countdown_text.text = "3";
        yield return new WaitForSeconds(1);
        countdown_text.text = "2";
        yield return new WaitForSeconds(1);
        countdown_text.text = "1";
        yield return new WaitForSeconds(1);
        countdown_text.text = "FIGHT!";
        yield return new WaitForSeconds(1);
        countdown_text.text = "";

        // Unfreeze both players after countdown
        foreach (GameObject player in players)
        {
            player.GetComponent<PlayerMovement>().isCountingDown = false;
            if(player.GetComponent<MeleeAttack>() != null)
            {
                player.GetComponent<MeleeAttack>().isCountingDown = false; 
            }
            else if(player.GetComponent<ShootingAttack>() != null)
            {
                player.GetComponent<ShootingAttack>().isCountingDown = false; 
            }
        }
        if (Attack_btn != null)
        {
            Attack_btn.SetCountdownState(false);
        }

        SetGameplayUiVisible(true);
    }

    private void ResolveGameplayUiReferences()
    {
        if (attackButtonRoot == null)
        {
            GameObject attackObject = GameObject.Find("Attack");
            if (attackObject != null)
                attackButtonRoot = attackObject;
        }

        if (jumpButtonRoot == null)
        {
            GameObject jumpObject = GameObject.Find("Jump");
            if (jumpObject != null)
                jumpButtonRoot = jumpObject;
        }

        if (joystickRoot == null)
        {
            FloatingJoystick joystick = FindObjectOfType<FloatingJoystick>();
            if (joystick != null)
                joystickRoot = joystick.gameObject;
        }
    }

    private void SetGameplayUiVisible(bool isVisible)
    {
        // Keep joystick visible during countdown.
        SetObjectVisible(joystickRoot, true);
        SetObjectVisible(jumpButtonRoot, isVisible);
        SetObjectVisible(attackButtonRoot, isVisible);

        if (healthBar1 != null)
            healthBar1.gameObject.SetActive(isVisible);
        if (healthBar2 != null)
            healthBar2.gameObject.SetActive(isVisible);

        if (healthBar1 == null && healthBar2 == null && UIManager.Instance != null)
        {
            if (UIManager.Instance.healthBar1 != null)
                UIManager.Instance.healthBar1.gameObject.SetActive(isVisible);
            if (UIManager.Instance.healthBar2 != null)
                UIManager.Instance.healthBar2.gameObject.SetActive(isVisible);
        }

        if (isVisible)
            ResetLocalJumpButtonState();
    }

    private static void SetObjectVisible(GameObject target, bool isVisible)
    {
        if (target != null)
            target.SetActive(isVisible);
    }

    private static bool IsSinglePlayerMode()
    {
        NetworkRunner runner = Object.FindObjectOfType<NetworkRunner>();
        return runner != null && runner.GameMode == GameMode.Single;
    }

    private static bool IsBlackScreenVisible(CameraController camController)
    {
        if (camController == null || camController.blackScreenCanvasGroup == null)
            return false;

        return camController.blackScreenCanvasGroup.alpha > 0.01f;
    }

    private static void ResetLocalJumpButtonState()
    {
        PlayerMovement[] players = Object.FindObjectsOfType<PlayerMovement>();
        foreach (PlayerMovement player in players)
            player.ResetJumpButtonState();
    }
}