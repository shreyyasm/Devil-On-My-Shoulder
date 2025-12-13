using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfimaGames.LowPolyShooterPack;
using static WeaponSelectionManager;
using UnityEngine.TextCore.Text;
using System.Linq;

public class WeaponSelectionManager : MonoBehaviour
{
    public static WeaponSelectionManager instance;
    [System.Serializable]
    public class Weapon
    {
        public GameObject weaponPrefab; // The weapon's 3D model prefab
        public string weaponName;
        public int requiredLevel; // The level required to unlock the weapon
        public int weaponID;
        public bool isUnlocked;

        // Weapon stats
        public int damage;
        public float fireRate;
        public int magazineSize;
    }

    public List<Weapon> weapons;
    public TextMeshProUGUI weaponNameText;
    public Button selectButton;
    public TextMeshProUGUI selectedButtontext;
    public GameObject lockedButton; // New button to display when locked
    public GameObject SelectedTick;
    public Button leftArrowButton, rightArrowButton;
    public TextMeshProUGUI levelText; // Displays the player's current level

    // Sliders for weapon stats
    public Slider damageSlider;
    public Slider fireRateSlider;
    public Slider magazineSizeSlider;

    public Transform unlockedParent; // Parent for unlocked weapons
    public Transform lockedParent;   // Parent for locked weapons
    // UI to display the unlock level of the weapon
    public TextMeshProUGUI unlockLevelText;

    public int currentIndex = 0;
    public int currentSelectedWeaponIndex = 0;

    private const string SelectedWeaponKey = "SelectedWeapon";
    private const string WeaponUnlockedKeyPrefix = "WeaponUnlocked_";

    public bool MainMenu;

    public AudioClip NextWeaponButonSFX;
    public AudioClip WeaponSelectedButonSFX;
    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        LoadWeaponData();
        UpdateUI();

        if (weaponNameText == null)
            return;
        leftArrowButton.onClick.AddListener(SelectPreviousWeapon);
        rightArrowButton.onClick.AddListener(SelectNextWeapon);
        selectButton.onClick.AddListener(SelectWeapon);

