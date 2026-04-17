using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill;
    public TMP_Text playerNameText;

    // We no longer need [Networked] properties here because 
    // the PlayerHealth script will provide the data.

    public void SetMaxHealth(int health)
    {
        if (slider)
        {
            slider.maxValue = health;
            slider.value = health;
        }
        UpdateColor();
    }

    public void SetHealth(int health)
    {
        if (slider)
        {
            slider.value = health;
        }
        UpdateColor();
    }

    public void SetPlayerName(string playerName)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;
    }

    private void UpdateColor()
    {
        if (fill && gradient != null && slider != null)
        {
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }
    }
}