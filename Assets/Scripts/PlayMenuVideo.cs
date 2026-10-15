using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class PlayMenuVideo : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    private void Awake()
    {

        string url = Application.streamingAssetsPath + "/boomshakalak.mp4";
        videoPlayer.url = url;
        videoPlayer.Play();
    }
    // Start is called before the first frame update
    void Start()
    {

      
    }

    // Update is called once per frame
    void Update()
    {

    }
}
