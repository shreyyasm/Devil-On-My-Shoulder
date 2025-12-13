using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Abilties : MonoBehaviour
{
    public GameObject frontCard;
    public GameObject backCard;
    public float flipSpeed = 10f; // Adjust for desired smoothness

    public bool isFlipped = false;
    public bool isFlipping = false;
    public bool isFlippingBack = false;
    public float rotationY = 0f;
    public float targetRotation;
    float targetRotationFront = 90;
    float targetRotationBack = 0;
    bool halfWay;


    public AudioSource audioSource;
    public AudioClip FlipSFX;
 
    private void Update()
    {
        if (isFlipping)
        {

            rotationY = Mathf.MoveTowards(rotationY, targetRotation, Time.unscaledDeltaTime * flipSpeed);

            if (isFlipped)
            {
                if (!halfWay)
                {
                    targetRotation = targetRotationFront;
                    frontCard.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
                }
                if (rotationY >= 88) // Switch cards at halfway
                {
                    halfWay = true;

                }
                if (halfWay)
                {
                    frontCard.SetActive(false);
                    backCard.SetActive(true);
                    targetRotation = targetRotationBack;
                    backCard.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
                }
            }


            //// Stop flipping once close to the target
            //if (Mathf.Abs(rotationY - targetRotation) < 1f)
            //{
            //    isFlipping = false;
            //}
        }
        else
        {
            if (isFlippingBack)
            {
                rotationY = Mathf.MoveTowards(rotationY, targetRotation, Time.unscaledDeltaTime * flipSpeed);
                if (!halfWay)
                {
                    targetRotation = targetRotationFront;
                    backCard.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
                }
                if (rotationY >= 88) // Switch cards at halfway
                {
                    halfWay = true;

                }
                if (halfWay)
                {
                    frontCard.SetActive(true);
                    backCard.SetActive(false);
                    targetRotation = targetRotationBack;
                    frontCard.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
                }
                //if(rotationY <= 88)
                //{
                //    isFlippingBack = false;
                //}
            }

        }
    }

    public void Entercard()
    {
        if (!isFlipping && !isFlipped)
        {
            ActivateAbility();
            isFlipping = true;
            isFlipped = true;
            isFlippingBack = false;
            halfWay = false;
            audioSource.PlayOneShot(FlipSFX);
            string sfxKey = $"PowerUp/CardFlip";
            SFXManager.Instance.PlaySFX(sfxKey, 1f);
           
        }
    }

    public void ExitCard()
    {
        if (isFlipping && isFlipped)
        {
            //Debug.Log("Leave");
            isFlipping = false;
            isFlippingBack = true;
            isFlipped = false;
            halfWay = false;
            audioSource.PlayOneShot(FlipSFX);
            string sfxKey = $"PowerUp/CardFlip";
            SFXManager.Instance.PlaySFX(sfxKey, 1f);
        }
    }

    [Header("Assign objects in inspector")]
    public List<GameObject> cards;

    /// <summary>
    /// Activates the object at the given index and disables all others.
    /// </summary>
    /// <param name="index">Index of the object to activate</param>
    /// pu
    public List<GameObject> gunCards; // assign in Inspector
    public List<GameObject> abilityCards; // assign in Inspector

    public void ActivateAbility()
    {
        float randomValue = Random.value; // between 0 and 1

        // 40% chance for gun, 60% chance for ability
        if (randomValue < 0.4f && gunCards.Count > 0)
        {
            // Pick a random gun
            int num = Random.Range(0, gunCards.Count);
            ActivateCard(gunCards[num]);
        }
        else if (abilityCards.Count > 0)
        {
            // Pick a random ability
            int num = Random.Range(0, abilityCards.Count);
            ActivateCard(abilityCards[num]);
        }
    }

    private void ActivateCard(GameObject chosen)
    {
        foreach (GameObject c in cards)
        {
            c.SetActive(c == chosen);
        }
    }

}
