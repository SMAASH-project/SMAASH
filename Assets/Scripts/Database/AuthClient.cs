using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable] public class GameLoginRequest { public string email; public string password; }
[Serializable] public class PlayerProfileDto { public int id; public string display_name; public long coins; public string last_login; }
[Serializable] public class GameLoginResponse { public string accessToken; public string refreshToken; public PlayerProfileDto profile; }
[Serializable] public class RefreshRequestDto { public string refreshToken; }
[Serializable] public class RefreshResponseDto { public string accessToken; public string refreshToken; }
[Serializable] public class JwtPayload { public long exp; }

public class AuthClient : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:8080";
    [SerializeField] private string mainSceneName = "sc_main";

    public string AccessToken { get; private set; }

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    private const string AccessKey = "access_token";
    private const string RefreshKey = "refresh_token";

    private void Start()
    {
        if (passwordInput != null)
            passwordInput.contentType = TMP_InputField.ContentType.Password;

        StartCoroutine(TryAutoLogin());
    }

    // ──────────────────────────────────────────────
    // LOGIN
    // ──────────────────────────────────────────────

    public void OnLoginButtonClicked()
    {
        StartCoroutine(Login(emailInput.text, passwordInput.text, (success, message) =>
        {
            if (success)
            {
                Debug.Log($"Login successful! Welcome {message}");
                SceneManager.LoadScene(mainSceneName);
            }
            else
            {
                Debug.LogError($"Login failed: {message}");
            }
        }));
    }

    public IEnumerator Login(string email, string password, Action<bool, string> done)
    {
        var reqBody = JsonUtility.ToJson(new GameLoginRequest { email = email, password = password });

        using var req = new UnityWebRequest($"{baseUrl}/api/game-login", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(reqBody));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(false, req.downloadHandler.text);
            yield break;
        }

        var resp = JsonUtility.FromJson<GameLoginResponse>(req.downloadHandler.text);
        SaveTokens(resp.accessToken, resp.refreshToken);
        Debug.Log($"Received access token: {AccessToken}");
        done(true, resp.profile.display_name);
    }

    // ──────────────────────────────────────────────
    // REFRESH
    // ──────────────────────────────────────────────

    private IEnumerator RefreshToken(Action<bool> done)
    {
        string currentRefresh = PlayerPrefs.GetString(RefreshKey, "");
        if (string.IsNullOrEmpty(currentRefresh))
        {
            done?.Invoke(false);
            yield break;
        }

        var body = new RefreshRequestDto { refreshToken = currentRefresh };
        string json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest($"{baseUrl}/api/auth/refresh", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success || req.responseCode != 200)
        {
            Debug.LogWarning("Refresh failed, clearing tokens");
            ClearTokens();
            done?.Invoke(false);
            yield break;
        }

        var resp = JsonUtility.FromJson<RefreshResponseDto>(req.downloadHandler.text);
        SaveTokens(resp.accessToken, resp.refreshToken);
        Debug.Log("Tokens refreshed successfully");
        done?.Invoke(true);
    }

    // ──────────────────────────────────────────────
    // AUTO LOGIN
    // ──────────────────────────────────────────────

    private IEnumerator TryAutoLogin()
    {
        string savedAccess = PlayerPrefs.GetString(AccessKey, "");
        string savedRefresh = PlayerPrefs.GetString(RefreshKey, "");

        if (string.IsNullOrEmpty(savedAccess) && string.IsNullOrEmpty(savedRefresh))
            yield break;

        // Access token still valid
        if (!string.IsNullOrEmpty(savedAccess) && IsJwtNotExpired(savedAccess))
        {
            AccessToken = savedAccess;
            Debug.Log("Valid saved access token found. Skipping login.");
            SceneManager.LoadScene(mainSceneName);
            yield break;
        }

        // Access expired but refresh exists -> try refresh
        if (!string.IsNullOrEmpty(savedRefresh))
        {
            bool refreshed = false;
            yield return RefreshToken(success => refreshed = success);

            if (refreshed)
            {
                Debug.Log("Auto-login via refresh succeeded.");
                SceneManager.LoadScene(mainSceneName);
            }
            else
            {
                Debug.Log("Auto-login failed. User must log in again.");
            }
        }
    }

    // ──────────────────────────────────────────────
    // TOKEN STORAGE
    // ──────────────────────────────────────────────

    private void SaveTokens(string access, string refresh)
    {
        AccessToken = access;
        PlayerPrefs.SetString(AccessKey, access);
        PlayerPrefs.SetString(RefreshKey, refresh);
        PlayerPrefs.Save();
    }

    private void ClearTokens()
    {
        AccessToken = null;
        PlayerPrefs.DeleteKey(AccessKey);
        PlayerPrefs.DeleteKey(RefreshKey);
        PlayerPrefs.Save();
    }

    public void Logout()
    {
        ClearTokens();
    }

    // ──────────────────────────────────────────────
    // JWT HELPERS
    // ──────────────────────────────────────────────

    private bool IsJwtNotExpired(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return false;

        try
        {
            var json = DecodeBase64Url(parts[1]);
            var payload = JsonUtility.FromJson<JwtPayload>(json);
            if (payload == null || payload.exp <= 0) return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return payload.exp > now;
        }
        catch { return false; }
    }

    private string DecodeBase64Url(string base64Url)
    {
        var output = base64Url.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(output));
    }
}