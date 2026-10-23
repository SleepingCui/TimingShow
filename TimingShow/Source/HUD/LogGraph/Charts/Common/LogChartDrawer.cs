using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public abstract class LogChartDrawer : MaskableGraphic
    {
        protected static readonly Color TipBgColor = new Color(0f, 0f, 0f, 0.90f);
        protected static readonly Color TipTextColor = new Color(0.95f, 0.96f, 0.98f, 1f);

        private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();

        protected bool Initialized;
        protected TMP_FontAsset Font;
        protected float UiScale = 1.25f;
        protected float FontSize = 13f;
        protected float LastWidth = -1f;
        protected float LastHeight = -1f;
        protected bool TextDirty = true;
        protected Rect PlotRect;
        protected bool HasPlot;

        protected static Settings LogSettings { get { return ModContext.Settings; } }

        protected static float Scale
        {
            get
            {
                Settings settings = LogSettings;
                return settings != null ? Mathf.Clamp(settings.LogGraph_UIScale, 0.75f, 3f) : 1.25f;
            }
        }

        protected Color BgColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_BgColor : new Color(0f, 0f, 0f, 0.60f); } }
        protected Color GridColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_GridColor : new Color(1f, 1f, 1f, 0.16f); } }
        protected Color PointColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_PointColor : new Color(0.30f, 0.76f, 1f, 1f); } }
        protected Color AxisTextColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_AxisTextColor : new Color(0.80f, 0.80f, 0.80f, 1f); } }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal virtual void Initialize(TMP_FontAsset font)
        {
            if (Initialized)
            {
                SetFont(font);
                return;
            }

            Initialized = true;
            Font = font;
            CreateTextComponents();
            ApplyFonts();
            InvalidateLayout();
        }

        protected abstract void CreateTextComponents();

        internal virtual void SetFont(TMP_FontAsset font)
        {
            if (Font == font) return;
            Font = font;
            ApplyFonts();
            InvalidateLayout();
        }

        internal virtual void ReloadAppearance()
        {
            ApplyFonts();
            InvalidateLayout();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InvalidateLayout();
        }

        protected virtual void Update()
        {
            if (!Initialized || !isActiveAndEnabled) return;

            if (EnsureLayout()) SetVerticesDirty();

            if (TextDirty)
            {
                TextDirty = false;
                ApplyTextLayout(LastWidth, LastHeight);
            }

            UpdateInteraction();
        }

        protected virtual void UpdateInteraction()
        {
        }

        protected bool EnsureLayout()
        {
            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            if (rect.width == LastWidth && rect.height == LastHeight) return false;

            LastWidth = rect.width;
            LastHeight = rect.height;
            UiScale = Scale;
            FontSize = Mathf.Clamp(Mathf.Round(12f * UiScale), 8f, 40f);
            ComputeLayout(rect.width, rect.height);
            TextDirty = true;
            return true;
        }

        protected abstract void ComputeLayout(float width, float height);

        protected abstract void ApplyTextLayout(float width, float height);

        protected void InvalidateLayout()
        {
            LastWidth = -1f;
            LastHeight = -1f;
            TextDirty = true;
            SetVerticesDirty();
        }

        protected void ApplyFonts()
        {
            TMP_FontAsset target = Font != null ? Font : TMP_Settings.defaultFontAsset;
            for (int i = 0; i < _texts.Count; i++)
            {
                TextMeshProUGUI text = _texts[i];
                if (text == null || target == null || text.font == target) continue;
                text.font = target;
                text.SetVerticesDirty();
                text.SetLayoutDirty();
            }
        }

        protected TextMeshProUGUI CreateText(string name, TextAlignmentOptions alignment, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = Font != null ? Font : TMP_Settings.defaultFontAsset;
            text.fontSize = FontSize;
            text.alignment = alignment;
            text.color = AxisTextColor;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = pivot;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(10f, FontSize * 1.6f);

            _texts.Add(text);
            return text;
        }

        protected static void SetActive(TextMeshProUGUI text, bool active)
        {
            if (text == null) return;
            if (text.gameObject.activeSelf != active) text.gameObject.SetActive(active);
        }

        protected static void SetText(TextMeshProUGUI text, string content)
        {
            if (text == null) return;
            if (text.text != content) text.text = content;
        }

        protected static void Place(TextMeshProUGUI text, Vector2 position, Vector2 size, Vector2 pivot)
        {
            if (text == null) return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        protected static float EstimateTextWidth(string content, float fontSize)
        {
            if (string.IsNullOrEmpty(content)) return 0f;
            return content.Length * fontSize * 0.62f;
        }

        protected static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        protected static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        protected static void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            if (!(max.x > min.x) || !(max.y > min.y)) return;
            if (!IsFinite(min) || !IsFinite(max)) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        protected static void DrawTriangleQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c) || !IsFinite(d)) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(a.x, a.y), color, Vector2.zero);
            vh.AddVert(new Vector3(b.x, b.y), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x, c.y), color, Vector2.zero);
            vh.AddVert(new Vector3(d.x, d.y), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        protected static void DrawSegment(VertexHelper vh, Vector2 from, Vector2 to, float halfWidth, Color color)
        {
            if (!IsFinite(from) || !IsFinite(to)) return;
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (!(length > 1e-4f)) return;

            Vector2 normal = new Vector2(-delta.y, delta.x) / length * halfWidth;
            if (!IsFinite(normal)) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(from.x - normal.x, from.y - normal.y), color, Vector2.zero);
            vh.AddVert(new Vector3(from.x + normal.x, from.y + normal.y), color, Vector2.zero);
            vh.AddVert(new Vector3(to.x + normal.x, to.y + normal.y), color, Vector2.zero);
            vh.AddVert(new Vector3(to.x - normal.x, to.y - normal.y), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        protected static void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color color, int segments)
        {
            if (!(radius > 0.01f)) return;
            if (segments < 6) segments = 6;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(center.x, center.y), color, Vector2.zero);
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vh.AddVert(new Vector3(center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius), color, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int a = start + 1 + i;
                int b = start + 1 + ((i + 1) % segments);
                vh.AddTriangle(start, a, b);
            }
        }

        protected void DrawDashedVertical(VertexHelper vh, float x, float dash, float gap, float halfWidth, Color color)
        {
            if (!IsFinite(x)) return;
            if (!(dash > 0.1f))
            {
                DrawSegment(vh, new Vector2(x, PlotRect.yMin), new Vector2(x, PlotRect.yMax), halfWidth, color);
                return;
            }
            if (!(gap > 0f)) gap = dash;

            float y = PlotRect.yMin;
            float end = PlotRect.yMax;
            int guard = 0;
            while (y < end && guard++ < 2000)
            {
                float stop = Mathf.Min(y + dash, end);
                DrawSegment(vh, new Vector2(x, y), new Vector2(x, stop), halfWidth, color);
                y = stop + gap;
            }
        }

        protected void DrawDashedHorizontal(VertexHelper vh, float y, float dash, float gap, float halfWidth, Color color)
        {
            if (!IsFinite(y)) return;
            if (!(dash > 0.1f))
            {
                DrawSegment(vh, new Vector2(PlotRect.xMin, y), new Vector2(PlotRect.xMax, y), halfWidth, color);
                return;
            }
            if (!(gap > 0f)) gap = dash;

            float x = PlotRect.xMin;
            float end = PlotRect.xMax;
            int guard = 0;
            while (x < end && guard++ < 2000)
            {
                float stop = Mathf.Min(x + dash, end);
                DrawSegment(vh, new Vector2(x, y), new Vector2(stop, y), halfWidth, color);
                x = stop + gap;
            }
        }

        protected static string ColorToHex(Color color)
        {
            Color32 c = color;
            return c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
        }
    }
}
