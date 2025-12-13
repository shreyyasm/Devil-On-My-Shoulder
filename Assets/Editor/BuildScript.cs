using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildScript
{
    public static void BuildWebGL()
    {
        // Set compression format to disabled
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        
        // Define the scenes to build
        List<string> scenes = new List<string>();
        scenes.Add("Assets/Scenes/LoginScene.unity");
        scenes.Add("Assets/Scenes/LoggedInScene.unity");

        // Build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes.ToArray();
        buildPlayerOptions.locationPathName = "Build/WebGL";
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        // Perform the build
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded!");
        }
        else
        {
            Debug.LogError("Build failed!");
        }
    }
}