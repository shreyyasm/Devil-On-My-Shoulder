// PauseMenu.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;   // Assign your Pause panel root here

    [Header("Options")]
    [SerializeField] private bool pauseAudio = true;  // Sets AudioListener.pause
    [SerializeField] private bool lockCursorOnResume = true;

    public GameObject settingsMenu;
    public static bool IsPaused { get; private set; }

    void Awake()
    {
        ConfigureSettingsButtons();
        if (pausePanel) pausePanel.SetActive(false);
        ResumeInternal(); // ensure clean state
    }

    private void ConfigureSettingsButtons()
    {
        if (pausePanel == null)
            return;

        Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            string buttonName = button.name.ToLowerInvariant();

            // The Settings Generator buttons only apply or hide their own UI.
            // Finish the pause session as well so the camera, shooting, cursor,
            // and time scale are restored without requiring a second Escape.
            if (buttonName.Contains("(apply)") || buttonName.Contains("(close)"))
                button.onClick.AddListener(Resume);
        }
    }
    [Header("Input Action for Pause")]
    public InputActionReference pauseAction; // assign from Input Actions asset (e.g. "UI/Pause")

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
        if (IsPaused)
            ResumeInternal();
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        TogglePause();
    }

    //void Update()
    //{
    //    // Works with old Input Manager and New Input System (both send KeyDown events)
    //    if (Input.GetKeyDown(KeyCode.Escape))
    //    {
    //        TogglePause();
    //    }
    //}

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
       
        Time.timeScale = 0f;
        if (pauseAudio) AudioListener.pause = true;

        if (pausePanel) pausePanel.SetActive(true);

        // Show and free the cursor for menu navigation
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        FindFirstObjectByType<Character>()?.SetCursorLockedForGameplay(false);
        GamepadCursor.Instance.OnControlsChanged();

        // Clear any gameplay selection so pointer clicks, including Apply,
        // always reach the settings UI.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

    }

    public void Resume()
    {
        if (!IsPaused) return;
        ResumeInternal();
    }

    void ResumeInternal()
    {
        IsPaused = false;

        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;

        if (pausePanel) pausePanel.SetActive(false);

        // Restore your preferred gameplay cursor state
        Cursor.visible = !lockCursorOnResume;
        Cursor.lockState = lockCursorOnResume ? CursorLockMode.Locked : CursorLockMode.None;
        FindFirstObjectByType<Character>()?.SetCursorLockedForGameplay(lockCursorOnResume);

        GamepadCursor.Instance.DisableCursor();
    }

    // --- Optional buttons you can hook from the UI ---

    // Called by a "Resume" button
    //public void OnResumeButton() => Resume();

    // Called by a "Restart" button
    public void MainMenu()
    {
        
        if (ScoreManager.Instance != null)
        {
            Destroy(ScoreManager.Instance.gameObject);
        }
      SceneManager.LoadScene(3);

    }
    public void OpenSettings()
    {
        settingsMenu.SetActive(true );
    }
    public void closeSettings()
    {
        Resume();
    }

    
}
