using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Player : MonoBehaviour
{
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    public Character_Database characterDatabase;
    public SpriteRenderer artworkSprite;
    private int selectedOption = 0;

    public GameObject[] playerPrefabs;

    void Start()
    {
        if (!PlayerPrefs.HasKey("selectedOption"))
        {
            selectedOption = 0;
        }
        else
        {
            Load();
        }
        UpdateCharacter(selectedOption);
    }

    private void UpdateCharacter(int selectedOption)
    {
        Character character = characterDatabase.GetCharacter(selectedOption);
        artworkSprite.sprite = character.characterSprite;
    }

    private void Load()
    {
        selectedOption = PlayerPrefs.GetInt("selectedOption");

        // Find a running NetworkRunner
        NetworkRunner runner = NetworkRunner.Instances.Count > 0 ? NetworkRunner.Instances[0] : null;
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogWarning("Fusion: No running NetworkRunner found. Cannot spawn networked player.");
            return;
        }

        GameObject prefab = playerPrefabs != null && selectedOption >= 0 && selectedOption < playerPrefabs.Length
            ? playerPrefabs[selectedOption]
            : null;

        if (prefab == null)
        {
            Debug.LogError("Fusion: Selected player prefab is null or index out of range.");
            return;
        }

        if (!prefab.TryGetComponent<NetworkObject>(out _))
        {
            Debug.LogError("Fusion: Player prefab must have a NetworkObject component.");
            return;
        }

        // Original code spawned at (0, 1). Keep same behavior.
        Vector3 spawnPos = new Vector3(0f, 1f, 0f);
        runner.Spawn(prefab, spawnPos, Quaternion.identity, runner.LocalPlayer);
    }
}
