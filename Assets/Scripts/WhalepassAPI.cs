using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Whalepass;

public class WhalePassAPI : MonoBehaviour
{
    public static WhalePassAPI instance;
    public string GameId = ""; // Replace with your Game ID
    public string ApiKey = ""; // Replace with your API Key
    public string BattlePassId = ""; // Replace with your Battlepass ID

    public string playerId = "";

    public int currentLevel;
    public int CurrentTotalExp;
    public int CurrentExp;
    public int NextLevelExp;
    public int ExpRequiredLastlevel;

    public LevelManager levelManager;
    public int CurrentLevel
    {
        get => currentLevel;
        set
        {
            if (currentLevel != value) // Only trigger when the value actually changes
            {
                currentLevel = value;
                OnLevelChanged?.Invoke(currentLevel); // Trigger event
            }
        }
    }
    // Event triggered when the level changes
    public event Action<int> OnLevelChanged;
    private void Awake()
    {
        if(instance == null)
            instance = this;

        playerId = LoadPlayerId();

        if (string.IsNullOrEmpty(playerId))
        {
            playerId = GeneratePlayerId();
            SavePlayerId(playerId);
           
        }

        EnrollPlayer();
        CheckPlayer_Inventory();
        GettingBattlePass();
        PlayerBaseResponse();
        LeanTween.delayedCall(2f, () => { levelManager.UpdateUI(); });
        
    }
    private void Start()
    {
        

    }
    private void Update()
    {
       
        
    }
    private string GeneratePlayerId()
    {
        return Guid.NewGuid().ToString();
    }

    private void SavePlayerId(string id)
    {
        PlayerPrefs.SetString("PlayerId", id);
        PlayerPrefs.Save();
        Debug.Log("Player ID saved locally: " + id);
    }

    private string LoadPlayerId()
    {
        if (PlayerPrefs.HasKey("PlayerId"))
        {
            string id = PlayerPrefs.GetString("PlayerId");
            Debug.Log("Loaded Player ID: " + id);
            return id;
        }

        Debug.Log("Player ID not found, generating a new one.");
        return "";
    }

   public void EnrollPlayer()
    {
        WhalepassSdkManager.enroll(playerId, response =>
        {
            Debug.Log(response.succeed);
        });
    }
    public void AddExp(int exp)
    {
        WhalepassSdkManager.updateExp(playerId, exp, response =>
        {
            Debug.Log(response.succeed);
            Debug.Log(response.responseBody);
        });
    }
    public void CheckPlayer_Inventory()
    {
        WhalepassSdkManager.getPlayerInventory(playerId, response =>
        {
            Debug.Log(response.succeed);
            Debug.Log(response.responseBody);
        });
    }
    public void RedirectPlayer_Rewards()
    {
        WhalepassSdkManager.getPlayerRedirectionLink(playerId, response =>
        {
            
            Application.OpenURL(response.link.redirectionLink);
        });
    }
    public void GettingBattlePass()
    {
        WhalepassSdkManager.getBattlepass(BattlePassId, false, false, response =>
        {
           
        });
    }
    public void CompletingChallenge(string challengeID)
    {
        WhalepassSdkManager.completeChallenge(playerId, challengeID, response =>
        {
            Debug.Log(response.succeed);
            Debug.Log(response.responseBody);
        });
    }
    public void PlayerBaseResponse()
    {
        WhalepassSdkManager.getPlayerBaseProgress(playerId, response =>
        {
            
            NextLevelExp = (int)response.result.expRequiredForNextLevel;
            CurrentTotalExp = (int)response.result.currentExp;
            CurrentLevel = (int)response.result.lastCompletedLevel;
            CurrentExp = (int)response.result.currentExp - (int)response.result.expRequiredForLastLevel;
            ExpRequiredLastlevel = (int)response.result.expRequiredForLastLevel;
            levelManager._playerLevel = (int)response.result.lastCompletedLevel;
            
        });
    }
}
