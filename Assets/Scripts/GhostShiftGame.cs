using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GhostShift
{
    public sealed class GhostShiftGame : MonoBehaviour
    {
        private const float LaneDistance = 310f;
        private readonly List<Hazard> hazards = new List<Hazard>();
        private readonly List<Ghost> ghosts = new List<Ghost>();
        private RectTransform field;
        private RectTransform player;
        private Text scoreText;
        private Text bestText;
        private Text titleText;
        private Text messageText;
        private Image flash;
        private Sprite sprite;
        private int lane = -1;
        private int score;
        private int best;
        private float elapsed;
        private float spawnTimer;
        private float switchLock;
        private bool playing;

        private readonly Color ink = new Color(0.055f, 0.065f, 0.12f);
        private readonly Color mint = new Color(0.15f, 1f, 0.75f);
        private readonly Color coral = new Color(1f, 0.33f, 0.43f);
        private readonly Color lavender = new Color(0.58f, 0.46f, 1f);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            best = PlayerPrefs.GetInt("GhostShiftBest", 0);
            sprite = MakeSprite();
            BuildView();
            ShowStart();
        }

        private void Update()
        {
            if (!playing)
            {
                if (Pressed()) StartRun();
                return;
            }

            if (Pressed() && switchLock <= 0f) Shift();
            switchLock -= Time.deltaTime;
            elapsed += Time.deltaTime;
            score = Mathf.FloorToInt(elapsed * 10f);
            scoreText.text = score.ToString("0000");
            bestText.text = "BEST " + best.ToString("0000");

            var speed = 620f + Mathf.Min(700f, elapsed * 15f);
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnHazard();
                spawnTimer = Mathf.Max(0.36f, 0.82f - elapsed * 0.008f);
            }
            MoveHazards(speed);
            MoveGhosts();
        }

        private bool Pressed()
        {
            return Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        }

        private void StartRun()
        {
            foreach (var hazard in hazards) Destroy(hazard.view.gameObject);
            foreach (var ghost in ghosts) Destroy(ghost.view.gameObject);
            hazards.Clear(); ghosts.Clear();
            lane = -1; score = 0; elapsed = 0f; spawnTimer = 0.45f; switchLock = 0.15f;
            player.anchoredPosition = new Vector2(lane * LaneDistance, -470f);
            playing = true;
            titleText.gameObject.SetActive(false);
            messageText.gameObject.SetActive(false);
            flash.color = Color.clear;
        }

        private void Shift()
        {
            var oldLane = lane;
            lane *= -1;
            player.anchoredPosition = new Vector2(lane * LaneDistance, -470f);
            switchLock = 0.10f;
            var trail = NewImage("Ghost", mint, new Vector2(92f, 92f));
            trail.rectTransform.anchoredPosition = new Vector2(oldLane * LaneDistance, -470f);
            trail.color = new Color(mint.r, mint.g, mint.b, 0.45f);
            ghosts.Add(new Ghost { view = trail.rectTransform, life = 0.72f, lane = oldLane });
        }

        private void SpawnHazard()
        {
            var targetLane = Random.value > 0.36f ? lane : -lane;
            var image = NewImage("Glitch", elapsed > 25f && Random.value > 0.55f ? lavender : coral, new Vector2(112f, 66f));
            image.rectTransform.anchoredPosition = new Vector2(targetLane * LaneDistance, 900f);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-18f, 18f));
            hazards.Add(new Hazard { view = image.rectTransform, lane = targetLane });
        }

        private void MoveHazards(float speed)
        {
            for (var i = hazards.Count - 1; i >= 0; i--)
            {
                var hazard = hazards[i];
                hazard.view.anchoredPosition += Vector2.down * speed * Time.deltaTime;
                var y = hazard.view.anchoredPosition.y;
                if (Mathf.Abs(y + 470f) < 63f)
                {
                    if (hazard.lane == lane)
                    {
                        EndRun();
                        return;
                    }
                    for (var g = ghosts.Count - 1; g >= 0; g--)
                    {
                        if (ghosts[g].lane != hazard.lane) continue;
                        Destroy(ghosts[g].view.gameObject);
                        ghosts.RemoveAt(g);
                        Destroy(hazard.view.gameObject);
                        hazards.RemoveAt(i);
                        score += 8;
                        break;
                    }
                }
                if (i < hazards.Count && y < -980f)
                {
                    Destroy(hazard.view.gameObject);
                    hazards.RemoveAt(i);
                }
            }
        }

        private void MoveGhosts()
        {
            for (var i = ghosts.Count - 1; i >= 0; i--)
            {
                var ghost = ghosts[i];
                ghost.life -= Time.deltaTime;
                var image = ghost.view.GetComponent<Image>();
                image.color = new Color(mint.r, mint.g, mint.b, Mathf.Clamp01(ghost.life / 0.72f) * 0.45f);
                if (ghost.life <= 0f) { Destroy(ghost.view.gameObject); ghosts.RemoveAt(i); }
            }
        }

        private void EndRun()
        {
            playing = false;
            if (score > best) { best = score; PlayerPrefs.SetInt("GhostShiftBest", best); PlayerPrefs.Save(); }
            flash.color = new Color(coral.r, coral.g, coral.b, 0.18f);
            titleText.text = "SIGNAL LOST";
            messageText.text = "SCORE " + score.ToString("0000") + "\n\nTAP TO SHIFT AGAIN";
            titleText.gameObject.SetActive(true);
            messageText.gameObject.SetActive(true);
        }

        private void ShowStart()
        {
            titleText.text = "GHOST\nSHIFT";
            messageText.text = "TAP TO CHANGE LANES\nYOUR GHOST BLOCKS ONE GLITCH\n\nTAP TO START";
        }

        private void BuildView()
        {
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            field = canvas.GetComponent<RectTransform>();
            NewImage("Background", ink, Vector2.zero, field).rectTransform.anchorMin = Vector2.zero;
            var background = field.GetChild(field.childCount - 1).GetComponent<RectTransform>();
            background.anchorMax = Vector2.one; background.offsetMin = background.offsetMax = Vector2.zero;
            for (var i = -1; i <= 1; i += 2)
            {
                var rail = NewImage("Rail", new Color(0.26f, 0.29f, 0.42f, 0.45f), new Vector2(5f, 1700f));
                rail.rectTransform.anchoredPosition = new Vector2(i * LaneDistance, 0f);
            }
            player = NewImage("Player", mint, new Vector2(102f, 102f)).rectTransform;
            player.anchoredPosition = new Vector2(-LaneDistance, -470f);
            scoreText = NewText("0000", 94, TextAnchor.UpperCenter, mint, new Vector2(700f, 130f), new Vector2(0f, 790f));
            bestText = NewText("BEST 0000", 28, TextAnchor.UpperCenter, Color.white, new Vector2(400f, 60f), new Vector2(0f, 695f));
            titleText = NewText("", 120, TextAnchor.MiddleCenter, Color.white, new Vector2(900f, 310f), new Vector2(0f, 185f));
            titleText.fontStyle = FontStyle.Bold;
            messageText = NewText("", 35, TextAnchor.MiddleCenter, new Color(0.77f, 0.8f, 0.92f), new Vector2(900f, 300f), new Vector2(0f, -135f));
            flash = NewImage("Flash", Color.clear, Vector2.zero).GetComponent<Image>();
            flash.rectTransform.anchorMin = Vector2.zero; flash.rectTransform.anchorMax = Vector2.one;
            flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;
        }

        private Image NewImage(string name, Color color, Vector2 size, RectTransform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.color = color;
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent == null ? field : parent, false); rect.sizeDelta = size;
            return image;
        }

        private Text NewText(string value, int size, TextAnchor alignment, Color color, Vector2 dimensions, Vector2 position)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); text.text = value;
            text.fontSize = size; text.alignment = alignment; text.color = color; text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(field, false); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            return text;
        }

        private static Sprite MakeSprite()
        {
            var texture = new Texture2D(2, 2); texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
        }

        private sealed class Hazard { public RectTransform view; public int lane; }
        private sealed class Ghost { public RectTransform view; public float life; public int lane; }
    }
}
