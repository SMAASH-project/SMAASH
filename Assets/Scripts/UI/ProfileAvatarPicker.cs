using System.IO;
using TMPro;
using UnityEngine;

public class ProfileAvatarPicker : MonoBehaviour
{
    [SerializeField] private CreateProfileSceneUI createProfileSceneUI;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        EnsureCreateProfileSceneUiRef();
    }

    public void OnPickAvatarClicked()
    {
        EnsureCreateProfileSceneUiRef();

#if UNITY_ANDROID || UNITY_IOS
        SetStatus("Opening photo library...");
    NativeGallery.GetImageFromGallery(OnAvatarPickedFromGallery, "Select Avatar", "image/*");
    #elif UNITY_EDITOR
        string selectedPath = TryOpenSystemPicker();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            SetStatus("No image selected.");
            return;
        }

        LoadAvatarFromPath(selectedPath);
#else
        SetStatus("Gallery picker is available on Android/iOS builds.");
#endif
    }

    private void OnAvatarPickedFromGallery(string selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            SetStatus("No image selected.");
            return;
        }

        Texture2D texture = NativeGallery.LoadImageAtPath(selectedPath, 1024, false);
        if (texture == null)
        {
            SetStatus("Failed to load image.");
            return;
        }

        createProfileSceneUI.SetSelectedAvatarTexture(texture);
        SetStatus("Avatar selected.");
    }

    public void OnClearAvatarClicked()
    {
        EnsureCreateProfileSceneUiRef();

        if (createProfileSceneUI != null)
            createProfileSceneUI.ClearSelectedAvatarPreview();

        SetStatus("Avatar cleared.");
    }

    public void LoadAvatarFromPath(string filePath)
    {
        EnsureCreateProfileSceneUiRef();

        if (createProfileSceneUI == null)
        {
            SetStatus("CreateProfileSceneUI is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            SetStatus("Invalid file path.");
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(bytes, false))
            {
                SetStatus("Failed to decode image.");
                return;
            }

            createProfileSceneUI.SetSelectedAvatarTexture(texture);
            SetStatus("Avatar selected.");
        }
        catch
        {
            SetStatus("Failed to load selected image.");
        }
    }

    private void EnsureCreateProfileSceneUiRef()
    {
        if (createProfileSceneUI == null)
            createProfileSceneUI = FindObjectOfType<CreateProfileSceneUI>();
    }

#if UNITY_EDITOR
    private string TryOpenSystemPicker()
    {
        string[] filters = { "Image files", "png,jpg,jpeg", "All files", "*" };
        return UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select Avatar", "", filters);
    }
#endif

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }
}
