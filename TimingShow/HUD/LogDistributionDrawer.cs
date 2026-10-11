using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogDistributionDrawer : MaskableGraphic
    {
        private const int YTickCount = 3;
        private const int XTickCount = 3;
        private const int YGridDivisions = 4;

        private static readonly Color MeanLineColor = new Color(1f, 0.7176471f, 0.3019608f, 1f);
        private static readonly Color SigmaLineColor = new Color(0.3098039f, 0.7647059f, 0.9686275f, 0.6f);
        private static readonly Color ClipWarningColor = new Color(1f, 0.78f, 0.32f, 0.92f);
        private static readonly Color TipBgColor = new Color(0f, 0f, 0f, 0.90f);
        private static readonly Color TipTextColor = new Color(0.95f, 0.96f, 0.98f, 1f);

        private DistributionStats _stats;
        private bool _isAngle;
        private bool _initialized;
        private TMP_FontAsset _font;

        private TextMeshProUGUI[] _yLabels;
        private TextMeshProUGUI[] _xLabels;
        private TextMeshProUGUI _yAxisTitle;
        private TextMeshProUGUI _xAxisTitle;
        private TextMeshProUGUI _meanLabel;
        private TextMeshProUGUI _sigmaHighLabel;
        private TextMeshProUGUI _sigmaLowLabel;
        private TextMeshProUGUI _clipLabel;
        private TextMeshProUGUI _emptyLabel;
        private TextMeshProUGUI _tipLabel;

        private int _hoverBin = -1;
        private bool _tipLayoutDirty = true;
        private bool _textDirty = true;
        private Vector2 _tipMin;
        private Vector2 _tipMax;

        private float _lastWidth = -1f;
        private float _lastHeight = -1f;
        private float _uiScale = 1.25f;
        private float _fontSize = 13f;
        private float _yMax = 1f;
        private float _labelWidth = 0f;
        private Rect _plot = new Rect(0f, 0f, 0f, 0f);
        private bool _hasPlot;

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
        private Color BarColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_PointColor : new Color(0.30f, 0.76f, 1f, 1f); } }
        private Color CurveColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_AvgLineColor : new Color(1f, 0.85f, 0.20f, 0.95f); } }
        private Color ZeroLineColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_ZeroLineColor : new Color(1f, 1f, 1f, 0.65f); } }
        private Color AxisTextColor { get { Settings s = LogSettings; return s != null ? s.LogGraph_AxisTextColor : new Color(0.80f, 0.80f, 0.80f, 1f); } }

        private Color BarBorderColor
        {
            get
            {
                Color c = Color.Lerp(BarColor, Color.white, 0.45f);
                c.a = Mathf.Clamp01(Mathf.Max(c.a, 0.85f));
                return c;
            }
        }

        private Color BarHoverColor
        {
            get
            {
                Color c = Color.Lerp(BarColor, Color.white, 0.35f);
                c.a = Mathf.Clamp01(Mathf.Max(c.a, 0.98f));
                return c;
            }
        }

        private string UnitSuffix { get { return _isAngle ? "\u00b0" : "ms"; } }

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
            _meanLabel = CreateText("MeanLabel", TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f));
            _sigmaHighLabel = CreateText("SigmaHighLabel", TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
            _sigmaLowLabel = CreateText("SigmaLowLabel", TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
            _clipLabel = CreateText("ClipLabel", TextAlignmentOptions.TopRight, new Vector2(1f, 1f));
            _emptyLabel = CreateText("EmptyLabel", TextAlignmentOptions.Midline, new Vector2(0.5f, 0.5f));
            _tipLabel = CreateText("TipLabel", TextAlignmentOptions.TopLeft, new Vector2(0f, 0f));
            if (_tipLabel != null) _tipLabel.gameObject.SetActive(false);

            ApplyFont();
            InvalidateLayout();
        }

        internal void SetData(DistributionStats stats, bool isAngle)
        {
            if (ReferenceEquals(_stats, stats) && _isAngle == isAngle) return;
            _stats = stats;
            _isAngle = isAngle;
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
            ApplyFontTo(_meanLabel);
            ApplyFontTo(_sigmaHighLabel);
            ApplyFontTo(_sigmaLowLabel);
            ApplyFontTo(_clipLabel);
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
                MarkLabelsDirty();
            }

            UpdateHover();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _hoverBin = -1;
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

        private void MarkLabelsDirty()
        {
            if (_yLabels != null)
                for (int i = 0; i < _yLabels.Length; i++) MarkDirty(_yLabels[i]);
            if (_xLabels != null)
                for (int i = 0; i < _xLabels.Length; i++) MarkDirty(_xLabels[i]);
            MarkDirty(_yAxisTitle);
            MarkDirty(_xAxisTitle);
            MarkDirty(_meanLabel);
            MarkDirty(_sigmaHighLabel);
            MarkDirty(_sigmaLowLabel);
            MarkDirty(_clipLabel);
        }

        private static void MarkDirty(TextMeshProUGUI text)
        {
            if (text == null || !text.gameObject.activeInHierarchy) return;
            text.SetVerticesDirty();
            text.SetLayoutDirty();
        }

        private void ComputeLayout(float width, float height)
        {
            _uiScale = Scale;
            _fontSize = Mathf.Clamp(Mathf.Round(12f * _uiScale), 8f, 40f);

            float pad = 6f * _uiScale;
            float gap = 6f * _uiScale;
            float margin = 3f * _uiScale;

            _yMax = 1f;
            if (_stats != null && _stats.HasData)
            {
                float maxCount = _stats.MaxBinCount;
                float curveMax = _stats.HasCurve ? _stats.CurveMax : 0f;
                float top = Mathf.Max(maxCount, curveMax);
                if (!(top > 0f) || float.IsNaN(top) || float.IsInfinity(top)) top = 1f;
                _yMax = top * 1.10f;
            }

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

        private void ApplyTextLayout(float width, float height)
        {
            if (!_initialized) return;

            bool hasData = _stats != null && _stats.HasData && _hasPlot;
            SetActive(_emptyLabel, !hasData);
            SetActive(_yAxisTitle, hasData);
            SetActive(_xAxisTitle, hasData);
            for (int i = 0; i < YTickCount; i++) SetActive(_yLabels[i], hasData);
            for (int i = 0; i < XTickCount; i++) SetActive(_xLabels[i], hasData);

            if (!hasData)
            {
                SetText(_emptyLabel, i18n.T("LogGraph_DistNoData"));
                _emptyLabel.color = AxisTextColor;
                _emptyLabel.fontSize = _fontSize;
                Place(_emptyLabel, new Vector2(width * 0.5f, height * 0.5f), new Vector2(Mathf.Max(40f, width - 20f), _fontSize * 2f), new Vector2(0.5f, 0.5f));
                SetActive(_meanLabel, false);
                SetActive(_sigmaHighLabel, false);
                SetActive(_sigmaLowLabel, false);
                SetActive(_clipLabel, false);
                return;
            }

            float axisMin = _stats.AxisMin;
            float axisMax = _stats.AxisMax;

            for (int i = 0; i < YTickCount; i++)
            {
                TextMeshProUGUI text = _yLabels[i];
                float y = _plot.yMin + _plot.height * i / (YTickCount - 1);
                Place(text, new Vector2(_plot.xMin - AxisLabelGap, y),
                    new Vector2(Mathf.Max(30f, _labelWidth + 8f), _fontSize * 1.6f), new Vector2(1f, 0.5f));
                SetText(text, YLabelContent(i));
                text.color = AxisTextColor;
                text.fontSize = _fontSize * 0.95f;
            }

            for (int i = 0; i < XTickCount; i++)
            {
                TextMeshProUGUI text = _xLabels[i];
                float t = i / (float)(XTickCount - 1);
                float x = _plot.xMin + _plot.width * t;
                Place(text, new Vector2(x, _plot.yMin - 1f * _uiScale),
                    new Vector2(_plot.width, _fontSize * 1.5f), new Vector2(0.5f, 1f));
                SetText(text, FormatAxisValue(axisMin + (axisMax - axisMin) * t));
                text.color = AxisTextColor;
                text.fontSize = _fontSize * 0.95f;
            }

            SetText(_yAxisTitle, i18n.T("LogGraph_DistAxisCount"));
            _yAxisTitle.color = AxisTextColor;
            _yAxisTitle.fontSize = _fontSize * 0.95f;
            Place(_yAxisTitle, new Vector2(_plot.xMin, _plot.yMax + 2f * _uiScale),
                new Vector2(Mathf.Max(40f, _plot.width * 0.6f), _fontSize * 1.5f), new Vector2(0f, 0f));

            SetText(_xAxisTitle, string.Format(i18n.T("LogGraph_DistAxisOffset"), UnitSuffix));
            _xAxisTitle.color = AxisTextColor;
            _xAxisTitle.fontSize = _fontSize * 0.95f;
            Place(_xAxisTitle, new Vector2(_plot.center.x, _plot.yMin - _fontSize * 1.45f),
                new Vector2(_plot.width, _fontSize * 1.5f), new Vector2(0.5f, 1f));

            float mean = (float)_stats.Mean;
            bool meanVisible = IsFinite(mean) && mean >= axisMin && mean <= axisMax;
            SetActive(_meanLabel, meanVisible);
            if (meanVisible)
            {
                SetText(_meanLabel, string.Format(i18n.T("LogGraph_DistMeanLabel"), FormatValue(_stats.Mean) + UnitSuffix));
                _meanLabel.color = MeanLineColor;
                _meanLabel.fontSize = _fontSize * 0.95f;
                Place(_meanLabel, new Vector2(XToPx(mean) + 3f * _uiScale, _plot.yMin + 2f * _uiScale),
                    new Vector2(120f * _uiScale, _fontSize * 1.5f), new Vector2(0f, 0f));
            }

            float sigma = (float)_stats.StdDev;
            bool sigmaVisible = sigma > 0f && IsFinite(sigma);
            float high = mean + sigma;
            float low = mean - sigma;
            bool highVisible = sigmaVisible && IsFinite(high) && high >= axisMin && high <= axisMax;
            bool lowVisible = sigmaVisible && IsFinite(low) && low >= axisMin && low <= axisMax;

            SetActive(_sigmaHighLabel, highVisible);
            if (highVisible)
            {
                SetText(_sigmaHighLabel, i18n.T("LogGraph_DistSigmaHigh"));
                _sigmaHighLabel.color = SigmaLineColor;
                _sigmaHighLabel.fontSize = _fontSize * 0.95f;
                Place(_sigmaHighLabel, new Vector2(XToPx(high) + 3f * _uiScale, _plot.yMax - 2f * _uiScale),
                    new Vector2(80f * _uiScale, _fontSize * 1.5f), new Vector2(0f, 1f));
            }

            SetActive(_sigmaLowLabel, lowVisible);
            if (lowVisible)
            {
                SetText(_sigmaLowLabel, i18n.T("LogGraph_DistSigmaLow"));
                _sigmaLowLabel.color = SigmaLineColor;
                _sigmaLowLabel.fontSize = _fontSize * 0.95f;
                Place(_sigmaLowLabel, new Vector2(XToPx(low) + 3f * _uiScale, _plot.yMax - _fontSize * 1.6f - 2f * _uiScale),
                    new Vector2(80f * _uiScale, _fontSize * 1.5f), new Vector2(0f, 1f));
            }

            bool clipped = _stats.ClippedCount > 0;
            SetActive(_clipLabel, clipped);
            if (clipped)
            {
                SetText(_clipLabel, string.Format(i18n.T("LogGraph_DistClipped"), _stats.ClippedCount));
                _clipLabel.color = ClipWarningColor;
                _clipLabel.fontSize = _fontSize * 0.9f;
                Place(_clipLabel, new Vector2(_plot.xMax, _plot.yMax + 2f * _uiScale),
                    new Vector2(Mathf.Max(60f, _plot.width), _fontSize * 1.5f), new Vector2(1f, 0f));
            }
        }

        private void UpdateHover()
        {
            int bin = -1;

            if (_initialized && _hasPlot && _stats != null && _stats.HasData && rectTransform != null)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out local))
                {
                    if (local.x >= _plot.xMin && local.x <= _plot.xMax
                        && local.y >= _plot.yMin && local.y <= _plot.yMax)
                    {
                        bin = BinAt(local.x);
                    }
                }
            }

            if (bin == _hoverBin && !_tipLayoutDirty) return;

            _hoverBin = bin;
            _tipLayoutDirty = false;
            RefreshTip();
            SetVerticesDirty();
        }

        private int BinAt(float localX)
        {
            if (_stats == null || _stats.Bins == null) return -1;

            HistogramBin[] bins = _stats.Bins;
            for (int i = 0; i < bins.Length; i++)
            {
                float x0;
                float x1;
                if (!BinXRange(i, out x0, out x1)) continue;
                if (localX >= x0 && localX <= x1) return i;
            }

            return -1;
        }

        private bool BinXRange(int index, out float x0, out float x1)
        {
            x0 = 0f;
            x1 = 0f;
            if (_stats == null || _stats.Bins == null || !_hasPlot) return false;
            if (index < 0 || index >= _stats.Bins.Length) return false;

            x0 = XToPx(_stats.Bins[index].Start);
            x1 = XToPx(_stats.Bins[index].End);
            if (x1 < x0) { float swap = x0; x0 = x1; x1 = swap; }
            if (x1 - x0 < 0.5f) x1 = x0 + 0.5f;

            return IsFinite(x0) && IsFinite(x1);
        }

        private void RefreshTip()
        {
            if (_tipLabel == null) return;

            if (_hoverBin < 0 || _stats == null || !_stats.HasData || !_hasPlot
                || _stats.Bins == null || _hoverBin >= _stats.Bins.Length)
            {
                SetActive(_tipLabel, false);
                _tipMin = Vector2.zero;
                _tipMax = Vector2.zero;
                return;
            }

            float x0;
            float x1;
            if (!BinXRange(_hoverBin, out x0, out x1))
            {
                SetActive(_tipLabel, false);
                _tipMin = Vector2.zero;
                _tipMax = Vector2.zero;
                return;
            }

            HistogramBin bin = _stats.Bins[_hoverBin];
            int valid = _stats.ValidCount > 0 ? _stats.ValidCount : 1;
            float share = 100f * bin.Count / valid;

            string line1 = string.Format(i18n.T("LogGraph_DistTipRange"), FormatValue(bin.Start), FormatValue(bin.End), UnitSuffix);
            string line2 = string.Format(i18n.T("LogGraph_DistTipCount"), bin.Count, share.ToString("F1", CultureInfo.InvariantCulture));
            string content = line1 + "\n" + line2;

            float tipFontSize = _fontSize * 0.92f;
            SetText(_tipLabel, content);
            _tipLabel.color = TipTextColor;
            _tipLabel.fontSize = tipFontSize;

            float padX = 6f * _uiScale;
            float padY = 4f * _uiScale;

            float contentWidth;
            float contentHeight;
            Vector2 preferred = _tipLabel.GetPreferredValues(content);
            if (IsFinite(preferred.x) && preferred.x > 0f && IsFinite(preferred.y) && preferred.y > 0f)
            {
                contentWidth = preferred.x;
                contentHeight = preferred.y;
            }
            else
            {
                contentWidth = Mathf.Max(EstimateTextWidth(line1, tipFontSize), EstimateTextWidth(line2, tipFontSize));
                contentHeight = tipFontSize * 2.2f;
            }

            float boxWidth = contentWidth + padX * 2f;
            float boxHeight = contentHeight + padY * 2f;

            Rect rect = rectTransform.rect;
            float minX = (x0 + x1) * 0.5f - boxWidth * 0.5f;
            float minY = YToPx(bin.Count) + 8f * _uiScale;
            if (!IsFinite(minY)) minY = _plot.yMin;

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

        private string YLabelContent(int index)
        {
            float value = _yMax * index / (YTickCount - 1);
            if (!IsFinite(value)) value = 0f;
            int rounded = Mathf.RoundToInt(value);
            if (rounded < 0) rounded = 0;
            return rounded.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatAxisValue(float value)
        {
            if (!IsFinite(value)) return "0";
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }

        private static string FormatValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "-";
            return value.ToString("F2", CultureInfo.InvariantCulture);
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

        private float XToPx(float value)
        {
            float min = _stats.AxisMin;
            float span = _stats.AxisMax - min;
            if (!IsFinite(value) || !(Mathf.Abs(span) > 1e-9f)) return _plot.center.x;
            float t = (value - min) / span;
            if (!IsFinite(t)) return _plot.center.x;
            return _plot.xMin + Mathf.Clamp01(t) * _plot.width;
        }

        private float YToPx(float count)
        {
            if (!IsFinite(count) || !(_yMax > 0f)) return _plot.yMin;
            return _plot.yMin + Mathf.Clamp01(count / _yMax) * _plot.height;
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

            if (_stats == null || !_stats.HasData || !_hasPlot) return;

            float axisMin = _stats.AxisMin;
            float axisMax = _stats.AxisMax;
            float axisSpan = axisMax - axisMin;
            if (!IsFinite(axisSpan) || !(axisSpan > 0f)) return;

            Color grid = GridColor;
            grid.a *= 0.75f;
            float gridHalf = 0.6f * _uiScale;
            for (int i = 0; i <= YGridDivisions; i++)
            {
                float y = _plot.yMin + _plot.height * i / YGridDivisions;
                DrawSegment(vh, new Vector2(_plot.xMin, y), new Vector2(_plot.xMax, y), gridHalf, grid);
            }
            for (int i = 0; i < XTickCount; i++)
            {
                float x = _plot.xMin + _plot.width * i / (XTickCount - 1);
                DrawSegment(vh, new Vector2(x, _plot.yMin), new Vector2(x, _plot.yMax), gridHalf, grid);
            }

            Color bar = BarColor;
            bar.a = Mathf.Clamp01(bar.a) * 0.85f;
            Color barBorder = BarBorderColor;
            Color barHover = BarHoverColor;
            Color barHoverBorder = Color.white;
            float barBorderHalf = 0.6f * _uiScale;
            HistogramBin[] bins = _stats.Bins;
            for (int i = 0; i < bins.Length; i++)
            {
                if (bins[i].Count <= 0) continue;
                float x0;
                float x1;
                if (!BinXRange(i, out x0, out x1)) continue;
                float top = YToPx(bins[i].Count);
                bool hovered = i == _hoverBin;
                DrawQuad(vh, new Vector2(x0, _plot.yMin), new Vector2(x1, top), hovered ? barHover : bar);
                DrawBarOutline(vh, x0, x1, _plot.yMin, top, barBorderHalf, hovered ? barHoverBorder : barBorder);
            }

            if (_stats.HasCurve && _stats.CurveX != null && _stats.CurveY != null)
            {
                int count = Mathf.Min(_stats.CurveX.Length, _stats.CurveY.Length);
                Vector2 previous = Vector2.zero;
                bool hasPrevious = false;
                for (int i = 0; i < count; i++)
                {
                    Vector2 point = new Vector2(XToPx(_stats.CurveX[i]), YToPx(_stats.CurveY[i]));
                    if (hasPrevious) DrawSegment(vh, previous, point, 1.4f * _uiScale, CurveColor);
                    previous = point;
                    hasPrevious = true;
                }
            }

            if (0f >= axisMin && 0f <= axisMax)
            {
                float zeroX = XToPx(0f);
                DrawSegment(vh, new Vector2(zeroX, _plot.yMin), new Vector2(zeroX, _plot.yMax), 0.8f * _uiScale, ZeroLineColor);
            }

            float mean = (float)_stats.Mean;
            if (IsFinite(mean) && mean >= axisMin && mean <= axisMax)
                DrawDashedVertical(vh, XToPx(mean), 5f * _uiScale, 5f * _uiScale, 1f * _uiScale, MeanLineColor);

            float sigma = (float)_stats.StdDev;
            if (sigma > 0f && IsFinite(sigma))
            {
                float high = mean + sigma;
                float low = mean - sigma;
                if (IsFinite(high) && high >= axisMin && high <= axisMax)
                    DrawDashedVertical(vh, XToPx(high), 3f * _uiScale, 3f * _uiScale, 0.75f * _uiScale, SigmaLineColor);
                if (IsFinite(low) && low >= axisMin && low <= axisMax)
                    DrawDashedVertical(vh, XToPx(low), 3f * _uiScale, 3f * _uiScale, 0.75f * _uiScale, SigmaLineColor);
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

            if (_hoverBin >= 0 && _tipMax.x > _tipMin.x && _tipMax.y > _tipMin.y)
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
            if (!(dash > 0.1f)) { DrawSegment(vh, new Vector2(x, _plot.yMin), new Vector2(x, _plot.yMax), halfWidth, color); return; }
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

        private static void DrawBarOutline(VertexHelper vh, float x0, float x1, float bottom, float top, float halfWidth, Color color)
        {
            if (!(top > bottom) || !(x1 > x0)) return;

            Vector2 bl = new Vector2(x0, bottom);
            Vector2 br = new Vector2(x1, bottom);
            Vector2 tr = new Vector2(x1, top);
            Vector2 tl = new Vector2(x0, top);
            DrawSegment(vh, bl, br, halfWidth, color);
            DrawSegment(vh, br, tr, halfWidth, color);
            DrawSegment(vh, tr, tl, halfWidth, color);
            DrawSegment(vh, tl, bl, halfWidth, color);
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

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static float AxisLabelGap
        {
            get { return 6f * (LogSettings != null ? Mathf.Clamp(LogSettings.LogGraph_UIScale, 0.75f, 3f) : 1.25f); }
        }
    }
}
