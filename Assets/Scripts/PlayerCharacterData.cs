using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCharacterData : MonoBehaviour
{
    public static PlayerCharacterData Instance;

    [Header("Selected Character")]
    public CharacterData selectedCharacter;

    [Header("CharacterIndex")]
    public int characterIndex;

    [Header("Movement Settings")]
    public float moveSpeed = 4500;

    [Header("Jump Settings")]
    public float jumpForce = 550f;

    [Header("Slide Settings")]
    public float slideForce = 15f;

    [Header("Health UI")]
    public float maxHealth = 100f;

    [Header("Damage")]
    public float damageMultipler = 100f;

   
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

  
        // Apply selected character if available
        if (CharacterSelectionManager.Instance != null &&
            CharacterSelectionManager.Instance.selectedCharacter != null)
        {
            selectedCharacter = CharacterSelectionManager.Instance.selectedCharacter;
            LoadPlayerData();
            Debug.Log("Loaded Character Data");
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            IncreaseMaxHealth(10);
        }
    }
    public void LoadPlayerData()
    {
        characterIndex = selectedCharacter.characterIndex;

        // Movement
        moveSpeed = selectedCharacter.moveSpeed;

        // Jump
        jumpForce = selectedCharacter.jumpForce;

        // Slide
        slideForce = selectedCharacter.slideForce;

        //Health
        maxHealth = selectedCharacter.maxHealth;

        //Damage
        damageMultipler = selectedCharacter.damageMultiplier;

       
    }

    public void IncreasePlayerSpeed(int moveValue,int slideValue)
    {
        moveSpeed += moveValue;
        slideForce += slideValue;
        Debug.Log("PLayerSpeed Increased");
    }
    public void IncreaseMaxHealth(int value)
    {
        maxHealth += value;
        Debug.Log("Max Health Increased");
    }
    public void IncreaseDamage(int value)
    {
        damageMultipler += value;
        Debug.Log("Max Health Increased");
    }
}
