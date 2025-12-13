using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    public int SceneNextIndex;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LoadLevel()
    {
        int CurrentScence = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(SceneNextIndex);
    }
    private void OnTriggerEnter(Collider other)
    {
        LoadLevel();
    }
}
