using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkinManager : MonoBehaviour
{
    [Header("Skin Management")]
    [SerializeField] private List<SnakeSkin> availableSkins = new List<SnakeSkin>();
    [SerializeField] private SnakeSkin defaultSkin;
    
    [Header("Current Skin")]
    [SerializeField] private SnakeSkin currentSkin;
    
    private static SkinManager _instance;
    public static SkinManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SkinManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SkinManager");
                    _instance = go.AddComponent<SkinManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSkins();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Load saved skin selection and unlock states
        LoadSavedSkin();
        
        if (currentSkin == null)
        {
            currentSkin = defaultSkin;
        }
    }
    
    /// <summary>
    /// Load all skins from the Resources/Skins folder
    /// </summary>
    private void LoadSkins()
    {
        // Clear existing skins to prevent duplicates
        availableSkins.Clear();
        
        // Load all SnakeSkin ScriptableObjects from Resources
        SnakeSkin[] loadedSkins = Resources.LoadAll<SnakeSkin>("Skins");
        
        // Add skins and prevent duplicates by checking skinId
        foreach (var skin in loadedSkins)
        {
            if (!availableSkins.Any(existingSkin => existingSkin.skinId == skin.skinId))
            {
                // Ensure default skins and free skins (price 0) are always unlocked
                skin.EnsureDefaultSkinUnlocked();
                
                // Also ensure skins with price 0 are unlocked
                if (skin.unlockPrice == 0)
                {
                    skin.isUnlocked = true;
                }
                
                availableSkins.Add(skin);
            }
        }
        
        // Set default skin if not set
        if (defaultSkin == null && availableSkins.Count > 0)
        {
            defaultSkin = availableSkins.FirstOrDefault(skin => skin.isDefault) ?? availableSkins[0];
        }
        
        Debug.Log($"Loaded {availableSkins.Count} unique skins");
    }
    
    /// <summary>
    /// Get all available skins
    /// </summary>
    /// <returns>List of all available skins</returns>
    public List<SnakeSkin> GetAllSkins()
    {
        return new List<SnakeSkin>(availableSkins);
    }
    
    /// <summary>
    /// Get unlocked skins for a player
    /// </summary>
    /// <param name="playerScore">Player's current score</param>
    /// <param name="playerKills">Player's total kills</param>
    /// <param name="gamesPlayed">Player's games played</param>
    /// <returns>List of unlocked skins</returns>
    public List<SnakeSkin> GetUnlockedSkins(int playerScore, int playerKills, int gamesPlayed)
    {
        return availableSkins.Where(skin => skin.CanUnlock(playerScore, playerKills, gamesPlayed)).ToList();
    }
    
    /// <summary>
    /// Get skin by ID
    /// </summary>
    /// <param name="skinId">The skin ID to find</param>
    /// <returns>The skin with the specified ID, or null if not found</returns>
    public SnakeSkin GetSkinById(int skinId)
    {
        return availableSkins.FirstOrDefault(skin => skin.skinId == skinId);
    }
    
    /// <summary>
    /// Set the current skin
    /// </summary>
    /// <param name="skin">The skin to set as current</param>
    public void SetCurrentSkin(SnakeSkin skin)
    {
        if (skin != null && availableSkins.Contains(skin))
        {
            currentSkin = skin;
            Debug.Log($"Current skin set to: {skin.skinName}");
        }
    }
    
    /// <summary>
    /// Get the current skin
    /// </summary>
    /// <returns>The current skin</returns>
    public SnakeSkin GetCurrentSkin()
    {
        return currentSkin ?? defaultSkin;
    }
    
    /// <summary>
    /// Get the default skin
    /// </summary>
    /// <returns>The default skin</returns>
    public SnakeSkin GetDefaultSkin()
    {
        return defaultSkin;
    }
    
    /// <summary>
    /// Apply the current skin to a snake
    /// </summary>
    /// <param name="snakeController">The snake controller to apply the skin to</param>
    public void ApplyCurrentSkinToSnake(SnakeController snakeController)
    {
        SnakeSkin skin = GetCurrentSkin();
        if (skin != null)
        {
            skin.ApplyToSnake(snakeController);
        }
    }
    
    /// <summary>
    /// Apply the current skin to a new body segment
    /// </summary>
    /// <param name="segment">The body segment to apply the skin to</param>
    /// <param name="isFirstSegment">Whether this is the first body segment (should use head sprite)</param>
    public void ApplyCurrentSkinToSegment(Transform segment, bool isFirstSegment = false)
    {
        SnakeSkin skin = GetCurrentSkin();
        if (skin != null)
        {
            skin.ApplyToSegment(segment, isFirstSegment);
        }
    }
    
    /// <summary>
    /// Create a skin from the provided head and body sprites
    /// </summary>
    /// <param name="skinName">Name of the skin</param>
    /// <param name="headSprite">Head sprite</param>
    /// <param name="bodySprite">Body sprite</param>
    /// <param name="skinId">Unique ID for the skin</param>
    /// <returns>The created skin</returns>
    public SnakeSkin CreateSkin(string skinName, Sprite headSprite, Sprite bodySprite, int skinId)
    {
        SnakeSkin newSkin = ScriptableObject.CreateInstance<SnakeSkin>();
        newSkin.skinName = skinName;
        newSkin.headSprite = headSprite;
        newSkin.bodySprite = bodySprite;
        newSkin.skinId = skinId;
        newSkin.isUnlocked = true;
        newSkin.isDefault = false;
        
        availableSkins.Add(newSkin);
        return newSkin;
    }
    
    /// <summary>
    /// Save the current skin selection (you can implement PlayerPrefs or other save system)
    /// </summary>
    public void SaveCurrentSkin()
    {
        if (currentSkin != null)
        {
            PlayerPrefs.SetInt("CurrentSkinId", currentSkin.skinId);
            PlayerPrefs.Save();
        }
        
        // Save unlock states for all skins
        SaveSkinUnlockStates();
    }
    
    /// <summary>
    /// Save unlock states for all skins
    /// </summary>
    public void SaveSkinUnlockStates()
    {
        foreach (var skin in availableSkins)
        {
            PlayerPrefs.SetInt($"SkinUnlocked_{skin.skinId}", skin.isUnlocked ? 1 : 0);
        }
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Load the saved skin selection
    /// </summary>
    public void LoadSavedSkin()
    {
        // Load unlock states first
        LoadSkinUnlockStates();
        
        int savedSkinId = PlayerPrefs.GetInt("CurrentSkinId", -1);
        if (savedSkinId != -1)
        {
            SnakeSkin savedSkin = GetSkinById(savedSkinId);
            if (savedSkin != null)
            {
                SetCurrentSkin(savedSkin);
            }
        }
    }
    
    /// <summary>
    /// Load unlock states for all skins
    /// </summary>
    public void LoadSkinUnlockStates()
    {
        foreach (var skin in availableSkins)
        {
            // Ensure default skins are always unlocked
            skin.EnsureDefaultSkinUnlocked();
            
            // Ensure skins with price 0 are always unlocked
            if (skin.unlockPrice == 0)
            {
                skin.isUnlocked = true;
            }
            // For non-default skins with price > 0, load unlock state from PlayerPrefs
            else if (!skin.isDefault)
            {
                int unlocked = PlayerPrefs.GetInt($"SkinUnlocked_{skin.skinId}", 0);
                skin.isUnlocked = unlocked == 1;
            }
        }
    }
    
    /// <summary>
    /// Get a random skin from available skins
    /// </summary>
    /// <returns>A random skin, or null if no skins available</returns>
    public SnakeSkin GetRandomSkin()
    {
        if (availableSkins.Count == 0) return null;
        
        int randomIndex = Random.Range(0, availableSkins.Count);
        return availableSkins[randomIndex];
    }
} 