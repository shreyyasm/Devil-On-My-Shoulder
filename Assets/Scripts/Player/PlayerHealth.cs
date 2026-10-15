using DamageNumbersPro;
using InfimaGames.LowPolyShooterPack;
using QFSW.MOP2;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;
    public float healthDecreaseRate;
    [Header("Health UI")]
    public SpriteRenderer healthBarSprite;
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public float maxYScale = 0.02f;
    private float displayedHealth;   // This is what the bar actually shows
    public float smoothSpeed = 5f;   // Higher = faster animation


    public AudioSource BgMusic;
    public AudioSource StarBG;

    public PostProcessVolume volume;
    ChromaticAberration chromaticAberration;
    public bool hit;

    public Animator healthAnim;

    public PowerUpSelect powerUpSelect;
    

    [Header("Lift Health Recovery")]
    [SerializeField, UnityEngine.Min(.1f)] private float liftHealDuration = 1f;
    private Coroutine liftHealRoutine;
public bool isTick;
    public ObjectPool bloodPool;
    public bool dead;

    private void Awake()
    {
        
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        ImpactFrameEffect.Instance.GoColor();
        //volume.profile.TryGetSettings(out chromaticAberration);
        displayedHealth = currentHealth;
        powerUpSelect = FindFirstObjectByType<PowerUpSelect>();

        currentBPM = Mathf.Clamp(currentBPM, 0f, maxBPM);
        OnBPMChanged?.Invoke(currentBPM);
    }


    public bool playerDead;

    // Update is called once per frame
    void Update()
    {
        if (PlayerMovement.mainMenu) return;

        // Smoothly move displayedHealth towards currentHealth
        displayedHealth = Mathf.Lerp(displayedHealth, currentHealth, Time.deltaTime * smoothSpeed);

        // Normalize health for scaling
        float healthPercent = Mathf.Clamp01(displayedHealth / maxHealth);

        // Scale the sprite
        //Vector3 scale = healthBarSprite.transform.localScale;
        //scale.y = healthPercent * maxYScale;
        //healthBarSprite.transform.localScale = scale;
        //if (regenerateHealth && health <= 100)
        //{
        //    health += Time.deltaTime * 2;
        //}

        if (currentHealth <= 0 && !playerDead && !timerRunning)
        {
            // Start countdown
            StartCountdown();
            //RaceCountdown.Instance.GameOver();

        }


       
        if (!isTick) return;

        if (PlayerMovement.characterAbilities.GetHealthModifier())
            return;

        if (PlayerMovement.finisherActive)
            return;

        if (currentHealth >= 0)
        {
            currentHealth -= healthDecreaseRate * Time.deltaTime;
        }
       

        //if (powerUpSelect.batteryDrain)
        //    healthDecreaseRate = 6;

        //else
        //    healthDecreaseRate = 10;

      
    }
   
    public PlayerMovement PlayerMovement;
    public Character character;



   
    public void ManageHealth(float amount)
    {
        float oldHealth = currentHealth;

        // Apply health change and clamp between 0 and maxHealth
        currentHealth = Mathf.Clamp(
            currentHealth + amount,
            0f,
            maxHealth
        );
       
        if (amount < 0)
        {
            if (PlayerMovement.finisherActive)
                return;

            SFXManager.Instance.PlaySFX(
                "Player/HealthMinus",
                1f
            );

            // Apply combo penalty automatically
            ComboManager.Instance.ApplyDamagePenalty();

            bloodPool.GetObject(
                transform.position,
                Quaternion.identity
            );
            VoiceLineManager.instance.PlayRandom("Taking Damage", 20f);
            ImpactFrameEffect.Instance.GoBlack();
            LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });
            ScoreManager.Instance.hitsTakken++;
            CameraShake.Instance.ShakeWithValue(0.3f,0.5f);
        }

        if (amount > 0)
        {
            // Health was added
            // Any healing beyond maxHealth is automatically discarded.

            StopCountdown();
          
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

public void PauseHealthDrain()
    {
        isTick = false;
    }

    /// <summary>
    /// The end lift is a safe transition. If the player reaches it while the
    /// zero-health countdown is active, stop that pending death and retain a
    /// minimal valid health value until lift recovery restores them fully.
    /// </summary>
    public void CancelDeathCountdownForLift()
    {
        StopCountdown();

        dead = false;
        playerDead = false;
        currentHealth = Mathf.Max(currentHealth, 1f);
        displayedHealth = currentHealth;
    }

    public void ResumeHealthDrain()
    {
        if (!playerDead)
            isTick = true;
    }

    public void RestoreHealthDuringLiftTravel()
    {
        if (liftHealRoutine != null)
            StopCoroutine(liftHealRoutine);

        isTick = false;
        liftHealRoutine = StartCoroutine(RestoreHealthToMaximum());
    }

    private IEnumerator RestoreHealthToMaximum()
    {
        float startHealth = currentHealth;
        if (startHealth >= maxHealth)
        {
            currentHealth = maxHealth;
            displayedHealth = currentHealth;
            liftHealRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < liftHealDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / liftHealDuration);
            currentHealth = Mathf.Lerp(startHealth, maxHealth, progress);
            // Keep the displayed value in sync so the visible meter rises
            // smoothly at the same time as the actual health.
            displayedHealth = currentHealth;
            yield return null;
        }

        currentHealth = maxHealth;
        displayedHealth = currentHealth;
        StopCountdown();
        liftHealRoutine = null;
    }

    public TextMeshProUGUI countdownDisplay;
    public int countdownTime = 5;
    public bool timerRunning = false;   

    [Header("Death Countdown Images")]
    [SerializeField] private Image countdownImage;
    [SerializeField] private Sprite countdownThreeSprite;
    [SerializeField] private Sprite countdownTwoSprite;
    [SerializeField] private Sprite countdownOneSprite;
    [SerializeField] private Image deathLogoImage;
    [SerializeField] private Sprite deathLogoSprite;
    [SerializeField, UnityEngine.Min(.01f)] private float countdownPopDuration = .18f;
    [SerializeField, UnityEngine.Min(.1f)] private float countdownPopStartScale = .55f;
    [SerializeField, UnityEngine.Min(1f)] private float countdownPopOvershootScale = 1.15f;

    private Coroutine countdownCoroutine;
    private bool isCancelled;

    public void StartCountdown()
    {
        //// If a countdown is already running, cancel it first
        //if (countdownCoroutine != null)
        //    StopCountdown();

        timerRunning = true;
        isCancelled = false;
        countdownDisplay.gameObject.SetActive(true);
        if (countdownImage != null)
            countdownImage.gameObject.SetActive(false);
        if (deathLogoImage != null)
            deathLogoImage.gameObject.SetActive(false);
        countdownCoroutine = StartCoroutine(CountdownToStart());

    }

    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            timerRunning = false;
            countdownDisplay.gameObject.SetActive(false);
            if (countdownImage != null)
                countdownImage.gameObject.SetActive(false);
            if (deathLogoImage != null)
                deathLogoImage.gameObject.SetActive(false);
            isCancelled = true;
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            countdownAudioSource.Stop();
            countdownDisplay.text = ""; // Clear UI or handle however you like
         
        }
    }
    public AudioSource countdownAudioSource;

    private IEnumerator CountdownToStart()
    {
        int timeLeft = countdownTime;
        countdownAudioSource.Play();
        while (timeLeft > 0)
        {
            if (isCancelled)
                yield break;

            ShowCountdownNumber(timeLeft);
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }

        dead = true;
        RoguelikeManager.Instance?.ResetRunStats();
        ShowDeathMessage();
        VoiceLineManager.instance.PlayRandom("Player Dies", 30f);
        ScoreManager.Instance.CalculateOverallScore();
        SFXManager.Instance.PlaySFX("Dead/GameOver", 1f);
        ScoreManager.Instance.ManageBGAudio(0.3f);
        playerDead = true;
        yield return StartCoroutine(FinishDeathAfterFall());
        countdownCoroutine = null;
    }

    public void PlayerDead()
    {
        RoguelikeManager.Instance?.ResetRunStats();
        ShowDeathMessage();
        ScoreManager.Instance.CalculateOverallScore();
        SFXManager.Instance.PlaySFX("Dead/GameOver", 1f);
        ScoreManager.Instance.ManageBGAudio(0.3f);
        playerDead = true;
        StartCoroutine(FinishDeathAfterFall());
    }

    private void ShowCountdownNumber(int number)
    {
        Sprite sprite = number == 3 ? countdownThreeSprite :
                        number == 2 ? countdownTwoSprite :
                        number == 1 ? countdownOneSprite : null;

        if (countdownImage == null || sprite == null)
        {
            countdownDisplay.gameObject.SetActive(true);
            countdownDisplay.text = number.ToString();
            return;
        }

        countdownDisplay.gameObject.SetActive(false);
        countdownImage.sprite = sprite;
        countdownImage.preserveAspect = true;
        countdownImage.gameObject.SetActive(true);

        RectTransform imageTransform = countdownImage.rectTransform;
        LeanTween.cancel(countdownImage.gameObject);
        imageTransform.localScale = Vector3.one * countdownPopStartScale;
        LeanTween.scale(countdownImage.gameObject, Vector3.one * countdownPopOvershootScale, countdownPopDuration)
            .setEaseOutBack()
            .setIgnoreTimeScale(true)
            .setOnComplete(() => LeanTween.scale(countdownImage.gameObject, Vector3.one, countdownPopDuration * .55f)
                .setEaseInOutQuad()
                .setIgnoreTimeScale(true));
    }

    private void ShowDeathMessage()
    {
        if (countdownImage != null)
            countdownImage.gameObject.SetActive(false);

        if (deathLogoImage != null && deathLogoSprite != null)
        {
            countdownDisplay.gameObject.SetActive(false);
            deathLogoImage.sprite = deathLogoSprite;
            deathLogoImage.preserveAspect = true;
            deathLogoImage.gameObject.SetActive(true);
            RectTransform logoTransform = deathLogoImage.rectTransform;
            LeanTween.cancel(deathLogoImage.gameObject);
            logoTransform.localScale = Vector3.one * countdownPopStartScale;
            LeanTween.scale(deathLogoImage.gameObject, Vector3.one * countdownPopOvershootScale, countdownPopDuration)
                .setEaseOutBack()
                .setIgnoreTimeScale(true)
                .setOnComplete(() => LeanTween.scale(deathLogoImage.gameObject, Vector3.one, countdownPopDuration * .55f)
                    .setEaseInOutQuad()
                    .setIgnoreTimeScale(true));
            return;
        }

        countdownDisplay.gameObject.SetActive(true);
        countdownDisplay.text = "You're Dead!";
    }

    [Header("Death Presentation")]
    [SerializeField, Range(0.1f, 0.8f)] private float blinkStartDuringFallPortion = 0.45f;
    [SerializeField, UnityEngine.Min(0f)] private float additionalBlinkDelay = 1f;

    private IEnumerator FinishDeathAfterFall()
    {
        PlayerDeathFall deathFall = GetComponent<PlayerDeathFall>();
        float fallDuration = deathFall != null ? deathFall.FallDuration : 0.85f;
        float blinkStartDelay = (fallDuration * blinkStartDuringFallPortion) + additionalBlinkDelay;
        float blinkDuration = Mathf.Max(0.1f, fallDuration * (1f - blinkStartDuringFallPortion));
        DeathBlinkTransition blinkTransition = DeathBlinkTransition.Instance;

        // Keep the fall clearly visible before the eyes begin to close.
        if (deathFall != null)
            deathFall.PlayDeathFall();

        yield return new WaitForSecondsRealtime(blinkStartDelay);

        if (blinkTransition != null)
            blinkTransition.Play(blinkDuration, ShowScoreCanvas);

        yield return new WaitForSecondsRealtime(blinkDuration);
        gameObject.GetComponent<PlayerMovement>().ExplodeItself();

        if (blinkTransition == null)
            ShowScoreCanvas();
    }

    private void ShowScoreCanvas()
    {
        if (ScoreManager.Instance != null && ScoreManager.Instance.scoreCanvas != null)
        {
            Canvas scoreCanvas = ScoreManager.Instance.scoreCanvas.GetComponent<Canvas>();
            if (scoreCanvas != null && DeathBlinkTransition.Instance != null)
            {
                scoreCanvas.overrideSorting = true;
                scoreCanvas.sortingOrder = DeathBlinkTransition.Instance.ScoreCanvasSortingOrder;
            }

            ScoreManager.Instance.scoreCanvas.SetActive(true);
        }

        LeanTween.delayedCall(3f, () =>
        {
            Cursor.lockState = CursorLockMode.None;
            GamepadCursor.Instance.OnControlsChanged();
        });
    }

    [Header("Critical Thresholds")]
    public float criticalBPM = 40f;
    public float lowBPM = 70f;

    public event Action<float> OnBPMChanged;
    public event Action OnFlatline;

    [Header("BPM Health")]
    public float maxBPM = 180f;
    public float currentBPM = 120f;

    public void TakeDamage(float bpmLoss)
    {
        if (currentBPM <= 0f) return;

        currentBPM -= bpmLoss;
        currentBPM = Mathf.Clamp(currentBPM, 0f, maxBPM);

        OnBPMChanged?.Invoke(currentBPM);

        if (currentBPM <= 0f)
            Flatline();
    }

    public void HealPlayer(float bpmGain)
    {
        if (currentBPM <= 0f) return;

        currentBPM += bpmGain;
        currentBPM = Mathf.Clamp(currentBPM, 0f, maxBPM);

        OnBPMChanged?.Invoke(currentBPM);
    }

    private void Flatline()
    {
        OnFlatline?.Invoke();
    }

    public bool IsCritical => currentBPM <= criticalBPM;
    public bool IsLow => currentBPM <= lowBPM && currentBPM > criticalBPM;

}



