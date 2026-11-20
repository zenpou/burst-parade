using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildParade
{
    [MenuItem("Burst Parade/Build Windows Player")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        const string path = "Assets/Scenes/Parade.unity";
        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var rig = new GameObject("Parade Stage");
            SceneManager.MoveGameObjectToScene(rig, scene);
            rig.AddComponent<Camera>();
            rig.AddComponent<AudioListener>();
            rig.AddComponent<Parade.Stage>();
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        PlayerSettings.companyName = "Parade Arcade";
        PlayerSettings.productName = "BURST PARADE";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        AssetDatabase.SaveAssets();
        var dest = Environment.GetEnvironmentVariable("PARADE_BUILD_PATH") ?? "Build/BurstParade.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        var result = BuildPipeline.BuildPlayer(new[] { path }, dest, BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + result.summary.result);
        Debug.Log("PARADE_BUILD_OK " + result.summary.totalSize);
    }
}
