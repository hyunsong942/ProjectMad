using UnityEngine;
using UnityEngine.UI;

namespace MadLab
{
    public sealed class LabDamageNumbers : MonoBehaviour
    {
        private const int Capacity = 24;
        private const float Lifetime = 0.85f;
        private sealed class Popup
        {
            public Text label;
            public Vector3 world;
            public float born, offset;
            public Color color;
            public bool active;
        }
        private readonly Popup[] pool = new Popup[Capacity];
        private Canvas canvas;
        private RectTransform canvasRect;
        private Camera worldCamera;
        private Font font;
        private bool fontDirty;
        private int next;
        public int TotalShown { get; private set; }
        public float LastAmount { get; private set; }
        public LabFaction LastFaction { get; private set; }
        public int ActiveCount
        {
            get { int count = 0; foreach (var p in pool) if (p != null && p.active) count++; return count; }
        }

        public void Build(Font sharedFont)
        {
            font = sharedFont;
            var root = new GameObject("DamageNumbersCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasRect = root.GetComponent<RectTransform>();
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("DamageNumber_" + i, typeof(RectTransform), typeof(Text), typeof(Outline));
                go.transform.SetParent(root.transform, false);
                var label = go.GetComponent<Text>();
                label.font = font; label.fontSize = 44; label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
                label.rectTransform.sizeDelta = new Vector2(180, 70);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                var outline = go.GetComponent<Outline>();
                outline.effectColor = new Color(.015f, .02f, .025f, .95f);
                outline.effectDistance = new Vector2(2, -2);
                pool[i] = new Popup { label = label };
                go.SetActive(false);
            }
            LabHealth.DamageApplied += Show;
            Font.textureRebuilt += OnFontRebuilt;
        }
        public void SetVisible(bool visible) { if (canvas != null) canvas.enabled = visible; }
        public void Clear()
        {
            foreach (var p in pool) if (p != null) { p.active = false; p.label.gameObject.SetActive(false); }
            worldCamera = null;
        }
        private void Show(LabHealth target, float amount)
        {
            if (target == null || amount <= 0f || LabGameSession.GameplayBlocked) return;
            var p = pool[next++ % Capacity];
            var body = target.GetComponent<CharacterController>();
            p.world = target.transform.position + Vector3.up * (body != null ? Mathf.Max(2.05f, body.height + .25f) : 2.05f);
            p.born = Time.time;
            p.offset = ((next % 3) - 1) * 28f;
            p.color = target.Faction == LabFaction.Player ? new Color(1f, .36f, .29f) : new Color(1f, .86f, .46f);
            p.label.text = amount.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            p.active = true;
            p.label.gameObject.SetActive(true);
            TotalShown++; LastAmount = amount; LastFaction = target.Faction;
            fontDirty = true;
        }
        private void LateUpdate()
        {
            if (canvas == null) return;
            if (worldCamera == null) worldCamera = Camera.main;
            foreach (var p in pool)
            {
                if (p == null || !p.active) continue;
                float t = (Time.time - p.born) / Lifetime;
                if (t >= 1f) { p.active = false; p.label.gameObject.SetActive(false); continue; }
                if (worldCamera == null) { p.label.enabled = false; continue; }
                Vector3 screen = worldCamera.WorldToScreenPoint(p.world);
                bool visible = screen.z > 0f && screen.x >= 0 && screen.x <= UnityEngine.Screen.width &&
                    screen.y >= 0 && screen.y <= UnityEngine.Screen.height;
                p.label.enabled = visible;
                if (!visible) continue;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
                p.label.rectTransform.anchoredPosition = local + new Vector2(p.offset, 16f + 95f * t);
                p.label.rectTransform.localScale = Vector3.one * (1f + .2f * Mathf.Clamp01(1f - t * 5f));
                var color = p.color; color.a = 1f - Mathf.InverseLerp(.4f, 1f, t);
                p.label.color = color;
                if (fontDirty) p.label.FontTextureChanged();
            }
            fontDirty = false;
        }
        private void OnFontRebuilt(Font rebuilt) { if (rebuilt == font) fontDirty = true; }
        private void OnDestroy()
        {
            LabHealth.DamageApplied -= Show;
            Font.textureRebuilt -= OnFontRebuilt;
        }
    }
}
