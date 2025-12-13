using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class RaceCountdown : MonoBehaviour
{
    public static RaceCountdown Instance;
    
    public int countdownTime;
    public float gameTimer = 180;
    public TextMeshProUGUI countdownDisplay;
    public TextMeshProUGUI GameTimerText;

    public AudioClip startSound;
    public AudioSource audioSource;
    public bool gameStart;

    public GameObject gameOvertext;
    public AudioClip gameOverSFX;
    public AudioClip gameOverMusic;
    public GameObject bgMusic;

    public bool timerLevelNot;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    void Start()
    {
        StartCoroutine(CountdownToStart());
        
    }
    private void Update()
    {
        if(gameStart)
        {
            gameTimer -= Time.deltaTime;
            DisplayTime(gameTimer);
            if (gameTimer <= 0 && !timerLevelNot)
            {
                PlayerMovement.Instance.ExplodeItself();
                bgMusic.SetActive(false);
                StartCoroutine(Restart());
                gameOvertext.SetActive(true);
                audioSource.PlayOneShot(gameOverSFX, 1);
                gameStart = false;
            }
               
        }
            
    }
    IEnumerator Restart()
    {
        yield return new WaitForSeconds(3);
        audioSource.PlayOneShot(gameOverMusic, 1);
    }
    public bool notSpawn;
    IEnumerator CountdownToStart()
    {
        audioSource.PlayOneShot(startSound, 1);
        while(countdownTime > 0)
        {
            
            countdownDisplay.text = countdownTime.ToString();
            yield return new WaitForSeconds(1f);

            countdownTime--;
        }

        countdownDisplay.text = "Go!";
        gameStart = true;
        GameTimerText.enabled = true;
        //begin game
        //HorseBetNumbers.instance.StartGame();
        //if(!notSpawn)
        //EnemySpawner.Instance.StartGame();
       


        
        countdownDisplay.gameObject.SetActive(false);
       
    }
    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        GameTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    public LevelManager levelManager;
    public TextMeshProUGUI ExpTotalGained;
    public TextMeshProUGUI ExpXKills;
    public int TotalExpGain;
    public void GameOver()
    {
        ExpXKills.text = KillStreak.Instance.TotalKills + " Kills X 15 Xp";
        ExpTotalGained.text = KillStreak.Instance.TotalKills * 200 + " Xp";
        TotalExpGain = KillStreak.Instance.TotalKills * 200;
       

        //levelManager.AddExperienceAfterGame(TotalExpGain);
        PlayerPrefs.Save();
        bgMusic.SetActive(false);
        StartCoroutine(Restart());
        gameOvertext.SetActive(true);
        audioSource.PlayOneShot(gameOverSFX, 1);
        gameStart = false;
        StartCoroutine(LoadRetryLevel());
    }
    IEnumerator LoadRetryLevel()
    {
        yield return new WaitForSeconds(8f);
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene - 1);
    }
}
