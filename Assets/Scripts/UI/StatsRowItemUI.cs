using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsRowItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text metricText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite fallbackIcon;

    public void Bind(int rank, string title, string subtitle, string metric, Sprite icon = null)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (titleText != null) titleText.text = string.IsNullOrWhiteSpace(title) ? "Unknown" : title;

        if (subtitleText != null)
        {
            bool hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
            subtitleText.gameObject.SetActive(hasSubtitle);
            subtitleText.text = hasSubtitle ? subtitle : string.Empty;
        }

        if (metricText != null) metricText.text = metric;

        if (iconImage != null)
        {
            iconImage.sprite = icon != null ? icon : fallbackIcon;
            iconImage.preserveAspect = true;
        }
    }
}
