using InfimaGames.LowPolyShooterPack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static WeaponSelectionManager;

public class GunSelect : MonoBehaviour
{
    public GameObject defaultGun;
    public List<GameObject> weaponsPrefabs;

   
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void Update()
    {
      
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        unlockedParent = GameObject.FindGameObjectWithTag("UnlockedParent").transform;
        lockedParent = GameObject.FindGameObjectWithTag("LockedParent").transform;
        inventory = GameObject.FindGameObjectWithTag("UnlockedParent").GetComponent<Inventory>();
        defaultGun = GameObject.FindGameObjectWithTag("Gun 1");

        weaponsPrefabs.RemoveAll(e => e == null);

        // Get all Weapon components in the project (including inactive ones)
        var allWeapons = Resources.FindObjectsOfTypeAll<InfimaGames.LowPolyShooterPack.Weapon>();

        foreach (var weapon in allWeapons)
        {
            // Only add if it's part of a loaded scene (not a prefab in the project)
            if (weapon.gameObject.scene.isLoaded)
            {
                if (weapon.gameObject.tag == "Gun 1")
                    continue;
                weaponsPrefabs.Add(weapon.gameObject);
            }
        }
        weaponsPrefabs.Reverse();
    }
    public Inventory inventory;

    public Transform unlockedParent; // Parent for unlocked weapons
    public Transform lockedParent;   // Parent for locked weapons
    public void EquipGun(string weaponTag)
    {
        ScoreManager.Instance.abilityActive = true;

        // Lock all weapons + default gun
        foreach (var weapon in weaponsPrefabs)
        {
            weapon.transform.SetParent(lockedParent);
            weapon.SetActive(false);
        }

        defaultGun.transform.SetParent(lockedParent);
        defaultGun.SetActive(false);

        // Find weapon by tag
        GameObject targetWeapon = weaponsPrefabs.Find(w => w.CompareTag(weaponTag));

        if (targetWeapon != null)
        {
            targetWeapon.transform.SetParent(unlockedParent);
            targetWeapon.SetActive(true);   // don't forget to activate it!
        }
        else
        {
            Debug.LogWarning($"⚠️ No weapon found with tag: {weaponTag}");
        }

        ScoreManager.Instance.EndChoice(true);
        inventory.Init();

        string sfxKey = $"PowerUp/Ability On";
        SFXManager.Instance.PlaySFX(sfxKey, 1f);
    }
    

    public void EquipDefault()
    {
      
        foreach (var weapon in weaponsPrefabs)
        {
            
           
            //Lock all Weapon apart Default
            weapon.transform.SetParent(lockedParent);
            weapon.SetActive(false);

        }
        ScoreManager.Instance.abilityActive = false;
        defaultGun.transform.SetParent(unlockedParent);
        defaultGun.SetActive(true);
        inventory.Init();
    }
}
