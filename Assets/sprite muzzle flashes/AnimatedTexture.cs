using UnityEngine;
using System.Collections;

public class AnimatedTexture : MonoBehaviour
{
    public float fps = 30.0f;
    public Texture2D[] frames;

    private int frameIndex;
    private MeshRenderer rendererMy;
    private Coroutine playOnceCoroutine;

    void Start()
    {
        rendererMy = GetComponent<MeshRenderer>();
        rendererMy.enabled = false; // disable initially
        //NextFrame();
        //InvokeRepeating("NextFrame", 1 / fps, 1 / fps);
    }

    void NextFrame()
    {
        if (!rendererMy.enabled) return; // skip if disabled
        rendererMy.sharedMaterial.SetTexture("_MainTex", frames[frameIndex]);
        frameIndex = (frameIndex + 1) % frames.Length;
    }

    /// <summary>
    /// Play animation once from start to end, disabling renderer before and after
    /// </summary>
    public void PlayOnce()
    {
        if (playOnceCoroutine != null)
            StopCoroutine(playOnceCoroutine);

        playOnceCoroutine = StartCoroutine(PlayOnceCoroutine());
    }

    private IEnumerator PlayOnceCoroutine()
    {
        rendererMy.enabled = true; // enable renderer

        for (int i = 0; i < frames.Length; i++)
        {
            rendererMy.sharedMaterial.SetTexture("_MainTex", frames[i]);
            yield return new WaitForSeconds(1 / fps);
            Debug.Log("work");
        }

        rendererMy.enabled = false; // disable renderer after finishing
    }
}
