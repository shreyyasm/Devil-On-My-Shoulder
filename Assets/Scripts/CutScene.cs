using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutScene : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public bool haschanged = false;
    private LTDescr delayedCall;

    private void Awake()
    {

        string url = Application.streamingAssetsPath + "/CutScene.mp4";
        videoPlayer.url = url;
        videoPlayer.Play();
    }
    // Start is called before the first frame update
    void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 2)
        {
            // store the LeanTween call so we can cancel it later
            delayedCall = LeanTween.delayedCall(22f, () =>
            {
                if (!haschanged) // only load if not already changed
                {
                    SceneManager.LoadScene(3);
                }
            });
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (SceneManager.GetActiveScene().buildIndex == 2 && Input.GetKey(KeyCode.E) && !haschanged)
        {
            haschanged = true;

            // cancel delayed call
            if (delayedCall != null)
                LeanTween.cancel(delayedCall.id);

            SceneManager.LoadScene(3);
        }
    }
}
