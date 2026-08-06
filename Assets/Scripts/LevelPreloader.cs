//using System.Collections;
//using UnityEngine;
//using UnityEngine.SceneManagement;

///// <summary>
///// Loads and generates the next level ADDITIVELY in the background while the player
///// keeps playing the current one, then swaps the player into it once it's fully ready
///// (scene loaded + rooms generated + NavMesh baked) and unloads the old scene.
/////
///// This is only ever kicked off from LevelComplete's OnTriggerEnter - never from
///// Start() - so a single self-reloading scene never triggers a chain of nested loads.
///// </summary>
//public class LevelPreloader : MonoBehaviour
//{
//    [Tooltip("How far away (on X) to generate the next level so it doesn't overlap " +
//             "the currently-playing level, since additive scenes share world space.")]
//    public Vector3 nextLevelOriginOffset = new Vector3(10000f, 0f, 0f);

//    [Tooltip("Optional: a CanvasGroup used to fade to black during the final swap.")]
//    public CanvasGroup fadeOverlay;
//    public float fadeDuration = 0.5f;

//    private string pendingSceneName;
//    private Scene preloadedScene;
//    private ProceduralGenerationManager preloadedGenerator;
//    private bool sceneLoaded = false;
//    private bool generationDone = false;
//    private bool preloadStarted = false;

//    public bool IsNextLevelReady => sceneLoaded && generationDone;

//    /// <summary>Begins loading + generating the next level in the background. Call this ONLY from the trigger.</summary>
//    public void BeginPreload(string sceneName)
//    {
//        if (preloadStarted)
//            return; // guard against double-trigger firing this twice

//        preloadStarted = true;
//        pendingSceneName = sceneName;
//        sceneLoaded = false;
//        generationDone = false;
//        StartCoroutine(PreloadRoutine());
//    }

//    private IEnumerator PreloadRoutine()
//    {
//        AsyncOperation op = SceneManager.LoadSceneAsync(pendingSceneName, LoadSceneMode.Additive);
//        op.allowSceneActivation = false;

//        // Scene assets stream in here while the player keeps playing the current level.
//        while (op.progress < 0.9f)
//            yield return null;

//        op.allowSceneActivation = true;
//        yield return op;

//        preloadedScene = SceneManager.GetSceneByName(pendingSceneName);
//        sceneLoaded = true;

//        // Find the generator in the newly loaded scene, move it away so it doesn't
//        // generate on top of the level currently being played, and wait for it to finish.
//        foreach (GameObject root in preloadedScene.GetRootGameObjects())
//        {
//            var gen = root.GetComponentInChildren<ProceduralGenerationManager>(true);
//            if (gen != null)
//            {
//                preloadedGenerator = gen;
//                gen.transform.position += nextLevelOriginOffset;
//                gen.OnGenerationComplete += () => generationDone = true;
//                break;
//            }
//        }

//        if (preloadedGenerator == null)
//        {
//            Debug.LogWarning($"[LevelPreloader] No ProceduralGenerationManager found in '{pendingSceneName}' - " +
//                               "treating scene load alone as 'ready'.");
//            generationDone = true;
//        }
//    }

//    /// <summary>
//    /// Waits for the preload to finish (if it hasn't already) then swaps the player into
//    /// the new level and unloads the old one.
//    /// </summary>
//    public IEnumerator SwitchToNextLevel(Transform player)
//    {
//        if (!preloadStarted)
//        {
//            Debug.LogError("[LevelPreloader] SwitchToNextLevel called without BeginPreload first.");
//            yield break;
//        }

//        while (!IsNextLevelReady)
//            yield return null;

//        if (fadeOverlay != null)
//            yield return StartCoroutine(Fade(0f, 1f));

//        Scene oldScene = SceneManager.GetActiveScene();

//        // Move the player object into the new scene BEFORE unloading the old one,
//        // otherwise unloading the old scene would destroy the player with it.
//        Transform playerRoot = player.root;
//        SceneManager.MoveGameObjectToScene(playerRoot.gameObject, preloadedScene);

//        playerRoot.position = preloadedGenerator != null
//            ? preloadedGenerator.transform.position
//            : nextLevelOriginOffset;

//        SceneManager.SetActiveScene(preloadedScene);
//        yield return SceneManager.UnloadSceneAsync(oldScene);

//        if (fadeOverlay != null)
//            yield return StartCoroutine(Fade(1f, 0f));
//    }

//    private IEnumerator Fade(float from, float to)
//    {
//        if (fadeOverlay == null)
//            yield break;

//        float t = 0f;
//        fadeOverlay.alpha = from;

//        while (t < fadeDuration)
//        {
//            t += Time.deltaTime;
//            fadeOverlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
//            yield return null;
//        }

//        fadeOverlay.alpha = to;
//    }
//}