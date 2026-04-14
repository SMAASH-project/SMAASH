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
        ApplyArtworkPlacement(character.characterSprite);
        nameText.text = character.character_name;
    }

    private void ApplyArtworkPlacement(Sprite sprite)
    {
        Transform artworkTransform = artworkSprite.transform;
        bool isKnightIcon = sprite != null && string.Equals(sprite.name, "KnightIcon", System.StringComparison.OrdinalIgnoreCase);

        float zScale = artworkTransform.localScale.z;
        float zPos = artworkTransform.localPosition.z;

        if (isKnightIcon)
        {
            artworkTransform.localScale = new Vector3(2f, 2f, zScale);
            artworkTransform.localPosition = new Vector3(-0.5f, 0f, zPos);
            return;
        }

        artworkTransform.localScale = new Vector3(3f, 3f, zScale);
        artworkTransform.localPosition = new Vector3(0f, 0f, zPos);
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
