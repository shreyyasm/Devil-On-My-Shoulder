using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JockyBat : MonoBehaviour
{
    public GameObject WeaponPrefab;

    public ParticleSystem PunchBlood;
    public ParticleSystem KnifeBlood;
    public ParticleSystem KickBlood;
    public ParticleSystem KickBlood2;

    public EnemyManager EnemyManager;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void ActivateBatCollider()
    {
        WeaponPrefab.SetActive(true);
    }
    public void DisableBatCollider()
    {
        WeaponPrefab.SetActive(false);
    }
    public void ScreenEffect()
    {
        //ImpactFrameEffect.Instance.GoBlack();
        //LeanTween.delayedCall(0.05f, () => { ImpactFrameEffect.Instance.GoColor(); });
        //CameraShakeFinisher.Instance.Shake(0.05f, 0.02f);

    }

    public void SpawnBloodPunch()
    {
        PunchBlood.Play();
        DoTimeSlow();
        RedVisionToggle.Instance.EnableRedVisionForSeconds(0.1f);
        GetComponent<HitstopShake>().DoShake();
    }
    public void SpawnBloodKnife()
    {
        KnifeBlood.Play();
        DoTimeSlow();
        RedVisionToggle.Instance.EnableRedVisionForSeconds(0.1f);
        GetComponent<HitstopShake>().DoShake();
    }
    public void SpawnBloodKick()
    {
        KickBlood.Play();
        DoTimeSlow();
        RedVisionToggle.Instance.EnableRedVisionForSeconds(0.1f);
        GetComponent<HitstopShake>().DoShake();
    }
    public void SpawnBloodKick2()
    {
        KickBlood2.Play();
        DoTimeSlow();
        RedVisionToggle.Instance.EnableRedVisionForSeconds(0.1f);
        GetComponent<HitstopShake>().DoShake();
    }
    public void SpawnBloodDouble()
    {
        PunchBlood.Play();
        KnifeBlood.Play();
        DoTimeSlow();
        RedVisionToggle.Instance.EnableRedVisionForSeconds(0.1f);
        GetComponent<HitstopShake>().DoShake();



    }
    [Header("Time Slow Settings")]
    [SerializeField] private float slowTimeScale = 0.15f;
    [SerializeField] private float slowDuration = 1f;

    private Coroutine timeRoutine;
    public void DoTimeSlow()
    {
        if (timeRoutine != null)
            StopCoroutine(timeRoutine);

        timeRoutine = StartCoroutine(TimeSlowCoroutine());
    }

    private IEnumerator TimeSlowCoroutine()
    {
        // Apply slow motion
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // IMPORTANT: use unscaled time
        yield return new WaitForSecondsRealtime(slowDuration);

        // Restore normal time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        //Flash(0.2f, 0f, 0.1f);
        timeRoutine = null;
    }
    public void AttackSound()
    {
        EnemyManager.AttackPlaySoundEffect();
    }
    public void FinisherDie() 
    {
        LeanTween.delayedCall(0.3f, () => { EnemyManager.GetComponent<EnemyHealth>().Die(false, true, false, false); });


    }
    public void KickDie()
    {
        LeanTween.delayedCall(0.3f, () => { EnemyManager.GetComponent<EnemyHealth>().Die(false, false, true, false); });


    }
}
