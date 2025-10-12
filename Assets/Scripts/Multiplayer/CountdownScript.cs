using System.Collections;
using UnityEngine;
using TMPro;

public class CountdownScript : MonoBehaviour
{
    public TMP_Text countdown_text;

    void Start()
    {
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

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