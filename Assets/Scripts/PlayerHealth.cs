using InfimaGames.LowPolyShooterPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering.PostProcessing;

public class PlayerHealth : MonoBehaviour
{
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
    bool drain = true;

    // Start is called before the first frame update
    void Start()
    {
        ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f);
        //volume.profile.TryGetSettings(out chromaticAberration);
        displayedHealth = currentHealth;
        powerUpSelect = FindFirstObjectByType<PowerUpSelect>();
        LeanTween.delayedCall(2f, () => { drain = false; });
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
        Vector3 scale = healthBarSprite.transform.localScale;
        scale.y = healthPercent * maxYScale;
        healthBarSprite.transform.localScale = scale;
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


       
        if(drain) return; 

       
        currentHealth -= healthDecreaseRate * Time.deltaTime;

        //if (powerUpSelect.batteryDrain)
        //    healthDecreaseRate = 6;

        //else
        //    healthDecreaseRate = 10;


    }
   
    public PlayerMovement PlayerMovement;
    public Character character;
   
  
 

    public void ManageHealth(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if(amount < 0)
        {
            SFXManager.Instance.PlaySFX("Player/HealthMinus", 1f);
            healthAnim.SetTrigger("HealthMinus");
            ScoreManager.Instance.hitsTakken++;

        }
           

        if (amount > 0)
        {
            healthAnim.SetTrigger("HealthAdd");
            SFXManager.Instance.PlaySFX("Player/HealthAdd", 1f);

            // Cancel countdown anytime
            StopCountdown();
        }
           
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
    public TextMeshProUGUI countdownDisplay;
    public int countdownTime = 5;
    public bool timerRunning = false;   

    private Coroutine countdownCoroutine;
    private bool isCancelled;

    public void StartCountdown()
    {
        //// If a countdown is already running, cancel it first
        //if (countdownCoroutine != null)
        //    StopCountdown();

        timerRunning = true;
        isCancelled = false;
        countdownCoroutine = StartCoroutine(CountdownToStart());
        countdownDisplay.gameObject.SetActive(true);
    }

    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            timerRunning = false;
            countdownDisplay.gameObject.SetActive(false);
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
                yield break; // immediately exit if cancelled

            countdownDisplay.text = timeLeft.ToString();
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }
        ImpactFrameEffect.Instance.TriggerImpact(0, 1.5f);
        countdownDisplay.text = "You're Dead!";
        ScoreManager.Instance.CalculateOverallScore();
        //countdownDisplay.gameObject.SetActive(false);
        string sfxKey = $"Dead/GameOver";
        SFXManager.Instance.PlaySFX(sfxKey, 1f);
        ScoreManager.Instance.ManageBGAudio(0.3f);
        gameObject.GetComponent<PlayerMovement>().ExplodeItself();
        playerDead = true;
        countdownCoroutine = null; // mark finished
        yield return new WaitForSeconds(2f);
        ScoreManager.Instance.scoreCanvas.SetActive(true);
        LeanTween.delayedCall(3f, () => {
            Cursor.lockState = CursorLockMode.None;
            GamepadCursor.Instance.OnControlsChanged();
        });
      
    }
    public void PlayerDead()
    {
        ImpactFrameEffect.Instance.TriggerImpact(0, 1.5f);
        countdownDisplay.text = "You're Dead!";
        ScoreManager.Instance.CalculateOverallScore();
        //countdownDisplay.gameObject.SetActive(false);
        string sfxKey = $"Dead/GameOver";
        SFXManager.Instance.PlaySFX(sfxKey, 1f);
        ScoreManager.Instance.ManageBGAudio(0.3f);
        gameObject.GetComponent<PlayerMovement>().ExplodeItself();
        playerDead = true;

        ScoreManager.Instance.scoreCanvas.SetActive(true);
        LeanTween.delayedCall(3f, () => {
            Cursor.lockState = CursorLockMode.None;
            GamepadCursor.Instance.OnControlsChanged();
        });
    }

}
