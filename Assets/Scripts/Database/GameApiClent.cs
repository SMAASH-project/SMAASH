using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// ── DTOs ──
[Serializable] public class GameLoginRequest { public string email; public string password; }
[Serializable] public class GameLoginResponse { public string accessToken; public string refreshToken; }
[Serializable] public class RefreshRequestDto { public string refreshToken; }
[Serializable] public class RefreshResponseDto { public string accessToken; public string refreshToken; }
[Serializable] public class JwtPayload { public long exp; }
[Serializable] public class GameJwtPayload { public long exp; public float sub; }
[Serializable] public class ApiErrorMessageDto { public string message; public string error; public string detail; }

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

[Serializable]
public class BackendCharacterDto
{
    public int id;
    public string name;
    public string description;
    public int price;
    public string rarity;
    public string[] categories;
    public string img_uri;
}

[Serializable]
public class PurchaseCreateDto
{
    public int player_profile_id;
    public int character_id;
}

public class GameApiClent : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private bool useLocalhost = false;
    [SerializeField] private string localhostUrl = "http://localhost:8080";
    [SerializeField] private string deployedUrl = "https://smaash-web.onrender.com";

    [SerializeField] private string profileSelectScene = "sc_profile_select";
    [SerializeField] private string loadingSceneName = "sc_loading";
    [SerializeField] private string loginScene = "sc_login";
    [SerializeField] private bool clearPrefsOnStartForTesting = false;

    private static GameApiClent _instance;

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
    private const string SelectedProfileCoinsKey = "selected_profile_coins";
    private const string DisplayNameKey = "display_name";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If we're in the login scene, destroy the old DontDestroyOnLoad instance
            // and keep this instance (with fresh UI refs from the scene)
            if (SceneManager.GetActiveScene().name == loginScene)
            {
                Debug.LogWarning("GameApiClent instance being re-created on login scene. Destroying old DontDestroyOnLoad instance.");
                Destroy(_instance.gameObject);
                _instance = this;
                DontDestroyOnLoad(gameObject);
                RestoreAccessTokenFromPrefs();
                StartCoroutine(TryAutoLogin());
                return;
            }

            // For other scenes, keep the existing singleton
            Debug.LogWarning("Multiple GameApiClent instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Disable native OS mobile keyboard for login inputs
        if (emailInput != null)
            emailInput.shouldHideMobileInput = true;
        if (passwordInput != null)
            passwordInput.shouldHideMobileInput = true;
        
        // Debug: Show active backend
        string activeBackend = useLocalhost ? "LOCALHOST" : "DEPLOYED";
        string activeUrl = BaseUrl;
        Debug.Log($"[GameApiClent] Backend: {activeBackend} → {activeUrl}");
        
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
        string email = emailInput != null ? emailInput.text : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (statusText != null)
            {
                statusText.text = "Email and password cannot be empty.";
                statusText.color = Color.red;
            }
            return;
        }

        if (!email.Contains("@"))
        {
            if (statusText != null)
            {
                statusText.text = "Please enter a valid email address.";
                statusText.color = Color.red;
            }
            return;
        }

        if (password.Length < 8)
        {
            if (statusText != null)
            {
                statusText.text = "Password must be at least 8 characters long.";
                statusText.color = Color.red;
            }
            return;
        }

        if (statusText != null)
        {
            statusText.text = "Logging in...";
            statusText.color = Color.white;
        }

        StartCoroutine(Login(email, password, (success, msg) =>
        {
            if (success)
            {
                if (statusText != null)
                {
                    statusText.text = "Login successful!";
                    statusText.color = Color.green;
                }
                Debug.Log("Login successful!");
                SceneManager.LoadScene(profileSelectScene);
            }
            else
            {
                string conciseError = SimplifyAuthError(msg, isLogin: true);
                if (statusText != null)
                {
                    statusText.text = conciseError;
                    statusText.color = Color.red;
                }
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
        string email = emailInput != null ? emailInput.text : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (statusText != null)
            {
                statusText.text = "Email and password cannot be empty.";
                statusText.color = Color.red;
            }
            return;
        }

        if (!email.Contains("@"))
        {
            if (statusText != null)
            {
                statusText.text = "Please enter a valid email address.";
                statusText.color = Color.red;
            }
            return;
        }

        if (password.Length < 8)
        {
            if (statusText != null)
            {
                statusText.text = "Password must be at least 8 characters long.";
                statusText.color = Color.red;
            }
            return;
        }

        if (statusText != null)
        {
            statusText.text = "Creating account...";
            statusText.color = Color.white;
        }

        StartCoroutine(SignUp(email, password, (success, msg) =>
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
                string conciseError = SimplifyAuthError(msg, isLogin: false);
                Debug.LogError($"Signup failed: {msg}");
                if (statusText != null)
                {
                    statusText.text = conciseError;
                    statusText.color = Color.red;
                }
            }
        }));
    } 

    private static string SimplifyAuthError(string rawError, bool isLogin)
    {
        string fallback = isLogin ? "Login failed. Please try again." : "Signup failed. Please try again.";
        if (string.IsNullOrWhiteSpace(rawError))
            return fallback;

        string candidate = rawError.Trim();

        if (TryExtractErrorMessage(candidate, out string extracted))
            candidate = extracted;

        string lower = candidate.ToLowerInvariant();

        if (lower.Contains("user not found") || lower.Contains("does not exist") || lower.Contains("not exist"))
            return "User not found.";

        if (lower.Contains("invalid credentials") ||
            lower.Contains("invalid email") ||
            lower.Contains("incorrect password") ||
            lower.Contains("wrong password") ||
            lower.Contains("unauthorized"))
            return "Invalid email or password.";

        if (!isLogin && (lower.Contains("already exists") || lower.Contains("already in use") || lower.Contains("duplicate")))
            return "Email is already in use.";

        return fallback;
    }

    private static bool TryExtractErrorMessage(string rawError, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(rawError))
            return false;

        string trimmed = rawError.Trim();
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
            return false;

        try
        {
            var payload = JsonUtility.FromJson<ApiErrorMessageDto>(trimmed);
            if (!string.IsNullOrWhiteSpace(payload?.message))
            {
                message = payload.message;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(payload?.error))
            {
                message = payload.error;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(payload?.detail))
            {
                message = payload.detail;
                return true;
            }
        }
        catch
        {
            // Ignore parse errors and keep fallback behavior.
        }

        return false;
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


    /// ──────────────────────────────────────────────
    /// DELETE PROFILE
    /// ──────────────────────────────────────────────

    public IEnumerator DeleteProfile(int profileId, Action<bool, string> done)
    {
        if (profileId <= 0)
        {
            done?.Invoke(false, "Invalid profile id");
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

        using var req = UnityWebRequest.Delete($"{BaseUrl}/api/profiles/{profileId}");
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode == 204;
        string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
        string message = ok ? "Profile deleted successfully" : $"Delete failed: HTTP {req.responseCode}";

        if (!ok)
            Debug.LogError($"DeleteProfile failed. profile_id={profileId}, status={req.responseCode}, result={req.result}, body={body}");

        done?.Invoke(ok, message);
    }

    private PlayerProfileDto TryParseCreatedProfile(string createMsg)
    {
        try
        {
            if (JsonHelper.TryFromJsonObject<PlayerProfileCreateResponseDto>(createMsg, out var response) && response != null)
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

        if (!JsonHelper.TryFromJsonObject<RefreshResponseDto>(req.downloadHandler.text, out var resp) || resp == null)
        {
            ClearTokens();
            done?.Invoke(false);
            yield break;
        }

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

    // ──────────────────────────────────────────────
    // CHARACTERS
    // ──────────────────────────────────────────────

    public IEnumerator GetAvailableCharacters(Action<bool, BackendCharacterDto[], string> done)
    {
        yield return GetAuthorizedArray("/api/characters", done);
    }

    public IEnumerator GetProfileCharacters(int profileId, Action<bool, BackendCharacterDto[], string> done)
    {
        if (profileId <= 0)
        {
            done?.Invoke(false, Array.Empty<BackendCharacterDto>(), "Invalid profile id.");
            yield break;
        }

        yield return GetAuthorizedArray($"/api/profiles/{profileId}/characters", done);
    }

    public IEnumerator PurchaseCharacter(int profileId, int characterId, Action<bool, string> done)
    {
        if (profileId <= 0)
        {
            done?.Invoke(false, "Invalid profile id.");
            yield break;
        }

        if (characterId <= 0)
        {
            done?.Invoke(false, "Invalid character id.");
            yield break;
        }

        var payload = new PurchaseCreateDto
        {
            player_profile_id = profileId,
            character_id = characterId
        };

        yield return PostAuthorizedJson("/api/purchases", payload, done);
    }

    private IEnumerator GetAuthorizedArray<T>(string endpoint, Action<bool, T[], string> done)
    {
        string token = AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            token = PlayerPrefs.GetString(AccessKey, "");

        if (string.IsNullOrWhiteSpace(token))
        {
            done?.Invoke(false, Array.Empty<T>(), "Missing access token.");
            yield break;
        }

        string normalizedEndpoint = endpoint.StartsWith("/") ? endpoint : "/" + endpoint;
        using var req = UnityWebRequest.Get($"{BaseUrl}{normalizedEndpoint}");
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
            done?.Invoke(false, Array.Empty<T>(), "Failed to parse response.");
            yield break;
        }

        done?.Invoke(true, parsed, string.Empty);
    }

    // ──────────────────────────────────────────────
    // UTILITIES
    // ──────────────────────────────────────────────

    private bool TryParseProfilesJson(string rawJson, out PlayerProfileDto[] profiles)
    {
        profiles = Array.Empty<PlayerProfileDto>();

        try
        {
            if (JsonHelper.TryFromJsonArray<PlayerProfileDto>(rawJson, out var arrayProfiles))
            {
                profiles = arrayProfiles;
                return true;
            }

            if (JsonHelper.TryFromJsonObject<PlayerProfileListResponse>(rawJson, out var response) && response != null)
            {
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
        PlayerPrefs.SetString(SelectedProfileCoinsKey, profile.coins.ToString());
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

        Debug.Log($"[AUTH POST] POST {BaseUrl}{normalizedEndpoint}");
        Debug.Log($"[AUTH POST] Authorization header present={(!string.IsNullOrWhiteSpace(token))}, tokenLength={(token != null ? token.Length : 0)}");
        Debug.Log($"[AUTH POST] Request JSON: {json}");

        using var req = new UnityWebRequest($"{BaseUrl}{normalizedEndpoint}", UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
        Debug.Log($"[AUTH POST] Result={req.result}, status={req.responseCode}, body={body}");
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
            if (!JsonHelper.TryFromJsonObject<GameJwtPayload>(json, out var payload) || payload == null)
            {
                Debug.LogError("GetUserIdFromToken failed: invalid token payload JSON");
                return -1;
            }

            Debug.Log(json + " => sub=" + payload.sub + ", exp=" + payload.exp);
            if (payload.sub <= 0)
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
            if (!JsonHelper.TryFromJsonObject<JwtPayload>(DecodeBase64Url(parts[1]), out var payload) || payload == null)
                return false;
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