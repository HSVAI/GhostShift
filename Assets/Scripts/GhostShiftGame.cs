using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GhostShift
{
    // Native UI geometry and synthesized sound; no network or downloaded assets.
    public sealed class GhostShiftGame : MonoBehaviour
    {
        private const float LaneX = 255f, PlayerY = -475f, GhostLife = .65f;
        private readonly Color ink = new Color(.035f, .047f, .085f);
        private readonly Color mint = new Color(.22f, 1f, .78f);
        private readonly Color coral = new Color(1f, .29f, .4f);
        private readonly Color dim = new Color(.42f, .49f, .64f);
        private readonly List<Hazard> hazards = new List<Hazard>();
        private readonly List<Echo> echoes = new List<Echo>();
        private readonly List<Spark> sparks = new List<Spark>();
        private RectTransform root, world, player, panel, soundButton;
        private Text scoreLabel, bestLabel, waveLabel, comboLabel, title, description, action, soundLabel, exitHint;
        private Image glow;
        private Font font;
        private AudioSource audioSource;
        private AudioClip shiftSound, breakSound, deathSound;
        private RunScore tally = new RunScore();
        private int lane = -1, best, blocks, chain;
        private float elapsed, spawnTimer, shiftLock, menuLock, feedbackLife, backExitDeadline;
        private bool playing, paused, muted;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            best = PlayerPrefs.GetInt("GhostShiftBest", 0);
            muted = PlayerPrefs.GetInt("GhostShiftMuted", 0) == 1;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            audioSource = gameObject.AddComponent<AudioSource>();
            gameObject.AddComponent<AudioListener>();
            shiftSound = Tone(520, 880, .08f);
            breakSound = Tone(880, 1480, .13f);
            deathSound = Tone(190, 45, .35f);
            BuildView();
            Menu("GHOST\nSHIFT", "ONE TAP. TWO LANES.\n\nDodge the falling glitches.\nYour echo catches one for +25.", "TAP TO CONNECT");
            RefreshScore();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .1f);
            menuLock -= Time.unscaledDeltaTime;
            if (backExitDeadline > 0 && Time.unscaledTime > backExitDeadline)
            {
                backExitDeadline = 0;
                if (exitHint != null) exitHint.gameObject.SetActive(false);
            }
            AnimateSparks(dt);
            bool pressed = Pressed();
            if (pressed && RectTransformUtility.RectangleContainsScreenPoint(soundButton, Pointer()))
            {
                muted = !muted;
                PlayerPrefs.SetInt("GhostShiftMuted", muted ? 1 : 0);
                PlayerPrefs.Save();
                soundLabel.text = muted ? "SFX OFF" : "SFX ON";
                pressed = false;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) { HandleBack(); return; }
            if (paused) { if (pressed && menuLock <= 0) SetPaused(false); return; }
            if (!playing)
            {
                action.color = Color.Lerp(mint, Color.white, .25f + .25f * Mathf.Sin(Time.unscaledTime * 3));
                if (pressed && menuLock <= 0) StartRun();
                return;
            }
            shiftLock -= dt;
            if (pressed && shiftLock <= 0) Shift();
            elapsed += dt;
            tally.Advance(dt);
            AnimateEchoes(dt);
            spawnTimer -= dt;
            if (spawnTimer <= 0) { SpawnHazard(); spawnTimer = Mathf.Max(.4f, .94f - elapsed * .007f); }
            MoveHazards((590f + Mathf.Min(650f, elapsed * 11f)) * dt);
            if (!playing) return;
            feedbackLife -= dt;
            comboLabel.color = new Color(mint.r, mint.g, mint.b, Mathf.Clamp01(feedbackLife * 2));
            glow.color = new Color(mint.r, mint.g, mint.b, .10f + .06f * Mathf.Sin(Time.time * 5));
            RefreshScore();
        }

        private static bool Pressed()
        {
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) ||
                   (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        }
        private static Vector2 Pointer() { return Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition; }

        private void StartRun()
        {
            foreach (var h in hazards) Destroy(h.view.gameObject);
            foreach (var e in echoes) Destroy(e.image.gameObject);
            foreach (var s in sparks) Destroy(s.image.gameObject);
            hazards.Clear(); echoes.Clear(); sparks.Clear();
            lane = -1; elapsed = 0; blocks = 0; chain = 0; tally = new RunScore();
            spawnTimer = .55f; shiftLock = .15f; feedbackLife = 0;
            player.anchoredPosition = new Vector2(lane * LaneX, PlayerY);
            player.GetComponent<Image>().color = mint;
            playing = true; paused = false;
            panel.gameObject.SetActive(false);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            RefreshScore(); Play(shiftSound);
            Debug.Log("GHOSTSHIFT_RUN_STARTED");
        }

        private void Shift()
        {
            // One echo per lane, so rapid tapping cannot stack shields.
            for (int i = echoes.Count - 1; i >= 0; i--)
                if (echoes[i].lane == lane) { Destroy(echoes[i].image.gameObject); echoes.RemoveAt(i); }
            var image = Box("Echo", new Vector2(78, 78), new Vector2(lane * LaneX, PlayerY), mint, world);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            echoes.Add(new Echo { image = image, lane = lane, life = GhostLife });
            Burst(player.anchoredPosition, mint, 5);
            lane = -lane; player.anchoredPosition = new Vector2(lane * LaneX, PlayerY);
            shiftLock = .14f; Play(shiftSound);
        }

        private void SpawnHazard()
        {
            int target = Random.value < .64f ? lane : -lane;
            var image = Box("Glitch", new Vector2(126, 55), new Vector2(target * LaneX, 830), coral, world);
            Box("Core", new Vector2(100, 5), Vector2.zero, new Color(1, .8f, .85f), image.rectTransform);
            hazards.Add(new Hazard { view = image.rectTransform, lane = target });
        }

        private void MoveHazards(float distance)
        {
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                var h = hazards[i];
                float previous = h.view.anchoredPosition.y;
                h.view.anchoredPosition += Vector2.down * distance;
                float y = h.view.anchoredPosition.y;
                if (RunScore.CrossesPlayer(previous, y, PlayerY, 70))
                {
                    if (h.lane == lane) { EndRun(); return; }
                    bool caught = false;
                    for (int e = echoes.Count - 1; e >= 0; e--)
                    {
                        if (echoes[e].lane != h.lane) continue;
                        Destroy(echoes[e].image.gameObject); echoes.RemoveAt(e);
                        Burst(h.view.anchoredPosition, mint, 12);
                        Destroy(h.view.gameObject); hazards.RemoveAt(i);
                        blocks++; chain++; tally.CatchEcho();
                        comboLabel.text = "ECHO +25" + (chain > 1 ? "   /   " + chain + " CHAIN" : "");
                        feedbackLife = 1.25f; Play(breakSound); caught = true; break;
                    }
                    if (caught) continue;
                }
                if (y < -900) { chain = 0; Destroy(h.view.gameObject); hazards.RemoveAt(i); }
            }
        }

        private void AnimateEchoes(float dt)
        {
            for (int i = echoes.Count - 1; i >= 0; i--)
            {
                var echo = echoes[i]; echo.life -= dt;
                echo.image.color = new Color(mint.r, mint.g, mint.b, Mathf.Max(0, echo.life / GhostLife) * .38f);
                if (echo.life <= 0) { Destroy(echo.image.gameObject); echoes.RemoveAt(i); }
            }
        }

        private void EndRun()
        {
            playing = false;
            bool record = tally.Value > best;
            if (record) { best = tally.Value; PlayerPrefs.SetInt("GhostShiftBest", best); PlayerPrefs.Save(); }
            player.GetComponent<Image>().color = coral;
            Burst(player.anchoredPosition, coral, 18); Play(deathSound); RefreshScore();
            Menu("SIGNAL\nLOST", (record ? "NEW PERSONAL BEST\n" : "RUN COMPLETE\n") +
                 "\n" + tally.Value.ToString("0000") + " POINTS\n" + blocks + " ECHO CATCHES  /  " + Mathf.FloorToInt(elapsed) + " SECONDS", "TAP TO RECONNECT");
            menuLock = .65f; Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Debug.Log("GHOSTSHIFT_RUN_ENDED score=" + tally.Value + " echoes=" + blocks);
        }

        private void SetPaused(bool value)
        {
            if (!playing) return;
            paused = value;
            if (value) { Menu("SIGNAL\nPAUSED", "Your run is safe.\nTap when you are ready.", "TAP TO RESUME"); menuLock = .3f; }
            else panel.gameObject.SetActive(false);
            Screen.sleepTimeout = value ? SleepTimeout.SystemSetting : SleepTimeout.NeverSleep;
            Debug.Log(value ? "GHOSTSHIFT_PAUSED" : "GHOSTSHIFT_RESUMED");
        }
        private void HandleBack()
        {
            if (backExitDeadline <= 0 || Time.unscaledTime > backExitDeadline)
            {
                backExitDeadline = Time.unscaledTime + 2f;
                if (exitHint != null) exitHint.gameObject.SetActive(true);
                Debug.Log("GHOSTSHIFT_BACK_ARMED");
                return;
            }
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Debug.Log("GHOSTSHIFT_BACK_EXIT");
            Application.Quit();
        }
        private void OnApplicationPause(bool value) { if (value) SetPaused(true); }
        private void OnApplicationFocus(bool value) { if (!value) SetPaused(true); }
        private void RefreshScore()
        {
            scoreLabel.text = tally.Value.ToString("0000"); bestLabel.text = "BEST  " + best.ToString("0000");
            waveLabel.text = "SECTOR " + (1 + Mathf.FloorToInt(elapsed / 15)).ToString("00");
        }
        private void Menu(string heading, string body, string button)
        {
            panel.gameObject.SetActive(true); title.text = heading; description.text = body; action.text = button;
        }

        private void BuildView()
        {
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = canvas.GetComponent<RectTransform>();
            Stretch(Box("Background", Vector2.zero, Vector2.zero, ink, root).rectTransform);
            world = new GameObject("Playfield", typeof(RectTransform)).GetComponent<RectTransform>();
            world.SetParent(root, false); world.sizeDelta = new Vector2(1080, 1920);
            for (int x = -1; x <= 1; x += 2)
            {
                Box("Lane", new Vector2(182, 1710), new Vector2(x * LaneX, 0), new Color(.065f, .085f, .13f), world);
                Box("Rail", new Vector2(2, 1710), new Vector2(x * LaneX, 0), new Color(.14f, .22f, .29f), world);
            }
            for (int y = -780; y <= 780; y += 130)
                Box("Grid", new Vector2(810, 1), new Vector2(0, y), new Color(.09f, .12f, .18f), world);
            Box("ShiftLine", new Vector2(780, 2), new Vector2(0, PlayerY), new Color(.14f, .32f, .32f), world);
            Label("SWITCH ZONE", 20, dim, new Vector2(600, 40), new Vector2(0, PlayerY - 115), world);
            player = Box("Player", new Vector2(78, 78), new Vector2(-LaneX, PlayerY), mint, world).rectTransform;
            player.localRotation = Quaternion.Euler(0, 0, 45);
            glow = Box("Glow", new Vector2(115, 115), Vector2.zero, new Color(mint.r, mint.g, mint.b, .15f), player);
            Box("Eye", new Vector2(21, 21), Vector2.zero, ink, player);
            // Content expands its margins on tall phones. HUD stays inside the notch/gesture areas.
            Label("G H O S T  /  S H I F T", 24, dim, new Vector2(550, 50), new Vector2(-165, 840), root);
            soundLabel = Label(muted ? "SFX OFF" : "SFX ON", 23, mint, new Vector2(190, 100), new Vector2(355, 840), root);
            soundButton = soundLabel.rectTransform;
            scoreLabel = Label("0000", 108, Color.white, new Vector2(700, 135), new Vector2(0, 700), root);
            bestLabel = Label("", 25, dim, new Vector2(350, 50), new Vector2(-245, 605), root);
            waveLabel = Label("", 25, mint, new Vector2(350, 50), new Vector2(245, 605), root);
            comboLabel = Label("", 30, mint, new Vector2(900, 55), new Vector2(0, -280), root);
            Label("TAP ANYWHERE TO SHIFT", 24, dim, new Vector2(850, 60), new Vector2(0, -805), root);
            exitHint = Label("PRESS BACK AGAIN TO EXIT", 24, coral, new Vector2(850, 60), new Vector2(0, -700), root);
            exitHint.gameObject.SetActive(false);
            panel = Box("Menu", new Vector2(940, 1070), new Vector2(0, 30), new Color(ink.r, ink.g, ink.b, .96f), root).rectTransform;
            Box("Accent", new Vector2(66, 6), new Vector2(0, 450), mint, panel);
            Label("O N E - T A P  S U R V I V A L", 23, mint, new Vector2(850, 60), new Vector2(0, 380), panel);
            title = Label("", 126, Color.white, new Vector2(880, 300), new Vector2(0, 170), panel); title.fontStyle = FontStyle.Bold;
            description = Label("", 31, new Color(.66f, .73f, .82f), new Vector2(830, 290), new Vector2(0, -110), panel);
            Box("ButtonOutline", new Vector2(740, 110), new Vector2(0, -370), new Color(.1f, .22f, .24f), panel);
            action = Label("", 30, mint, new Vector2(740, 110), new Vector2(0, -370), panel);
            Label("OFFLINE  /  NO ADS  /  JUST ONE MORE RUN", 18, dim, new Vector2(900, 40), new Vector2(0, -485), panel);
        }

        private Image Box(string name, Vector2 size, Vector2 position, Color color, RectTransform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            var rect = image.rectTransform; rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
            return image;
        }
        private Text Label(string value, int size, Color color, Vector2 dimensions, Vector2 position, RectTransform parent)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var label = obj.GetComponent<Text>(); label.font = font; label.text = value;
            label.fontSize = size; label.alignment = TextAnchor.MiddleCenter; label.color = color; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = label.rectTransform; rect.SetParent(parent, false); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            return label;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private void Burst(Vector2 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var image = Box("Spark", new Vector2(9, 9), position, color, world);
                sparks.Add(new Spark { image = image, velocity = Random.insideUnitCircle * 430, color = color, life = .45f });
            }
        }
        private void AnimateSparks(float dt)
        {
            if (paused) return;
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i]; s.life -= dt; s.image.rectTransform.anchoredPosition += s.velocity * dt;
                s.image.color = new Color(s.color.r, s.color.g, s.color.b, Mathf.Max(0, s.life / .45f));
                if (s.life <= 0) { Destroy(s.image.gameObject); sparks.RemoveAt(i); }
            }
        }
        private void Play(AudioClip clip) { if (!muted) audioSource.PlayOneShot(clip, .18f); }
        private static AudioClip Tone(float from, float to, float duration)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)]; float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length; phase += 2 * Mathf.PI * Mathf.Lerp(from, to, t) / rate;
                samples[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * (1 - t);
            }
            var clip = AudioClip.Create("Synth", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private sealed class Hazard { public RectTransform view; public int lane; }
        private sealed class Echo { public Image image; public int lane; public float life; }
        private sealed class Spark { public Image image; public Vector2 velocity; public Color color; public float life; }
    }
}
