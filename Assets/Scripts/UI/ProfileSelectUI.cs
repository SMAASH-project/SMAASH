using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProfileSelectUI : MonoBehaviour
{
    [SerializeField] private AuthClient authClient;
    [SerializeField] private Transform listRoot;
    [SerializeField] private Button profileButtonPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField, Range(1, 5)] private int maxProfiles = 5;
    [SerializeField] private Sprite fallbackAvatar;

    [SerializeField] private Button addProfileButton;
    [SerializeField] private string createProfileSceneName = "sc_create_profile";

    private void Start()
    {
        if (authClient == null)
            authClient = FindObjectOfType<AuthClient>();

        if (statusText == null)
            Debug.LogWarning("ProfileSelectUI: statusText is not assigned.");

        if (authClient == null)
        {
            SetStatus("AuthClient not found.");
            Debug.LogError("ProfileSelectUI: AuthClient missing.");
            return;
        }

        if (listRoot == null || profileButtonPrefab == null)
        {
            SetStatus("Profile UI setup is incomplete.");
            Debug.LogError("ProfileSelectUI: listRoot or profileButtonPrefab is missing.");
            return;
        }

        FindLogOutButton();

        if (addProfileButton != null)
        {
            addProfileButton.onClick.RemoveAllListeners();
            addProfileButton.onClick.AddListener(OnOpenCreateProfileFlow);
        }

        LoadProfiles();
    }

    public void LoadProfiles()
    {
        ClearList();
        SetStatus("Checking profiles...");
        SetAddProfileButtonVisible(true);

        StartCoroutine(authClient.GetMyProfiles((success, profiles) =>
        {
            if (!success)
            {
                SetStatus("Failed to load profiles.");
                SetAddProfileButtonVisible(true);
                return;
            }

            int maxCount = Mathf.Clamp(maxProfiles, 1, 5);
            int totalCount = profiles != null ? profiles.Length : 0;
            SetAddProfileButtonVisible(totalCount < maxCount);

            if (profiles == null || profiles.Length == 0)
            {
                SetStatus("No profiles found. Add a new profile.");
                return;
            }

            int createdCount = 0;

            foreach (var p in profiles)
            {
                if (p == null) continue;
                if (createdCount >= maxCount) break;

                var btn = Instantiate(profileButtonPrefab, listRoot);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"{p.display_name}  •  Coins: {p.coins}";

                var pfpUri = $"{authClient.BaseUrl}/api/profiles/{p.id}/pfp"; // /api/profiles/{id}/pfp
                TrySetProfileAvatar(btn, pfpUri);

                btn.onClick.AddListener(() => authClient.SelectProfile(p));
                createdCount++;
            }

            if (createdCount == 0)
            {
                SetStatus("No valid profiles found.");
                return;
            }

            if (profiles.Length > createdCount)
                SetStatus($"Select a profile ({createdCount}/{profiles.Length}, max {maxCount})");
            else
                SetStatus($"Select a profile ({createdCount})");
        }));
    }

    private void TrySetProfileAvatar(Button button, string pfpUri)
    {
        var avatarImage = FindAvatarImage(button);
        if (avatarImage == null) return;

        if (fallbackAvatar != null)
            avatarImage.sprite = fallbackAvatar;

        if (!string.IsNullOrWhiteSpace(pfpUri))
            StartCoroutine(LoadAvatarFromUri(pfpUri, avatarImage));
    }

    private Image FindAvatarImage(Button button)
    {
        if (button == null) return null;

        var images = button.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image.gameObject == button.gameObject) continue;

            var lower = image.gameObject.name.ToLowerInvariant();
            if (lower.Contains("avatar") || lower.Contains("pfp") || lower.Contains("profile"))
                return image;
        }

        foreach (var image in images)
        {
            if (image.gameObject != button.gameObject)
                return image;
        }

        return null;
    }

    private IEnumerator LoadAvatarFromUri(string uri, Image targetImage)
    {
        using var request = UnityWebRequestTexture.GetTexture(uri); 

        var token = authClient != null ? authClient.AccessToken : string.Empty;
        // Fallback to PlayerPrefs if token is not available in authClient
        if (string.IsNullOrWhiteSpace(token))
            token = PlayerPrefs.GetString("access_token", "");
        
        if (!string.IsNullOrWhiteSpace(token))
            request.SetRequestHeader("Authorization", $"Bearer {token}");

        request.SetRequestHeader("Accept", "image/*");
        yield return request.SendWebRequest();

        if (targetImage == null)
            yield break;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"ProfileSelectUI: failed to load avatar from '{uri}' (HTTP {request.responseCode}): {request.error}");
            if (request.responseCode == 401)
                SetStatus("Unauthorized avatar request. Please log in again.");
            yield break;
        }

        var texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
            yield break;

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        targetImage.sprite = sprite;
        targetImage.preserveAspect = true;
    }

    private void ClearList()
    {
        if (listRoot == null) return;
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log(message);
    }

    private void SetAddProfileButtonVisible(bool isVisible)
    {
        if (addProfileButton != null)
            addProfileButton.gameObject.SetActive(isVisible);
    }

    public void OnOpenCreateProfileFlow()
    {
        if (string.IsNullOrWhiteSpace(createProfileSceneName))
        {
            SetStatus("Create profile scene name is empty.");
            return;
        }

        SceneManager.LoadScene(createProfileSceneName);
    }

    private void FindLogOutButton()
    {
        if (authClient == null)
        {
            Debug.LogError("Cannot setup logout button: authClient is null");
            return;
        }
        
        var logoutBtn = GameObject.Find("LogOut")?.GetComponent<Button>();

        if (logoutBtn != null)
        {
            logoutBtn.onClick.RemoveAllListeners();
            logoutBtn.onClick.AddListener(() => authClient.Logout());
            Debug.Log("Logout button successfully registered");
        }
        else
        {
            Debug.LogWarning("Logout button not found. Checked for: 'LogOut', 'logout', 'Logout'. Check the exact button name and that it is active in the scene.");
        }
    }
}