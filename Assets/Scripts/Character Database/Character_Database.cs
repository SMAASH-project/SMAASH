using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

[CreateAssetMenu]
public class Character_Database : ScriptableObject
{
    public Character[] character;

    public int CharacterCount
    {
        get
        {
            return character.Length;
        }
    }

    public Character GetCharacter(int index)
    {
        return character[index];
    }

    public Character GetCharacterById(int characterId)
    {
        if (character == null)
            return null;

        for (int i = 0; i < character.Length; i++)
        {
            var candidate = character[i];
            if (candidate != null && candidate.character_id == characterId)
                return candidate;
        }

        return null;
    }

    public int GetCharacterIndexById(int characterId)
    {
        if (character == null)
            return -1;

        for (int i = 0; i < character.Length; i++)
        {
            var candidate = character[i];
            if (candidate != null && candidate.character_id == characterId)
                return i;
        }

        return -1;
    }
}
