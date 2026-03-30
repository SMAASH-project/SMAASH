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
}

[Serializable] public class PlayerProfileCreateResponseDto
{
    public int ID;
    public string DisplayName;
    public long Coins;
    public string PfpUri;
    public int UserID;
}

[Serializable] public class UserCreateDto
{
    public string email;
    public string password;
}

[Serializable] public class PlayerProfileCreateDto
{
    public int user_id;
    public string display_name;
}

[Serializable] public class PlayerProfileListResponse { public PlayerProfileDto[] profiles; }

public class AuthClient : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private bool useLocalhost = false;
    [SerializeField] private string localhostUrl = "http://localhost:8080";
    [SerializeField] private string deployedUrl = "https://smaash-web.onrender.com";

    [SerializeField] private string profileSelectScene = "sc_profile_select";
    [SerializeField] private string loadingSceneName = "sc_loading";
    [SerializeField] private string loginScene = "sc_login";
    [SerializeField] private bool clearPrefsOnStartForTesting = false;

    private static AuthClient _instance;

    public string AccessToken { get; private set; }
    public string BaseUrl => (useLocalhost ? localhostUrl : deployedUrl).TrimEnd('/');

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    //public TMP_InputField signUpEmailInput;
    //public TMP_InputField signUpPasswordInput;

    public TMP_Text statusText;

    private const string AccessKey = "access_token";
    private const string RefreshKey = "refresh_token";
    private const string SelectedProfileKey = "selected_profile_id";
    private const string DisplayNameKey = "display_name";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If we're in the login scene, destroy the old DontDestroyOnLoad instance
            // and keep this instance (with fresh UI refs from the scene)
            if (SceneManager.GetActiveScene().name == loginScene)
            {
                Debug.LogWarning("AuthClient instance being re-created on login scene. Destroying old DontDestroyOnLoad instance.");
                Destroy(_instance.gameObject);
                _instance = this;
                DontDestroyOnLoad(gameObject);
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
        
        // Debug: Show active backend
        string activeBackend = useLocalhost ? "LOCALHOST" : "DEPLOYED";
        string activeUrl = BaseUrl;
        Debug.Log($"[AuthClient] Backend: {activeBackend} → {activeUrl}");
        
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
        // Validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            done(false, "Email and/or password cannot be empty.");
            yield break;
        }

        if (!email.Contains("@"))
        {
            done(false, "Please enter a valid email address.");
            yield break;
        }

        if (password.Length < 8)
        {
            done(false, "Password must be at least 8 characters long.");
            yield break;
        }

        
        var json = JsonUtility.ToJson(new GameLoginRequest { email = email, password = password });

        using var req = new UnityWebRequest($"{BaseUrl}/api/game-login", "POST");
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

    /// ──────────────────────────────────────────────
    /// SIGNUP
    /// ──────────────────────────────────────────────

    public void OnSignUpButtonClicked()
    {
        StartCoroutine(SignUp(emailInput.text, passwordInput.text, (success, msg) =>
        {
            if (success)
            {
                Debug.Log("Signup successful! Please log in.");
                if (statusText != null)
                {
                    statusText.text = "Signup successful! Please log in.";
                    statusText.color = Color.green;
                }

                if (SceneManager.GetActiveScene().name != loginScene)
                    SceneManager.LoadScene(loginScene);
            }
            else
            {
                Debug.LogError($"Signup failed: {msg}");
                if (statusText != null)
                {
                    statusText.text = $"Signup failed: {msg}";
                    statusText.color = Color.red;
                }
            }
        }));
    } 

    private IEnumerator SignUp(string email, string password, Action<bool, string> done)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            done(false, "Email and/or password cannot be empty.");
            yield break;
        }

        if (!email.Contains("@"))
        {
            done(false, "Please enter a valid email address.");
            yield break;
        }

        if (password.Length < 8)
        {
            done(false, "Password must be at least 8 characters long.");
            yield break;
        }

        var json = JsonUtility.ToJson(new UserCreateDto { email = email, password = password });

        using var req = new UnityWebRequest($"{BaseUrl}/api/auth/signup", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(false, req.downloadHandler.text);
            yield break;
        }

        done(true, "Signup successful! Please log in.");
    }

    /// ──────────────────────────────────────────────
    /// CREATE PROFILE
    /// ──────────────────────────────────────────────
    
    public IEnumerator CreateProfile(string displayName, Action<bool, string, PlayerProfileDto> done)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            done(false, "Display name cannot be empty.", null);
            yield break;
        }

        string token = AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            token = PlayerPrefs.GetString(AccessKey, "");

        if (string.IsNullOrWhiteSpace(token))
        {
            done(false, "Missing access token", null);
            yield break;
        }

        int userId = GetUserIdFromToken(token);
        if (userId < 0)
        {
            done(false, "Invalid token payload (missing user id)", null);
            yield break;
        }

        var payload = new PlayerProfileCreateDto
        {
            user_id = userId,
            display_name = displayName.Trim()
        };

        bool createSuccess = false;
        string createMsg = string.Empty;

        yield return PostAuthorizedJson("/api/profiles", payload, (success, msg) =>
        {
            createSuccess = success;
            createMsg = msg;
        });

        if (!createSuccess)
        {
            Debug.LogError($"Profile creation failed: {createMsg}");
            done(false, $"Profile creation failed: {createMsg}", null);
            yield break;
        }

        PlayerProfileDto createdProfile = TryParseCreatedProfile(createMsg);

        Debug.Log("Raw backend response: " + createMsg);
        Debug.Log("Parsed profile Id: " + createdProfile?.id);
        Debug.Log("Parsed profile display_name: " + createdProfile?.display_name);
        
        if (!HasValidProfileId(createdProfile))
        {
            string parseError = "Profile created, but backend response is not compatible with PlayerProfileDto (missing valid id/display_name).";
            Debug.LogError($"{parseError} Raw response: {createMsg}");
            done(false, parseError, null);
            yield break;
        }

        Debug.Log("Profile created successfully!");
        done(true, "Profile created successfully!", createdProfile);
    }

    public IEnumerator UploadProfilePicture(int profileId, byte[] imageBytes, string fileName, Action<bool, string> done)
    {
        if (profileId <= 0)
        {
            done?.Invoke(false, "Invalid profile id");
            yield break;
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            done?.Invoke(false, "No image data to upload");
            yield break;
        }

        string token = AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            token = PlayerPrefs.GetString(AccessKey, "");

        if (string.IsNullOrWhiteSpace(token))
        {
            done?.Invoke(false, "Missing access token");
            yield break;
        }

        var safeFileName = string.IsNullOrWhiteSpace(fileName) ? "profile.png" : fileName;
        var form = new WWWForm();
        form.AddBinaryData("profilePicture", imageBytes, safeFileName, "image/png");


        using var req = UnityWebRequest.Post($"{BaseUrl}/api/profiles/{profileId}/pfp", form);
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        req.SetRequestHeader("Accept", "application/json, text/plain, */*");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
        string message = string.IsNullOrWhiteSpace(body) ? $"HTTP {req.responseCode}" : $"HTTP {req.responseCode}: {body}";

        if (!ok)
            Debug.LogError($"UploadProfilePicture failed. endpoint=/api/profiles/{profileId}/pfp, status={req.responseCode}, result={req.result}, body={body}");

        done?.Invoke(ok, message);
    }

    private PlayerProfileDto TryParseCreatedProfile(string createMsg)
    {
        try
        {
            var response = JsonUtility.FromJson<PlayerProfileCreateResponseDto>(createMsg);
            if (response != null)
            {
                return new PlayerProfileDto
                {
                    id = response.ID,
                    display_name = response.DisplayName,
                    coins = response.Coins
                };
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private bool HasValidProfileId(PlayerProfileDto profile)
    {
        return profile != null && profile.id > 0;
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

        using var req = new UnityWebRequest($"{BaseUrl}/api/game-refresh", "POST");
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

        using var req = UnityWebRequest.Get($"{BaseUrl}/api/users/{userId}/profiles");
        req.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        if (!ok)
        {
            string errorBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            Debug.LogError($"GetProfiles failed: HTTP {req.responseCode}, result={req.result}, body={errorBody}");
            done(false, null);
            yield break;
        }

        var rawJson = req.downloadHandler.text?.Trim();
        if (!TryParseProfilesJson(rawJson, out var profiles))
        {
            done(false, null);
            yield break;
        }

        Debug.Log("Response JSON: " + rawJson);
        Debug.Log("Profiles: " + string.Join(", ", Array.ConvertAll(profiles, p => p.display_name)));
        done(true, profiles);
    }

    private bool TryParseProfilesJson(string rawJson, out PlayerProfileDto[] profiles)
    {
        profiles = Array.Empty<PlayerProfileDto>();

        if (string.IsNullOrWhiteSpace(rawJson) || string.Equals(rawJson, "null", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            if (rawJson.StartsWith("["))
            {
                profiles = JsonHelper.FromJsonArray<PlayerProfileDto>(rawJson) ?? Array.Empty<PlayerProfileDto>();
                return true;
            }

            if (rawJson.StartsWith("{"))
            {
                var response = JsonUtility.FromJson<PlayerProfileListResponse>(rawJson);
                profiles = response?.profiles ?? Array.Empty<PlayerProfileDto>();
                return true;
            }

            Debug.LogError($"GetProfiles returned unexpected JSON format: {rawJson}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"GetProfiles JSON parse failed: {ex.Message}. Body={rawJson}");
            return false;
        }
    }

    public void SelectProfile(PlayerProfileDto profile)
    {
        PlayerPrefs.SetInt(SelectedProfileKey, profile.id);
        PlayerPrefs.SetString(DisplayNameKey, profile.display_name);
        PlayerPrefs.Save();
        SceneManager.LoadScene(loadingSceneName);
    }

    public IEnumerator PostAuthorizedJson<TPayload>(string endpoint, TPayload payload, Action<bool, string> done)
    {
        string token = AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            token = PlayerPrefs.GetString(AccessKey, "");

        if (string.IsNullOrWhiteSpace(token))
        {
            done?.Invoke(false, "Missing access token");
            yield break;
        }

        string normalizedEndpoint = endpoint.StartsWith("/") ? endpoint : "/" + endpoint;
        string json = JsonUtility.ToJson(payload);

        using var req = new UnityWebRequest($"{BaseUrl}{normalizedEndpoint}", UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
        done?.Invoke(ok, string.IsNullOrWhiteSpace(body) ? $"HTTP {req.responseCode}" : body);
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
        ClearTokens();
        ClearProfileSelection();

        if (NetworkHandler.Instance != null)
        {
            NetworkHandler.Instance.DisposeForLogout();
        }

        if (_instance == this)
        {
            _instance = null;
        }

        SceneManager.LoadScene(loginScene);
        Destroy(gameObject);
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