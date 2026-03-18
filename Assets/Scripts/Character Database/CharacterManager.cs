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
    private int selectedOption = 0;
    private string loadingScene = "sc_loading";

    // Start is called before the first frame update
    
    //Lementett karakter kivalasztasa
    void Start()
    {
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
        nameText.text = character.character_name;
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
    public void changeToLoadingScene()
    {
        SceneManager.LoadScene(loadingScene);
    }

}
