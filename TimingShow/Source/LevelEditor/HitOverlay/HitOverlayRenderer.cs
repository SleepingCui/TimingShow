using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.LevelEditor
{
    internal sealed class HitOverlayGraphic : MaskableGraphic
    {
        private readonly List<Vector3> _positions = new List<Vector3>(4096);
        private readonly List<Color32> _tints = new List<Color32>(4096);
        private readonly List<int> _indices = new List<int>(8192);

        public override Texture mainTexture
        {
            get { return Texture2D.whiteTexture; }
        }

        internal void Reset()
        {
            _positions.Clear();
            _tints.Clear();
            _indices.Clear();
        }

        internal void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = _positions.Count;
            Color32 tint = color;
            _positions.Add(a);
            _positions.Add(b);
            _positions.Add(c);
            _positions.Add(d);
            _tints.Add(tint);
            _tints.Add(tint);
            _tints.Add(tint);
            _tints.Add(tint);
            _indices.Add(start);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
            _indices.Add(start + 2);
            _indices.Add(start + 3);
            _indices.Add(start);
        }

        internal void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = _positions.Count;
            Color32 tint = color;
            _positions.Add(a);
            _positions.Add(b);
            _positions.Add(c);
            _tints.Add(tint);
            _tints.Add(tint);
            _tints.Add(tint);
            _indices.Add(start);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            for (int i = 0; i < _positions.Count; i++)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vertex.position = _positions[i];
                vertex.color = _tints[i];
                vh.AddVert(vertex);
            }

            for (int i = 0; i < _indices.Count; i += 3)
                vh.AddTriangle(_indices[i], _indices[i + 1], _indices[i + 2]);
        }
    }

    internal sealed class HitOverlayRenderer : MonoBehaviour
    {
        private const int InitialTextPool = 24;
        // TextMeshPro drops every glyph when an Ellipsis label rect is shorter than one line height (1.45 em for the CJK font).
        private const float MinLineHeight = 1.6f;

        private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
        private HitOverlayGraphic _graphic;
        private TMP_FontAsset _font;
        private RectTransform _rect;
        private int _usedTexts;

        internal RectTransform RectTransform
        {
            get { return _rect; }
        }

        internal void Initialize(TMP_FontAsset font)
        {
            _font = font;
            _rect = (RectTransform)transform;
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.zero;
            _rect.pivot = Vector2.zero;
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(Screen.width, Screen.height);

            _graphic = gameObject.AddComponent<HitOverlayGraphic>();
            _graphic.raycastTarget = false;
            _graphic.color = Color.white;

            for (int i = 0; i < InitialTextPool; i++)
                _texts.Add(CreateText());
        }

        internal void Begin()
        {
            if (_graphic == null) return;

            _graphic.Reset();
            _usedTexts = 0;
            if (_rect != null && (_rect.sizeDelta.x < Screen.width - 1f || _rect.sizeDelta.y < Screen.height - 1f))
                _rect.sizeDelta = new Vector2(Screen.width, Screen.height);
        }

        internal void Fill(Rect rect, Color color)
        {
            if (_graphic == null || rect.width <= 0f || rect.height <= 0f) return;

            Vector2 min = rect.min;
            Vector2 max = rect.max;
            _graphic.AddQuad(new Vector2(min.x, min.y), new Vector2(min.x, max.y), new Vector2(max.x, max.y), new Vector2(max.x, min.y), color);
        }

        internal void Line(Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (_graphic == null || thickness <= 0f) return;

            Vector2 direction = end - start;
            float length = direction.magnitude;
            if (length < 0.0001f) return;

            Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f / length);
            _graphic.AddQuad(start - normal, start + normal, end + normal, end - normal, color);
        }

        internal void Outline(Rect rect, Color color, float thickness = 1f)
        {
            if (_graphic == null) return;

            Fill(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            Fill(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.xMin, rect.yMin + thickness, thickness, rect.height - thickness * 2f), color);
            Fill(new Rect(rect.xMax - thickness, rect.yMin + thickness, thickness, rect.height - thickness * 2f), color);
        }

        internal void Triangle(Rect rect, Color color, bool pointsRight)
        {
            if (_graphic == null) return;

            Vector2 center = rect.center;
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;
            if (pointsRight)
            {
                _graphic.AddTriangle(new Vector2(center.x - halfWidth, center.y + halfHeight), new Vector2(center.x - halfWidth, center.y - halfHeight), new Vector2(center.x + halfWidth, center.y), color);
            }
            else
            {
                _graphic.AddTriangle(new Vector2(center.x - halfWidth, center.y + halfHeight), new Vector2(center.x + halfWidth, center.y + halfHeight), new Vector2(center.x, center.y - halfHeight), color);
            }
        }

        internal void Text(Rect rect, string text, Color color, TextAlignmentOptions alignment, float fontSize)
        {
            if (string.IsNullOrEmpty(text)) return;

            TextMeshProUGUI label = TakeText();
            if (label == null) return;

            float height = Mathf.Max(rect.height, fontSize * MinLineHeight);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchoredPosition = new Vector2(rect.xMin, rect.center.y - height * 0.5f);
            labelRect.sizeDelta = new Vector2(rect.width, height);
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            if (label.text != text) label.text = text;
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
        }

        internal void End()
        {
            for (int i = _usedTexts; i < _texts.Count; i++)
            {
                TextMeshProUGUI label = _texts[i];
                if (label != null && label.gameObject.activeSelf) label.gameObject.SetActive(false);
            }

            if (_graphic != null) _graphic.SetVerticesDirty();
        }

        private TextMeshProUGUI TakeText()
        {
            if (_usedTexts < _texts.Count)
            {
                TextMeshProUGUI existing = _texts[_usedTexts];
                if (existing != null)
                {
                    _usedTexts++;
                    return existing;
                }
            }

            TextMeshProUGUI created = CreateText();
            _texts.Add(created);
            _usedTexts++;
            return created;
        }

        private TextMeshProUGUI CreateText()
        {
            GameObject go = new GameObject("HitPanelText", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.richText = false;
            label.fontSize = HitOverlaySkin.FontSize;
            label.color = HitOverlaySkin.TextPrimary;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.font = _font != null ? _font : TMP_Settings.defaultFontAsset;
            return label;
        }
    }
}
