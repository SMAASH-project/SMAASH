using System.IO;
using TMPro;
using UnityEngine;

public class ProfileAvatarPicker : MonoBehaviour
{
    [SerializeField] private CreateProfileSceneUI createProfileSceneUI;
    [SerializeField] private TMP_InputField imagePathInput;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        EnsureCreateProfileSceneUiRef();
    }

    public void OnPickAvatarClicked()
    {
        EnsureCreateProfileSceneUiRef();

        string selectedPath = TryOpenSystemPicker();

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            selectedPath = imagePathInput != null ? imagePathInput.text?.Trim() : string.Empty;
        }

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            SetStatus("No image selected.");
            return;
        }

        LoadAvatarFromPath(selectedPath);
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

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
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

    private string TryOpenSystemPicker()
    {
#if UNITY_EDITOR
        string[] filters = { "Image files", "png,jpg,jpeg", "All files", "*" };
        return UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select Avatar", "", filters);
#else
        SetStatus("System picker is available in Unity Editor only. Paste a path in imagePathInput on this platform.");
        return string.Empty;
#endif
    }

        private void EnsureCreateProfileSceneUiRef()
        {
        if (createProfileSceneUI == null)
            createProfileSceneUI = FindObjectOfType<CreateProfileSceneUI>();
        }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }
}
