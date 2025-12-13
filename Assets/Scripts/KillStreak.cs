using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Random = UnityEngine.Random;
using System.Reflection;
public class KillStreak : MonoBehaviour
{
    public static KillStreak Instance;
    public int TotalKills;
    public int KillStreakNum;
    public float resetTimer = 5f;
    public bool startTimer;

    public List<AudioClip> KillSFX;
    public List<string> KillLines;
    public AudioSource audioSource;
    public TextMeshProUGUI KillText;

    public int CoinsToCollets;
    public int TotalCoins = 100;
    public Slider slider;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        //slider.maxValue = 25f;
    }

    // Update is called once per frame
    void Update()
    {
        if(startTimer)
        {
            resetTimer -= Time.deltaTime;
            if(resetTimer <= 0)
            {
                startTimer = false;
                KillStreakNum = 0;
            }
            if (resetTimer <= 2.5f)
                KillText.enabled = false;
        }
    }
    public int invicibiltyKills= 0;
    
    public void KilledEnemy()
    {
        TotalKills++;
        KillStreakNum++;
        startTimer = true;
        resetTimer = 5f;
        KillStreakSound(KillStreakNum);
     
        //LevelManager.Instance.UpdateUI();


       
           
    }
    public void KillStreakSound(int killNo)
    {
        if(killNo <= 13)
        {
            KillText.enabled = true;
            KillText.text = KillLines[killNo - 1]+ " X" + killNo;
            audioSource.PlayOneShot(KillSFX[killNo - 1], 1f);
        }
        else
        {
            int num = Random.Range(3, 13);
            KillText.enabled = true;
            KillText.text = KillLines[num] + " X" + killNo;
            audioSource.PlayOneShot(KillSFX[num], 1f);
        }
        
    }
    public GameObject levelCompletedText;
    public AudioClip levelCompleteSFX;
    public EnemyHead[] enemies;
    public void CoinCollectProgress()
    {
        
        CoinsToCollets++;
        slider.value = CoinsToCollets;
        //if(CoinsToCollets >= TotalCoins)
        //{
        //    levelCompletedText.SetActive(true);
        //    audioSource.PlayOneShot(levelCompleteSFX, 1f);
        //    EnemySpawner.Instance.cancelSpawning();
        //    enemies = FindObjectsOfType(typeof(EnemyHead)) as EnemyHead[];
        //    foreach (EnemyHead i in enemies)
        //    {
        //        i.Explode();
        //    }
        //}

    }
    public PlayerHealth playerHealth;
    public bool level_Three;
    public void CompletedLevel()
    {
        levelCompletedText.SetActive(true);
        audioSource.PlayOneShot(levelCompleteSFX, 1f);
        //EnemySpawner.Instance.cancelSpawning();
        enemies = FindObjectsOfType(typeof(EnemyHead)) as EnemyHead[];
        foreach (EnemyHead i in enemies)
        {
            i.Explode();
        }
        StartCoroutine(DisableText());
        if (!playerHealth.hit)
            Challenges.instance.Challenge_Two(1);

        if (level_Three)
        {
            if (TotalKills == 0)
                Challenges.instance.Challenge_Four(1);
        }
            


    }
    IEnumerator DisableText()
    {
        yield return new WaitForSeconds(3f);
        levelCompletedText.SetActive(false);
    }
}
