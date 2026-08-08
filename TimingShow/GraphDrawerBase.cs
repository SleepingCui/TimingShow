using UnityEngine;
using UnityEngine.UI;

namespace TimingShow
{
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class GraphDrawerBase : Graphic
    {
        protected abstract bool IsEnabled { get; }
        protected abstract bool ShowGraph { get; }
        protected abstract float Scale { get; }
        protected abstract float Width { get; }
        protected abstract float Height { get; }
        protected abstract float PosX { get; }
        protected abstract float PosY { get; }
        protected abstract Color BgColor { get; }
        protected abstract Color GridColor { get; }
        protected abstract Color LineColor { get; }
        protected abstract int MaxPoints { get; }
        public abstract string GraphName { get; }

        protected abstract void UpdateData();
        protected abstract int GetDataCount();
        protected abstract float GetDataValue(int index);
        protected abstract float GetMinY();
        protected abstract float GetMaxY();

        private Text _titleText;
        private Text _topLabelText;
        private Text _tidLabelText;
        private Text _botLabelText;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            material = defaultMaterial;
            CreateTextComponents();
        }

        private void CreateTextComponents()
        {
            Font font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _titleText = CreateSingleText("TitleText", font, TextAnchor.UpperLeft);
            _topLabelText = CreateSingleText("TopLabel", font, TextAnchor.MiddleRight);
            _tidLabelText = CreateSingleText("MidLabel", font, TextAnchor.MiddleRight);
            _botLabelText = CreateSingleText("BotLabel", font, TextAnchor.MiddleRight);
        }

        private Text CreateSingleText(string name, Font font, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);

            Text t = go.AddComponent<Text>();
            t.font = font;
            t.alignment = alignment;
            t.color = Color.white;
            t.raycastTarget = false;

            RectTransform rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(1f, 0.5f); 

            return t;
        }

        protected virtual void Update()
        {
            if (!IsEnabled || !ShowGraph)
            {
                if (!canvasRenderer.cull) canvasRenderer.cull = true;
                ToggleTexts(false);
                return;
            }

            canvasRenderer.cull = false;
            ToggleTexts(true);
            UpdateTransform();
            UpdateData();
            UpdateTextLayoutAndValues();
            SetVerticesDirty();
        }

        private void ToggleTexts(bool active)
        {
            if (_titleText != null && _titleText.gameObject.activeSelf != active) _titleText.gameObject.SetActive(active);
            if (_topLabelText != null && _topLabelText.gameObject.activeSelf != active) _topLabelText.gameObject.SetActive(active);
            if (_tidLabelText != null && _tidLabelText.gameObject.activeSelf != active) _tidLabelText.gameObject.SetActive(active);
            if (_botLabelText != null && _botLabelText.gameObject.activeSelf != active) _botLabelText.gameObject.SetActive(active);
        }

        private void UpdateTransform()
        {
            RectTransform rect = rectTransform;
            float scale = Mathf.Max(0.01f, Scale);
            float w = Width * scale;
            float h = Height * scale;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(w, h);

            float posX = Screen.width * PosX;
            float posY = Screen.height * (1.0f - PosY);
            rect.anchoredPosition = new Vector2(posX, posY);
        }

        protected virtual void UpdateTextLayoutAndValues()
        {
            float scale = Mathf.Max(0.01f, Scale);
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            float minY = GetMinY();
            float maxY = GetMaxY();
            float midY = (minY + maxY) * 0.5f;

            if (_titleText != null)
            {
                int titleFontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.1f), 10, 100);
                _titleText.fontSize = titleFontSize;
                _titleText.text = GraphName;
                _titleText.color = new Color(1f, 1f, 1f, 102f / 255f);

                RectTransform rt = _titleText.rectTransform;
                rt.pivot = new Vector2(0f, 1f); 
                rt.anchoredPosition = new Vector2(6f * scale, h - 2f * scale);
                rt.sizeDelta = new Vector2(w * 0.8f, h * 0.4f);
            }

            int scaleFontSize = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 8, 32);
            Color scaleColor = new Color(1f, 1f, 1f, 0.8f);
            float leftMargin = -6f * scale;
            SetupLeftScaleText(_topLabelText, $"{maxY:F1}%", new Vector2(leftMargin, h), scaleFontSize, scaleColor);
            SetupLeftScaleText(_tidLabelText, $"{midY:F1}%", new Vector2(leftMargin, h * 0.5f), scaleFontSize, scaleColor);
            SetupLeftScaleText(_botLabelText, $"{minY:F1}%", new Vector2(leftMargin, 0f), scaleFontSize, scaleColor);
        }

        private void SetupLeftScaleText(Text t, string content, Vector2 localPos, int fontSize, Color color)
        {
            if (t == null) return;
            t.fontSize = fontSize;
            t.text = content;
            t.color = color;
            t.alignment = TextAnchor.MiddleRight; 

            RectTransform rt = t.rectTransform;
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = localPos;
            rt.sizeDelta = new Vector2(120f * (fontSize / 12f), 24f * (fontSize / 12f));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            int count = GetDataCount();
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;

            if (w <= 0 || h <= 0) return;

            DrawQuad(vh, Vector2.zero, new Vector2(w, h), BgColor);
            DrawGridLines(vh, w, h);

            if (count < 2) return;

            float minY = GetMinY();
            float maxY = GetMaxY();
            float rangeY = Mathf.Max(0.01f, maxY - minY);
            int maxCapacity = MaxPoints > 0 ? MaxPoints : 250;
            float stepX = w / Mathf.Max(1, maxCapacity - 1);
            float lineWidth = 2.0f * Scale;

            Vector2 prevPoint = Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                float normY = Mathf.Clamp01((GetDataValue(i) - minY) / rangeY);
                Vector2 curPoint = new Vector2(i * stepX, normY * h);
                if (i > 0)
                    DrawSegment(vh, prevPoint, curPoint, lineWidth * 0.5f, LineColor);
                prevPoint = curPoint;
            }
        }

        private void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            int baseIdx = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
        }

        private void DrawSegment(VertexHelper vh, Vector2 p1, Vector2 p2, float halfWidth, Color color)
        {
            Vector2 dir = (p2 - p1).normalized;
            if (dir == Vector2.zero) return;
            Vector2 normal = new Vector2(-dir.y, dir.x) * halfWidth;

            int baseIdx = vh.currentVertCount;
            vh.AddVert(p1 - normal, color, Vector2.zero);
            vh.AddVert(p1 + normal, color, Vector2.zero);
            vh.AddVert(p2 + normal, color, Vector2.zero);
            vh.AddVert(p2 - normal, color, Vector2.zero);
            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
        }

        protected virtual void DrawGridLines(VertexHelper vh, float w, float h)
        {
            float halfGridWidth = 1.0f * Scale * 0.5f;
            DrawSegment(vh, new Vector2(0, 0), new Vector2(w, 0), halfGridWidth, GridColor);
            DrawSegment(vh, new Vector2(0, h * 0.5f), new Vector2(w, h * 0.5f), halfGridWidth, GridColor);
            DrawSegment(vh, new Vector2(0, h), new Vector2(w, h), halfGridWidth, GridColor);
        }
    }
}