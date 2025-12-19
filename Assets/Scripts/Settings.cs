using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
public class Settings : MonoBehaviour
{
    
    public GameObject mainScreen;   
    public GameObject optionsCanvas;
    public GameObject BattlePassCanvas;

    public Slider volumeSlider;
    public Slider senstivitySlider;

    public PlayerMovement player;
    bool opendOptions;
    // Start is called before the first frame update
    void Start()
    {
       


        if (PlayerPrefs.GetInt("VolumeSetter") == 0)
        {
           

            PlayerPrefs.SetFloat("Senstivity", 50);
            if (volumeSlider != null)
            {
                volumeSlider.value = 1;
                senstivitySlider.value = 50f;
                AudioListener.volume = volumeSlider.value;


            }
            //if(player != null)
            //    player.sensitivity = 50f;

        }

        else
        {
            if(volumeSlider != null)
            {
                volumeSlider.value = PlayerPrefs.GetFloat("Volume");
                senstivitySlider.value = PlayerPrefs.GetFloat("Senstivity");
            }



            //if (player != null)
            //    player.sensitivity = PlayerPrefs.GetFloat("Senstivity");
            
        }

        if (volumeSlider != null)
            AudioListener.volume = volumeSlider.value;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(!opendOptions)
            {
                optionsCanvas.SetActive(true);
                //Cursor.visible = true;
                opendOptions = true;
                //Time.timeScale = 0;
            }
                
            else
            {
                optionsCanvas.SetActive(false);
                //Cursor.visible = false;
                opendOptions = false;
                //Time.timeScale = 1;
            }
                
        }
        if(!opendOptions)
        {
            //Cursor.visible = false;
        }
            
    }
    public void PlayButton()
    {
        // 
        SceneManager.LoadScene(2);
       
    }
    public void OpenBattleButton()
    {
        // 
        BattlePassCanvas.SetActive(true);

    }
    public void CloseBattlePass()
    {
        BattlePassCanvas.SetActive(false);
    }
    public void DiscoverGames()
    {
        WhalePassAPI.instance.RedirectPlayer_Rewards();
    }
    public void CloseOptions()
    {
        optionsCanvas.SetActive(false);
    }
    public void OpenOptions()
    {
        //mainScreen.SetActive(false);
        optionsCanvas.SetActive(true);
    }
    public void BackToMainScreen()
    {
        //mainScreen.SetActive(true);
        optionsCanvas.SetActive(false);
    }
    public void OnApplicationQuit()
    {
        Application.Quit();
    }
 
    public void SliderVolume()
    {

        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        AudioListener.volume = volumeSlider.value;
        PlayerPrefs.SetInt("VolumeSetter", 1);
    }
    public void SliderSensitivty()
    {

        PlayerPrefs.SetFloat("Senstivity", senstivitySlider.value);
        
        //if(player != null)
        //    player.sensitivity = PlayerPrefs.GetFloat("Senstivity");

    }  
   
    public void MainMenu(int index)
    {
       
        SceneManager.LoadScene(index);
    }
    public void Retry(int index)
    {
        int currentGameIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.UnloadSceneAsync(currentGameIndex);
        SceneManager.LoadScene(index);
    }
    public GameObject challenges;
    public void OpenChallenges()
    {
        challenges.SetActive(true);
    }
    public void CloseChallenges()
    {
        challenges.SetActive(false);
    }
}