        // Ensure only the current weapon's prefab is active at the start
        UpdateWeaponDisplay();
        LeanTween.delayedCall(0.5f, () => { DisplayNextUnlockableWeapon(); });
        
    }

    void SelectPreviousWeapon()
    {
        currentIndex = (currentIndex - 1 + weapons.Count) % weapons.Count;
        AudioSource.PlayClipAtPoint(NextWeaponButonSFX, Camera.main.transform.position);
        UpdateUI();
    }

    void SelectNextWeapon()
    {
        currentIndex = (currentIndex + 1) % weapons.Count;
        AudioSource.PlayClipAtPoint(NextWeaponButonSFX, Camera.main.transform.position);
        UpdateUI();
    }

    void SelectWeapon()
    {
        Weapon currentWeapon = weapons[currentIndex];

        if (currentWeapon.isUnlocked)
        {
            SelectedTick.SetActive(true);
            selectedButtontext.text = "Selected";
            currentSelectedWeaponIndex = currentWeapon.weaponID;
            PlayerPrefs.SetInt(SelectedWeaponKey, currentWeapon.weaponID);
            Debug.Log($"{currentWeapon.weaponName} selected!");
            AudioSource.PlayClipAtPoint(WeaponSelectedButonSFX, Camera.main.transform.position);
        }
    }
    public Inventory inventory;
    public List<GameObject> weaponsPrefabs;
    public int NewUnlockedWeaponNumber;
    public bool activeGunchange;
    public void CheckWeaponUnlocks()
    {
        foreach (var weapon in weapons)
        {
            if (levelManager._playerLevel >= weapon.requiredLevel)
            {
                // Mark weapon as unlocked
                weapon.isUnlocked = true;

                // Move the weapon object to the unlocked parent
                if (MainMenu)
                    return;
                weaponsPrefabs[weapon.weaponID].transform.SetParent(unlockedParent);
                NewUnlockedWeaponNumber = weapon.weaponID;
                //weapon.weaponPrefab.transform.SetParent(unlockedParent);
               
                    inventory.Init();
                    activeGunchange = true;
                
                    //GrabNewestWeapon(weapons[NewUnlockedWeaponNumber].weaponID);
                
                
               

            }
            else if (!weapon.isUnlocked)
            {
                // Ensure locked weapons are under the locked parent
                if (MainMenu)
                    return;
                weapon.weaponPrefab.transform.SetParent(lockedParent);
            }
        }
        // Update weapon unlocks and UI
        DisplayNextUnlockableWeapon();
        // Update the UI after processing all weapons
        UpdateUI();
    }
    public TextMeshProUGUI GunUnlockedTextShowCase;
    public AudioClip GunUnlockedSFX;
    public void CheckWeaponUnlocksAndSelectsNew()
    {
        foreach (var weapon in weapons)
        {
            if (levelManager._playerLevel >= weapon.requiredLevel)
            {
              

                // Mark weapon as unlocked
                weapon.isUnlocked = true;

                // Move the weapon object to the unlocked parent
                if (MainMenu)
                    return;
                weaponsPrefabs[weapon.weaponID].transform.SetParent(unlockedParent);
                NewUnlockedWeaponNumber = weapon.weaponID;
             
               
                GrabNewestWeapon(weapons[NewUnlockedWeaponNumber].weaponID);

                if (weapon.isUnlocked && levelManager._playerLevel == weapon.requiredLevel)
                {
                    GunUnlockedTextShowCase.enabled = true;
                    GunUnlockedTextShowCase.text = "Unlocked: " + weapon.weaponName;
                    
                    
                    //AudioSource.PlayClipAtPoint(GunUnlockedSFX, Camera.main.transform.position);
                    LeanTween.delayedCall(5f, () => { GunUnlockedTextShowCase.enabled = false; });
                }

            }
            else if (!weapon.isUnlocked)
            {
                // Ensure locked weapons are under the locked parent
                if (MainMenu)
                    return;
                weapon.weaponPrefab.transform.SetParent(lockedParent);
            }
        }
        // Update weapon unlocks and UI
        DisplayNextUnlockableWeapon();
        // Update the UI after processing all weapons
        UpdateUI();
    }
    public void GrabNewestWeapon(int index)
    {
        
        inventory.ChecksForNewWeapon(index);
        UpdateWeaponDisplay();
    }
    public void UpdateUI()
    {
        if (weaponNameText != null)
        {
            Weapon currentWeapon = weapons[currentIndex];

            // Update weapon name
            weaponNameText.text = currentWeapon.weaponName;

            // Update sliders
            damageSlider.value = currentWeapon.damage;
            fireRateSlider.value = currentWeapon.fireRate;
            magazineSizeSlider.value = currentWeapon.magazineSize;

            // Update player's level display
            //levelText.text = $"Level: {playerLevel}";

            // Only show the unlock level text if the weapon is locked
            if (currentWeapon.isUnlocked)
            {
                unlockLevelText.gameObject.SetActive(false);
            }
            else
            {
                unlockLevelText.gameObject.SetActive(true);
                unlockLevelText.text = $"Unlocks at level {currentWeapon.requiredLevel}";
            }

            // Check if the weapon should be unlocked based on player level
            UnlockWeaponBasedOnLevel(currentWeapon);

            // Update button visibility
            if (currentWeapon.isUnlocked)
            {
                selectButton.gameObject.SetActive(true);
                lockedButton.gameObject.SetActive(false);
                if(currentWeapon.weaponID == currentSelectedWeaponIndex)
                {
                    SelectedTick.SetActive(true);
                    selectedButtontext.text = "Selected";
                }
                else
                {
                    SelectedTick.SetActive(false);
                    selectedButtontext.text = "Select";
                }
            }
            else
            {
                selectButton.gameObject.SetActive(false);
                lockedButton.gameObject.SetActive(true);
                SelectedTick.SetActive(false);
            }

            // Update weapon prefab display
            UpdateWeaponDisplay();
            InitializeWeaponUnlocks();
            DisplayNextUnlockableWeapon();
        }

    }

    void UnlockWeaponBasedOnLevel(Weapon weapon)
    {
        if (!weapon.isUnlocked && levelManager._playerLevel >= weapon.requiredLevel)
        {
            weapon.isUnlocked = true;
            PlayerPrefs.SetInt(WeaponUnlockedKeyPrefix + weapon.weaponID, 1);
            PlayerPrefs.Save();
            Debug.Log($"{weapon.weaponName} unlocked at level {levelManager._playerLevel}!");
        }
    }

    void UpdateWeaponDisplay()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].weaponPrefab != null)
            {
                // Only activate the prefab of the current weapon
                weapons[i].weaponPrefab.SetActive(i == currentIndex);
            }
        }
    }

    void LoadWeaponData()
    {
        foreach (var weapon in weapons)
        {
            weapon.isUnlocked = PlayerPrefs.GetInt(WeaponUnlockedKeyPrefix + weapon.weaponID, 0) == 1;
            currentSelectedWeaponIndex = PlayerPrefs.GetInt(SelectedWeaponKey);
        }
    }
    [System.Serializable]
    public class WeaponUI
    {
        public GameObject weaponObject; // The weapon GameObject
        public int unlockLevel; // Level required to unlock the weapon
        public bool isUnlocked; // Whether the weapon is unlocked
        public GameObject weaponUI; // The associated UI element
    }
    public List<WeaponUI> weaponUIList; // List of all weapon UI elements
    public TextMeshProUGUI nextUnlockText; // Text to display next unlock level
    public LevelManager levelManager;
    
    // Ensure the weapons' unlock status is correctly initialized
    private void InitializeWeaponUnlocks()
    {
        foreach (var weaponUI in weaponUIList)
        {
            // Unlock weapons if the player's level is greater than or equal to the weapon's unlock level
            if (levelManager._playerLevel >= weaponUI.unlockLevel)
            {
                weaponUI.isUnlocked = true; // Mark as unlocked
                weaponUI.weaponObject.SetActive(true); // Show the weapon
                weaponUI.weaponUI.SetActive(false); // Hide the "locked" UI
              
            }
            else
            {
                weaponUI.isUnlocked = false; // Mark as locked
                weaponUI.weaponObject.SetActive(false); // Hide the weapon
                weaponUI.weaponUI.SetActive(true); // Show the "locked" UI
            }
        }
    }
    public void DisplayNextUnlockableWeapon()
    {
        if (MainMenu)
            return;

        WeaponUI nextUnlockableWeapon = null;

        // Find the next weapon that the player can unlock after their current level
        foreach (var weaponUI in weaponUIList)
        {
            if (!weaponUI.isUnlocked && weaponUI.unlockLevel > levelManager._playerLevel &&
                (nextUnlockableWeapon == null || weaponUI.unlockLevel < nextUnlockableWeapon.unlockLevel))
            {
                nextUnlockableWeapon = weaponUI;
            }
        }

        // Update the UI
        if (nextUnlockableWeapon != null)
        {
            // Show the next weapon to unlock
            nextUnlockText.text = $"Unlocks at {nextUnlockableWeapon.unlockLevel}";
            foreach (var weaponUI in weaponUIList)
            {
                weaponUI.weaponUI.SetActive(weaponUI == nextUnlockableWeapon);
            }
        }
        else
        {
            // If no more weapons can be unlocked, display "All weapons unlocked!"
            nextUnlockText.text = "All weapons unlocked!";
            foreach (var weaponUI in weaponUIList)
            {
                weaponUI.weaponUI.SetActive(false);
            }
        }
    }


}
