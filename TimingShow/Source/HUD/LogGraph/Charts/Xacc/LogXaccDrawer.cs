using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogXaccDrawer : MaskableGraphic
    {
        private const int YTickCount = 5;
        private const int XTickCount = 5;
        private const int YGridDivisions = YTickCount - 1;
        private const int XGridDivisions = XTickCount - 1;
        private const float MaxY = 100f;
        private const float MinYRange = 1f;

        private static readonly Color TipBgColor = new Color(0f, 0f, 0f, 0.90f);
        private static readonly Color TipTextColor = new Color(0.95f, 0.96f, 0.98f, 1f);

        private XaccData _data;
        private int _selectedPlayer = -1;
        private bool _initialized;
        private TMP_FontAsset _font;

        private TextMeshProUGUI[] _yLabels;
        private TextMeshProUGUI[] _xLabels;
        private TextMeshProUGUI _yAxisTitle;
        private TextMeshProUGUI _xAxisTitle;
        private TextMeshProUGUI _legendLabel;
        private TextMeshProUGUI _emptyLabel;
        private TextMeshProUGUI _tipLabel;

        private int _hoverOrdinal;
        private bool _tipLayoutDirty = true;
        private Vector2 _tipMin;
        private Vector2 _tipMax;

        private bool _textDirty = true;
        private float _lastWidth = -1f;
        private float _lastHeight = -1f;
        private float _uiScale = 1.25f;
        private float _fontSize = 13f;
        private float _labelWidth;
        private float _yMin;
        private float _yMax = MaxY;
        private int _xMax = 1;
        private Rect _plot = new Rect(0f, 0f, 0f, 0f);
        private bool _hasPlot;
        private int _visibleSeriesCount;

        private readonly StringBuilder _legendBuilder = new StringBuilder(64);
        private readonly StringBuilder _tipBuilder = new StringBuilder(128);

        private static Settings LogSettings { get { return ModContext.Settings; } }

        private static float Scale
        {
            get
            {
                Settings settings = LogSettings;
                return settings != null ? Mathf.Clamp(settings.LogGraph_UIScale, 0.75f, 3f) : 1.25f;
            }
        }

        private Color BgColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_BgColor : new Color(0f, 0f, 0f, 0.60f); } }
        private Color GridColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_GridColor : new Color(1f, 1f, 1f, 0.16f); } }
        private Color LineColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_PointColor : new Color(0.30f, 0.76f, 1f, 1f); } }
        private Color AxisTextColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_AxisTextColor : new Color(0.80f, 0.80f, 0.80f, 1f); } }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            material = defaultMaterial;
            color = Color.white;
            useLegacyMeshGeneration = false;
        }

        internal void Initialize(TMP_FontAsset font)
        {
            if (_initialized)
            {
                SetFont(font);
                return;
            }

            _initialized = true;
            _font = font;

            _yLabels = new TextMeshProUGUI[YTickCount];
            for (int i = 0; i < YTickCount; i++)
                _yLabels[i] = CreateText("YLabel" + i, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f));

            _xLabels = new TextMeshProUGUI[XTickCount];
            for (int i = 0; i < XTickCount; i++)
                _xLabels[i] = CreateText("XLabel" + i, TextAlignmentOptions.Top, new Vector2(0.5f, 1f));

            _yAxisTitle = CreateText("YAxisTitle", TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f));
            _xAxisTitle = CreateText("XAxisTitle", TextAlignmentOptions.Top, new Vector2(0.5f, 1f));
            _legendLabel = CreateText("LegendLabel", TextAlignmentOptions.TopRight, new Vector2(1f, 0f));
            _legendLabel.richText = true;
            _emptyLabel = CreateText("EmptyLabel", TextAlignmentOptions.Midline, new Vector2(0.5f, 0.5f));
            _tipLabel = CreateText("TipLabel", TextAlignmentOptions.TopLeft, new Vector2(0f, 0f));
            _tipLabel.richText = true;
            if (_tipLabel != null) _tipLabel.gameObject.SetActive(false);

            ApplyFont();
            InvalidateLayout();
        }

        internal void SetData(XaccData data, int selectedPlayerId)
        {
            if (ReferenceEquals(_data, data) && _selectedPlayer == selectedPlayerId) return;

            _data = data;
            _selectedPlayer = selectedPlayerId;
            InvalidateLayout();
        }

        internal void SetFont(TMP_FontAsset font)
        {
            if (_font == font) return;
            _font = font;
            ApplyFont();
            InvalidateLayout();
        }

        internal void ReloadAppearance()
        {
            ApplyFont();
            InvalidateLayout();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InvalidateLayout();
        }

        private void InvalidateLayout()
        {
            _lastWidth = -1f;
            _lastHeight = -1f;
            _textDirty = true;
            _tipLayoutDirty = true;
            SetVerticesDirty();
        }

        private void ApplyFont()
        {
            ApplyFontTo(_yLabels);
            ApplyFontTo(_xLabels);
            ApplyFontTo(_yAxisTitle);
            ApplyFontTo(_xAxisTitle);
            ApplyFontTo(_legendLabel);
            ApplyFontTo(_emptyLabel);
            ApplyFontTo(_tipLabel);
        }

        private void ApplyFontTo(TextMeshProUGUI[] texts)
        {
            if (texts == null) return;
            for (int i = 0; i < texts.Length; i++) ApplyFontTo(texts[i]);
        }

        private void ApplyFontTo(TextMeshProUGUI text)
        {
            TMP_FontAsset target = _font != null ? _font : TMP_Settings.defaultFontAsset;
            if (text == null || target == null || text.font == target) return;
            text.font = target;
            text.SetVerticesDirty();
            text.SetLayoutDirty();
        }

        private TextMeshProUGUI CreateText(string name, TextAlignmentOptions alignment, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = _font != null ? _font : TMP_Settings.defaultFontAsset;
            text.fontSize = _fontSize;
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
            rect.sizeDelta = new Vector2(10f, _fontSize * 1.6f);
            return text;
        }

        private void Update()
        {
            if (!_initialized || !isActiveAndEnabled) return;

            if (EnsureLayout()) SetVerticesDirty();

            if (_textDirty)
            {
                _textDirty = false;
                _tipLayoutDirty = true;
                ApplyTextLayout(_lastWidth, _lastHeight);
            }

            UpdateHover();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _hoverOrdinal = 0;
            _tipLayoutDirty = true;
        }

        private bool EnsureLayout()
        {
            Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            if (rect.width == _lastWidth && rect.height == _lastHeight) return false;
            _lastWidth = rect.width;
            _lastHeight = rect.height;
            ComputeLayout(rect.width, rect.height);
            _textDirty = true;
            return true;
        }

        private void ComputeLayout(float width, float height)
        {
            _uiScale = Scale;
            _fontSize = Mathf.Clamp(Mathf.Round(12f * _uiScale), 8f, 40f);

            float pad = 6f * _uiScale;
            float gap = 6f * _uiScale;
            float margin = 3f * _uiScale;

            ComputeRanges();

            float labelWidth = 0f;
            for (int i = 0; i < YTickCount; i++)
                labelWidth = Mathf.Max(labelWidth, EstimateTextWidth(YLabelContent(i), _fontSize * 0.95f));
            _labelWidth = labelWidth;

            float left = labelWidth + gap + margin;
            float bottom = _fontSize * 1.30f + _fontSize * 1.30f + margin * 2f;
            float topPad = _fontSize * 1.85f;
            float right = gap + margin;

            float plotWidth = Mathf.Max(1f, width - pad * 2f - left - right);
            float plotHeight = Mathf.Max(1f, height - pad * 2f - bottom - topPad);
            _plot = new Rect(pad + left, pad + bottom, plotWidth, plotHeight);
            _hasPlot = plotWidth > 4f && plotHeight > 4f;
        }

        private void ComputeRanges()
        {
            _yMin = 0f;
            _yMax = MaxY;
            _xMax = 1;
            _visibleSeriesCount = 0;

            if (_data == null || _data.Series == null)
                return;

            float minValue = float.MaxValue;
            bool hasValue = false;

            for (int p = 0; p < _data.Series.Length; p++)
            {
                XaccSeries series = _data.Series[p];
                if (series == null || !HasValidValue(series)) continue;
                if (!IsSeriesVisible(series.PlayerId)) continue;

                _visibleSeriesCount++;
                if (series.HitCount > _xMax) _xMax = series.HitCount;
                if (series.MinValue < minValue) minValue = series.MinValue;
                hasValue = true;
            }

            if (!hasValue) return;

            float floorMin = Mathf.Floor(minValue - 1f);
            if (floorMin < 0f) floorMin = 0f;
            if (MaxY - minValue < 1f) floorMin = Mathf.Floor(minValue * 10f) / 10f;
            _yMin = floorMin;
            if (_yMax - _yMin < MinYRange) _yMin = Mathf.Max(0f, _yMax - MinYRange);
        }

        private bool HasValidValue(XaccSeries series)
        {
            return series != null && series.HasData && series.ValidCount > 0;
        }

        private bool IsSeriesVisible(int playerId)
        {
            return _selectedPlayer < 0 || _selectedPlayer == playerId;
        }

        private void ApplyTextLayout(float width, float height)
        {
            if (!_initialized) return;

            bool hasData = _data != null && _data.HasData && _hasPlot && _visibleSeriesCount > 0;

            SetActive(_emptyLabel, !hasData);
            SetActive(_yAxisTitle, hasData);
            SetActive(_xAxisTitle, hasData);
            for (int i = 0; i < YTickCount; i++) SetActive(_yLabels[i], hasData);
            for (int i = 0; i < XTickCount; i++) SetActive(_xLabels[i], hasData);
            SetActive(_legendLabel, hasData && _visibleSeriesCount > 1);

            _tipLayoutDirty = true;

            if (!hasData)
            {
                _hoverOrdinal = 0;
                SetActive(_tipLabel, false);
                SetText(_emptyLabel, i18n.T("LogGraph_XaccNoData"));
                _emptyLabel.color = AxisTextColor;
                _emptyLabel.fontSize = _fontSize;
                Place(_emptyLabel, new Vector2(width * 0.5f, height * 0.5f),
                    new Vector2(Mathf.Max(40f, width - 20f), _fontSize * 2f), new Vector2(0.5f, 0.5f));
                return;
            }

            for (int i = 0; i < YTickCount; i++)
            {
                TextMeshProUGUI text = _yLabels[i];
                float y = _plot.yMin + _plot.height * i / (YTickCount - 1);
                Place(text, new Vector2(_plot.xMin - 6f * _uiScale, y),
                    new Vector2(Mathf.Max(30f, _labelWidth + 8f), _fontSize * 1.6f), new Vector2(1f, 0.5f));
                SetText(text, YLabelContent(i));
                text.color = AxisTextColor;
                text.fontSize = _fontSize * 0.95f;
            }

            string previousXLabel = null;
            for (int i = 0; i < XTickCount; i++)
            {
                TextMeshProUGUI text = _xLabels[i];
                float t = i / (float)(XTickCount - 1);
                float x = _plot.xMin + _plot.width * t;
                Place(text, new Vector2(x, _plot.yMin - 1f * _uiScale),
                    new Vector2(_plot.width, _fontSize * 1.5f), new Vector2(0.5f, 1f));

                string label = XLabelContent(t);
                SetText(text, label == previousXLabel ? "" : label);
                previousXLabel = label;
                text.color = AxisTextColor;
                text.fontSize = _fontSize * 0.95f;
            }

            SetText(_yAxisTitle, i18n.T("LogGraph_XaccAxisY"));
            _yAxisTitle.color = AxisTextColor;
            _yAxisTitle.fontSize = _fontSize * 0.95f;
            Place(_yAxisTitle, new Vector2(_plot.xMin, _plot.yMax + 2f * _uiScale),
                new Vector2(Mathf.Max(40f, _plot.width * 0.6f), _fontSize * 1.5f), new Vector2(0f, 0f));

            SetText(_xAxisTitle, i18n.T("LogGraph_XaccAxisX"));
            _xAxisTitle.color = AxisTextColor;
            _xAxisTitle.fontSize = _fontSize * 0.95f;
            Place(_xAxisTitle, new Vector2(_plot.center.x, _plot.yMin - _fontSize * 1.45f),
                new Vector2(_plot.width, _fontSize * 1.5f), new Vector2(0.5f, 1f));

            UpdateLegend();
        }

        private void UpdateLegend()
        {
            _legendBuilder.Length = 0;
            int visible = 0;

            for (int p = 0; p < _data.Series.Length; p++)
            {
                XaccSeries series = _data.Series[p];
                if (!HasValidValue(series) || !IsSeriesVisible(series.PlayerId)) continue;

                if (visible > 0) _legendBuilder.Append("  ");
                _legendBuilder.Append("<color=#");
                _legendBuilder.Append(ColorUtility.ToHtmlStringRGB(LineColorFor(series.PlayerId, _visibleSeriesCount)));
                _legendBuilder.Append('>');
                _legendBuilder.Append(Coop.Label(series.PlayerId));
                _legendBuilder.Append("</color>");
                visible++;
            }

            SetText(_legendLabel, _legendBuilder.ToString());
            _legendLabel.color = AxisTextColor;
            _legendLabel.fontSize = _fontSize * 0.95f;
            Place(_legendLabel, new Vector2(_plot.xMax, _plot.yMax + 2f * _uiScale),
                new Vector2(Mathf.Max(1f, _plot.width * 0.8f), _fontSize * 1.5f), new Vector2(1f, 0f));
        }

        private void UpdateHover()
        {
            int ordinal = 0;

            if (_initialized && _hasPlot && _data != null && _data.HasData && _visibleSeriesCount > 0
                && rectTransform != null)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rectTransform, Input.mousePosition, null, out local))
                {
                    if (local.x >= _plot.xMin && local.x <= _plot.xMax
                        && local.y >= _plot.yMin && local.y <= _plot.yMax)
                    {
                        float t = _plot.width > 1e-4f ? (local.x - _plot.xMin) / _plot.width : 0f;
                        int max = Mathf.Max(1, _xMax);
                        ordinal = 1 + Mathf.RoundToInt(Mathf.Clamp01(t) * (max - 1));
                        if (ordinal < 1) ordinal = 1;
                        if (ordinal > max) ordinal = max;
                    }
                }
            }

            if (ordinal == _hoverOrdinal && !_tipLayoutDirty) return;

            _hoverOrdinal = ordinal;
            _tipLayoutDirty = false;
            RefreshTip();
            SetVerticesDirty();
        }

        private void RefreshTip()
        {
            if (_tipLabel == null) return;

            if (_hoverOrdinal <= 0 || _data == null || !_data.HasData || !_hasPlot || _visibleSeriesCount <= 0)
            {
                SetActive(_tipLabel, false);
                _tipMin = Vector2.zero;
                _tipMax = Vector2.zero;
                return;
            }

            _tipBuilder.Length = 0;
            _tipBuilder.Append(string.Format(i18n.T("LogGraph_XaccTipHit"), _hoverOrdinal));

            for (int p = 0; p < _data.Series.Length; p++)
            {
                XaccSeries series = _data.Series[p];
                if (!HasValidValue(series) || !IsSeriesVisible(series.PlayerId)) continue;

                _tipBuilder.Append('\n');
                _tipBuilder.Append("<color=#");
                _tipBuilder.Append(ColorUtility.ToHtmlStringRGB(LineColorFor(series.PlayerId, _visibleSeriesCount)));
                _tipBuilder.Append('>');
                _tipBuilder.Append(Coop.Label(series.PlayerId));
                _tipBuilder.Append("  ");

                float value = GetValueAt(series, _hoverOrdinal);
                if (float.IsNaN(value) || !IsFinite(value))
                {
                    _tipBuilder.Append('-');
                }
                else
                {
                    _tipBuilder.Append(value.ToString("R", CultureInfo.InvariantCulture));
                    _tipBuilder.Append('%');
                }
                _tipBuilder.Append("</color>");
            }

            string content = _tipBuilder.ToString();
            float tipFontSize = _fontSize * 0.92f;
            SetText(_tipLabel, content);
            _tipLabel.color = TipTextColor;
            _tipLabel.fontSize = tipFontSize;

            float padX = 6f * _uiScale;
            float padY = 4f * _uiScale;

            Vector2 preferred = _tipLabel.GetPreferredValues(content);
            float contentWidth;
            float contentHeight;
            if (IsFinite(preferred.x) && preferred.x > 0f && IsFinite(preferred.y) && preferred.y > 0f)
            {
                contentWidth = preferred.x;
                contentHeight = preferred.y;
            }
            else
            {
                contentWidth = EstimateTextWidth("P1  100.00%", tipFontSize) + 20f;
                contentHeight = tipFontSize * 1.2f * Mathf.Max(2, _visibleSeriesCount + 1);
            }

            float boxWidth = contentWidth + padX * 2f;
            float boxHeight = contentHeight + padY * 2f;

            Vector2 mouseLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, Input.mousePosition, null, out mouseLocal);

            Rect rect = rectTransform.rect;
            float minX = XToPx(_hoverOrdinal) + 10f * _uiScale;
            float minY = mouseLocal.y + 10f * _uiScale;

            float lowX = rect.xMin + 2f * _uiScale;
            float lowY = rect.yMin + 2f * _uiScale;
            float highX = Mathf.Max(lowX, rect.xMax - boxWidth - 2f * _uiScale);
            float highY = Mathf.Max(lowY, rect.yMax - boxHeight - 2f * _uiScale);
            minX = Mathf.Clamp(minX, lowX, highX);
            minY = Mathf.Clamp(minY, lowY, highY);

            _tipMin = new Vector2(minX, minY);
            _tipMax = new Vector2(minX + boxWidth, minY + boxHeight);

            Place(_tipLabel, new Vector2(minX + padX, minY + padY),
                new Vector2(contentWidth + 4f * _uiScale, contentHeight + 2f * _uiScale), new Vector2(0f, 0f));
            SetActive(_tipLabel, true);
        }

        private static float GetValueAt(XaccSeries series, int ordinal)
        {
            if (series == null || series.Values == null) return float.NaN;

            int index = ordinal - 1;
            if (index < 0 || index >= series.Values.Length) return float.NaN;
            return series.Values[index];
        }

        private string YLabelContent(int index)
        {
            float span = _yMax - _yMin;
            float value = _yMin + span * index / (YTickCount - 1);
            if (!IsFinite(value)) value = 0f;
            return value.ToString("F1", CultureInfo.InvariantCulture) + "%";
        }

        private string XLabelContent(float t)
        {
            int max = Mathf.Max(1, _xMax);
            int value = Mathf.RoundToInt(1f + (max - 1) * t);
            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect local = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            float width = local.width;
            float height = local.height;
            if (!(width > 0f) || !(height > 0f)) return;

            EnsureLayout();

            DrawQuad(vh, Vector2.zero, new Vector2(width, height), BgColor);

            if (_data == null || !_data.HasData || !_hasPlot || _visibleSeriesCount <= 0) return;

            Color grid = GridColor;
            grid.a *= 0.75f;
            float gridHalf = 0.6f * _uiScale;
            for (int i = 0; i <= YGridDivisions; i++)
            {
                float y = _plot.yMin + _plot.height * i / YGridDivisions;
                DrawSegment(vh, new Vector2(_plot.xMin, y), new Vector2(_plot.xMax, y), gridHalf, grid);
            }
            for (int i = 0; i <= XGridDivisions; i++)
            {
                float x = _plot.xMin + _plot.width * i / XGridDivisions;
                DrawSegment(vh, new Vector2(x, _plot.yMin), new Vector2(x, _plot.yMax), gridHalf, grid);
            }

            for (int p = 0; p < _data.Series.Length; p++)
            {
                XaccSeries series = _data.Series[p];
                if (!HasValidValue(series) || !IsSeriesVisible(series.PlayerId)) continue;
                DrawSeries(vh, series, LineColorFor(series.PlayerId, _visibleSeriesCount));
            }

            Color frame = GridColor;
            frame.a = Mathf.Clamp01(frame.a);
            float frameHalf = 0.7f * _uiScale;
            Vector2 bl = new Vector2(_plot.xMin, _plot.yMin);
            Vector2 br = new Vector2(_plot.xMax, _plot.yMin);
            Vector2 tr = new Vector2(_plot.xMax, _plot.yMax);
            Vector2 tl = new Vector2(_plot.xMin, _plot.yMax);
            DrawSegment(vh, bl, br, frameHalf, frame);
            DrawSegment(vh, br, tr, frameHalf, frame);
            DrawSegment(vh, tr, tl, frameHalf, frame);
            DrawSegment(vh, tl, bl, frameHalf, frame);

            DrawHover(vh);
        }

        private void DrawHover(VertexHelper vh)
        {
            if (_hoverOrdinal <= 0 || !_hasPlot) return;

            float x = XToPx(_hoverOrdinal);
            Color cross = AxisTextColor;
            cross.a *= 0.55f;
            DrawDashedVertical(vh, x, 5f * _uiScale, 4f * _uiScale, 0.8f * _uiScale, cross);

            float markerHalf = 2.2f * _uiScale;
            for (int p = 0; p < _data.Series.Length; p++)
            {
                XaccSeries series = _data.Series[p];
                if (!HasValidValue(series) || !IsSeriesVisible(series.PlayerId)) continue;

                float value = GetValueAt(series, _hoverOrdinal);
                if (float.IsNaN(value) || !IsFinite(value)) continue;

                float y = YToPx(value);
                Vector2 min = new Vector2(x - markerHalf, y - markerHalf);
                Vector2 max = new Vector2(x + markerHalf, y + markerHalf);
                DrawQuad(vh, min, max, LineColorFor(series.PlayerId, _visibleSeriesCount));
            }

            if (_tipMax.x > _tipMin.x && _tipMax.y > _tipMin.y)
            {
                DrawQuad(vh, _tipMin, _tipMax, TipBgColor);

                Color tipBorder = GridColor;
                tipBorder.a = 0.75f;
                float tipHalf = 0.6f * _uiScale;
                Vector2 tipBl = new Vector2(_tipMin.x, _tipMin.y);
                Vector2 tipBr = new Vector2(_tipMax.x, _tipMin.y);
                Vector2 tipTr = new Vector2(_tipMax.x, _tipMax.y);
                Vector2 tipTl = new Vector2(_tipMin.x, _tipMax.y);
                DrawSegment(vh, tipBl, tipBr, tipHalf, tipBorder);
                DrawSegment(vh, tipBr, tipTr, tipHalf, tipBorder);
                DrawSegment(vh, tipTr, tipTl, tipHalf, tipBorder);
                DrawSegment(vh, tipTl, tipBl, tipHalf, tipBorder);
            }
        }

        private void DrawDashedVertical(VertexHelper vh, float x, float dash, float gap, float halfWidth, Color color)
        {
            if (!IsFinite(x)) return;
            if (!(dash > 0.1f))
            {
                DrawSegment(vh, new Vector2(x, _plot.yMin), new Vector2(x, _plot.yMax), halfWidth, color);
                return;
            }
            if (!(gap > 0f)) gap = dash;

            float y = _plot.yMin;
            float end = _plot.yMax;
            int guard = 0;
            while (y < end && guard++ < 1000)
            {
                float stop = Mathf.Min(y + dash, end);
                DrawSegment(vh, new Vector2(x, y), new Vector2(x, stop), halfWidth, color);
                y = stop + gap;
            }
        }

        private void DrawSeries(VertexHelper vh, XaccSeries series, Color color)
        {
            float[] xs = series.RenderX;
            float[] ys = series.RenderY;
            if (xs == null || ys == null) return;

            int count = Mathf.Min(xs.Length, ys.Length);
            if (count <= 0) return;

            float halfWidth = 1.2f * _uiScale;
            Vector2 previous = Vector2.zero;
            bool hasPrevious = false;

            for (int i = 0; i < count; i++)
            {
                float value = ys[i];
                if (float.IsNaN(value) || !IsFinite(value))
                {
                    hasPrevious = false;
                    continue;
                }

                Vector2 point = new Vector2(XToPx(xs[i]), YToPx(value));
                if (!IsFinite(point))
                {
                    hasPrevious = false;
                    continue;
                }

                if (hasPrevious) DrawSegment(vh, previous, point, halfWidth, color);
                previous = point;
                hasPrevious = true;
            }
        }

        private Color LineColorFor(int playerId, int visibleSeriesCount)
        {
            if (visibleSeriesCount <= 1) return LineColor;
            return Coop.PlayerColor(playerId);
        }

        private float XToPx(float ordinal)
        {
            int max = Mathf.Max(1, _xMax);
            if (max <= 1) return _plot.xMin;
            float t = (ordinal - 1f) / (max - 1f);
            return _plot.xMin + Mathf.Clamp01(t) * _plot.width;
        }

        private float YToPx(float value)
        {
            float span = _yMax - _yMin;
            if (!IsFinite(value) || !(span > 1e-6f)) return _plot.yMin;
            float t = (value - _yMin) / span;
            return _plot.yMin + Mathf.Clamp01(t) * _plot.height;
        }

        private static float EstimateTextWidth(string content, float fontSize)
        {
            if (string.IsNullOrEmpty(content)) return 0f;
            return content.Length * fontSize * 0.62f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static void SetActive(TextMeshProUGUI text, bool active)
        {
            if (text == null) return;
            if (text.gameObject.activeSelf != active) text.gameObject.SetActive(active);
        }

        private static void SetText(TextMeshProUGUI text, string content)
        {
            if (text == null) return;
            if (text.text != content) text.text = content;
        }

        private static void Place(TextMeshProUGUI text, Vector2 position, Vector2 size, Vector2 pivot)
        {
            if (text == null) return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
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

        private static void DrawSegment(VertexHelper vh, Vector2 from, Vector2 to, float halfWidth, Color color)
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
    }
}
