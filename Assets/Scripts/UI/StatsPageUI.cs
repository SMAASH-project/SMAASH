using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsPageUI : MonoBehaviour
{
    private static readonly Color SelectedButtonColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    private static readonly Color UnselectedButtonColor = Color.white;

    [Header("Services")]
    [SerializeField] private StatsClient statsClient;

    [Header("Top Navigation Buttons (4 stats tabs)")]
    [SerializeField] private Button activePlayersButton;
    [SerializeField] private Button popularItemsButton;
    [SerializeField] private Button playedLevelsButton;
    [SerializeField] private Button favouriteCharactersButton;

    [Header("Optional Back To Leaderboard Button")]
    [SerializeField] private Button leaderboardButton;

    [Header("Panels")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject activePlayersPanel;
    [SerializeField] private GameObject popularItemsPanel;
    [SerializeField] private GameObject playedLevelsPanel;
    [SerializeField] private GameObject favouriteCharactersPanel;

    [Header("List Roots")]
    [SerializeField] private Transform leaderboardListRoot;
    [SerializeField] private Transform activePlayersListRoot;
    [SerializeField] private Transform popularItemsListRoot;
    [SerializeField] private Transform playedLevelsListRoot;
    [SerializeField] private Transform favouriteCharactersListRoot;

    [Header("Row Prefab")]
    [SerializeField] private StatsRowItemUI rowPrefab;

    private bool leaderboardLoaded;
    private bool activePlayersLoaded;
    private bool popularItemsLoaded;
    private bool playedLevelsLoaded;
    private bool favouriteCharactersLoaded;

    private void Start()
    {
        if (statsClient == null)
            statsClient = FindObjectOfType<StatsClient>();

        if (statsClient == null)
        {
            SetStatus("StatsClient not found.");
            return;
        }

        if (rowPrefab == null)
        {
            SetStatus("Stats row prefab is missing.");
            return;
        }

        WireButtons();

        // Leaderboard is the default first screen.
        ShowLeaderboard();
    }

    private void WireButtons()
    {
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveAllListeners();
            leaderboardButton.onClick.AddListener(ShowLeaderboard);
        }

        if (activePlayersButton != null)
        {
            activePlayersButton.onClick.RemoveAllListeners();
            activePlayersButton.onClick.AddListener(ShowActivePlayers);
        }

        if (popularItemsButton != null)
        {
            popularItemsButton.onClick.RemoveAllListeners();
            popularItemsButton.onClick.AddListener(ShowPopularItems);
        }

        if (playedLevelsButton != null)
        {
            playedLevelsButton.onClick.RemoveAllListeners();
            playedLevelsButton.onClick.AddListener(ShowPlayedLevels);
        }

        if (favouriteCharactersButton != null)
        {
            favouriteCharactersButton.onClick.RemoveAllListeners();
            favouriteCharactersButton.onClick.AddListener(ShowFavouriteCharacters);
        }
    }

    public void ShowLeaderboard()
    {
        ShowOnlyPanel(leaderboardPanel);
        SetSelectedButton(leaderboardButton);
        StartCoroutine(LoadLeaderboard());
    }

    public void ShowActivePlayers()
    {
        ShowOnlyPanel(activePlayersPanel);
        SetSelectedButton(activePlayersButton);
        StartCoroutine(LoadActivePlayers());
    }

    public void ShowPopularItems()
    {
        ShowOnlyPanel(popularItemsPanel);
        SetSelectedButton(popularItemsButton);
        StartCoroutine(LoadPopularItems());
    }

    public void ShowPlayedLevels()
    {
        ShowOnlyPanel(playedLevelsPanel);
        SetSelectedButton(playedLevelsButton);
        StartCoroutine(LoadPlayedLevels());
    }

    public void ShowFavouriteCharacters()
    {
        ShowOnlyPanel(favouriteCharactersPanel);
        SetSelectedButton(favouriteCharactersButton);
        StartCoroutine(LoadFavouriteCharacters());
    }

    public void RefreshCurrentTab()
    {
        if (leaderboardPanel != null && leaderboardPanel.activeSelf)
        {
            leaderboardLoaded = false;
            StartCoroutine(LoadLeaderboard());
            return;
        }

        if (activePlayersPanel != null && activePlayersPanel.activeSelf)
        {
            activePlayersLoaded = false;
            StartCoroutine(LoadActivePlayers());
            return;
        }

        if (popularItemsPanel != null && popularItemsPanel.activeSelf)
        {
            popularItemsLoaded = false;
            StartCoroutine(LoadPopularItems());
            return;
        }

        if (playedLevelsPanel != null && playedLevelsPanel.activeSelf)
        {
            playedLevelsLoaded = false;
            StartCoroutine(LoadPlayedLevels());
            return;
        }

        if (favouriteCharactersPanel != null && favouriteCharactersPanel.activeSelf)
        {
            favouriteCharactersLoaded = false;
            StartCoroutine(LoadFavouriteCharacters());
        }
    }

    private IEnumerator LoadLeaderboard()
    {
        SetStatus("Loading leaderboard...");
        ClearList(leaderboardListRoot);

        bool ok = false;
        BestPlayerStatDto[] data = Array.Empty<BestPlayerStatDto>();
        string err = string.Empty;

        yield return statsClient.GetLeaderboard((success, result, error) =>
        {
            ok = success;
            data = result ?? Array.Empty<BestPlayerStatDto>();
            err = error;
        });

        LogJsonResponse("GetLeaderboard", data, err, ok);

        if (!ok)
        {
            SetStatus($"Leaderboard failed: {err}");
            yield break;
        }

        RenderRows(leaderboardListRoot, data.Length, i =>
        {
            var d = data[i];
            return new RowData
            {
                Title = d.display_name,
                Metric = $"Wins: {d.Wins}"
            };
        });

        leaderboardLoaded = true;
        SetStatus("Leaderboard ready.");
        Debug.Log($"Updated: {DateTime.Now:HH:mm:ss}");
    }

    private IEnumerator LoadActivePlayers()
    {
        SetStatus("Loading most active players...");
        ClearList(activePlayersListRoot);

        bool ok = false;
        TopPlayerStatDto[] data = Array.Empty<TopPlayerStatDto>();
        string err = string.Empty;

        yield return statsClient.GetMostActivePlayers((success, result, error) =>
        {
            ok = success;
            data = result ?? Array.Empty<TopPlayerStatDto>();
            err = error;
        });

        LogJsonResponse("GetMostActivePlayers", data, err, ok);

        if (!ok)
        {
            SetStatus($"Active players failed: {err}");
            yield break;
        }

        RenderRows(activePlayersListRoot, data.Length, i =>
        {
            var d = data[i];
            return new RowData
            {
                Title = d.display_name,
                Metric = $"Matches: {d.Matches}"
            };
        });

        activePlayersLoaded = true;
        SetStatus("Most active players ready.");
        Debug.Log($"Updated: {DateTime.Now:HH:mm:ss}");
    }

    private IEnumerator LoadPopularItems()
    {
        SetStatus("Loading popular items...");
        ClearList(popularItemsListRoot);

        bool ok = false;
        TopItemStatDto[] data = Array.Empty<TopItemStatDto>();
        string err = string.Empty;

        yield return statsClient.GetMostPopularItems((success, result, error) =>
        {
            ok = success;
            data = result ?? Array.Empty<TopItemStatDto>();
            err = error;
        });

        LogJsonResponse("GetMostPopularItems", data, err, ok);

        if (!ok)
        {
            SetStatus($"Popular items failed: {err}");
            yield break;
        }

        RenderRows(popularItemsListRoot, data.Length, i =>
        {
            var d = data[i];
            return new RowData
            {
                Title = d.Title,
                Metric = $"Purchases: {d.Purchases}"
            };
        });

        popularItemsLoaded = true;
        SetStatus("Popular items ready.");
        Debug.Log($"Updated: {DateTime.Now:HH:mm:ss}");
    }

    private IEnumerator LoadPlayedLevels()
    {
        SetStatus("Loading played levels...");
        ClearList(playedLevelsListRoot);

        bool ok = false;
        TopLevelStatDto[] data = Array.Empty<TopLevelStatDto>();
        string err = string.Empty;

        yield return statsClient.GetMostPlayedLevels((success, result, error) =>
        {
            ok = success;
            data = result ?? Array.Empty<TopLevelStatDto>();
            err = error;
        });

        LogJsonResponse("GetMostPlayedLevels", data, err, ok);

        if (!ok)
        {
            SetStatus($"Played levels failed: {err}");
            yield break;
        }

        RenderRows(playedLevelsListRoot, data.Length, i =>
        {
            var d = data[i];
            return new RowData
            {
                Title = d.name,
                Metric = $"Plays: {d.Plays}"
            };
        });

        playedLevelsLoaded = true;
        SetStatus("Played levels ready.");
        Debug.Log($"Updated: {DateTime.Now:HH:mm:ss}");
    }

    private IEnumerator LoadFavouriteCharacters()
    {
        SetStatus("Loading favourite characters...");
        ClearList(favouriteCharactersListRoot);

        bool ok = false;
        FavouriteCharacterStatDto[] data = Array.Empty<FavouriteCharacterStatDto>();
        string err = string.Empty;

        yield return statsClient.GetFavouriteCharactersForSelectedProfile((success, result, error) =>
        {
            ok = success;
            data = result ?? Array.Empty<FavouriteCharacterStatDto>();
            err = error;
        });

        LogJsonResponse("GetFavouriteCharactersForSelectedProfile", data, err, ok);

        if (!ok)
        {
            SetStatus($"Favourite characters failed: {err}");
            yield break;
        }

        RenderRows(favouriteCharactersListRoot, data.Length, i =>
        {
            var d = data[i];
            return new RowData
            {
                Title = d.name,
                Metric = $"Plays: {d.Plays}"
            };
        });

        favouriteCharactersLoaded = true;
        SetStatus("Favourite characters ready.");
        Debug.Log($"Updated: {DateTime.Now:HH:mm:ss}");
    }

    private void ShowOnlyPanel(GameObject activePanel)
    {
        SetPanelActive(leaderboardPanel, activePanel == leaderboardPanel);
        SetPanelActive(activePlayersPanel, activePanel == activePlayersPanel);
        SetPanelActive(popularItemsPanel, activePanel == popularItemsPanel);
        SetPanelActive(playedLevelsPanel, activePanel == playedLevelsPanel);
        SetPanelActive(favouriteCharactersPanel, activePanel == favouriteCharactersPanel);
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
            panel.SetActive(isActive);
    }

    private void SetSelectedButton(Button selectedButton)
    {
        SetButtonColor(leaderboardButton, leaderboardButton == selectedButton);
        SetButtonColor(activePlayersButton, activePlayersButton == selectedButton);
        SetButtonColor(popularItemsButton, popularItemsButton == selectedButton);
        SetButtonColor(playedLevelsButton, playedLevelsButton == selectedButton);
        SetButtonColor(favouriteCharactersButton, favouriteCharactersButton == selectedButton);
    }

    private void SetButtonColor(Button button, bool isSelected)
    {
        if (button == null || button.image == null)
            return;

        button.image.color = isSelected ? SelectedButtonColor : UnselectedButtonColor;
    }

    private void ClearList(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private void RenderRows(Transform root, int count, Func<int, RowData> make)
    {
        if (root == null)
            return;

        if (count <= 0)
        {
            SetStatus("No data available.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(rowPrefab, root);
            var data = make(i);
            row.Bind(i + 1, data.Title, data.Metric);
        }
    }

    private void SetStatus(string message)
    {
        Debug.Log(message);
    }

    private void LogJsonResponse<T>(string endpoint, T[] data, string error, bool success)
    {
        string json = ToJsonArray(data);
        Debug.Log($"[{endpoint}] success={success}, error='{error}', payload={json}");
    }

    private string ToJsonArray<T>(T[] data)
    {
        var wrapper = new JsonArrayWrapper<T>
        {
            items = data ?? Array.Empty<T>()
        };

        return JsonUtility.ToJson(wrapper, true);
    }

    private struct RowData
    {
        public string Title;
        public string Metric;
    }

    [Serializable]
    private class JsonArrayWrapper<T>
    {
        public T[] items;
    }
}
