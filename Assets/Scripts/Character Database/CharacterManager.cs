using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class CharacterManager : MonoBehaviour
{

    public Character_Database characterDatabase;

    public TMP_Text nameText;
    public SpriteRenderer artworkSprite;
    public Button startButton;
    
    private int selectedOption = 0;
    private string loadingScene = "sc_loading";
    private readonly List<int> _availableCharacterIndices = new List<int>();
    private bool _ownershipLoadedSuccessfully;

    [SerializeField] private global::GameApiClent gameApiClient;
    [SerializeField] private NetworkHandler networkHandler;
    // Start is called before the first frame update
    
    //Lementett karakter kivalasztasa
    IEnumerator Start()
    {
        if (gameApiClient == null)
            gameApiClient = FindObjectOfType<global::GameApiClent>();

        networkHandler = NetworkHandler.Instance != null
            ? NetworkHandler.Instance
            : FindObjectOfType<NetworkHandler>();

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(changeToWaitingRoom);
        }

        yield return LoadAvailableCharacters();

        if (!_ownershipLoadedSuccessfully)
        {
            ShowUnavailableState("Unable to load owned characters.");
            yield break;
        }

        if (_availableCharacterIndices.Count == 0)
        {
            ShowUnavailableState("No owned characters found.");
            yield break;
        }

        Load();
        ClampSelectionToAvailableCharacters();
        UpdateCharacter(selectedOption);
        Save();
    }

    //Kovetkezo karakter
    public void NextOption()
    {
        if (_availableCharacterIndices.Count == 0)
            return;

        selectedOption++;

        if(selectedOption >= _availableCharacterIndices.Count)
            selectedOption = 0;

        UpdateCharacter(selectedOption);
        Save();
    }

    //Elozo karakter
    public void BackOption()
    {
        if (_availableCharacterIndices.Count == 0)
            return;

        selectedOption--;

        if(selectedOption < 0)
            selectedOption = _availableCharacterIndices.Count - 1;

        UpdateCharacter(selectedOption);
        Save();
    }  

    //Megjelenik a kepernyon a kivalasztott karakter
    private void UpdateCharacter(int selectedOption)
    {
        if (_availableCharacterIndices.Count == 0 || characterDatabase == null)
            return;

        int characterIndex = _availableCharacterIndices[Mathf.Clamp(selectedOption, 0, _availableCharacterIndices.Count - 1)];
        Character character = characterDatabase.GetCharacter(characterIndex);

        if (character == null)
            return;

        artworkSprite.sprite = character.characterSprite;
        ApplyArtworkPlacement(character);
        nameText.text = character.character_name;
    }

    private void ApplyArtworkPlacement(Character character)
    {
        Transform artworkTransform = artworkSprite.transform;
        Sprite sprite = character != null ? character.characterSprite : null;
        string spriteName = sprite != null ? sprite.name : string.Empty;
        string characterName = character != null ? character.character_name : string.Empty;

        bool isKnightIcon = string.Equals(spriteName, "KnightIcon", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(characterName, "Knight", System.StringComparison.OrdinalIgnoreCase);

        bool isBandit = string.Equals(spriteName, "BanditIcon", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(characterName, "Bandit", System.StringComparison.OrdinalIgnoreCase)
            || characterName.IndexOf("Bandit", System.StringComparison.OrdinalIgnoreCase) >= 0;

        float zScale = artworkTransform.localScale.z;
        float zPos = artworkTransform.localPosition.z;

        if (isKnightIcon)
        {
            artworkTransform.localScale = new Vector3(2f, 2f, zScale);
            artworkTransform.localPosition = new Vector3(-0.5f, 0f, zPos);
            artworkSprite.flipX = false;
            return;
        }

        if (isBandit)
        {
            artworkTransform.localScale = new Vector3(2f, 2f, zScale);
            artworkTransform.localPosition = new Vector3(-0.1f, 0f, zPos);
            artworkSprite.flipX = true;
            return;
        }

        artworkTransform.localScale = new Vector3(3f, 3f, zScale);
        artworkTransform.localPosition = new Vector3(0f, 0f, zPos);
        artworkSprite.flipX = false;
    }

    //Lekerdezi a karakter sorszamat
    private void Load()
    {
        selectedOption = PlayerPrefs.GetInt("selectedOption", 0);
    }
    //Lementi a karakter sorszamat
    private void Save()
    {
        PlayerPrefs.SetInt("selectedOption", GetCharacterDatabaseIndexForAvailableIndex(selectedOption));
        PlayerPrefs.SetString("character_name", nameText.text);
        PlayerPrefs.Save();
    }

    //Atvalt a loadingre
    public void changeToWaitingRoom()
    {
        if (networkHandler == null)
        {
            networkHandler = NetworkHandler.Instance != null
                ? NetworkHandler.Instance
                : FindObjectOfType<NetworkHandler>();
        }

        if (networkHandler == null)
        {
            Debug.LogError("[CharacterManager] NetworkHandler not found when trying to start matchmaking.");
            return;
        }

        networkHandler.RoomCreateAndJoin();
    }

    private IEnumerator LoadAvailableCharacters()
    {
        _availableCharacterIndices.Clear();
        _ownershipLoadedSuccessfully = false;

        if (gameApiClient == null)
            yield break;

        int profileId = PlayerPrefs.GetInt("selected_profile_id", -1);
        if (profileId <= 0)
            yield break;

        bool done = false;
        bool success = false;
        BackendCharacterDto[] ownedCharacters = null;

        yield return StartCoroutine(gameApiClient.GetProfileCharacters(profileId, (ok, result, error) =>
        {
            success = ok;
            ownedCharacters = result;
            done = true;
        }));

        if (!done || !success || ownedCharacters == null)
            yield break;

        _ownershipLoadedSuccessfully = true;

        foreach (var ownedCharacter in ownedCharacters)
        {
            if (ownedCharacter == null)
                continue;

            int index = characterDatabase.GetCharacterIndexById(ownedCharacter.id);
            if (index >= 0 && !_availableCharacterIndices.Contains(index))
                _availableCharacterIndices.Add(index);
        }

        _availableCharacterIndices.Sort();
    }

    private void ShowUnavailableState(string message)
    {
        if (nameText != null)
            nameText.text = message;

        if (artworkSprite != null)
            artworkSprite.sprite = null;

        if (startButton != null)
            startButton.interactable = false;

        Debug.LogWarning($"[CharacterManager] {message}");
    }

    private void ClampSelectionToAvailableCharacters()
    {
        if (_availableCharacterIndices.Count == 0)
        {
            selectedOption = 0;
            return;
        }

        int savedCharacterIndex = PlayerPrefs.GetInt("selectedOption", 0);
        int mappedIndex = _availableCharacterIndices.IndexOf(savedCharacterIndex);
        if (mappedIndex >= 0)
        {
            selectedOption = mappedIndex;
            return;
        }

        if (selectedOption < 0 || selectedOption >= _availableCharacterIndices.Count)
            selectedOption = 0;
    }

    private int GetCharacterDatabaseIndexForAvailableIndex(int availableIndex)
    {
        if (characterDatabase == null || characterDatabase.character == null || _availableCharacterIndices.Count == 0)
            return 0;

        int clampedIndex = Mathf.Clamp(availableIndex, 0, _availableCharacterIndices.Count - 1);
        return _availableCharacterIndices[clampedIndex];
    }

    private int GetCharacterIdForAvailableIndex(int availableIndex)
    {
        if (characterDatabase == null || characterDatabase.character == null || _availableCharacterIndices.Count == 0)
            return -1;

        int clampedIndex = Mathf.Clamp(availableIndex, 0, _availableCharacterIndices.Count - 1);
        int characterIndex = _availableCharacterIndices[clampedIndex];
        Character character = characterDatabase.GetCharacter(characterIndex);
        return character != null ? character.character_id : -1;
    }

}
