using TMPro;
using UnityEngine;

public class StatsRowItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text metricText;

    public void Bind(int rank, string title, string metric)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (titleText != null) titleText.text = string.IsNullOrWhiteSpace(title) ? "Unknown" : title;

        if (metricText != null)
            metricText.text = metric;

    }
}
