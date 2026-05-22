using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RedVisionToggle : MonoBehaviour
{
    public static RedVisionToggle Instance;
    [Header("Renderer Feature")]
    public ScriptableRendererFeature redVisionFeature;

    [Header("Duration")]
    public float activeTime = 3f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        if (redVisionFeature != null)
            redVisionFeature.SetActive(false);
    }

    void Update()
    {
       
    }

    public void EnableRedVisionForSeconds(float seconds)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(RedVisionRoutine(seconds));
    }

    IEnumerator RedVisionRoutine(float seconds)
    {
        redVisionFeature.SetActive(true);

        yield return new WaitForSeconds(seconds);

        redVisionFeature.SetActive(false);

        currentRoutine = null;
    }

    public void EnableRedVision()
    {
        redVisionFeature.SetActive(true);
    }

    public void DisableRedVision()
    {
        redVisionFeature.SetActive(false);
    }
}