using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class LevelCompletion : MonoBehaviour
{
    public static LevelCompletion Instance;
    public GameObject gate;
    public Transform openGateTransform;
    public float moveSpeed = 2f; // Adjust the speed of the gate movement

    public TextMeshProUGUI gamlaText;

    private bool isMoving = false;
    public int numberOfFools;
    public int numberOfCoins;

    private void Awake()
    {

        if (Instance == null)
            Instance = this;
    }
    void Update()
    {
        
    }
    public bool levelTwo;
    public void CoinCheck()
    {
        numberOfCoins++;
       // gamlaText.text = numberOfFools + " X 4";
        if (numberOfCoins == 25 && !isMoving && !levelTwo)
        {
            KillStreak.Instance.CompletedLevel();
            StartCoroutine(MoveGate());
        }
    }
    public void GamlaCheck()
    {
        numberOfFools++;
        gamlaText.text = numberOfFools + " X 4";
        if (numberOfFools == 4 && !isMoving)
        {
            KillStreak.Instance.CompletedLevel();
            StartCoroutine(MoveGate());
        }
    }
    public void Win()
    {
        KillStreak.Instance.CompletedLevel();
        LeanTween.delayedCall(5f, () => { SceneManager.LoadScene(11); });
       
       // StartCoroutine(loadMainMenu());
    }
    public AudioClip clip;
    IEnumerator loadMainMenu()
    {
        yield return new WaitForSeconds(3f);
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 1f);
        int currentscene = SceneManager.GetActiveScene().buildIndex;
        yield return new WaitForSeconds(5f);
        SceneManager.UnloadSceneAsync(currentscene);
        SceneManager.LoadScene(0);
    }
    IEnumerator MoveGate()
    {
        isMoving = true;

        Vector3 startPosition = gate.transform.position;
        Quaternion startRotation = gate.transform.rotation;

        float journeyLength = Vector3.Distance(startPosition, openGateTransform.position);
        float startTime = Time.time;

        while (Vector3.Distance(gate.transform.position, openGateTransform.position) > 0.01f)
        {
            float distanceCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;
            gate.transform.position = Vector3.Lerp(startPosition, openGateTransform.position, fractionOfJourney);
            gate.transform.rotation = Quaternion.Lerp(startRotation, openGateTransform.rotation, fractionOfJourney);
            yield return null;
        }

        gate.transform.position = openGateTransform.position;
        gate.transform.rotation = openGateTransform.rotation;

        isMoving = false;
    }

}
