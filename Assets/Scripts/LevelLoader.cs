using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class LevelLoader : MonoBehaviour
{
    public Slider slider;
    public bool load;
    public GameObject SpaceText;
    public int nextLevelIndex;
    // Start is called before the first frame update
    void Start()
    {
        load = true;
    }

    // Update is called once per frame
    void Update()
    {
        //Cursor.visible = false;
        if(load && slider.value <= 10f)
        {
            slider.value += Time.deltaTime;
        }
        if(slider.value >= 10f)
        {
            SpaceText.SetActive(true);
            if(Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene(nextLevelIndex);
            }
        }
    }
}
