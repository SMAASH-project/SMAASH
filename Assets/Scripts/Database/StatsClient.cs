using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class BestPlayerStatDto
{
    public int id;
    public string display_name;
    public int count_of_wins;
    public int countOfWins;

    public int Wins => count_of_wins > 0 ? count_of_wins : countOfWins;
}

[Serializable]
public class TopPlayerStatDto
{
    public int id;
    public string display_name;
    public int count_of_matches;
    public int countOfMatches;

    public int Matches => count_of_matches > 0 ? count_of_matches : countOfMatches;
}

[Serializable]
public class TopItemStatDto
{
    public int id;
    public string name;
    public string display_name;
    public int count_of_purchases;
    public int countOfPurchases;

    public string Title => !string.IsNullOrWhiteSpace(name) ? name : display_name;
    public int Purchases => count_of_purchases > 0 ? count_of_purchases : countOfPurchases;
}

[Serializable]
public class TopLevelStatDto
{
    public int id;
    public string name;
    public int count_of_plays;
    public int countOfPlays;

    public int Plays => count_of_plays > 0 ? count_of_plays : countOfPlays;
}

[Serializable]
public class FavouriteCharacterStatDto
{
    public int id;
    public string name;
    public int count_of_plays;
    public int countOfPlays;

    public int Plays => count_of_plays > 0 ? count_of_plays : countOfPlays;
}

public class StatsClient : MonoBehaviour
{
    [SerializeField] private global::GameApiClent gameApiClient;

    private const string SelectedProfileKey = "selected_profile_id";

    private void Awake()
    {
        if (gameApiClient == null)
            gameApiClient = FindObjectOfType<global::GameApiClent>();
    }

    public IEnumerator GetLeaderboard(Action<bool, BestPlayerStatDto[], string> done)
    {
        yield return GetArray("/api/stats/leaderboard", done);
    }

    public IEnumerator GetMostActivePlayers(Action<bool, TopPlayerStatDto[], string> done)
    {
        yield return GetArray("/api/stats/top/players", done);
    }

    public IEnumerator GetMostPopularItems(Action<bool, TopItemStatDto[], string> done)
    {
        yield return GetArray("/api/stats/top/items", done);
    }

    public IEnumerator GetMostPlayedLevels(Action<bool, TopLevelStatDto[], string> done)
    {
        yield return GetArray("/api/stats/top/levels", done);
    }

    public IEnumerator GetFavouriteCharactersForSelectedProfile(Action<bool, FavouriteCharacterStatDto[], string> done)
    {
        int profileId = PlayerPrefs.GetInt(SelectedProfileKey, -1);
        if (profileId <= 0)
        {
            done?.Invoke(false, Array.Empty<FavouriteCharacterStatDto>(), "No selected profile.");
            yield break;
        }

        yield return GetFavouriteCharactersByProfileId(profileId, done);
    }

    public IEnumerator GetFavouriteCharactersByProfileId(int profileId, Action<bool, FavouriteCharacterStatDto[], string> done)
    {
        if (profileId <= 0)
        {
            done?.Invoke(false, Array.Empty<FavouriteCharacterStatDto>(), "Invalid profile id.");
            yield break;
        }

        yield return GetArray($"/api/stats/profiles/{profileId}/favourite", done);
    }

    private IEnumerator GetArray<T>(string endpoint, Action<bool, T[], string> done)
    {
        if (gameApiClient == null)
            gameApiClient = FindObjectOfType<global::GameApiClent>();

        if (gameApiClient == null)
        {
            done?.Invoke(false, Array.Empty<T>(), "GameApiClent missing.");
            yield break;
        }

        string token = gameApiClient.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            token = PlayerPrefs.GetString("access_token", "");

        if (string.IsNullOrWhiteSpace(token))
        {
            done?.Invoke(false, Array.Empty<T>(), "Missing access token.");
            yield break;
        }

        string normalized = endpoint.StartsWith("/") ? endpoint : "/" + endpoint;
        using var req = UnityWebRequest.Get($"{gameApiClient.BaseUrl}{normalized}");
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

        if (!ok)
        {
            string message = string.IsNullOrWhiteSpace(body) ? $"HTTP {req.responseCode}" : body;
            done?.Invoke(false, Array.Empty<T>(), message);
            yield break;
        }

        if (!JsonHelper.TryFromJsonArray(body, out T[] parsed))
        {
            done?.Invoke(false, Array.Empty<T>(), "Failed to parse stats response.");
            yield break;
        }

        done?.Invoke(true, parsed, string.Empty);
    }
}
