using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    [SerializeField] private NetworkHandler networkHandler;
    // Start is called before the first frame update
    
    //Lementett karakter kivalasztasa
    void Start()
    {
        networkHandler = NetworkHandler.Instance != null
            ? NetworkHandler.Instance
            : FindObjectOfType<NetworkHandler>();

        startButton.onClick.AddListener(changeToWaitingRoom);
        if(!PlayerPrefs.HasKey("selectedOption"))
        {
            selectedOption = 0;
        }else
        {
            Load();
        }
        UpdateCharacter(selectedOption);
    }

    //Kovetkezo karakter
    public void NextOption()
    {
        selectedOption++;

        if(selectedOption >= characterDatabase.CharacterCount)
        {
            selectedOption = 0;
        }

        UpdateCharacter(selectedOption);
        Save();
    }

    //Elozo karakter
    public void BackOption()
    {
        selectedOption--;

        if(selectedOption < 0)
        {
            selectedOption = characterDatabase.CharacterCount - 1;
        }

        UpdateCharacter(selectedOption);
        Save();
    }  

    //Megjelenik a kepernyon a kivalasztott karakter
    private void UpdateCharacter(int selectedOption)
    {
        Character character = characterDatabase.GetCharacter(selectedOption);
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
        selectedOption = PlayerPrefs.GetInt("selectedOption");
    }
    //Lementi a karakter sorszamat
    private void Save()
    {
        PlayerPrefs.SetInt("selectedOption", selectedOption);
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

}
