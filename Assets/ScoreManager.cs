using InfimaGames.LowPolyShooterPack;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public GunSelect gunSelect;
    public PowerUpSelect powerUpSelect;
    [Header("Score Settings")]
    [Tooltip("Points per second")]
    public float scorePerSecond = 100f;

    // internal float accumulator
    private float internalScore = 0f;

    /// <summary>
    /// The integer score shown to the player.
    /// </summary>
    public int gameScore { get; private set; } = 0;

    //[Tooltip("How many points between each choice event (e.g. 3000)")]
    //public int thresholdInterval = 3000;

    [Header("UI")]
    public TextMeshProUGUI scoreText;     // assign in inspector
    public GameObject choiceCanvas;       // assign in inspector (disabled by default)
    public Slider choiceTimerSlider;      // assign a UI Slider (acts as countdown)
    public float choiceTime = 5f;         // seconds to choose

    [Header("Behavior")]
    [Tooltip("Score stops increasing while choice canvas is active.")]
    public bool pauseScoreDuringChoice = true;  // ✅ default true

    public bool isChoosing = false;
    private float choiceTimer = 0f;
    private int nextThreshold = 0;

    public Character character;

    // clamp delta time to avoid large jumps (e.g. first frame or hitches)
    private const float MAX_DT = 0.05f; // 50 ms max per frame contribution


 
    void Awake()
    {
        //powerUpSelect.bullet.bulletDamage = 60;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public FunticoSDKExample FunticoSDKExample;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // This will automatically find the new PlayerMovement in the new scene
        PlayerMovement = FindObjectOfType<PlayerMovement>();
        FunticoSDKExample = FindFirstObjectByType<FunticoSDKExample>();
        character = PlayerMovement.GetComponent<Character>();
        powerUpSelect.DeSelectPower();
    }
    void Start()
    {
        GamepadCursor.Instance.DisableCursor();
        LoadHighScore();
        ResetAccuracy();
        StartTimer();
        // initialize
        internalScore = 0f;
        gameScore = 0;
        UpdateScoreUI();

        //nextThreshold = Mathf.Max(1, thresholdInterval); // avoid zero
        if (choiceCanvas) choiceCanvas.SetActive(false);
        if (choiceTimerSlider && choiceTimerSlider.gameObject) choiceTimerSlider.gameObject.SetActive(false);

        // lock & hide cursor at start
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;


    }
    bool powerdown;
    void Update()
    {

        if(PlayerMovement.playerHealth.playerDead) return;

        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        // clamp delta time
        float dt = Mathf.Min(Time.unscaledDeltaTime, MAX_DT);

        // Increase score (only when not choosing OR if pauseScoreDuringChoice is false)
        if (!isChoosing || !pauseScoreDuringChoice)
        {
            internalScore += scorePerSecond * dt;

            int newDisplayed = Mathf.FloorToInt(internalScore + 0.00001f);
            if (newDisplayed != gameScore)
            {
                gameScore = newDisplayed;
                UpdateScoreUI();
            }
        }

        //// Trigger choice when threshold reached and not currently choosing
        //if (!isChoosing && gameScore >= nextThreshold)
        //{
        //    StartChoice();
        //    // compute next threshold as next multiple above current score
        //    int multiples = gameScore / thresholdInterval;
        //    nextThreshold = (multiples + 1) * thresholdInterval;
        //}

        // Handle choice countdown
        if (isChoosing)
        {
            choiceTimer -= Time.unscaledDeltaTime;

            if (choiceTimerSlider)
            {
                choiceTimerSlider.value = Mathf.Clamp01(choiceTimer / choiceTime);
            }

            if (choiceTimer <= 0f)
            {
                EndChoice();
            }
        }


        //if (abilityActive)
        //{
        //    abilityTimer -= Time.deltaTime;
        //    abilityTimerSlider.value = abilityTimer;
        //    if (abilityTimer <= 0.8f && !powerdown)
        //    {
        //        ImpactFrameEffect.Instance.TriggerImpact(0, 1.5f);
        //        LeanTween.delayedCall(0.1f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });

        //        LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.TriggerImpact(0, 1.5f); });
        //        LeanTween.delayedCall(0.3f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });

        //        LeanTween.delayedCall(0.4f, () => { ImpactFrameEffect.Instance.TriggerImpact(0, 1.5f); });
        //        LeanTween.delayedCall(0.5f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });

        //        LeanTween.delayedCall(0.6f, () => { ImpactFrameEffect.Instance.TriggerImpact(0, 1.5f); });
        //        LeanTween.delayedCall(0.7f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });

        //        LeanTween.delayedCall(0.8f, () => { ImpactFrameEffect.Instance.TriggerImpact(0, 1.5f); });
        //        LeanTween.delayedCall(0.9f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });
        //        string sfxKey = $"PowerUp/PowerDown";
        //        SFXManager.Instance.PlaySFX(sfxKey, 1f);
        //        powerdown = true;
        //    }
        //    if (abilityTimer <= 0f)
        //    {
        //        gunSelect.EquipDefault();
        //        powerUpSelect.DeSelectPower();
        //        abilityTimerSlider.gameObject.SetActive(false);

        //        string sfxKey = $"PowerUp/Ability On";
        //        SFXManager.Instance.PlaySFX(sfxKey, 1f);


        //    }
        //}
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = gameScore.ToString("N0"); // formatted with commas
    }

    public void StartChoice()
    {
        // Cancel countdown anytime
        PlayerMovement.GetComponent<PlayerHealth>().StopCountdown();

        PlayerMovement.readyToSlide = true;
        PlayerMovement.isSliding = false;
        PlayerMovement.anim.ResetTrigger("Running");
        PlayerMovement.speedline.ResetTrigger("SpeedLines");

        if (choiceCanvas) choiceCanvas.SetActive(true);
        Card1.SetTrigger("GoUp");
        Card2.SetTrigger("GoUp");

        StartCoroutine(SmoothVolume(BgAudio, 0.3f, 1f));
        string sfxKey = $"PowerUp/Woosh";
        SFXManager.Instance.PlaySFX(sfxKey, 1f);
        character.holdingButtonFire = false;
        isChoosing = true;
        choiceTimer = choiceTime;

       
        if (choiceTimerSlider)
        {
            choiceTimerSlider.gameObject.SetActive(true);
            choiceTimerSlider.minValue = 0f;
            choiceTimerSlider.maxValue = 1f;
            choiceTimerSlider.value = 1f;
        }

        // pause game world if needed
        Time.timeScale = 0f;

        // unlock & show cursor
        //Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        GamepadCursor.Instance.OnControlsChanged();
    }

    public void PickCard(int cardIndex)
    {
        Debug.Log($"Player picked card {cardIndex}");
        // TODO: apply card effect here
        EndChoice();
    }
    public Animator Card1;
    public Animator Card2;
    public PlayerMovement PlayerMovement;
    public void EndChoice(bool choosed = false)
    {
        Card1.SetTrigger("GoDown");
        Card2.SetTrigger("GoDown");
        string sfxKey = $"PowerUp/Woosh";
        powerdown = false;
        GamepadCursor.Instance.DisableCursor();
        LeanTween.delayedCall(0.4f, () => 
        {
            StartCoroutine(SmoothVolume(BgAudio, 0.7f, 1f));
            if (choiceCanvas) choiceCanvas.SetActive(false);
            if (choiceTimerSlider && choiceTimerSlider.gameObject) choiceTimerSlider.gameObject.SetActive(false);

            isChoosing = false;
            Card1.ResetTrigger("GoUp");
            Card2.ResetTrigger("GoUp");
            Card1.ResetTrigger("GoDown");
            Card2.ResetTrigger("GoDown");

            PlayerMovement.readyToSlide = true;
            PlayerMovement.isSliding = false;
            PlayerMovement.anim.ResetTrigger("Running");
            PlayerMovement.speedline.ResetTrigger("SpeedLines");

        });
        if(choosed)
        {
            ImpactFrameEffect.Instance.GoBlack();
            LeanTween.delayedCall(0.1f, () => { ImpactFrameEffect.Instance.GoColor(); });

            LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoBlack(); });
            LeanTween.delayedCall(0.3f, () => { ImpactFrameEffect.Instance.GoColor(); });

            LeanTween.delayedCall(0.4f, () => { ImpactFrameEffect.Instance.GoBlack(); });
            LeanTween.delayedCall(0.5f, () => { ImpactFrameEffect.Instance.GoColor(); });

            LeanTween.delayedCall(0.6f, () => { ImpactFrameEffect.Instance.GoBlack(); });
            LeanTween.delayedCall(0.7f, () => { ImpactFrameEffect.Instance.GoColor(); });

            LeanTween.delayedCall(0.8f, () => { ImpactFrameEffect.Instance.GoBlack(); });
            LeanTween.delayedCall(0.9f, () => { ImpactFrameEffect.Instance.GoColor(); });
        }
      
        // lock & hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Time.timeScale = 1f;

        
    }

    // Optional helper to add/subtract score manually
    public void AddScore(float amount)
    {
        internalScore += amount;
        int newDisplayed = Mathf.FloorToInt(internalScore + 0.00001f);
        if (newDisplayed != gameScore)
        {
            gameScore = newDisplayed;
            UpdateScoreUI();
        }

        //// keep nextThreshold consistent if score is changed externally
        //if (gameScore >= nextThreshold)
        //{
        //    int multiples = gameScore / thresholdInterval;
        //    nextThreshold = (multiples + 1) * thresholdInterval;
        //}
    }

    public bool abilityActive;


    public AudioSource BgAudio;
    public void ManageBGAudio(float volume)
    {
        StartCoroutine(SmoothVolume(BgAudio, volume, 1f));
    }
    public static IEnumerator SmoothVolume(AudioSource source, float targetVolume, float speed)
    {
        if (source == null) yield break;

        while (!Mathf.Approximately(source.volume, targetVolume))
        {
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, speed * Time.unscaledDeltaTime);
            yield return null;
        }

        source.volume = targetVolume; // snap at the end
    }

    [Header("Game Timer")]
    private float elapsedTime;
    private bool isRunning = false;
    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }
    private void UpdateTimerDisplay()
    {
        int hours = Mathf.FloorToInt(elapsedTime / 3600);
        int minutes = Mathf.FloorToInt((elapsedTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);

        timeSurvivedText.text = $"Time: {hours}:{minutes:00}:{seconds:00}";
    }

    [Header("Accuracy Settings")]

    private int totalShotsFired;
    public int totalShotsHit;
    public void RegisterShotFired()
    {
        totalShotsFired++;
        UpdateAccuracyDisplay();
    }

    public void RegisterShotHit()
    {
        totalShotsHit++;
        UpdateAccuracyDisplay();
    }

    private void UpdateAccuracyDisplay()
    {
        int accuracyPercent = (totalShotsFired > 0)
            ? Mathf.RoundToInt(((float)totalShotsHit / totalShotsFired) * 100f)
            : 0;

        accuracyText.text = $"Accuracy: {accuracyPercent}%";
    }

    public void ResetAccuracy()
    {
        totalShotsFired = 0;
        totalShotsHit = 0;
        UpdateAccuracyDisplay();
    }

    public void IncreaseLevel()
    {
        level++;
        levelText.text = "Level: "+ level;
    }


    public void CalculateOverallScore()
    {
      
        TotalScoreText.text = "Total: " + gameScore;
        KillsText.text = "Kills: " + Kills;
        hitsTakkenText.text = "Hits Taken: " + hitsTakken;
        scoreText.enabled = false;
        CheckHighScore();

    }

    [Header("Score Display Settings")]
    public int timeSurvived;
    public int Kills;
    public int highScore;
    public int accuracy;
    public int level;
    public int hitsTakken;

    public TextMeshProUGUI timeSurvivedText;
    public TextMeshProUGUI KillsText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI TotalScoreText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hitsTakkenText;

    public GameObject highScoreBG;

    private const string HighScoreKey = "HighScore";

    public GameObject scoreCanvas;
    
    public void CheckHighScore()
    {
        FunticoSDKExample.SendScore(gameScore);
        if (gameScore > highScore)
        {
            highScore = gameScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            Debug.Log("🎯 New High Score: " + highScore);
            highScoreText.text = "New High Score";
            highScoreBG.SetActive(true);

            Debug.Log("Score Sended");
        }

        else
        {
            highScoreText.text = "HighScore: " + PlayerPrefs.GetInt(HighScoreKey, 0).ToString();
            Debug.Log("No new high score. Current: " + highScore);

           
           
        }
        
       
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        Debug.Log("Loaded High Score: " + highScore);
    }
    public void RestartGame()
    {
        if (ScoreManager.Instance != null)
        {
            Destroy(ScoreManager.Instance.gameObject);
        }

        SceneManager.LoadScene("Mad Dash Game");
    }
    public void MainMenu()
    {

        if (ScoreManager.Instance != null)
        {
            Destroy(ScoreManager.Instance.gameObject);
        }
        SceneManager.LoadScene(3);

    }

}
