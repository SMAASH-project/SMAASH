using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ── DTOs ──
[Serializable] public class GameLoginRequest { public string email; public string password; }
[Serializable] public class GameLoginResponse { public string accessToken; public string refreshToken; }
[Serializable] public class RefreshRequestDto { public string refreshToken; }
[Serializable] public class RefreshResponseDto { public string accessToken; public string refreshToken; }
[Serializable] public class JwtPayload { public long exp; }
[Serializable] public class GameJwtPayload { public long exp; public float sub; }

[Serializable] public class PlayerProfileDto
{
    public int id;
    public string display_name;
    public long coins;
    public string last_login;
}

[Serializable] public class PlayerProfileListResponse { public PlayerProfileDto[] profiles; }

public class AuthClient : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:8080"; // szerver: https://smaash-web.onrender.com
    [SerializeField] private string profileSelectScene = "sc_profile_select";
    [SerializeField] private string loadingSceneName = "sc_loading";
    [SerializeField] private string loginScene = "sc_register";
    [SerializeField] private bool clearPrefsOnStartForTesting = false;

    private static AuthClient _instance;

    public string AccessToken { get; private set; }
    public string BaseUrl => baseUrl.TrimEnd('/');

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    private const string AccessKey = "access_token";
    private const string RefreshKey = "refresh_token";
    private const string SelectedProfileKey = "selected_profile_id";
    private const string DisplayNameKey = "display_name";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If we're in the login scene, destroy the old DontDestroyOnLoad instance
            // and keep this scene-local instance instead
            if (SceneManager.GetActiveScene().name == loginScene)
            {
                Debug.LogWarning("AuthClient instance being re-created on login scene. Destroying old DontDestroyOnLoad instance.");
                Destroy(_instance.gameObject);
                _instance = this;
                // Don't mark as DontDestroyOnLoad - let it be destroyed with the scene
                RestoreAccessTokenFromPrefs();
                StartCoroutine(TryAutoLogin());
                return;
            }

            // For other scenes, keep the existing singleton
            Debug.LogWarning("Multiple AuthClient instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        RestoreAccessTokenFromPrefs();
        StartCoroutine(TryAutoLogin());
    }

    private void Start()
    {
        if (clearPrefsOnStartForTesting)
            PlayerPrefs.DeleteAll();

        //StartCoroutine(TryAutoLogin());
    }

    private void OnDestroy()
    {
        // Clean up the singleton reference if this is the current instance
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // ──────────────────────────────────────────────
    // LOGIN
    // ──────────────────────────────────────────────

    public void OnLoginButtonClicked()
    {
        StartCoroutine(Login(emailInput.text, passwordInput.text, (success, msg) =>
        {
            if (success)
            {
                Debug.Log("Login successful!");
                SceneManager.LoadScene(profileSelectScene);
            }
            else
            {
                Debug.LogError($"Login failed: {msg}");
            }
        }));
    }

    private IEnumerator Login(string email, string password, Action<bool, string> done)
    {
        var json = JsonUtility.ToJson(new GameLoginRequest { email = email, password = password });

        using var req = new UnityWebRequest($"{baseUrl}/api/game-login", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
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
        done(true, "");
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

        var json = JsonUtility.ToJson(new RefreshRequestDto { refreshToken = currentRefresh });

        using var req = new UnityWebRequest($"{baseUrl}/api/game-refresh", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success || req.responseCode != 200)
        {
            ClearTokens();
            done?.Invoke(false);
            yield break;
        }

        var resp = JsonUtility.FromJson<RefreshResponseDto>(req.downloadHandler.text);
        SaveTokens(resp.accessToken, resp.refreshToken);
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

        if (!string.IsNullOrEmpty(savedAccess) && IsJwtNotExpired(savedAccess))
        {
            AccessToken = savedAccess;
            if (SceneManager.GetActiveScene().name != profileSelectScene)
                SceneManager.LoadScene(profileSelectScene);
            yield break;
        }

        if (!string.IsNullOrEmpty(savedRefresh))
        {
            bool refreshed = false;
            yield return RefreshToken(ok => refreshed = ok);

            if (refreshed && SceneManager.GetActiveScene().name != profileSelectScene)
                SceneManager.LoadScene(profileSelectScene);
        }
    }

    // ──────────────────────────────────────────────
    // PROFILES
    // ──────────────────────────────────────────────

    public IEnumerator GetMyProfiles(Action<bool, PlayerProfileDto[]> done)
    {
        string token = AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            token = PlayerPrefs.GetString(AccessKey, "");
        }

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("GetProfiles failed: no access token found");
            done(false, null);
            yield break;
        }

        int userId = GetUserIdFromToken(token);
        if (userId < 0)
        {
            Debug.LogError("GetProfiles failed: invalid token payload (missing sub)");
            done(false, null);
            yield break;
        }

        using var req = UnityWebRequest.Get($"{baseUrl}/api/users/{userId}/profiles");
        req.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GetProfiles failed: {req.downloadHandler.text}");
            done(false, null);
            yield break;
        }

        var rawJson = req.downloadHandler.text?.Trim();
        PlayerProfileDto[] profiles;

        if (!string.IsNullOrEmpty(rawJson) && rawJson.StartsWith("["))
        {
            profiles = JsonHelper.FromJsonArray<PlayerProfileDto>(rawJson);
        }
        else
        {
            var resp = JsonUtility.FromJson<PlayerProfileListResponse>(rawJson);
            profiles = resp != null ? resp.profiles : Array.Empty<PlayerProfileDto>();
        }

        Debug.Log("Response JSON: " + rawJson);
        Debug.Log("Profiles: " + string.Join(", ", Array.ConvertAll(profiles, p => p.display_name)));
        done(true, profiles);
    }

    public void SelectProfile(PlayerProfileDto profile)
    {
        PlayerPrefs.SetInt(SelectedProfileKey, profile.id);
        PlayerPrefs.SetString(DisplayNameKey, profile.display_name);
        PlayerPrefs.Save();
        SceneManager.LoadScene(loadingSceneName);
    }

    private int GetUserIdFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("GetUserIdFromToken failed: no token provided");
            return -1;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            Debug.LogError("GetUserIdFromToken failed: token does not have enough parts");
            return -1;
        }

        try
        {
            var json = DecodeBase64Url(parts[1]);
            var payload = JsonUtility.FromJson<GameJwtPayload>(json);
            Debug.Log(json + " => sub=" + payload.sub + ", exp=" + payload.exp);
            if (payload == null || payload.sub <= 0)
            { 
                Debug.LogError("GetUserIdFromToken failed: invalid token payload (missing or non-positive sub)");
                return -1;
            }
            Debug.Log("Extracted user ID from token: " + (int)payload.sub);
            return (int)payload.sub;
            
        }
        catch
        {
            Debug.LogError("GetUserIdFromToken failed: error decoding token payload");
            return -1;
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

    private void ClearProfileSelection()
    {
        PlayerPrefs.DeleteKey(SelectedProfileKey);
        PlayerPrefs.DeleteKey(DisplayNameKey);
        PlayerPrefs.Save();
    }

    private void RestoreAccessTokenFromPrefs()
    {
        string savedAccess = PlayerPrefs.GetString(AccessKey, "");
        if (!string.IsNullOrEmpty(savedAccess))
        {
            AccessToken = savedAccess;
        }
    }

    public void Logout()
    {
        _instance = null;  // Clear the singleton reference before logout
        ClearTokens();
        ClearProfileSelection();

        if (NetworkHandler.Instance != null)
        {
            NetworkHandler.Instance.DisposeForLogout();
        }

        SceneManager.LoadScene(loginScene);
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
            var payload = JsonUtility.FromJson<JwtPayload>(DecodeBase64Url(parts[1]));
            return payload != null && payload.exp > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch { return false; }
    }

    private string DecodeBase64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}