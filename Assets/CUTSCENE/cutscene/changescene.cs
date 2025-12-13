using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class changescene : MonoBehaviour
{
    [Header("Scene to Load")]
    [Tooltip("Enter the name of the scene to load.")]
    public string sceneName;

    /// <summary>
    /// Call this method to change the scene.
    /// </summary>
    public void ChangeScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is not specified in the Inspector!");
        }
    }
}