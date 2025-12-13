using InfimaGames.LowPolyShooterPack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpSelect : MonoBehaviour
{
    public static PowerUpSelect Instance;

    [Header("Abilities")]
    public bool doubleDamage;
    public bool EnemyStill;
    public bool playerShielded;
    public bool batteryDrain;

    [Header("Double Damage")]
    public Projectile bullet;

    //[Header("Double Damage")]

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

    }
    private void Update()
    {

    }


    public void SelectPower(int index)     
    {
        if (index == 0)
        {
            bullet.bulletDamage = bullet.bulletDamage * 2;
            doubleDamage = true;
        }
        if(index == 1)
        {
            EnemyStill = true;
        }
        if(index == 2)
        {
            playerShielded = true;
        }
        if(index == 3)
        {
            batteryDrain = true;
        }

        //ScoreManager.Instance.abilityTimerSlider.gameObject.SetActive(true);
        //ScoreManager.Instance.abilityActive = true;
        //ScoreManager.Instance.abilityTimer = ScoreManager.Instance.abilityTime;
        //ScoreManager.Instance.abilityTimerSlider.maxValue = ScoreManager.Instance.abilityTime;
        ScoreManager.Instance.EndChoice(true);

        string sfxKey = $"PowerUp/Ability On";
        SFXManager.Instance.PlaySFX(sfxKey, 1f);
    }
    public void DeSelectPower()
    {

        bullet.bulletDamage = 60 ;
        doubleDamage = false;   
        EnemyStill = false;
        playerShielded = false;
        batteryDrain = false;
        ScoreManager.Instance.abilityActive = false;
    }
}
