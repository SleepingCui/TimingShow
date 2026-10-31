using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogRollingDrawer : LogChartDrawer
    {
        private const int YTickCount = 5;
        private const int XTickCount = 5;
        private const int GridDivisions = 4;

        private static readonly Color BandColor = new Color(0.31f, 0.76f, 0.97f, 0.14f);
        private static readonly Color BandEdgeColor = new Color(0.31f, 0.76f, 0.97f, 0.45f);
        private static readonly Color MeanColor = new Color(1f, 0.718f, 0.302f, 1f);

        private RollingData _data;
        private bool _isAngle;
        private int _windowSize = 50;
        private int _hoverIndex = -1;
        private bool _tipLayoutDirty = true;
        private Vector2 _tipMin;
        private Vector2 _tipMax;
        private float _yMin;
        private float _yMax = 1f;

        private TextMeshProUGUI[] _yLabels;
        private TextMeshProUGUI[] _xLabels;
        private TextMeshProUGUI _yAxisTitle;
        private TextMeshProUGUI _xAxisTitle;
        private TextMeshProUGUI _legendLabel;
        private TextMeshProUGUI _emptyLabel;
        private TextMeshProUGUI _tipLabel;
        private TextMeshProUGUI _windowLabel;
        private TextMeshProUGUI _windowValueLabel;
        private LogSlider _windowSlider;
        private readonly List<int> _renderIndices = new List<int>();

        public System.Action<int> WindowSizeChanged;

        internal int WindowSize { get { return _windowSize; } }

        protected override void CreateTextComponents()
        {
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

            _windowLabel = CreateText("WindowLabel", TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f));
            _windowValueLabel = CreateText("WindowValue", TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f));

            GameObject sliderGo = new GameObject("WindowSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(transform, false);
            _windowSlider = sliderGo.AddComponent<LogSlider>();
            _windowSlider.Initialize(5f, 500f, 5f, _windowSize, OnSliderChanged);
        }

        internal override void ReloadAppearance()
        {
            base.ReloadAppearance();
            if (_windowSlider != null) _windowSlider.SetVerticesDirty();
        }

        internal void SetData(RollingData data, bool isAngle)
        {
            if (ReferenceEquals(_data, data) && _isAngle == isAngle) return;
            _data = data;
            _isAngle = isAngle;
            BuildRenderIndices();
            InvalidateLayout();
        }

        private void BuildRenderIndices()
        {
            _renderIndices.Clear();
            if (_data == null || _data.HitCount <= 0) return;

            int count = _data.HitCount;
            int limit = 4000;
            int stride = count > limit ? Mathf.CeilToInt(count / (float)limit) : 1;
            for (int i = 0; i < count; i += stride) _renderIndices.Add(i);
            if (_renderIndices.Count == 0 || _renderIndices[_renderIndices.Count - 1] != count - 1)
                _renderIndices.Add(count - 1);
        }

        internal void SetWindowSize(int size, bool notify)
        {
            if (size < 1) size = 1;
            if (_windowSize == size) return;

            _windowSize = size;
            if (_windowSlider != null) _windowSlider.SetValueSilent(size);
            UpdateWindowLabels();
            InvalidateLayout();
            if (notify && WindowSizeChanged != null) WindowSizeChanged(size);
        }

        private void OnSliderChanged(float value)
        {
            SetWindowSize(Mathf.RoundToInt(value), true);
        }

        private void UpdateWindowLabels()
        {
            SetText(_windowLabel, i18n.T("LogGraph_RollingWindow"));
            SetText(_windowValueLabel, _windowSize.ToString(CultureInfo.InvariantCulture));
        }

        protected override void ComputeLayout(float width, float height)
        {
            float pad = 6f * UiScale;
            float gap = 6f * UiScale;
            float margin = 3f * UiScale;
            float topTools = 22f * UiScale;

            ComputeYRange();

            float labelWidth = 0f;
            for (int i = 0; i < YTickCount; i++)
                labelWidth = Mathf.Max(labelWidth, EstimateTextWidth(YLabelContent(i), FontSize * 0.95f));

            float left = labelWidth + gap + margin;
            float bottom = FontSize * 1.30f + FontSize * 1.30f + margin * 2f;
            float topPad = FontSize * 1.85f + topTools;
            float right = gap + margin;

            float plotWidth = Mathf.Max(1f, width - pad * 2f - left - right);
            float plotHeight = Mathf.Max(1f, height - pad * 2f - bottom - topPad);
            PlotRect = new Rect(pad + left, pad + bottom, plotWidth, plotHeight);
            HasPlot = plotWidth > 4f && plotHeight > 4f;
        }

        private void ComputeYRange()
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            bool hasValue = false;

            if (_data != null)
            {
                for (int i = 0; i < _data.HitCount; i++)
                {
                    AddRange(_data.Lower, i, ref min, ref max, ref hasValue);
                    AddRange(_data.Upper, i, ref min, ref max, ref hasValue);
                }
            }

            if (!hasValue)
            {
                _yMin = -1f;
                _yMax = 1f;
                return;
            }

            float span = max - min;
            if (span < 1e-4f) span = 1f;
            float padding = span * 0.05f;
            _yMin = min - padding;
            _yMax = max + padding;
        }

        private static void AddRange(float[] values, int index, ref float min, ref float max, ref bool hasValue)
        {
            if (values == null || index < 0 || index >= values.Length) return;
            float value = values[index];
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            if (value < min) min = value;
            if (value > max) max = value;
            hasValue = true;
        }

        protected override void ApplyTextLayout(float width, float height)
        {
            _tipLayoutDirty = true;
            bool hasData = _data != null && _data.HasData && HasPlot;
            SetActive(_emptyLabel, !hasData);
            SetActive(_yAxisTitle, hasData);
            SetActive(_xAxisTitle, hasData);
            SetActive(_legendLabel, hasData);
            for (int i = 0; i < YTickCount; i++) SetActive(_yLabels[i], hasData);
            for (int i = 0; i < XTickCount; i++) SetActive(_xLabels[i], hasData);
            SetActive(_tipLabel, hasData && _hoverIndex >= 0);

            PlaceWindowControls(width, height, hasData);

            if (!hasData)
            {
                _hoverIndex = -1;
                SetText(_emptyLabel, i18n.T("LogGraph_RollingNoData"));
                _emptyLabel.color = AxisTextColor;
                _emptyLabel.fontSize = FontSize;
                Place(_emptyLabel, new Vector2(width * 0.5f, height * 0.5f),
                    new Vector2(Mathf.Max(40f, width - 20f), FontSize * 2f), new Vector2(0.5f, 0.5f));
                return;
            }

            for (int i = 0; i < YTickCount; i++)
            {
                TextMeshProUGUI text = _yLabels[i];
                float y = PlotRect.yMin + PlotRect.height * i / (YTickCount - 1);
                Place(text, new Vector2(PlotRect.xMin - 6f * UiScale, y),
                    new Vector2(Mathf.Max(30f, EstimateTextWidth(YLabelContent(i), FontSize) + 8f),
                        FontSize * 1.6f), new Vector2(1f, 0.5f));
                SetText(text, YLabelContent(i));
                text.color = AxisTextColor;
                text.fontSize = FontSize * 0.95f;
            }

            string previousX = null;
            for (int i = 0; i < XTickCount; i++)
            {
                TextMeshProUGUI text = _xLabels[i];
                float t = i / (float)(XTickCount - 1);
                float x = PlotRect.xMin + PlotRect.width * t;
                Place(text, new Vector2(x, PlotRect.yMin - 1f * UiScale),
                    new Vector2(PlotRect.width, FontSize * 1.5f), new Vector2(0.5f, 1f));

                string label = XLabelContent(t);
                SetText(text, label == previousX ? "" : label);
                previousX = label;
                text.color = AxisTextColor;
                text.fontSize = FontSize * 0.95f;
            }

            SetText(_yAxisTitle, string.Format(i18n.T("LogGraph_RollingAxisY"), UnitSuffix));
            _yAxisTitle.color = AxisTextColor;
            _yAxisTitle.fontSize = FontSize * 0.95f;
            Place(_yAxisTitle, new Vector2(PlotRect.xMin, PlotRect.yMax + 2f * UiScale),
                new Vector2(Mathf.Max(40f, PlotRect.width * 0.6f), FontSize * 1.5f), new Vector2(0f, 0f));

            SetText(_xAxisTitle, i18n.T("LogGraph_XaccAxisX"));
            _xAxisTitle.color = AxisTextColor;
            _xAxisTitle.fontSize = FontSize * 0.95f;
            Place(_xAxisTitle, new Vector2(PlotRect.center.x, PlotRect.yMin - FontSize * 1.45f),
                new Vector2(PlotRect.width, FontSize * 1.5f), new Vector2(0.5f, 1f));

            UpdateLegend();
        }

        private void PlaceWindowControls(float width, float height, bool visible)
        {
            UpdateWindowLabels();
            float top = height - 6f * UiScale;
            float labelWidth = 44f * UiScale;
            float valueWidth = 42f * UiScale;
            float rowHeight = 18f * UiScale;
            float padding = 6f * UiScale;

            if (_windowLabel != null)
            {
                SetActive(_windowLabel, visible);
                _windowLabel.fontSize = FontSize * 0.9f;
                _windowLabel.color = AxisTextColor;
                Place(_windowLabel, new Vector2(padding, top),
                    new Vector2(labelWidth, rowHeight), new Vector2(0f, 1f));
            }

            if (_windowValueLabel != null)
            {
                SetActive(_windowValueLabel, visible);
                _windowValueLabel.fontSize = FontSize * 0.9f;
                _windowValueLabel.color = AxisTextColor;
                Place(_windowValueLabel, new Vector2(width - padding, top),
                    new Vector2(valueWidth, rowHeight), new Vector2(1f, 1f));
            }

            if (_windowSlider != null)
            {
                _windowSlider.gameObject.SetActive(visible);
                if (!visible) return;

                float sliderX = padding + labelWidth + 4f * UiScale;
                float sliderWidth = Mathf.Max(60f * UiScale,
                    width - sliderX - valueWidth - padding * 2f);
                RectTransform rect = _windowSlider.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(sliderX, top);
                rect.sizeDelta = new Vector2(sliderWidth, rowHeight);
            }
        }

        private void UpdateLegend()
        {
            SetText(_legendLabel,
                "<color=#" + ColorToHex(MeanColor) + ">" + i18n.T("LogGraph_RollingMean") + "</color>  " +
                "<color=#" + ColorToHex(BandEdgeColor) + ">" + i18n.T("LogGraph_RollingBand") + "</color>");
            _legendLabel.color = AxisTextColor;
            _legendLabel.fontSize = FontSize * 0.95f;
            Place(_legendLabel, new Vector2(PlotRect.xMax, PlotRect.yMax + 2f * UiScale),
                new Vector2(Mathf.Max(1f, PlotRect.width * 0.8f), FontSize * 1.5f), new Vector2(1f, 0f));
        }

        private string UnitSuffix { get { return _isAngle ? "°" : "ms"; } }

        private string YLabelContent(int index)
        {
            float span = _yMax - _yMin;
            float value = _yMin + span * index / (YTickCount - 1);
            if (!IsFinite(value)) value = 0f;
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private string XLabelContent(float t)
        {
            int max = _data != null ? Mathf.Max(1, _data.HitCount) : 1;
            int value = Mathf.RoundToInt(1f + (max - 1) * t);
            return value.ToString(CultureInfo.InvariantCulture);
        }

        protected override void UpdateInteraction()
        {
            int index = -1;
            if (Initialized && HasPlot && _data != null && _data.HasData && rectTransform != null)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out local)
                    && local.x >= PlotRect.xMin && local.x <= PlotRect.xMax
                    && local.y >= PlotRect.yMin && local.y <= PlotRect.yMax)
                {
                    int max = Mathf.Max(1, _data.HitCount);
                    float t = PlotRect.width > 1e-4f ? (local.x - PlotRect.xMin) / PlotRect.width : 0f;
                    index = Mathf.RoundToInt(Mathf.Clamp01(t) * (max - 1));
                    if (index < 0) index = 0;
                    if (index >= max) index = max - 1;
                }
            }

            if (index == _hoverIndex && !_tipLayoutDirty) return;

            _hoverIndex = index;
            _tipLayoutDirty = false;
            RefreshTip();
            SetVerticesDirty();
        }

        private void RefreshTip()
        {
            if (_tipLabel == null) return;

            if (_hoverIndex < 0 || _data == null || !_data.HasData || !HasPlot)
            {
                SetActive(_tipLabel, false);
                _tipMin = Vector2.zero;
                _tipMax = Vector2.zero;
                return;
            }

            int ordinal = _hoverIndex + 1;
            float mean = GetValue(_data.Mean, _hoverIndex);
            float upper = GetValue(_data.Upper, _hoverIndex);
            float lower = GetValue(_data.Lower, _hoverIndex);

            string content = string.Format(i18n.T("LogGraph_XaccTipHit"), ordinal);
            content += "\n" + i18n.T("LogGraph_RollingMean") + "  " + FormatValue(mean);
            content += "\n" + i18n.T("LogGraph_RollingBand") + "  " + FormatValue(lower) + " ~ " + FormatValue(upper);

            float tipFontSize = FontSize * 0.92f;
            SetText(_tipLabel, content);
            _tipLabel.color = TipTextColor;
            _tipLabel.fontSize = tipFontSize;

            float padX = 6f * UiScale;
            float padY = 4f * UiScale;

            Vector2 preferred = _tipLabel.GetPreferredValues(content);
            float contentWidth = IsFinite(preferred.x) && preferred.x > 0f ? preferred.x : 140f * UiScale;
            float contentHeight = IsFinite(preferred.y) && preferred.y > 0f ? preferred.y : tipFontSize * 3.6f;

            float boxWidth = contentWidth + padX * 2f;
            float boxHeight = contentHeight + padY * 2f;

            Vector2 mouseLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out mouseLocal);

            Rect rect = rectTransform.rect;
            float minX = XToPx(ordinal) + 10f * UiScale;
            float minY = mouseLocal.y + 10f * UiScale;
            float lowX = rect.xMin + 2f * UiScale;
            float lowY = rect.yMin + 2f * UiScale;
            float highX = Mathf.Max(lowX, rect.xMax - boxWidth - 2f * UiScale);
            float highY = Mathf.Max(lowY, rect.yMax - boxHeight - 2f * UiScale);
            minX = Mathf.Clamp(minX, lowX, highX);
            minY = Mathf.Clamp(minY, lowY, highY);

            _tipMin = new Vector2(minX, minY);
            _tipMax = new Vector2(minX + boxWidth, minY + boxHeight);

            Place(_tipLabel, new Vector2(minX + padX, minY + padY),
                new Vector2(contentWidth + 4f * UiScale, contentHeight + 2f * UiScale), new Vector2(0f, 0f));
            SetActive(_tipLabel, true);
        }

        private string FormatValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return "-";
            return value.ToString("R", CultureInfo.InvariantCulture) + UnitSuffix;
        }

        private static float GetValue(float[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length) return float.NaN;
            return values[index];
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

            if (_data == null || !_data.HasData || !HasPlot) return;

            Color grid = GridColor;
            grid.a *= 0.75f;
            float gridHalf = 0.6f * UiScale;
            for (int i = 0; i <= GridDivisions; i++)
            {
                float y = PlotRect.yMin + PlotRect.height * i / GridDivisions;
                DrawSegment(vh, new Vector2(PlotRect.xMin, y), new Vector2(PlotRect.xMax, y), gridHalf, grid);
            }
            for (int i = 0; i <= GridDivisions; i++)
            {
                float x = PlotRect.xMin + PlotRect.width * i / GridDivisions;
                DrawSegment(vh, new Vector2(x, PlotRect.yMin), new Vector2(x, PlotRect.yMax), gridHalf, grid);
            }

            DrawBand(vh);
            DrawMeanLine(vh);

            if (0f >= _yMin && 0f <= _yMax)
            {
                float zeroY = YToPx(0f);
                DrawDashedHorizontal(vh, zeroY, 5f * UiScale, 4f * UiScale, 0.8f * UiScale,
                    new Color(1f, 1f, 1f, 0.55f));
            }

            Color frame = GridColor;
            frame.a = Mathf.Clamp01(frame.a);
            float frameHalf = 0.7f * UiScale;
            Vector2 bl = new Vector2(PlotRect.xMin, PlotRect.yMin);
            Vector2 br = new Vector2(PlotRect.xMax, PlotRect.yMin);
            Vector2 tr = new Vector2(PlotRect.xMax, PlotRect.yMax);
            Vector2 tl = new Vector2(PlotRect.xMin, PlotRect.yMax);
            DrawSegment(vh, bl, br, frameHalf, frame);
            DrawSegment(vh, br, tr, frameHalf, frame);
            DrawSegment(vh, tr, tl, frameHalf, frame);
            DrawSegment(vh, tl, bl, frameHalf, frame);

            DrawHover(vh);
        }

        private void DrawBand(VertexHelper vh)
        {
            float[] upper = _data.Upper;
            float[] lower = _data.Lower;
            if (upper == null || lower == null) return;

            float edgeHalf = 0.5f * UiScale;
            for (int r = 1; r < _renderIndices.Count; r++)
            {
                int previousIndex = _renderIndices[r - 1];
                int index = _renderIndices[r];
                if (previousIndex >= upper.Length || index >= upper.Length
                    || previousIndex >= lower.Length || index >= lower.Length) continue;

                float u0 = upper[previousIndex];
                float l0 = lower[previousIndex];
                float u1 = upper[index];
                float l1 = lower[index];
                if (!IsFinite(u0) || !IsFinite(l0) || !IsFinite(u1) || !IsFinite(l1)) continue;

                Vector2 a = new Vector2(XToPx(previousIndex + 1), YToPx(u0));
                Vector2 b = new Vector2(XToPx(index + 1), YToPx(u1));
                Vector2 c = new Vector2(XToPx(index + 1), YToPx(l1));
                Vector2 d = new Vector2(XToPx(previousIndex + 1), YToPx(l0));
                DrawTriangleQuad(vh, a, b, c, d, BandColor);

                DrawSegment(vh, a, b, edgeHalf, BandEdgeColor);
                DrawSegment(vh, d, c, edgeHalf, BandEdgeColor);
            }
        }

        private void DrawMeanLine(VertexHelper vh)
        {
            float[] mean = _data.Mean;
            if (mean == null) return;

            float halfWidth = 1.2f * UiScale;
            Vector2 previous = Vector2.zero;
            bool hasPrevious = false;
            for (int r = 0; r < _renderIndices.Count; r++)
            {
                int index = _renderIndices[r];
                if (index >= mean.Length) continue;

                float value = mean[index];
                if (!IsFinite(value))
                {
                    hasPrevious = false;
                    continue;
                }

                Vector2 point = new Vector2(XToPx(index + 1), YToPx(value));
                if (hasPrevious) DrawSegment(vh, previous, point, halfWidth, MeanColor);
                previous = point;
                hasPrevious = true;
            }
        }

        private void DrawHover(VertexHelper vh)
        {
            if (_hoverIndex < 0) return;

            float x = XToPx(_hoverIndex + 1);
            Color cross = AxisTextColor;
            cross.a *= 0.55f;
            DrawDashedVertical(vh, x, 5f * UiScale, 4f * UiScale, 0.8f * UiScale, cross);

            float markerHalf = 2.2f * UiScale;
            float mean = GetValue(_data.Mean, _hoverIndex);
            if (IsFinite(mean))
            {
                float y = YToPx(mean);
                DrawQuad(vh, new Vector2(x - markerHalf, y - markerHalf),
                    new Vector2(x + markerHalf, y + markerHalf), MeanColor);
            }

            if (_tipMax.x > _tipMin.x && _tipMax.y > _tipMin.y)
            {
                DrawQuad(vh, _tipMin, _tipMax, TipBgColor);
                Color border = GridColor;
                border.a = 0.75f;
                float half = 0.6f * UiScale;
                Vector2 bl = _tipMin;
                Vector2 br = new Vector2(_tipMax.x, _tipMin.y);
                Vector2 tr = _tipMax;
                Vector2 tl = new Vector2(_tipMin.x, _tipMax.y);
                DrawSegment(vh, bl, br, half, border);
                DrawSegment(vh, br, tr, half, border);
                DrawSegment(vh, tr, tl, half, border);
                DrawSegment(vh, tl, bl, half, border);
            }
        }

        private float XToPx(float ordinal)
        {
            int max = _data != null ? Mathf.Max(1, _data.HitCount) : 1;
            if (max <= 1) return PlotRect.xMin;
            float t = (ordinal - 1f) / (max - 1f);
            return PlotRect.xMin + Mathf.Clamp01(t) * PlotRect.width;
        }

        private float YToPx(float value)
        {
            float span = _yMax - _yMin;
            if (!IsFinite(value) || !(span > 1e-6f)) return PlotRect.yMin;
            float t = (value - _yMin) / span;
            return PlotRect.yMin + Mathf.Clamp01(t) * PlotRect.height;
        }
    }
}
