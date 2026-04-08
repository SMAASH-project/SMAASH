using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentMenuBackground : MonoBehaviour
{
    public static PersistentMenuBackground Instance;

    [Header("Movement Settings")]
    public float scrollSpeed = 2f;
    public float spriteWidth; // The horizontal width of one sprite
    public Transform[] sprites; // Drag your 3 sprites here in order

    [Header("Blur Settings")]
    [Range(0, 1)] public float blurAmount = 0.5f;
    private Material[] spriteMaterials;

    void Awake()
    {
        // Singleton pattern: Keep this alive across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupMaterials();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupMaterials()
    {
        spriteMaterials = new Material[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            // Get the material from the SpriteRenderer
            spriteMaterials[i] = sprites[i].GetComponent<SpriteRenderer>().material;
        }
    }

    void Update()
    {
        // 1. Move the parent container to the left
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // 2. Check each sprite to see if it needs to loop
        foreach (Transform s in sprites)
        {
            // If sprite is too far left (Camera at 0,0), move it to the right of the line
            // Adjust '-spriteWidth' based on your camera view
            if (s.position.x + transform.position.x < -spriteWidth)
            {
                Vector3 newPos = s.localPosition;
                newPos.x += spriteWidth * sprites.Length;
                s.localPosition = newPos;
            }
        }
        
        // 3. Update Blur (Optional: only if your shader has a _BlurAmount property)
        UpdateBlur();
    }

    void UpdateBlur()
    {
        foreach (Material mat in spriteMaterials)
        {
            mat.SetFloat("_BlurAmount", blurAmount);
        }
    }

    // Logic to hide the background in the actual "Game" scene
    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Hide when in the gameplay scene, show in menus
        gameObject.SetActive(scene.name != "GameSceneName");
    }
}