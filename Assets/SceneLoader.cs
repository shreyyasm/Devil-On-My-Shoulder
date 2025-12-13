using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "HeavyScene";
    [SerializeField] private Slider progressBar;   // optional
    public GameObject SettingsMenu;
    public GameObject Buttons;


    [SerializeField] private float fakeLoadTime = 3f;   // How long to keep the slider filling before scene loads

    public void OpenSettings()
    {
        SettingsMenu.SetActive(true);
    }
    public void CloseSettings()
    {
        SettingsMenu.SetActive(false);
    }
    public GameObject keyboardControls;
    public GameObject gamepadControls;
    private void Awake()
    {
        if (!GamepadCursor.Instance.isGamepadConnected)
        {
            keyboardControls.SetActive(true);
            gamepadControls.SetActive(false);
        }
           
        else
        {
            gamepadControls.SetActive(true);
            keyboardControls.SetActive(false);
        }
            
    }
    private void Start()
    {
        GamepadCursor.Instance.DisableCursor();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; 

        if (progressBar != null)
            progressBar.value = 0f;

        StartCoroutine(LoadSceneWithDelay());
    }
    public bool tutorial;
    private IEnumerator LoadSceneWithDelay()
    {
        // Start loading scene in background but don't activate yet
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        float elapsed = 0f;

        // Simulate loading time for the player to read / wait
        while (elapsed < fakeLoadTime)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(elapsed / fakeLoadTime);
            }
            yield return null;
        }

        // Finish loading bar if actual scene loading is slower
        while (op.progress < 0.9f)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(op.progress / 0.9f);
            }
            yield return null;
        }

        // Activate the scene after the delay
        op.allowSceneActivation = true;
    }
}
