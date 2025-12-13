using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Challenges : MonoBehaviour
{
    public static Challenges instance;
    public TextMeshProUGUI mainText;
    public AudioClip ChallengeSFX;
    public Animator anim;

    //Challenge_One
    public TextMeshProUGUI challenge_OneText;
    public Slider challenge_OneSlider;
    public string challenge_OneID;
    public GameObject challengeOne_Tick;

    //Challenge_Two
    public TextMeshProUGUI challenge_TwoText;
    public Slider challenge_TwoSlider;
    public string challenge_TwoID;
    public GameObject challengeTwo_Tick;

    //Challenge_Three
    public TextMeshProUGUI challenge_ThreeText;
    public Slider challenge_ThreeSlider;
    public string challenge_ThreeID;
    public GameObject challengeThree_Tick;

    //Challenge_Four
    public TextMeshProUGUI challenge_FourText;
    public Slider challenge_FourSlider;
    public string challenge_FourID;
    public GameObject challengeFour_Tick;

    //Challenge_Five
    public TextMeshProUGUI challenge_FiveText;
    public Slider challenge_FiveSlider;
    public string challenge_FiveID;
    public GameObject challengeFive_Tick;

    private void Awake()
    {
       if(instance == null)
            instance = this;
         
    }
    // Start is called before the first frame update
    void Start()
    {
       
        if (challenge_OneText == null)
            return;

        //Challenge_One
        challenge_OneSlider.value = PlayerPrefs.GetInt("Challenge_One");
        challenge_OneText.text = "1. Kill 15 Enemies in a row. ( " + PlayerPrefs.GetInt("Challenge_One") + "/15 )";
        if(PlayerPrefs.GetInt("Challenge_One") >= 15)
            challengeOne_Tick.SetActive(true);

        //Challenge_Two
        challenge_TwoSlider.value = PlayerPrefs.GetInt("Challenge_Two");
        challenge_TwoText.text = "2. Complete a level without getting any hit.";
        if (PlayerPrefs.GetInt("Challenge_Two") == 1)
            challengeTwo_Tick.SetActive(true);

        //Challenge_Three
        challenge_ThreeSlider.value = PlayerPrefs.GetInt("Challenge_Three");
        challenge_ThreeText.text = "3. Kill 30 enemies with invincibilty stars.(" + PlayerPrefs.GetInt("Challenge_Three") + "/30)";
        if (PlayerPrefs.GetInt("Challenge_Three") == 30)
            challengeThree_Tick.SetActive(true);

        //Challenge_Four
        challenge_FourSlider.value = PlayerPrefs.GetInt("Challenge_Four");
        challenge_FourText.text = "4. Complete Level 3 Without Killing any enemy.";
        if (PlayerPrefs.GetInt("Challenge_Four") == 1)
            challengeFour_Tick.SetActive(true);

        //Challenge_Five
        challenge_FiveSlider.value = PlayerPrefs.GetInt("Challenge_Five");
        challenge_FiveText.text = "5.Kill the boss in 1 minute";
        if (PlayerPrefs.GetInt("Challenge_Five") == 1)
            challengeFive_Tick.SetActive(true);


    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Challenge_One(int index)
    {
        if (PlayerPrefs.GetInt("Challenge_One") >= 15)
            return;

        if (challenge_OneText != null)
        {
            challenge_OneSlider.value = index;
            challenge_OneText.text = "Kill 15 Enemies in a row. (" + index + "/15)";
        }
        if(index <= 15)
            PlayerPrefs.SetInt("Challenge_One", index);

        
        if (index == 15)
        {
            mainText.text = "Kill 15 Enemies in a row. (15/15)";
            if (ChallengeSFX != null)
                AudioSource.PlayClipAtPoint(ChallengeSFX, Camera.main.transform.position);
            Debug.Log("Challenge Comepleted");
            if(anim != null)
            {
                anim.SetBool("Play", true);
                LeanTween.delayedCall(6f, () => { anim.SetBool("Play", false); });
            }  
            WhalePassAPI.instance.CompletingChallenge(challenge_OneID);
            if (challenge_OneText != null)
                challengeOne_Tick.SetActive(true);

        }
    }
    public void Challenge_Two(int index)
    {
        if (PlayerPrefs.GetInt("Challenge_Two") == 1)
            return;

        if (challenge_TwoText != null)
        {
            challenge_TwoSlider.value = index;
            challenge_TwoText.text = "2. Complete a level without getting any hit.";
        }

        PlayerPrefs.SetInt("Challenge_Two", index);


        if (index == 1)
        {
            mainText.text = "2. Complete a level without getting any hit.";
            if (ChallengeSFX != null)
                AudioSource.PlayClipAtPoint(ChallengeSFX, Camera.main.transform.position);
            Debug.Log("Challenge Comepleted");
            if (anim != null)
            {
                anim.SetBool("Play", true);
                LeanTween.delayedCall(6f, () => { anim.SetBool("Play", false); });
            }
            WhalePassAPI.instance.CompletingChallenge(challenge_TwoID);
            if (challenge_TwoText != null)
                challengeTwo_Tick.SetActive(true);

        }
    }
    public void Challenge_Three(int index)
    {
        if (PlayerPrefs.GetInt("Challenge_Three") == 30)
            return;

        if (challenge_ThreeText != null)
        {
            challenge_ThreeSlider.value = index;
            challenge_ThreeText.text = "3. Kill 30 enemies with invincibilty stars.(" + index+ "/30)";
        }

        PlayerPrefs.SetInt("Challenge_Three", index);


        if (index == 30)
        {
            mainText.text = "3. Kill 30 enemies with invincibilty stars.(30/30)";
            if (ChallengeSFX != null)
                AudioSource.PlayClipAtPoint(ChallengeSFX, Camera.main.transform.position);
            Debug.Log("Challenge Comepleted");
            if (anim != null)
            {
                anim.SetBool("Play", true);
                LeanTween.delayedCall(6f, () => { anim.SetBool("Play", false); });
            }
            WhalePassAPI.instance.CompletingChallenge(challenge_ThreeID);
            if (challenge_ThreeText != null)
                challengeThree_Tick.SetActive(true);

        }
    }
    public void Challenge_Four(int index)
    {
        if (PlayerPrefs.GetInt("Challenge_Four") == 1)
            return;

        if (challenge_FourText != null)
        {
            challenge_FourSlider.value = index;
            challenge_FourText.text = "4. Complete Level 3 Without Killing any enemy.";
        }

        PlayerPrefs.SetInt("Challenge_Four", index);


        if (index == 1)
        {
            mainText.text = "4. Complete Level 3 Without Killing any enemy.";
            if (ChallengeSFX != null)
                AudioSource.PlayClipAtPoint(ChallengeSFX, Camera.main.transform.position);
            Debug.Log("Challenge Comepleted");
            if (anim != null)
            {
                anim.SetBool("Play", true);
                LeanTween.delayedCall(6f, () => { anim.SetBool("Play", false); });
            }
            WhalePassAPI.instance.CompletingChallenge(challenge_FourID);
            if (challenge_FourText != null)
                challengeFour_Tick.SetActive(true);

        }
    }
    public void Challenge_Five(int index)
    {
        if (PlayerPrefs.GetInt("Challenge_Five") == 1)
            return;

        if (challenge_FiveText != null)
        {
            challenge_FiveSlider.value = index;
            challenge_FiveText.text = "5.Kill the boss in 1 minute";
        }

        PlayerPrefs.SetInt("Challenge_Five", index);


        if (index == 1)
        {
            mainText.text = "5.Kill the boss in 1 minute";
            if(ChallengeSFX != null)
                AudioSource.PlayClipAtPoint(ChallengeSFX,Camera.main.transform.position);
            Debug.Log("Challenge Comepleted");
            if (anim != null)
            {
                anim.SetBool("Play", true);
                LeanTween.delayedCall(6f, () => { anim.SetBool("Play", false); });
            }
            WhalePassAPI.instance.CompletingChallenge(challenge_FiveID);
            if (challenge_FiveText != null)
                challengeFive_Tick.SetActive(true);

        }
    }
}
