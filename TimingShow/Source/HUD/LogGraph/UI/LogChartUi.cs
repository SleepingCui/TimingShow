using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogPillButton : MaskableGraphic,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.28f);
        private static readonly Color ActiveColor = new Color(0.30f, 0.76f, 1f, 0.55f);
        private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.18f);

        private TextMeshProUGUI _label;
        private System.Action _onClick;
        private bool _hovered;
        private bool _active;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal void Initialize(TMP_FontAsset font, System.Action onClick)
        {
            _onClick = onClick;

            GameObject go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            _label = go.AddComponent<TextMeshProUGUI>();
            _label.font = font != null ? font : TMP_Settings.defaultFontAsset;
            _label.fontSize = 12f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = new Color(1f, 1f, 1f, 0.86f);
            _label.raycastTarget = false;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform rect = _label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 1f);
            rect.offsetMax = new Vector2(-8f, -1f);
        }

        internal void SetFont(TMP_FontAsset font)
        {
            if (_label == null || font == null || _label.font == font) return;
            _label.font = font;
        }

        internal void SetLabel(string text)
        {
            if (_label == null) return;
            if (_label.text != text) _label.text = text;
        }

        internal void SetActiveState(bool active)
        {
            if (_active == active) return;
            _active = active;
            SetVerticesDirty();
        }

        internal void SetFontSize(float size)
        {
            if (_label == null) return;
            float clamped = Mathf.Max(8f, size);
            if (!Mathf.Approximately(_label.fontSize, clamped)) _label.fontSize = clamped;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_onClick != null) _onClick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            SetVerticesDirty();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;

            float corner = Mathf.Min(w, h) * LogRoundedRect.CapsuleRatio;
            Color fill = _active ? ActiveColor : (_hovered ? HoverColor : NormalColor);
            LogRoundedRect.DrawFilled(vh, rect, corner, fill, 8);

            float border = Mathf.Max(1f, Mathf.Min(w, h) * 0.06f);
            LogRoundedRect.DrawBorder(vh, rect, corner, border, BorderColor, 8);
        }

        private static void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            if (!(max.x > min.x) || !(max.y > min.y)) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }

    public class LogSlider : MaskableGraphic,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color FillColor = new Color(0.30f, 0.76f, 1f, 0.70f);
        private static readonly Color HandleColor = new Color(0.88f, 0.96f, 1f, 1f);
        private static readonly Color HandleBorderColor = new Color(0.10f, 0.35f, 0.55f, 0.9f);

        private float _min = 5f;
        private float _max = 500f;
        private float _value = 50f;
        private float _step = 5f;
        private System.Action<float> _onChanged;
        private bool _dragging;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal void Initialize(float min, float max, float step, float value, System.Action<float> onChanged)
        {
            _min = min;
            _max = max > min ? max : min + 1f;
            _step = step > 0f ? step : 1f;
            _value = Mathf.Clamp(value, _min, _max);
            _onChanged = onChanged;
            SetVerticesDirty();
        }

        internal void SetValueSilent(float value)
        {
            float clamped = Mathf.Clamp(value, _min, _max);
            if (Mathf.Approximately(_value, clamped)) return;
            _value = clamped;
            SetVerticesDirty();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            _dragging = true;
            UpdateFromPointer(eventData != null ? eventData.position : (Vector2)Input.mousePosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            _dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            UpdateFromPointer(eventData != null ? eventData.position : (Vector2)Input.mousePosition);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }

        private void UpdateFromPointer(Vector2 screenPosition)
        {
            if (rectTransform == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out local))
                return;

            Rect rect = rectTransform.rect;
            float pad = Mathf.Max(4f, rect.height * 0.5f);
            float usable = Mathf.Max(1f, rect.width - pad * 2f);
            float t = Mathf.Clamp01((local.x - rect.xMin - pad) / usable);
            float raw = _min + (_max - _min) * t;
            float snapped = Mathf.Round(raw / _step) * _step;
            snapped = Mathf.Clamp(snapped, _min, _max);
            if (Mathf.Approximately(snapped, _value)) return;

            _value = snapped;
            SetVerticesDirty();
            if (_onChanged != null) _onChanged(_value);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            float width = rect.width;
            float height = rect.height;
            if (!(width > 0f) || !(height > 0f)) return;

            float pad = Mathf.Max(4f, height * 0.5f);
            float usable = Mathf.Max(1f, width - pad * 2f);
            float t = Mathf.Clamp01((_value - _min) / Mathf.Max(1e-4f, _max - _min));
            float centerY = rect.yMin + height * 0.5f;
            float trackHalf = Mathf.Max(1f, height * 0.10f);

            Rect trackRect = new Rect(rect.xMin + pad, centerY - trackHalf, usable, trackHalf * 2f);
            float trackRadius = trackRect.height * LogRoundedRect.CapsuleRatio;
            LogRoundedRect.DrawFilled(vh, trackRect, trackRadius, TrackColor, 8);
            if (t > 0f)
            {
                Rect fillRect = new Rect(trackRect.xMin, trackRect.yMin, usable * t, trackRect.height);
                LogRoundedRect.DrawFilled(vh, fillRect, trackRadius, FillColor, 8);
            }

            float handleRadius = Mathf.Max(3f, height * 0.32f);
            float handleX = rect.xMin + pad + usable * t;
            DrawCircle(vh, new Vector2(handleX, centerY), handleRadius, HandleColor, 14);
            DrawCircleOutline(vh, new Vector2(handleX, centerY), handleRadius, 1.2f, HandleBorderColor, 14);
        }

        private static void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            if (!(max.x > min.x) || !(max.y > min.y)) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color color, int segments)
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

        private static void DrawCircleOutline(VertexHelper vh, Vector2 center, float radius, float width,
            Color color, int segments)
        {
            if (!(radius > 0.01f)) return;
            if (segments < 6) segments = 6;

            float inner = Mathf.Max(0f, radius - width);
            int start = vh.currentVertCount;
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vh.AddVert(new Vector3(center.x + cos * inner, center.y + sin * inner), color, Vector2.zero);
                vh.AddVert(new Vector3(center.x + cos * radius, center.y + sin * radius), color, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = start + i * 2;
                int b = start + i * 2 + 1;
                int c = start + next * 2 + 1;
                int d = start + next * 2;
                vh.AddTriangle(a, b, c);
                vh.AddTriangle(a, c, d);
            }
        }
    }

    public class LogChartMenu : MaskableGraphic,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerMoveHandler, IPointerExitHandler
    {
        private const int MaxRows = 12;

        private static readonly Color PanelColor = new Color(0.05f, 0.06f, 0.09f, 0.98f);
        private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color RowHoverColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color RowSelectedColor = new Color(0.30f, 0.76f, 1f, 0.28f);
        private static readonly Color SelectedTextColor = new Color(1f, 1f, 1f, 0.96f);
        private static readonly Color NormalTextColor = new Color(0.82f, 0.85f, 0.90f, 1f);

        private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[MaxRows];
        private readonly string[] _itemBuffer = new string[MaxRows];
        private int _itemCount;
        private int _selected = -1;
        private int _hover = -1;
        private System.Action<int> _onSelect;
        private TMP_FontAsset _font;
        private float _width = 180f;
        private float _rowHeight = 22f;
        private float _padding = 4f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal void Initialize(TMP_FontAsset font, System.Action<int> onSelect)
        {
            _font = font;
            _onSelect = onSelect;

            for (int i = 0; i < MaxRows; i++)
            {
                GameObject go = new GameObject("Row" + i, typeof(RectTransform));
                go.transform.SetParent(transform, false);

                TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
                text.font = font != null ? font : TMP_Settings.defaultFontAsset;
                text.fontSize = 12f;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.color = NormalTextColor;
                text.raycastTarget = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.gameObject.SetActive(false);
                _labels[i] = text;
            }
        }

        internal void SetFont(TMP_FontAsset font)
        {
            _font = font;
            TMP_FontAsset target = font != null ? font : TMP_Settings.defaultFontAsset;
            for (int i = 0; i < _labels.Length; i++)
            {
                TextMeshProUGUI text = _labels[i];
                if (text == null || target == null || text.font == target) continue;
                text.font = target;
            }
        }

        internal void SetItems(string[] items, int selected)
        {
            SetItems(items, items != null ? items.Length : 0, selected);
        }

        internal void SetItems(string[] items, int count, int selected)
        {
            if (count < 0) count = 0;
            if (count > MaxRows) count = MaxRows;
            if (items == null) count = 0;

            _itemCount = count;
            for (int i = 0; i < count; i++) _itemBuffer[i] = items[i];
            _selected = selected;
            _hover = -1;

            for (int i = 0; i < _labels.Length; i++)
            {
                TextMeshProUGUI text = _labels[i];
                if (text == null) continue;
                bool active = i < _itemCount;
                if (text.gameObject.activeSelf != active) text.gameObject.SetActive(active);
                if (active) text.text = _itemBuffer[i];
                text.color = i == _selected ? SelectedTextColor : NormalTextColor;
            }

            ApplyLayoutToRows();
            SetVerticesDirty();
        }

        internal void SetLayout(float width, float rowHeight, float padding)
        {
            _width = Mathf.Max(40f, width);
            _rowHeight = Mathf.Max(10f, rowHeight);
            _padding = Mathf.Max(0f, padding);

            float height = _padding * 2f + _rowHeight * Mathf.Max(1, _itemCount);
            rectTransform.sizeDelta = new Vector2(_width, height);
            ApplyLayoutToRows();
            SetVerticesDirty();
        }

        internal float MenuHeight
        {
            get { return _padding * 2f + _rowHeight * Mathf.Max(1, _itemCount); }
        }

        internal float MenuWidth { get { return _width; } }

        private void ApplyLayoutToRows()
        {
            float fontSize = Mathf.Clamp(Mathf.Round(_rowHeight * 0.52f), 8f, 24f);
            for (int i = 0; i < _labels.Length; i++)
            {
                TextMeshProUGUI text = _labels[i];
                if (text == null) continue;

                if (!Mathf.Approximately(text.fontSize, fontSize)) text.fontSize = fontSize;

                RectTransform rect = text.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(_padding + 6f, -(_padding + i * _rowHeight));
                rect.sizeDelta = new Vector2(-(_padding * 2f + 12f), _rowHeight);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;

            int index = RowAt(eventData != null ? eventData.position : (Vector2)Input.mousePosition);
            if (index < 0) return;
            if (_onSelect != null) _onSelect(index);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (eventData == null) return;
            int index = RowAt(eventData.position);
            if (index == _hover) return;
            _hover = index;
            SetVerticesDirty();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hover < 0) return;
            _hover = -1;
            SetVerticesDirty();
        }

        private int RowAt(Vector2 screenPosition)
        {
            if (rectTransform == null) return -1;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out local))
                return -1;

            Rect rect = rectTransform.rect;
            if (local.x < rect.xMin || local.x > rect.xMax || local.y < rect.yMin || local.y > rect.yMax)
                return -1;

            float fromTop = rect.yMax - local.y - _padding;
            if (fromTop < 0f) return -1;

            int index = Mathf.FloorToInt(fromTop / _rowHeight);
            if (index < 0 || index >= _itemCount) return -1;
            return index;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;

            float panelCorner = Mathf.Min(LogRoundedRect.MenuRadius, Mathf.Min(w, h) * 0.25f);
            LogRoundedRect.DrawFilled(vh, rect, panelCorner, PanelColor, 8);

            for (int i = 0; i < _itemCount; i++)
            {
                float top = rect.yMax - _padding - i * _rowHeight;
                float bottom = top - _rowHeight;
                if (i == _selected || i == _hover)
                {
                    Color color = i == _hover ? RowHoverColor : RowSelectedColor;
                    if (i == _selected && i == _hover)
                    {
                        color.a = Mathf.Clamp01(RowSelectedColor.a + RowHoverColor.a);
                    }
                    Rect rowRect = new Rect(rect.xMin + 2f, bottom, rect.width - 4f, _rowHeight);
                    LogRoundedRect.DrawFilled(vh, rowRect,
                        Mathf.Min(LogRoundedRect.MenuItemRadius, _rowHeight * 0.25f), color, 6);
                }
            }

            LogRoundedRect.DrawBorder(vh, rect, panelCorner, 1f, BorderColor, 8);
        }

        private static void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            if (!(max.x > min.x) || !(max.y > min.y)) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
