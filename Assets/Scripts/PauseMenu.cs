// PauseMenu.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

public class PauseMenu : MonoBehaviour
{
    private const string GameplaySceneName = "Mad Dash Game";
    private const string MainMenuSceneName = "Doms Main Menu";
    private const string RestartBlackScreenName = "Restart Loading Black Screen";
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;   // Assign your Pause panel root here

    [Header("Options")]
    [SerializeField] private bool pauseAudio = true;  // Sets AudioListener.pause
    [SerializeField] private bool lockCursorOnResume = true;

    public GameObject settingsMenu;
    public static bool IsPaused { get; private set; }
    private static bool sceneTransitionInProgress;

    private static void RemoveRestartBlackScreenAfterSceneLoad(Scene scene, LoadSceneMode mode)
    {
        // The pause/settings hierarchy can be inactive when a scene starts,
        // so its Awake method is not a reliable place to remove the overlay.
        // SceneManager always raises this event for the replacement gameplay
        // scene, whether the action came from the score canvas or Settings.
        SceneManager.sceneLoaded -= RemoveRestartBlackScreenAfterSceneLoad;
        DestroyAllRestartBlackScreens();
    }

    void Awake()
    {
        // Remove the transition overlay created by the previous scene after
        // the new scene has finished loading.
        DestroyAllRestartBlackScreens();

        // This static guard survives scene loads. Clear it when the new
        // scene's menu is created so later restart/menu actions are accepted.
        sceneTransitionInProgress = false;
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

            if (buttonName == "new game")
                button.onClick.AddListener(NewGame);
            else if (buttonName == "restart")
                button.onClick.AddListener(RestartGame);
            else if (buttonName == "main menu")
                button.onClick.AddListener(ReturnToMainMenu);
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

    private void Update()
    {
        if (pausePanel == null)
            return;

        // Settings Generator can show/hide the container directly. Keep the
        // pause state synchronized with the visible panel so music cannot
        // continue when settings are opened through that path.
        bool settingsVisible = pausePanel.activeInHierarchy;

        if (settingsVisible && !IsPaused)
            Pause();
        else if (!settingsVisible && IsPaused)
            Resume();
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
       
        PauseAllBackgroundMusicForMenu();
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
        Time.fixedDeltaTime = 0.02f;
        if (pauseAudio) AudioListener.pause = false;
        ResumeAllBackgroundMusicFromMenu();

        if (pausePanel) pausePanel.SetActive(false);

        // Restore your preferred gameplay cursor state
        Cursor.visible = !lockCursorOnResume;
        Cursor.lockState = lockCursorOnResume ? CursorLockMode.Locked : CursorLockMode.None;
        FindFirstObjectByType<Character>()?.SetCursorLockedForGameplay(lockCursorOnResume);

        GamepadCursor.Instance.DisableCursor();
    }

    private static void PauseAllBackgroundMusicForMenu()
    {
        BackgroundMusic[] musicControllers = FindObjectsByType<BackgroundMusic>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (musicControllers.Length == 0 && BackgroundMusic.Instance != null)
            BackgroundMusic.Instance.PauseForPauseMenu();

        foreach (BackgroundMusic musicController in musicControllers)
        {
            if (musicController != null)
                musicController.PauseForPauseMenu();
        }
    }

    private static void ResumeAllBackgroundMusicFromMenu()
    {
        BackgroundMusic[] musicControllers = FindObjectsByType<BackgroundMusic>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (musicControllers.Length == 0 && BackgroundMusic.Instance != null)
            BackgroundMusic.Instance.ResumeFromPauseMenu();

        foreach (BackgroundMusic musicController in musicControllers)
        {
            if (musicController != null)
                musicController.ResumeFromPauseMenu();
        }
    }

    // --- Optional buttons you can hook from the UI ---

    // Called by a "Resume" button
    //public void OnResumeButton() => Resume();

    public void NewGame()
    {
        StartNewGameSession();
    }

    public static void StartNewGameSession()
    {
        if (sceneTransitionInProgress)
            return;

        // New Game is a fresh run, not a retry. Reset the persistent
        // procedural singleton before destroying DontDestroyOnLoad objects so
        // the next Mad Dash scene starts on ActiveSet 0.
        ProceduralGenerationManager.ResetForNewGame();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        ReloadGameplaySession();
    }

    public void RestartGame()
    {
        RestartGameplaySession();
    }

    public static void RestartGameplaySession()
    {
        if (sceneTransitionInProgress)
            return;

        // A retry is a completely fresh Mad Dash run. The generator persists
        // across scene loads, so merely loading the scene would keep the
        // current procedural set (and its prepared level) alive.
        ProceduralGenerationManager.ResetForNewGame();
        ReloadGameplaySession();
    }

    // Kept for existing inspector events that already call MainMenu.
    public void MainMenu() => ReturnToMainMenu();

    public void ReturnToMainMenu()
    {
        ReturnToMainMenuSession();
    }

    public static void ReturnToMainMenuSession()
    {
        if (sceneTransitionInProgress)
            return;

        sceneTransitionInProgress = true;
        PrepareForSceneTransition();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private static void ReloadGameplaySession()
    {
        sceneTransitionInProgress = true;
        PrepareForSceneTransition();
        DestroyDontDestroyOnLoadObjects();
        CreateRestartBlackScreen();

        // Register before loading so this cleanup cannot be missed when the
        // gameplay scene loads synchronously.
        SceneManager.sceneLoaded -= RemoveRestartBlackScreenAfterSceneLoad;
        SceneManager.sceneLoaded += RemoveRestartBlackScreenAfterSceneLoad;

        SceneManager.LoadScene(GameplaySceneName);
    }

    private static void CreateRestartBlackScreen()
    {
        DestroyAllRestartBlackScreens();

        GameObject overlay = new GameObject(
            RestartBlackScreenName,
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(Image));

        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        Image image = overlay.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        DontDestroyOnLoad(overlay);
    }

    private static void DestroyAllRestartBlackScreens()
    {
        // Destroy() is deferred until the end of the frame. During a rapid
        // new-game/restart transition that can briefly leave two canvases
        // with this name alive. GameObject.Find returns only one of them,
        // which was leaving the newer black screen over the reloaded game.
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate == null || candidate.name != RestartBlackScreenName ||
                !candidate.scene.IsValid())
                continue;

            candidate.SetActive(false);
            Destroy(candidate);
        }
    }

    private static void PrepareForSceneTransition()
    {
        PlayerHealth.Instance?.ResetDeathPresentationForRestart();

        IsPaused = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;

        PauseMenu pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (pauseMenu != null && pauseMenu.pausePanel != null)
            pauseMenu.pausePanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private static void DestroyDontDestroyOnLoadObjects()
    {
        Scene persistentScene = SceneManager.GetSceneByName("DontDestroyOnLoad");
        if (!persistentScene.IsValid())
            return;

        foreach (GameObject root in persistentScene.GetRootGameObjects())
            Destroy(root);
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
