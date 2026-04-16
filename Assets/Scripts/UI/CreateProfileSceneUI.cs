using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreateProfileSceneUI : MonoBehaviour
{
    [SerializeField] private GameApiClent gameApiClient;
    [SerializeField] private TMP_InputField profileNameInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image avatarPreview;
    [SerializeField] private Sprite fallbackAvatar;
    [SerializeField] private string returnSceneName = "sc_profile_select";

    private Texture2D selectedAvatarTexture;

    private void Start()
    {
        if (gameApiClient == null)
            gameApiClient = FindObjectOfType<GameApiClent>();

        if (gameApiClient == null)
        {
            SetStatus("GameApiClent not found.");
            Debug.LogError("CreateProfileSceneUI: GameApiClent missing.");
            return;
        }

        if (avatarPreview != null && fallbackAvatar != null)
            avatarPreview.sprite = fallbackAvatar;
    }

    public void OnCreateProfileClicked()
    {
        if (gameApiClient == null)
        {
            SetStatus("Auth client unavailable.");
            return;
        }

        string displayName = profileNameInput != null ? profileNameInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            SetStatus("Please enter a display name.");
            return;
        }

        StartCoroutine(CreateAndMaybeUpload(displayName.Trim()));
    }

    public void OnCancelClicked()
    {
        if (string.IsNullOrWhiteSpace(returnSceneName))
        {
            SetStatus("Return scene is not configured.");
            return;
        }

        SceneManager.LoadScene(returnSceneName);
    }

    public void SetSelectedAvatarTexture(Texture2D texture)
    {
        selectedAvatarTexture = texture;

        if (avatarPreview == null)
            return;

        if (selectedAvatarTexture == null)
        {
            avatarPreview.sprite = fallbackAvatar;
            return;
        }

        var previewSprite = Sprite.Create(
            selectedAvatarTexture,
            new Rect(0f, 0f, selectedAvatarTexture.width, selectedAvatarTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        avatarPreview.sprite = previewSprite;
        avatarPreview.preserveAspect = true;
    }

    public void SetSelectedAvatarTexture(UnityEngine.Object textureObject)
    {
        if (textureObject == null)
        {
            SetSelectedAvatarTexture((Texture2D)null);
            return;
        }

        if (textureObject is Texture2D texture)
        {
            SetSelectedAvatarTexture(texture);
            return;
        }

        SetStatus("Selected object is not a Texture2D.");
    }

    public void ClearSelectedAvatarPreview()
    {
        selectedAvatarTexture = null;
        if (avatarPreview != null)
            avatarPreview.sprite = fallbackAvatar;
    }

    private IEnumerator CreateAndMaybeUpload(string displayName)
    {
        SetStatus("Creating profile...");

        bool createDone = false;
        bool createSuccess = false;
        string createMsg = string.Empty;
        PlayerProfileDto createdProfile = null;

        yield return gameApiClient.CreateProfile(displayName, (ok, msg, profile) =>
        {
            createDone = true;
            createSuccess = ok;
            createMsg = msg;
            createdProfile = profile;
        });

        if (!createDone || !createSuccess)
        {
            SetStatus($"Failed to create profile: {createMsg}");
            yield break;
        }

        if (selectedAvatarTexture == null)
        {
            SetStatus("Profile created.");
            SceneManager.LoadScene(returnSceneName);
            yield break;
        }

        if (createdProfile == null || createdProfile.id <= 0)
        {
            SetStatus("Profile created, but failed to resolve profile id for avatar upload.");
            yield break;
        }

        SetStatus("Uploading avatar...");
        byte[] pngData = selectedAvatarTexture.EncodeToPNG();

        bool uploadDone = false;
        bool uploadSuccess = false;
        string uploadMsg = string.Empty;

        yield return gameApiClient.UploadProfilePicture(createdProfile.id, pngData, $"profile_{createdProfile.id}.png", (ok, msg) =>
        {
            uploadDone = true;
            uploadSuccess = ok;
            uploadMsg = msg;
        });

        if (!uploadDone || !uploadSuccess)
        {
            SetStatus($"Profile created, avatar upload failed: {uploadMsg}");
            yield break;
        }

        SetStatus("Profile and avatar created.");
        SceneManager.LoadScene(returnSceneName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log(message);
    }
}
