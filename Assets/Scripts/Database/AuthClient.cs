using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable] public class GameLoginRequest { public string email; public string password; }
[Serializable] public class PlayerProfileDto { public int id; public string display_name; public long coins; public string last_login; }
[Serializable] public class GameLoginResponse { public string token; public PlayerProfileDto profile; }
[Serializable] public class JwtPayload { public long exp; }

public class AuthClient : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:8080"; // your PORT env value
    [SerializeField] private string mainSceneName = "sc_main";
    public string JwtToken { get; private set; }

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    private const string JwtKey = "jwt";

    private void Start()
    {
        TryAutoLogin();
    }

    public void OnLoginButtonClicked()
    {
        StartCoroutine(Login(emailInput.text, passwordInput.text, (success, message) =>
        {
            if (success)
            {
                Debug.Log($"Login successful! Welcome {message}");
                // CHANGE SCENE TO GET PLAYER PROFILES
                SceneManager.LoadScene(mainSceneName);
            }
            else
            {
                Debug.LogError($"Login failed: {message}");
            }
        }));
    }

    public IEnumerator Login(string email, string password, Action<bool,string> done)
    {
        var reqBody = JsonUtility.ToJson(new GameLoginRequest { email = email, password = password });
        using var req = new UnityWebRequest($"{baseUrl}/api/game-login", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(reqBody));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(false, req.downloadHandler.text); // backend returns {"error": "..."}
            yield break;
        }

        var resp = JsonUtility.FromJson<GameLoginResponse>(req.downloadHandler.text);
        SaveToken(resp.token);
        Debug.Log($"Received JWT: {JwtToken}");
        Debug.Log("successfully logged in, player profile: " + resp.profile.display_name);
        done(true, resp.profile.display_name);
    }

    private void TryAutoLogin()
    {
        var savedToken = PlayerPrefs.GetString(JwtKey, string.Empty);
        if (string.IsNullOrWhiteSpace(savedToken))
            return;

        if (!IsJwtNotExpired(savedToken))
        {
            ClearSavedToken();
            return;
        }

        JwtToken = savedToken;
        Debug.Log("Valid saved JWT found. Skipping login screen.");
        SceneManager.LoadScene(mainSceneName);
    }

    private void SaveToken(string token)
    {
        JwtToken = token;
        PlayerPrefs.SetString(JwtKey, token);
        PlayerPrefs.Save();
    }

    public void Logout()
    {
        ClearSavedToken();
    }

    private void ClearSavedToken()
    {
        JwtToken = null;
        PlayerPrefs.DeleteKey(JwtKey);
        PlayerPrefs.Save();
    }

    private bool IsJwtNotExpired(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return false;

        try
        {
            var json = DecodeBase64Url(parts[1]);
            var payload = JsonUtility.FromJson<JwtPayload>(json);
            if (payload == null || payload.exp <= 0)
                return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return payload.exp > now;
        }
        catch
        {
            return false;
        }
    }

    private string DecodeBase64Url(string base64Url)
    {
        var output = base64Url.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2:
                output += "==";
                break;
            case 3:
                output += "=";
                break;
        }

        var bytes = Convert.FromBase64String(output);
        return Encoding.UTF8.GetString(bytes);
    }
}