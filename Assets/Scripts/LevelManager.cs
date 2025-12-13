using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Whalepass;
using UnityEngine.Networking;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
  
    public int _playerLevel = 0; // Start level
    public int totalXP = 0; // Total experience
    public int xpToNextLevel = 100; // XP required for the next level
    public int remainingXP = 0; // The leftover XP after leveling up

    public TextMeshProUGUI levelText; // Text to display current level
    public TextMeshProUGUI xpText; // Text to display current XP
    public Slider xpSlider; // Slider to visually represent XP progress
    public Slider GameXpSlider;
   public WeaponSelectionManager weaponSelectionManager; // Reference to the weapon manager
    private void Awake()
    {
        if(Instance == null)
            Instance = this;

    }
    
    void Start()
    {
        LeanTween.delayedCall(0.4f, () => { GetPlayerDetails(); WeaponCheck(); weaponSelectionManager.UpdateUI(); }); 
        if (xpSlider != null)
        {
            xpSlider.maxValue = xpToNextLevel;
            xpSlider.value = totalXP;
        }
        // Get a reference to the WeaponSelectionManager

        //WhalePassAPI.instance.OnLevelChanged += HandleLevelChange;
        weaponSelectionManager.CheckWeaponUnlocks();
        UpdateUI();

        // Check for weapon unlocks at the start
        //weaponSelectionManager.CheckWeaponUnlocks(playerLevel);
    }
    public WhalePassAPI WhalePassAPI;
    public void UpdateUI()
    {
        if (xpSlider != null)
        {
            if(WhalePassAPI.CurrentLevel < 10)
            {
                levelText.text = $"Level: {WhalePassAPI.CurrentLevel}";
                xpText.text = $"Next Level: {WhalePassAPI.CurrentExp} / {WhalePassAPI.NextLevelExp - WhalePassAPI.ExpRequiredLastlevel}";
                if (GameXpSlider != null)
                {
                    GameXpSlider.maxValue = WhalePassAPI.NextLevelExp - WhalePassAPI.ExpRequiredLastlevel;
                    GameXpSlider.value = WhalePassAPI.CurrentExp;
                }

                weaponSelectionManager.UpdateUI();
               
            }
            else
            {
                levelText.text = $"Level: {WhalePassAPI.CurrentLevel}";
                xpText.text = $"Levels Completed";
                GameXpSlider.gameObject.SetActive(false);
            }
            
        }
            
    }

    public void GetPlayerDetails()
    {
        _playerLevel = WhalePassAPI.CurrentLevel;
        xpToNextLevel = WhalePassAPI.NextLevelExp;
        totalXP = WhalePassAPI.CurrentExp;
        
    }
    void WeaponCheck()
    {
        // Check for weapon unlocks based on the new level
        weaponSelectionManager.CheckWeaponUnlocks();
    }

  


    public TextMeshProUGUI NewlevelReached;
    public Animator Textanim;
   
    
    private void HandleLevelChange(int newLevel)
    {
        if(NewlevelReached != null)
        {
            Debug.Log($"Player level changed to: {newLevel}");
            NewlevelReached.enabled = true;
            NewlevelReached.text = "Leveled Up: " + newLevel;
            Textanim.SetBool("Play", true);

            LeanTween.delayedCall(5f, () => { NewlevelReached.enabled = false; Textanim.SetBool("Play", false); });
            GetPlayerDetails();
            WeaponCheck();
            weaponSelectionManager.CheckWeaponUnlocksAndSelectsNew();


            UpdateUI();
        }
        
    }

    
}
