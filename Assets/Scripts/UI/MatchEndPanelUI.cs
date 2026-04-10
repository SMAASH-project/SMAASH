using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchEndPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private Button exitButton;

    [Header("Hide Gameplay UI")]
    [SerializeField] private GameObject joystickRoot;
    [SerializeField] private GameObject jumpButtonRoot;
    [SerializeField] private GameObject attackButtonRoot;
    [SerializeField] private HealthBar healthBar1;
    [SerializeField] private HealthBar healthBar2;

    [Header("Text")]
    [SerializeField] private string winLabel = "YOU WIN";
    [SerializeField] private string loseLabel = "YOU LOSE";
    [SerializeField] private string coinFormat = "+{0} coins";

    private bool _isShowing;

    private void Start()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonPressed);
            exitButton.onClick.AddListener(OnExitButtonPressed);
        }

        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        NetworkHandler.OnLocalMatchEnded += HandleLocalMatchEnded;
    }

    private void OnDisable()
    {
        NetworkHandler.OnLocalMatchEnded -= HandleLocalMatchEnded;
    }

    private void HandleLocalMatchEnded(NetworkHandler.MatchEndUiData data)
    {
        _isShowing = true;

        if (resultText != null)
            resultText.text = data.IsWin ? winLabel : loseLabel;

        if (coinsText != null)
            coinsText.text = string.Format(coinFormat, data.RewardCoins);

        HideGameplayUI();
        SetPanelVisible(true);
    }

    private void OnExitButtonPressed()
    {
        if (!_isShowing)
            return;

        if (NetworkHandler.Instance != null)
        {
            NetworkHandler.Instance.ExitMatchToLobby();
            return;
        }

        Debug.LogWarning("MatchEndPanelUI: NetworkHandler instance is missing, cannot exit match.");
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
    }

    private void HideGameplayUI()
    {
        SetObjectVisible(joystickRoot, false);
        SetObjectVisible(jumpButtonRoot, false);
        SetObjectVisible(attackButtonRoot, false);

        if (healthBar1 != null)
            healthBar1.gameObject.SetActive(false);
        if (healthBar2 != null)
            healthBar2.gameObject.SetActive(false);

        if (healthBar1 == null && healthBar2 == null && UIManager.Instance != null)
        {
            if (UIManager.Instance.healthBar1 != null)
                UIManager.Instance.healthBar1.gameObject.SetActive(false);
            if (UIManager.Instance.healthBar2 != null)
                UIManager.Instance.healthBar2.gameObject.SetActive(false);
        }
    }

    private static void SetObjectVisible(GameObject target, bool isVisible)
    {
        if (target != null)
            target.SetActive(isVisible);
    }
}
