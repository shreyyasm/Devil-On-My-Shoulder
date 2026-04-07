using InfimaGames.LowPolyShooterPack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class Pillar : MonoBehaviour
{
    public LensDistortionAnimator LensDistortionAnimator;
    public GameObject Environment;
    public Volume volume;
    private Vignette vignette;
    public GameObject gun;

    public List<GameObject> playerStuff;
    public Character character;
    public Animator GUnMain;
    public GameObject BGBefore;
    public GameObject BGAfter;
    void Start()
    {
        //// Find the Volume in the scene
        //volume = FindObjectOfType<Volume>();

        if (volume != null && volume.profile.TryGet(out vignette))
        {
            Debug.Log("✅ Vignette found in URP volume!");
        }
        else
        {
            Debug.LogWarning("⚠️ No Vignette override found in Volume profile!");
        }

        if (!GamepadCursor.Instance.IsGamepadConnected())
        {
            wCanvas.SetActive(true);
        }
        else
        {
            stickCanvas.SetActive(true);
        }
        GamepadCursor.Instance.DisableCursor();
    }
    public void SetVignetteIntensity(float value)
    {
        if (vignette != null)
        {
            vignette.intensity.Override(value);
        }
    }
    public GameObject wCanvas;
    public GameObject stickCanvas;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.W) && !GamepadCursor.Instance.IsGamepadConnected())
        {
            wCanvas.SetActive(false);
        }
        if (GamepadCursor.Instance.IsGamepadConnected() && Gamepad.current.leftStick.ReadValue().y > 0.8f)
        {
            stickCanvas.SetActive(false);
        }

    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            BGBefore.SetActive(false);
            string sfxKey = $"Main Menu/StartMenu";
            SFXManager.Instance.PlaySFX(sfxKey, 1f);
            GUnMain.enabled = true;
           
            LensDistortionAnimator.enabled = true;
            Environment.SetActive(true);
            LeanTween.delayedCall(0.6f, () =>
            {
                Cursor.lockState = CursorLockMode.None;
                //Cursor.visible = true;
                GamepadCursor.Instance.OnControlsChanged();
                BGAfter.SetActive(true);
            
            });
            SetVignetteIntensity(0.2f);
           
            foreach (GameObject player in playerStuff)
            {
                player.GetComponent<SpriteRenderer>().enabled = true;

            }
            gun.SetActive(false);
            gameObject.SetActive(false);
            character.GetComponent<PlayerMovement>().mainOpen = true;

        }
       

    }
   
}
