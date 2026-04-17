using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopSceneController : MonoBehaviour
{
    [SerializeField] private GameApiClent gameApiClient;
    [SerializeField] private Character_Database characterDatabase;
    [SerializeField] private string lobbySceneName = "sc_lobby";
    [SerializeField] private TMP_Text coinsText;

    private readonly Dictionary<int, ShopCardView> _cardsByCharacterId = new Dictionary<int, ShopCardView>();
    private readonly Dictionary<int, int> _priceByCharacterId = new Dictionary<int, int>();
    private const string SelectedProfileCoinsKey = "selected_profile_coins";
    private long _coins;

    private void Start()
    {
        if (gameApiClient == null)
            gameApiClient = FindObjectOfType<GameApiClent>();

        if (characterDatabase == null && NetworkHandler.Instance != null)
            characterDatabase = NetworkHandler.Instance.characterDatabase;

        if (gameApiClient == null)
        {
            Debug.LogError("[ShopSceneController] GameApiClent not found.");
            return;
        }

        if (characterDatabase == null || characterDatabase.character == null || characterDatabase.character.Length == 0)
        {
            Debug.LogError("[ShopSceneController] Character database not assigned or empty.");
            return;
        }

        CacheSceneReferences();
        StartCoroutine(LoadShopData());
    }

    private void CacheSceneReferences()
    {
        _cardsByCharacterId.Clear();

        RegisterCard("BanditContainer");
        RegisterCard("KnightContainer");

        // Disable all buy buttons during initial load
        DisableAllBuyButtons();

        var backButton = GameObject.Find("BackButton")?.GetComponent<Button>();
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => SceneManager.LoadScene(lobbySceneName));
        }
    }

    private void DisableAllBuyButtons()
    {
        foreach (var card in _cardsByCharacterId.Values)
        {
            if (card.BuyButton != null)
                card.BuyButton.interactable = false;
        }
    }

    private void RegisterCard(string containerName)
    {
        var container = GameObject.Find(containerName);
        if (container == null)
        {
            Debug.LogWarning($"[ShopSceneController] Missing shop card container: {containerName}");
            return;
        }

        var card = new ShopCardView
        {
            Container = container,
            Icon = container.transform.Find("CharacterIcon")?.GetComponent<Image>(),
            NameText = container.transform.Find("CharacterName")?.GetComponent<TMP_Text>(),
            PriceText = container.transform.Find("PriceText")?.GetComponent<TMP_Text>(),
            BuyButton = container.transform.Find("BuyButton")?.GetComponent<Button>(),
            BuyButtonImage = container.transform.Find("BuyButton")?.GetComponent<Image>(),
            BuyButtonLabel = container.transform.Find("BuyButton/Text (TMP)")?.GetComponent<TMP_Text>()
        };

        if (card.BuyButton != null)
            card.BuyButton.onClick.RemoveAllListeners();

        int characterId = ResolveCharacterIdForContainer(containerName);
        if (characterId > 0)
            _cardsByCharacterId[characterId] = card;
    }

    private int ResolveCharacterIdForContainer(string containerName)
    {
        if (characterDatabase == null || characterDatabase.character == null)
            return -1;

        Character[] characters = characterDatabase.character;
        string lowered = containerName.ToLowerInvariant();

        for (int i = 0; i < characters.Length; i++)
        {
            var character = characters[i];
            if (character == null)
                continue;

            string characterName = (character.character_name ?? string.Empty).ToLowerInvariant();
            if (lowered.Contains(characterName) || characterName.Contains(lowered.Replace("container", string.Empty)))
                return character.character_id > 0 ? character.character_id : i + 1;
        }

        int fallbackIndex = lowered.Contains("bandit") ? 0 : 1;
        fallbackIndex = Mathf.Clamp(fallbackIndex, 0, characters.Length - 1);
        var fallbackCharacter = characterDatabase.GetCharacter(fallbackIndex);
        return fallbackCharacter != null && fallbackCharacter.character_id > 0 ? fallbackCharacter.character_id : fallbackIndex + 1;
    }

    private IEnumerator LoadShopData()
    {
        Debug.Log("[ShopSceneController] Loading shop...");

        int profileId = PlayerPrefs.GetInt("selected_profile_id", -1);
        if (profileId <= 0)
        {
            Debug.LogWarning("[ShopSceneController] Select a profile before visiting the shop.");
            yield break;
        }

        bool ownershipDone = false;
        bool catalogDone = false;
        bool profilesDone = false;
        BackendCharacterDto[] ownedCharacters = Array.Empty<BackendCharacterDto>();
        BackendCharacterDto[] catalogCharacters = Array.Empty<BackendCharacterDto>();
        PlayerProfileDto[] profiles = Array.Empty<PlayerProfileDto>();
        string ownershipError = string.Empty;
        string catalogError = string.Empty;
        string profilesError = string.Empty;

        yield return StartCoroutine(gameApiClient.GetAvailableCharacters((success, result, error) =>
        {
            catalogDone = true;
            if (success && result != null)
                catalogCharacters = result;
            else
                catalogError = error;
        }));

        yield return StartCoroutine(gameApiClient.GetProfileCharacters(profileId, (success, result, error) =>
        {
            ownershipDone = true;
            if (success && result != null)
                ownedCharacters = result;
            else
                ownershipError = error;
        }));

        yield return StartCoroutine(gameApiClient.GetMyProfiles((success, result) =>
        {
            profilesDone = true;
            if (success && result != null)
                profiles = result;
            else
                profilesError = "Failed to refresh profile coins.";
        }));

        if (!catalogDone || !ownershipDone || !profilesDone)
        {
            Debug.LogWarning("[ShopSceneController] Shop data could not be loaded.");
            yield break;
        }

        _priceByCharacterId.Clear();
        foreach (var character in catalogCharacters)
        {
            if (character == null || character.id <= 0)
                continue;

            _priceByCharacterId[character.id] = character.price;
        }

        PlayerProfileDto selectedProfile = profiles.FirstOrDefault(profile => profile != null && profile.id == profileId);
        if (selectedProfile != null)
        {
            _coins = Math.Max(0L, selectedProfile.coins);
            PlayerPrefs.SetString(SelectedProfileCoinsKey, _coins.ToString());
            PlayerPrefs.Save();
        }
        else
        {
            string cachedCoins = PlayerPrefs.GetString(SelectedProfileCoinsKey, "0");
            if (!long.TryParse(cachedCoins, out _coins))
                _coins = 0;
        }

        UpdateCoinText();

        HashSet<int> ownedIds = new HashSet<int>(ownedCharacters.Where(character => character != null).Select(character => character.id));
        bool ownsAnyCharacter = ownedIds.Count > 0;

        foreach (var entry in _cardsByCharacterId)
        {
            int characterId = entry.Key;
            ShopCardView card = entry.Value;

            if (!TryGetLocalCharacter(characterId, out Character localCharacter))
                continue;

            bool hasPrice = TryGetCharacterPrice(characterId, out int price);
            bool isOwned = ownedIds.Contains(characterId);
            bool canBuy = !isOwned && hasPrice && _coins >= price;

            ApplyCard(card, localCharacter, price, isOwned, canBuy, profileId);
        }

        if (!string.IsNullOrWhiteSpace(catalogError))
            Debug.LogWarning($"[ShopSceneController] Shop catalog warning: '{catalogError}'");

        if (!string.IsNullOrWhiteSpace(ownershipError))
            Debug.LogWarning($"[ShopSceneController] Shop ownership warning: '{ownershipError}'");

        if (!string.IsNullOrWhiteSpace(profilesError))
            Debug.LogWarning($"[ShopSceneController] Shop profiles warning: '{profilesError}'");

        Debug.Log($"[ShopSceneController] {(ownsAnyCharacter ? "Owned characters loaded." : "Choose a character to buy.")}");
    }

    private void ApplyCard(ShopCardView card, Character character, int price, bool isOwned, bool canBuy, int profileId)
    {
        if (card.Container == null || character == null)
            return;

        if (card.Icon != null)
        {
            card.Icon.sprite = character.characterSprite;
            card.Icon.preserveAspect = true;
        }

        if (card.NameText != null)
            card.NameText.text = character.character_name;

        if (card.PriceText != null)
        {
            card.PriceText.text = price > 0 ? price.ToString() : string.Empty;
            card.PriceText.gameObject.SetActive(!isOwned);
        }

        if (card.BuyButton != null)
        {
            card.BuyButton.onClick.RemoveAllListeners();
            card.BuyButton.onClick.AddListener(() => OnBuyClicked(profileId, character.character_id));
        }

        if (card.BuyButtonImage != null)
            card.BuyButtonImage.color = isOwned || !canBuy
                ? new Color(0.55f, 0.55f, 0.55f, 1f)
                : new Color(0.25f, 0.77f, 0.35f, 1f);

        if (card.BuyButtonLabel != null)
        {
            if (isOwned)
                card.BuyButtonLabel.text = "OWNED";
            else
                card.BuyButtonLabel.text = "BUY";

            card.BuyButtonLabel.alignment = TextAlignmentOptions.Center;
        }

        if (card.BuyButton != null)
            card.BuyButton.interactable = canBuy;
    }

    private void OnBuyClicked(int profileId, int characterId)
    {
        if (profileId <= 0 || characterId <= 0)
            return;

        if (!TryGetCharacterPrice(characterId, out int price))
        {
            Debug.LogWarning($"[ShopSceneController] Missing price for character {characterId}.");
            return;
        }

        if (_coins < price)
        {
            Debug.LogWarning($"[ShopSceneController] Not enough coins for character {characterId}. Need {price}, have {_coins}.");
            return;
        }

        // Disable all buttons during purchase to prevent double-clicks
        DisableAllBuyButtons();

        StartCoroutine(gameApiClient.PurchaseCharacter(profileId, characterId, (success, response) =>
        {
            if (!success)
            {
                Debug.LogWarning($"[ShopSceneController] {(string.IsNullOrWhiteSpace(response) ? "Purchase failed." : response)}");
                // Re-enable buttons on failure
                StartCoroutine(LoadShopData());
                return;
            }

            PlayerPrefs.SetInt("selectedOption", ResolveCharacterIndex(characterId));
            PlayerPrefs.Save();

            Debug.Log("[ShopSceneController] Purchase successful.");
            // Reload shop data to refresh coins and re-enable buttons
            StartCoroutine(LoadShopData());
        }));
    }

    private int ResolveCharacterIndex(int characterId)
    {
        if (characterDatabase == null || characterDatabase.character == null)
            return 0;

        for (int i = 0; i < characterDatabase.character.Length; i++)
        {
            var localCharacter = characterDatabase.character[i];
            if (localCharacter != null && localCharacter.character_id == characterId)
                return i;
        }

        return 0;
    }

    private bool TryGetCharacterPrice(int characterId, out int price)
    {
        if (_priceByCharacterId.TryGetValue(characterId, out price))
        {
            price = Mathf.Max(price, 0);
            return true;
        }

        price = 0;
        return false;
    }

    private bool TryGetLocalCharacter(int characterId, out Character character)
    {
        character = null;

        if (characterDatabase == null || characterDatabase.character == null)
            return false;

        for (int i = 0; i < characterDatabase.character.Length; i++)
        {
            var candidate = characterDatabase.character[i];
            if (candidate != null && candidate.character_id == characterId)
            {
                character = candidate;
                return true;
            }
        }

        return false;
    }

    private void UpdateCoinText()
    {
        if (coinsText != null)
            coinsText.text = $"Coins: {_coins}";
    }

    private struct ShopCardView
    {
        public GameObject Container;
        public Image Icon;
        public TMP_Text NameText;
        public TMP_Text PriceText;
        public Button BuyButton;
        public Image BuyButtonImage;
        public TMP_Text BuyButtonLabel;
    }
}