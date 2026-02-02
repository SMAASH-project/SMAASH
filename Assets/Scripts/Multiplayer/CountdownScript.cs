using System.Collections;
using UnityEngine;
using TMPro;

public class CountdownScript : MonoBehaviour
{
    public TMP_Text countdown_text;
    private int expectedPlayerCount = 2; // Set this to the number of players you expect
    private float maxWaitTime = 10f; // Maximum time to wait for players to spawn

    void Start()
    {
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        // Wait for players to spawn
        float waitTime = 0f;
        GameObject[] players = new GameObject[0];
        
        while (players.Length < expectedPlayerCount && waitTime < maxWaitTime)
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            Debug.Log("Waiting for players... Found: " + players.Length + "/" + expectedPlayerCount);
            
            if (players.Length < expectedPlayerCount)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
            }
        }

        Debug.Log("Players found for countdown: " + players.Length);

        // Freeze both players before countdown
        foreach (GameObject player in players)
        {
            player.GetComponent<PlayerMovement>().isCountingDown = true;
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
        }
    }
}