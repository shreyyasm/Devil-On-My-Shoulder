using DG.Tweening;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;

    [Header("Combo State")]
    public GameObject Parent;
    public bool comboActive = false;
    public int multiplier = 0;
    public float currentXP = 0f;
    public float requiredXP = 100f;

    [Header("XP Settings")]
    public float baseXPRequired = 100f;
    public float growthRate = 1.2f;

    [Header("XP Gain")]
    public float basicEnemyXP = 25f;
    public float strongEnemyXP = 50f;
    public float bossXP = 200f;

    [Header("Bonus XP")]
    public float multiKillBonus = 20f;
    public float bigMultiKillBonus = 50f;
    public float environmentalBonus = 30f;
    public float finisherBonus = 40f;
    public float airKillBonus = 25f;
    public float headshotBonus = 15f;

    [Header("Decay")]
    [Range(0f, 1f)]
    public float decayPerSecond = 0.2f; // 20% per second

    [Header("Damage Penalty")]
    public float fixedDamageXPLoss = 30f;

    [Header("Kill Chain")]
    public TextMeshProUGUI enemyKilledText;
    public float killChainResetTime = 2f;
    private float lastChainKillTime;
    private int multiKillCount = 0;

    [Header("Kill Feed")]
    public TextMeshProUGUI killFeedText;
    public float killFeedDuration = 2f;

    [Header("Kill Feed Timing")]
    public float killFeedDelay = 0.25f;
    private string delayedKillText;

    [Header("UI References")]
    public Image multiplierImage;
    public Sprite[] multiplierSprites = new Sprite[4];
    public Slider xpSlider;
    public TextMeshProUGUI comboNameText;

    [Header("Tween Settings")]
    public float multiplierPopScale = 1.5f;
    public float multiplierPopDuration = 0.2f;
    public float sliderAnimDuration = 0.25f;

    [Header("Combo Names")]
    public string[] comboNames = new string[]
    {
        "RAMPAGE","DESTRUCTION","CHAOS","ANNIHILATION","MASSACRE",
        "BLOODBATH","GENOCIDE","OBLITERATION","APOCALYPSE","GODLIKE"
    };


    public PlayerMovement playerMovement;
    void Awake()
    {
        Instance = this;
        Parent.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            SceneManager.LoadScene(0);
        }
        if (!comboActive) return;

        if(!playerMovement.finisherActive)
            ApplyDecay();

        // Kill chain reset
        if (Time.time - lastChainKillTime > killChainResetTime)
        {
            multiKillCount = 0;
            if (enemyKilledText != null)
                enemyKilledText.text = "";
        }
       
    }

    // =========================
    // CORE
    // =========================

    public void StartCombo()
    {
        Parent.SetActive(true);
        comboActive = true;
        multiplier = 1;
        requiredXP = CalculateXP(multiplier);
        currentXP = requiredXP * 0.5f; // small buffer

        UpdateMultiplierUI();
        UpdateXPUI();
        UpdateComboName();
    }

    public void AddXP(float amount, string reason = "")
    {
        if (!comboActive)
            StartCombo();

        currentXP += amount;

        LeanTween.delayedCall(0.5f, () => { AddScore(Mathf.RoundToInt(amount)); });
       
        // 🔥 HANDLE OVERFLOW PROPERLY
        while (currentXP >= requiredXP)
        {
            currentXP -= requiredXP; // carry extra XP forward
            LevelUp();
        }

        if (xpSlider != null)
        {
            Transform t = xpSlider.transform;

            t.DOKill();

            t.localScale = Vector3.one;

            t.DOScale(1.15f, 0.1f)
             .SetEase(Ease.OutQuad)
             .OnComplete(() =>
             {
                 t.DOScale(1f, 0.1f).SetEase(Ease.InQuad);
             });
        }
        UpdateXPUI();
    }

    void LevelUp()
    {
        multiplier++;
        requiredXP = CalculateXP(multiplier);


        UpdateMultiplierUI();
        UpdateComboName();
    }

    void LevelDown()
    {
        multiplier--;

        if (multiplier <= 0)
        {
            ResetCombo();
            return;
        }

        requiredXP = CalculateXP(multiplier);
        currentXP = requiredXP; // 🔥 FULL BAR when dropping

        UpdateMultiplierUI();

    }

    void ResetCombo()
    {
        comboActive = false;
        multiplier = 0;
        currentXP = 0;

        if (multiplierImage != null)
            multiplierImage.enabled = false;
        if (xpSlider != null) xpSlider.value = 0;
        if (comboNameText != null) comboNameText.text = "";

        Parent.SetActive(false);
    }

    float CalculateXP(int level)
    {
        return baseXPRequired * Mathf.Pow(level, growthRate);
    }

    // =========================
    // DECAY (UPDATED)
    // =========================

    void ApplyDecay()
    {
        float decayAmount = requiredXP * decayPerSecond * Time.deltaTime;
        currentXP -= decayAmount;

        if (currentXP <= 0)
        {
            LevelDown(); // 🔥 instead of ResetCombo
        }

        UpdateXPUI();
    }

    public void ApplyDamagePenalty()
    {
        if (!comboActive) return;

        currentXP -= fixedDamageXPLoss;

        if (multiplierImage != null)
        {
            multiplierImage.transform.DOKill();
            multiplierImage.transform.DOPunchScale(
                Vector3.one * 0.2f,
                0.25f,
                6,
                1f
            );
        }

        if (currentXP <= 0)
        {
            LevelDown(); // 🔥 downgrade instead of reset
        }

        UpdateXPUI();
    }

    // =========================
    // KILL SYSTEM
    // =========================

    public void RegisterKill(
        EnemyType type,
        bool isEnvironmental = false,
        bool isFinisher = false,
        bool isAirKill = false,
        bool isHeadshot = false
    )
    {
        float xp = 0;

        switch (type)
        {
            case EnemyType.Basic: xp += basicEnemyXP; break;
            case EnemyType.Strong: xp += strongEnemyXP; break;
            case EnemyType.Boss: xp += bossXP; break;
        }

        if (Time.time - lastChainKillTime <= killChainResetTime)
            multiKillCount++;
        else
            multiKillCount = 1;

        lastChainKillTime = Time.time;

        UpdateKillChainUI();

        if (multiKillCount == 2) xp += multiKillBonus;
        else if (multiKillCount >= 3) xp += bigMultiKillBonus;

        if (isEnvironmental) xp += environmentalBonus;
        if (isFinisher) xp += finisherBonus;
        if (isAirKill) xp += airKillBonus;
        if (isHeadshot) xp += headshotBonus;

        string killText = "";

        LocalizeStringEvent loc = killFeedText.GetComponent<LocalizeStringEvent>();

        if (isHeadshot)
        {
            loc.StringReference.SetReference("Kill Meter", "HEADSHOT");
            loc.StringReference.Arguments = new object[] { headshotBonus };
            loc.RefreshString();

            killText = UnityEngine.Localization.Settings.LocalizationSettings
                .StringDatabase
                .GetLocalizedString("Kill Meter", "HEADSHOT", new object[] { headshotBonus });
        }
        else if (isAirKill)
        {
            loc.StringReference.SetReference("Kill Meter", "AIR_KILL");
            loc.StringReference.Arguments = null;
            loc.RefreshString();

            killText = UnityEngine.Localization.Settings.LocalizationSettings
                .StringDatabase
                .GetLocalizedString("Kill Meter", "AIR_KILL");
        }
        else if (isFinisher)
        {
            loc.StringReference.SetReference("Kill Meter", "FINISHER");
            loc.StringReference.Arguments = null;
            loc.RefreshString();

            killText = UnityEngine.Localization.Settings.LocalizationSettings
                .StringDatabase
                .GetLocalizedString("Kill Meter", "FINISHER");
        }
        else if (isEnvironmental)
        {
            loc.StringReference.SetReference("Kill Meter", "ENVIRONMENTAL");
            loc.StringReference.Arguments = null;
            loc.RefreshString();

            killText = UnityEngine.Localization.Settings.LocalizationSettings
                .StringDatabase
                .GetLocalizedString("Kill Meter", "ENVIRONMENTAL");
        }

        ShowKillFeedDelayed(killText);

        AddXP(xp, killText);
    }

    // =========================
    // UI
    // =========================

    void UpdateMultiplierUI()
    {
        if (multiplierImage == null || multiplierSprites == null)
            return;

        // multiplier 1 → index 0
        // multiplier 2 → index 1
        // multiplier 3 → index 2
        // multiplier 4 → index 3
        int index = multiplier - 1;

        if (index >= 0 && index < multiplierSprites.Length)
        {
            multiplierImage.sprite = multiplierSprites[index];
            multiplierImage.enabled = true;
        }

        multiplierImage.transform.DOKill();
        multiplierImage.transform.localScale = Vector3.one;

        multiplierImage.transform
            .DOScale(multiplierPopScale, multiplierPopDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                multiplierImage.transform
                    .DOScale(1f, multiplierPopDuration)
                    .SetEase(Ease.InBack);
            });
    }

    void UpdateXPUI()
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = requiredXP;

            DOTween.Kill(xpSlider);

            DOTween.To(
                () => xpSlider.value,
                x => xpSlider.value = x,
                currentXP,
                sliderAnimDuration
            ).SetEase(Ease.OutQuad)
             .SetTarget(xpSlider);
        }
    }

    void UpdateComboName()
    {
        if (comboNameText == null) return;

        //int index = Mathf.Clamp(multiplier / 2, 0, comboNames.Length - 1);
        // Replace your old line with this:

        LocalizeStringEvent loc = comboNameText.GetComponent<LocalizeStringEvent>();

        loc.OnUpdateString.RemoveAllListeners();
        loc.OnUpdateString.AddListener((localizedText) =>
        {
            comboNameText.text =
    $"<size=130>{localizedText[0]}</size><space=0px><size=85>{localizedText.Substring(1)}</size>";
        });

        string key = "Combo_" + (multiplier - 1);
        loc.StringReference.SetReference("Kill Meter", key);
        loc.RefreshString();
        RectTransform rect = comboNameText.rectTransform;

        rect.DOKill();

        //// 🔥 Set start position
        //if (comboNameStart != null)
        //    rect.anchoredPosition = comboNameStart.anchoredPosition;

        //rect.localScale = Vector3.zero;
        //rect.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));

        Sequence seq = DOTween.Sequence();

        // 🔥 POP IN
        seq.Append(
            rect.DOScale(1.5f, 0.15f)
            .SetEase(Ease.OutBack)
        );

        //// 🔥 WAIT (NEW)
        //seq.AppendInterval(comboNameWaitBeforeMove);

        //// 🔥 MOVE TO TARGET
        //if (comboNameTarget != null)
        //{
        //    seq.Append(
        //        rect.DOAnchorPos(comboNameTarget.anchoredPosition, comboMoveDuration)
        //        .SetEase(Ease.OutCubic)
        //    );

        //    seq.Join(
        //        rect.DORotate(Vector3.zero, comboMoveDuration)
        //        .SetEase(Ease.OutCubic)
        //    );
        //}

        // 🔥 SNAP BACK SCALE
        seq.Append(
            rect.DOScale(1f, 0.15f)
            .SetEase(Ease.InBack)
        );

        // 🔥 OPTIONAL punch at end (feels great)
        seq.Append(
            rect.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 1f)
        );
    }
    public static string StylizeFirstLetter(string word, int firstSize, int restSize)
    {
        if (string.IsNullOrEmpty(word))
            return "";

        return $"<size={firstSize}>{word[0]}</size><size={restSize}>{word.Substring(1)}</size>";
    }
    void UpdateKillChainUI()
    {
        if (enemyKilledText == null) return;

        LocalizeStringEvent loc =
 enemyKilledText.GetComponent<LocalizeStringEvent>();

        loc.StringReference.Arguments = new object[]
        {
    multiKillCount
        };

        loc.RefreshString();

        enemyKilledText.transform.DOKill();
        enemyKilledText.transform.localScale = Vector3.one;

        enemyKilledText.transform
            .DOScale(1.4f, 0.2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                enemyKilledText.transform
                    .DOScale(1f, 0.2f)
                    .SetEase(Ease.InBack);
            });
    }

    // =========================
    // DELAYED FEED
    // =========================

    void ShowKillFeedDelayed(string text)
    {
        CancelInvoke(nameof(DelayedKillFeed));
        delayedKillText = text;
        Invoke(nameof(DelayedKillFeed), killFeedDelay);
    }

    void DelayedKillFeed()
    {
        ShowKillFeed(delayedKillText);
    }

    void ShowKillFeed(string text)
    {
        if (killFeedText != null)
        {
            killFeedText.text = text;
             
            killFeedText.transform.DOKill();
            killFeedText.DOKill();

            killFeedText.alpha = 1f;
            killFeedText.transform.localScale = Vector3.zero;

            killFeedText.transform
                .DOScale(1.3f, 0.15f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    killFeedText.transform
                        .DOScale(1f, 0.1f)
                        .SetEase(Ease.InBack);
                });

            Vector3 startPos = killFeedText.transform.localPosition;
            killFeedText.transform.localPosition = startPos - new Vector3(0, 20f, 0);

            killFeedText.transform
                .DOLocalMoveY(startPos.y, 0.3f)
                .SetEase(Ease.OutQuad);

            killFeedText.DOFade(0f, 0.5f)
                .SetDelay(killFeedDuration - 0.5f);

            CancelInvoke(nameof(ClearKillFeed));
            Invoke(nameof(ClearKillFeed), killFeedDuration);
        }
    }

    void ClearKillFeed()
    {
        if (killFeedText != null)
            killFeedText.text = "";
    }

    [Header("Combo Name Animation")]
    public RectTransform comboNameStart;
    public RectTransform comboNameTarget;
    public float comboMoveDuration = 0.4f;

    [Header("Combo Name Timing")]
    public float comboNameWaitBeforeMove = 0.25f;


    [Header("Score UI")]
    public TextMeshProUGUI scoreText;
    public float scoreCountDuration = 0.75f;
    public float scorePopScale = 1.3f;
    public float scorePopDuration = 0.15f;

    private int currentScore = 0;
    public void AddScore(float amount)
    {
        if (scoreText == null)
            return;

        DOTween.Kill(scoreText);
        scoreText.transform.DOKill();

        int startScore = currentScore;
        int targetScore = currentScore + Mathf.RoundToInt(amount);

        CultureInfo indianCulture = new CultureInfo("en-IN");

        // Start at normal scale
        scoreText.transform.localScale = Vector3.one;

        scoreText.text = currentScore.ToString("N0", indianCulture);

        //// POP UP and stay popped while counting
        scoreText.transform
            .DOScale(scorePopScale, scorePopDuration)
            .SetEase(Ease.OutBack);

        // Count the score
        DOTween.To(
            () => startScore,
            value =>
            {
                currentScore = value;

                scoreText.text = currentScore.ToString(
                    "N0",
                    indianCulture
                );
            },
            targetScore,
            scoreCountDuration
        )
        .SetEase(Ease.Linear)
        .SetTarget(scoreText)
        .OnComplete(() =>
        {
            // Counting has finished → POP DOWN
            scoreText.transform
                .DOScale(1f, scorePopDuration)
                .SetEase(Ease.InBack);
        });
    }
}

public enum EnemyType
{
    Basic,
    Strong,
    Boss
}