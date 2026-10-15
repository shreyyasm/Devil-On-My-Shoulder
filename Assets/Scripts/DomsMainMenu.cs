using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Owns the Doms Main Menu Play action.</summary>
public sealed class DomsMainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text spaceToStartText;
    [SerializeField] private string gameSceneName = "Mad Dash Game";
    [SerializeField, Min(.1f)] private float promptPulseDuration = 1.1f;

    private void Update()
    {
        if (spaceToStartText != null)
        {
            float phase = Mathf.PingPong(Time.unscaledTime / promptPulseDuration, 1f);
            Color color = spaceToStartText.color;
            color.a = Mathf.Lerp(.12f, 1f, phase);
            spaceToStartText.color = color;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Play();
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
