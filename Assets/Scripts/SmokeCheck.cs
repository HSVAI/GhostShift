#if UNITY_STANDALONE_LINUX
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GhostShift
{
    // Opt-in checks run against the actual built Linux player, never in the APK.
    public sealed class SmokeCheck : MonoBehaviour
    {
        private GhostShiftGame game;
        private int errors;
        private const string Output = "Builds/QA";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--ghostshift-smoke") >= 0)
                new GameObject("SmokeCheck").AddComponent<SmokeCheck>();
        }
        private void OnEnable() { Application.logMessageReceived += OnLog; }
        private void OnDisable() { Application.logMessageReceived -= OnLog; }
        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Output);
            yield return new WaitForSeconds(1);
            game = FindObjectOfType<GhostShiftGame>();
            Check(game != null, "Game exists");
            Check(Get<Text>("title").text == "GHOST\nSHIFT", "Menu text");
            yield return Screenshot("01-menu.png");
            Call("StartRun");
            yield return new WaitForSeconds(.4f);
            Check(Get<bool>("playing"), "Run started");
            Check(Get<float>("elapsed") > 0, "Clock advances");
            int lane = Get<int>("lane");
            Call("Shift");
            Check(Get<int>("lane") == -lane, "Tap changes lane");
            yield return new WaitForSeconds(.15f);
            yield return Screenshot("02-playing.png");
            Call("SetPaused", true);
            float elapsed = Get<float>("elapsed");
            yield return new WaitForSeconds(.25f);
            Check(Get<float>("elapsed") == elapsed, "Paused clock freezes");
            yield return Screenshot("03-paused.png");
            Call("SetPaused", false);
            yield return new WaitForSeconds(.1f);
            Check(Get<float>("elapsed") > elapsed, "Resume advances clock");
            Call("EndRun");
            Check(!Get<bool>("playing"), "Death ends run");
            Check(Get<Text>("title").text == "SIGNAL\nLOST", "Death panel visible");
            yield return Screenshot("04-gameover.png");
            Call("StartRun");
            Check(Get<float>("elapsed") == 0, "Restart resets clock");
            Check(Get<RunScore>("tally").Value == 0, "Restart resets score");
            yield return new WaitForSeconds(.1f);
            Check(errors == 0, "No player errors");
            Call("HandleBack");
            Check(Get<float>("backExitDeadline") > Time.unscaledTime, "First back arms exit confirmation");
            Check(Get<Text>("exitHint").gameObject.activeSelf, "Exit confirmation is visible");
            Debug.Log("GHOSTSHIFT_PLAYER_SMOKE_PASS");
            Call("HandleBack");
            yield return new WaitForSeconds(1f);
            Check(false, "Back exits the app");
        }
        private IEnumerator Screenshot(string name)
        {
            yield return new WaitForEndOfFrame();
            var path = Path.GetFullPath(Path.Combine(Output, name));
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSeconds(.2f);
            Check(File.Exists(path), "Screenshot saved " + name);
        }
        private T Get<T>(string name) { return (T)typeof(GhostShiftGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game); }
        private void Call(string name, params object[] args) { typeof(GhostShiftGame).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, args); }
        private static void Check(bool ok, string message)
        {
            if (ok) { Debug.Log("SMOKE_OK " + message); return; }
            Debug.LogError("SMOKE_FAILED " + message); Application.Quit(1);
            throw new Exception(message);
        }
    }
}
#endif
