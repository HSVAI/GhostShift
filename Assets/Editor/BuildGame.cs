using System.IO;
using GhostShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GhostShift.Editor
{
    public static class BuildGame
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        public static void BuildAndroid()
        {
            EnsureScene();
            PlayerSettings.productName = "Ghost Shift";
            PlayerSettings.applicationIdentifier = "com.ghtnql.ghostshift";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Android/GhostShift.apk", BuildTarget.Android, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Android build failed: " + report.summary.result);
        }

        public static void BuildLinux()
        {
            EnsureScene();
            Directory.CreateDirectory("Builds/Linux");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Linux/GhostShift.x86_64", BuildTarget.StandaloneLinux64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Linux build failed: " + report.summary.result);
        }

        private static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Ghost Shift");
            game.AddComponent<GhostShiftGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
    }
}
