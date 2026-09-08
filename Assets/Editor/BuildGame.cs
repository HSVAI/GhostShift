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
            VerifyRules();
            EnsureScene();
            ConfigureCommon();
            PlayerSettings.productName = "Ghost Shift";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ghtnql.ghostshift");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
            PlayerSettings.Android.bundleVersionCode = 2;
            // First playable is a local-test APK, not a store release.
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = "/opt/Unity/2022.3.62f3/Data/PlaybackEngines/AndroidPlayer/SDK";
            UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = "/opt/Unity/2022.3.62f3/Data/PlaybackEngines/AndroidPlayer/NDK";
            UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = "/opt/Unity/2022.3.62f3/Data/PlaybackEngines/AndroidPlayer/OpenJDK";
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Android/GhostShift.apk", BuildTarget.Android, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Android build failed: " + report.summary.result);
            Debug.Log("GHOSTSHIFT_ANDROID_BUILD_OK bytes=" + report.summary.totalSize);
        }

        public static void BuildLinux()
        {
            VerifyRules();
            EnsureScene();
            ConfigureCommon();
            // Headless QA has no window manager to focus the player window.
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 450;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            Directory.CreateDirectory("Builds/Linux");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Linux/GhostShift.x86_64", BuildTarget.StandaloneLinux64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Linux build failed: " + report.summary.result);
        }

        public static void BuildWebGL()
        {
            VerifyRules();
            EnsureScene();
            ConfigureCommon();
            PlayerSettings.defaultScreenWidth = 450;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            Directory.CreateDirectory("Builds/WebGL");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/WebGL", BuildTarget.WebGL, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("WebGL build failed: " + report.summary.result);
            TuneWebGLTemplate("Builds/WebGL", "Ghost Shift — Web Play", "#090c16");
            Debug.Log("GHOSTSHIFT_WEBGL_BUILD_OK bytes=" + report.summary.totalSize);
        }

        private static void TuneWebGLTemplate(string outputPath, string title, string background)
        {
            string indexPath = Path.Combine(outputPath, "index.html");
            string html = File.ReadAllText(indexPath)
                .Replace("<html lang=\"en-us\">", "<html lang=\"ko\">")
                .Replace("<title>Unity WebGL Player | Ghost Shift</title>", "<title>" + title + "</title>")
                .Replace("// config.autoSyncPersistentDataPath = true;", "config.autoSyncPersistentDataPath = true;")
                .Replace("// config.devicePixelRatio = 1;", "config.devicePixelRatio = 1;");
            File.WriteAllText(indexPath, html);

            string stylePath = Path.Combine(outputPath, "TemplateData/style.css");
            string css = File.ReadAllText(stylePath);
            const string marker = "/* RESPONSIVE_PORTRAIT */";
            if (!css.Contains(marker))
            {
                css += "\n" + marker + "\nhtml,body{width:100%;height:100%;overflow:hidden;background:" + background + ";}" +
                    "#unity-container,#unity-container.unity-desktop,#unity-container.unity-mobile{position:fixed;inset:0;left:0;top:0;transform:none;width:min(100vw,56.25vh);height:min(100vh,177.7778vw);margin:auto;}" +
                    "#unity-canvas,#unity-canvas.unity-mobile{display:block;width:100%!important;height:100%!important;}#unity-footer{display:none;}\n";
                File.WriteAllText(stylePath, css);
            }
        }

        private static void ConfigureCommon()
        {
            PlayerSettings.companyName = "ghtnql";
            PlayerSettings.productName = "Ghost Shift";
            PlayerSettings.bundleVersion = "0.1.1";
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        }

        public static void VerifyRules()
        {
            var score = new RunScore();
            Assert(score.Value == 0, "Initial score");
            score.Advance(1); Assert(score.Value == 10, "Survival score");
            score.CatchEcho(); Assert(score.Value == 35, "Echo bonus");
            score.Advance(1); Assert(score.Value == 45, "Bonus persists after next frame");
            score.CatchEcho(); score.CatchEcho(); Assert(score.Value == 95, "Multiple catches");
            score.Advance(-10); Assert(score.Value == 95, "No negative time");
            Assert(RunScore.CrossesPlayer(-300, -650, -475, 70), "Swept collision prevents tunneling");
            Assert(RunScore.CrossesPlayer(-405, -405, -475, 70), "Collision boundary");
            Assert(!RunScore.CrossesPlayer(500, 400, -475, 70), "Hazard above player");
            Assert(!RunScore.CrossesPlayer(-600, -700, -475, 70), "Hazard below player");
            Debug.Log("GHOSTSHIFT_RULE_TESTS_PASS assertions=10");
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition) throw new System.Exception("Rule test failed: " + name);
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
