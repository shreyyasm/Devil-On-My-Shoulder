using System.Collections;
using UnityEngine;
using TMPro;

public class FontLooper : MonoBehaviour
{
    public TextMeshProUGUI targetText;
    public TMP_FontAsset[] fonts;
    public float switchInterval = 0.5f; // seconds between switches

    private Coroutine loopCoroutine;
    public bool mainMenu;
    public TMP_FontAsset defaultFont;

    private void Start()
    {
    
    }
    void OnEnable()
    {
        if(mainMenu) return;

        if (targetText != null && fonts.Length > 0)
            loopCoroutine = StartCoroutine(SwitchFontsLoop());
    }

    void OnDisable()
    {
        if (loopCoroutine != null)
            StopCoroutine(loopCoroutine);
    }

    IEnumerator SwitchFontsLoop()
    {
        int index = 0;

        while (true)
        {
            if (targetText == null || fonts.Length == 0)
                yield break;

            targetText.font = fonts[index];
            targetText.ForceMeshUpdate(); // refresh text rendering

            index = (index + 1) % fonts.Length; // loop around
            yield return new WaitForSeconds(switchInterval);
        }
    }
    public void OnHoverEnter()
    {
        if (targetText != null && fonts.Length > 0)
            loopCoroutine = StartCoroutine(SwitchFontsLoop());
    }
    public void OnHoverExit()
    {
        if (loopCoroutine != null)
            StopCoroutine(loopCoroutine);

        targetText.font = defaultFont;
    }
}
