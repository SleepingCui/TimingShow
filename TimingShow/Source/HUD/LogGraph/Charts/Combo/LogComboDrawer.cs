using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogComboDrawer : LogChartDrawer,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        private const int XTickCount = 5;
        private const int MaxLegendEntries = 8;

        private static readonly Color[] Ramp =
        {
            new Color(0.227f, 0.420f, 0.204f, 1f),
            new Color(0.251f, 0.529f, 0.220f, 1f),
            new Color(0.271f, 0.643f, 0.227f, 1f),
            new Color(0.298f, 0.761f, 0.247f, 1f),
            new Color(0.322f, 0.878f, 0.263f, 1f),
            new Color(0.376f, 1.000f, 0.306f, 1f)
        };
        private static readonly Color BreakColor = new Color(0.20f, 0.20f, 0.20f, 1f);

        private ComboData _data;
        private int _hoverSegment = -1;
        private bool _tipLayoutDirty = true;
        private Vector2 _tipMin;
        private Vector2 _tipMax;
        private const float MinVisibleHits = 10f;

        private float _stripBottom;
        private float _stripTop;
        private float _viewStart;
        private float _viewEnd;
        private bool _panning;
        private Vector2 _panStartScreen;
        private float _panStartView;
        private readonly List<LegendEntry> _legend = new List<LegendEntry>();
        private readonly List<ComboSegment> _renderSegments = new List<ComboSegment>();

        private TextMeshProUGUI[] _xLabels;
        private TextMeshProUGUI _xAxisTitle;
        private TextMeshProUGUI _emptyLabel;
        private TextMeshProUGUI _tipLabel;
        private TextMeshProUGUI[] _legendLabels;
        private TextMeshProUGUI _breakLabel;

        private struct LegendEntry
        {
            public int Bin;
            public int Start;
            public int End;
        }

        protected override void CreateTextComponents()
        {
            _xLabels = new TextMeshProUGUI[XTickCount];
            for (int i = 0; i < XTickCount; i++)
                _xLabels[i] = CreateText("XLabel" + i, TextAlignmentOptions.Top, new Vector2(0.5f, 1f));

            _xAxisTitle = CreateText("XAxisTitle", TextAlignmentOptions.Top, new Vector2(0.5f, 1f));
            _emptyLabel = CreateText("EmptyLabel", TextAlignmentOptions.Midline, new Vector2(0.5f, 0.5f));
            _tipLabel = CreateText("TipLabel", TextAlignmentOptions.TopLeft, new Vector2(0f, 0f));
            _tipLabel.richText = true;
            if (_tipLabel != null) _tipLabel.gameObject.SetActive(false);

            _legendLabels = new TextMeshProUGUI[MaxLegendEntries];
            for (int i = 0; i < MaxLegendEntries; i++)
                _legendLabels[i] = CreateText("Legend" + i, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f));
            _breakLabel = CreateText("LegendBreak", TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f));
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
        }

        internal void SetData(ComboData data)
        {
            if (ReferenceEquals(_data, data)) return;
            _data = data;
            _viewStart = 0f;
            _viewEnd = data != null ? data.HitCount : 0f;
            BuildLegend();
            InvalidateLayout();
        }

        private float ViewSpan
        {
            get
            {
                float span = _viewEnd - _viewStart;
                if (!(span > 1e-3f) && _data != null) span = Mathf.Max(1f, _data.HitCount);
                return span;
            }
        }

        private void BuildLegend()
        {
            _legend.Clear();
            if (_data == null || _data.MaxRunLength <= 0) return;

            for (int length = 1; length <= _data.MaxRunLength; length++)
            {
                int bin = ComboBin(length, _data.MaxRunLength);
                if (_legend.Count > 0)
                {
                    LegendEntry last = _legend[_legend.Count - 1];
                    if (last.Bin == bin)
                    {
                        last.End = length;
                        _legend[_legend.Count - 1] = last;
                        continue;
                    }
                }

                LegendEntry entry = new LegendEntry();
                entry.Bin = bin;
                entry.Start = length;
                entry.End = length;
                _legend.Add(entry);
            }
        }

        private static int ComboBin(int length, int maxLength)
        {
            if (!(maxLength > 0)) return 0;
            int bin = Mathf.FloorToInt(Ramp.Length * Mathf.Sqrt((float)length / maxLength));
            if (bin < 0) bin = 0;
            if (bin >= Ramp.Length) bin = Ramp.Length - 1;
            return bin;
        }

        protected override void ComputeLayout(float width, float height)
        {
            float pad = 6f * UiScale;
            float margin = 3f * UiScale;

            float left = 8f * UiScale;
            float bottom = FontSize * 1.30f + FontSize * 1.30f + margin * 2f;
            float topPad = FontSize * 2.6f;
            float right = 8f * UiScale;

            float plotWidth = Mathf.Max(1f, width - pad * 2f - left - right);
            float plotHeight = Mathf.Max(1f, height - pad * 2f - bottom - topPad);
            PlotRect = new Rect(pad + left, pad + bottom, plotWidth, plotHeight);
            HasPlot = plotWidth > 4f && plotHeight > 4f;

            float stripHeight = Mathf.Clamp(plotHeight * 0.46f, 12f, 90f * UiScale);
            float centerY = PlotRect.yMin + plotHeight * 0.5f;
            _stripBottom = centerY - stripHeight * 0.5f;
            _stripTop = centerY + stripHeight * 0.5f;

            BuildRenderSegments();
        }

        private void BuildRenderSegments()
        {
            _renderSegments.Clear();
            if (_data == null || !_data.HasData || !HasPlot || _data.HitCount <= 0) return;

            float plotWidth = PlotRect.width;
            float span = ViewSpan;
            for (int i = 0; i < _data.Segments.Count; i++)
            {
                ComboSegment segment = _data.Segments[i];
                float overlapStart = Mathf.Max(segment.Start, _viewStart);
                float overlapEnd = Mathf.Min(segment.End, _viewEnd);
                if (overlapEnd <= overlapStart) continue;

                float width = plotWidth * (overlapEnd - overlapStart) / span;
                if (width >= 0.5f) _renderSegments.Add(segment);
            }
        }

        protected override void ApplyTextLayout(float width, float height)
        {
            _tipLayoutDirty = true;
            bool hasData = _data != null && _data.HasData && HasPlot;

            SetActive(_emptyLabel, !hasData);
            SetActive(_xAxisTitle, hasData);
            for (int i = 0; i < XTickCount; i++) SetActive(_xLabels[i], hasData);
            SetActive(_tipLabel, hasData && _hoverSegment >= 0);

            if (!hasData)
            {
                _hoverSegment = -1;
                SetText(_emptyLabel, i18n.T("LogGraph_ComboNoData"));
                _emptyLabel.color = AxisTextColor;
                _emptyLabel.fontSize = FontSize;
                Place(_emptyLabel, new Vector2(width * 0.5f, height * 0.5f),
                    new Vector2(Mathf.Max(40f, width - 20f), FontSize * 2f), new Vector2(0.5f, 0.5f));
                HideLegend();
                return;
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

            SetText(_xAxisTitle, i18n.T("LogGraph_XaccAxisX"));
            _xAxisTitle.color = AxisTextColor;
            _xAxisTitle.fontSize = FontSize * 0.95f;
            Place(_xAxisTitle, new Vector2(PlotRect.center.x, PlotRect.yMin - FontSize * 1.45f),
                new Vector2(PlotRect.width, FontSize * 1.5f), new Vector2(0.5f, 1f));

            ApplyLegendLayout();
        }

        private string XLabelContent(float t)
        {
            int max = _data != null ? Mathf.Max(1, _data.HitCount) : 1;
            int value = Mathf.RoundToInt(1f + _viewStart + ViewSpan * t);
            if (value < 1) value = 1;
            if (value > max) value = max;
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private void HideLegend()
        {
            if (_legendLabels != null)
                for (int i = 0; i < _legendLabels.Length; i++) SetActive(_legendLabels[i], false);
            SetActive(_breakLabel, false);
        }

        private void ApplyLegendLayout()
        {
            HideLegend();
            if (_legend.Count == 0) return;

            float y = PlotRect.yMax + FontSize * 1.1f;
            float x = PlotRect.xMin;
            float swatch = 9f * UiScale;
            float gap = 4f * UiScale;

            for (int i = 0; i < _legend.Count && i < _legendLabels.Length; i++)
            {
                LegendEntry entry = _legend[i];
                string label = entry.Start == entry.End
                    ? entry.Start.ToString(CultureInfo.InvariantCulture)
                    : entry.Start.ToString(CultureInfo.InvariantCulture) + "-" + entry.End.ToString(CultureInfo.InvariantCulture);

                TextMeshProUGUI text = _legendLabels[i];
                SetActive(text, true);
                SetText(text, label);
                text.color = AxisTextColor;
                text.fontSize = FontSize * 0.9f;
                float textWidth = EstimateTextWidth(label, FontSize * 0.9f) + 4f * UiScale;
                Place(text, new Vector2(x + swatch + gap * 0.5f, y),
                    new Vector2(textWidth, FontSize * 1.4f), new Vector2(0f, 0.5f));
                x += swatch + gap + textWidth + 7f * UiScale;
            }

            if (x + swatch + 30f * UiScale <= PlotRect.xMax)
            {
                SetActive(_breakLabel, true);
                SetText(_breakLabel, i18n.T("LogGraph_ComboBreak"));
                _breakLabel.color = AxisTextColor;
                _breakLabel.fontSize = FontSize * 0.9f;
                Place(_breakLabel, new Vector2(x + swatch + gap * 0.5f, y),
                    new Vector2(Mathf.Max(30f, PlotRect.xMax - x), FontSize * 1.4f), new Vector2(0f, 0.5f));
            }
        }

        protected override void UpdateInteraction()
        {
            int index = -1;
            if (Initialized && HasPlot && _data != null && _data.HasData && rectTransform != null)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out local)
                    && local.x >= PlotRect.xMin && local.x <= PlotRect.xMax
                    && local.y >= _stripBottom && local.y <= _stripTop)
                {
                    float t = PlotRect.width > 1e-4f ? (local.x - PlotRect.xMin) / PlotRect.width : 0f;
                    int hit = Mathf.Clamp(Mathf.FloorToInt(_viewStart + t * ViewSpan), 0, _data.HitCount - 1);
                    index = FindSegmentAt(hit);
                }
            }

            if (index == _hoverSegment && !_tipLayoutDirty) return;

            _hoverSegment = index;
            _tipLayoutDirty = false;
            RefreshTip();
            SetVerticesDirty();
        }

        private int FindSegmentAt(int hit)
        {
            if (_data == null || _data.Segments.Count == 0) return -1;

            int low = 0;
            int high = _data.Segments.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                ComboSegment segment = _data.Segments[mid];
                if (hit < segment.Start) high = mid - 1;
                else if (hit >= segment.End) low = mid + 1;
                else return mid;
            }
            return -1;
        }

        private void RefreshTip()
        {
            if (_tipLabel == null) return;

            if (_hoverSegment < 0 || _data == null || !_data.HasData || !HasPlot
                || _hoverSegment >= _data.Segments.Count)
            {
                SetActive(_tipLabel, false);
                _tipMin = Vector2.zero;
                _tipMax = Vector2.zero;
                return;
            }

            ComboSegment segment = _data.Segments[_hoverSegment];
            string content = segment.IsRun
                ? i18n.T("LogGraph_ComboRun") + ": " + segment.Length
                : i18n.T("LogGraph_ComboBreak") + ": " + segment.Length;
            content += "\n" + i18n.T("LogGraph_XaccAxisX") + "  "
                + (segment.Start + 1).ToString(CultureInfo.InvariantCulture) + " - "
                + segment.End.ToString(CultureInfo.InvariantCulture);

            float tipFontSize = FontSize * 0.92f;
            SetText(_tipLabel, content);
            _tipLabel.color = TipTextColor;
            _tipLabel.fontSize = tipFontSize;

            float padX = 6f * UiScale;
            float padY = 4f * UiScale;

            Vector2 preferred = _tipLabel.GetPreferredValues(content);
            float contentWidth = IsFinite(preferred.x) && preferred.x > 0f ? preferred.x : 120f * UiScale;
            float contentHeight = IsFinite(preferred.y) && preferred.y > 0f ? preferred.y : tipFontSize * 2.4f;

            float boxWidth = contentWidth + padX * 2f;
            float boxHeight = contentHeight + padY * 2f;

            float centerX = (IndexToPx(segment.Start) + IndexToPx(segment.End)) * 0.5f;
            float minX = centerX - boxWidth * 0.5f;
            float minY = _stripTop + 8f * UiScale;

            Rect rect = rectTransform.rect;
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
            float gridHalf = 0.5f * UiScale;
            for (int i = 0; i < XTickCount; i++)
            {
                float x = PlotRect.xMin + PlotRect.width * i / (XTickCount - 1);
                DrawSegment(vh, new Vector2(x, PlotRect.yMin), new Vector2(x, PlotRect.yMax), gridHalf, grid);
            }

            DrawSegments(vh);
            DrawLegendSwatches(vh);
            DrawHover(vh);
        }

        private void DrawSegments(VertexHelper vh)
        {
            float border = 1f * UiScale;
            for (int i = 0; i < _renderSegments.Count; i++)
            {
                ComboSegment segment = _renderSegments[i];
                if (segment.Length <= 0) continue;

                float x0 = IndexToPx(segment.Start);
                float x1 = IndexToPx(segment.End);
                if (x1 - x0 < 0.5f) continue;

                Color color = segment.IsRun
                    ? Ramp[ComboBin(segment.Length, _data.MaxRunLength)]
                    : BreakColor;
                if (_hoverSegment >= 0 && _hoverSegment < _data.Segments.Count
                    && ReferenceEquals(segment, _data.Segments[_hoverSegment]))
                    color = Color.Lerp(color, Color.white, 0.35f);

                DrawQuad(vh, new Vector2(x0 + border, _stripBottom + border),
                    new Vector2(x1 - border, _stripTop - border), color);
            }

            Color frame = GridColor;
            frame.a = Mathf.Clamp01(frame.a);
            float half = 0.7f * UiScale;
            DrawSegment(vh, new Vector2(PlotRect.xMin, _stripBottom), new Vector2(PlotRect.xMax, _stripBottom), half, frame);
            DrawSegment(vh, new Vector2(PlotRect.xMin, _stripTop), new Vector2(PlotRect.xMax, _stripTop), half, frame);
        }

        private void DrawLegendSwatches(VertexHelper vh)
        {
            if (_legend.Count == 0) return;

            float y = PlotRect.yMax + FontSize * 1.1f;
            float x = PlotRect.xMin;
            float swatch = 9f * UiScale;
            float gap = 4f * UiScale;

            for (int i = 0; i < _legend.Count && i < _legendLabels.Length; i++)
            {
                LegendEntry entry = _legend[i];
                string label = entry.Start == entry.End
                    ? entry.Start.ToString(CultureInfo.InvariantCulture)
                    : entry.Start.ToString(CultureInfo.InvariantCulture) + "-" + entry.End.ToString(CultureInfo.InvariantCulture);
                float textWidth = EstimateTextWidth(label, FontSize * 0.9f) + 4f * UiScale;

                DrawQuad(vh, new Vector2(x, y - swatch * 0.5f), new Vector2(x + swatch, y + swatch * 0.5f),
                    Ramp[entry.Bin]);
                x += swatch + gap + textWidth + 7f * UiScale;
            }

            if (x + swatch + 30f * UiScale <= PlotRect.xMax)
            {
                DrawQuad(vh, new Vector2(x, y - swatch * 0.5f), new Vector2(x + swatch, y + swatch * 0.5f), BreakColor);
            }
        }

        private void DrawHover(VertexHelper vh)
        {
            if (_hoverSegment < 0 || _hoverSegment >= _data.Segments.Count) return;

            ComboSegment segment = _data.Segments[_hoverSegment];
            float x0 = IndexToPx(segment.Start);
            float x1 = IndexToPx(segment.End);
            Color border = Color.white;
            border.a = 0.85f;
            float half = 1f * UiScale;
            DrawSegment(vh, new Vector2(x0, _stripBottom), new Vector2(x0, _stripTop), half, border);
            DrawSegment(vh, new Vector2(x1, _stripBottom), new Vector2(x1, _stripTop), half, border);

            if (_tipMax.x > _tipMin.x && _tipMax.y > _tipMin.y)
            {
                DrawQuad(vh, _tipMin, _tipMax, TipBgColor);
                Color tipBorder = GridColor;
                tipBorder.a = 0.75f;
                float tipHalf = 0.6f * UiScale;
                Vector2 bl = _tipMin;
                Vector2 br = new Vector2(_tipMax.x, _tipMin.y);
                Vector2 tr = _tipMax;
                Vector2 tl = new Vector2(_tipMin.x, _tipMax.y);
                DrawSegment(vh, bl, br, tipHalf, tipBorder);
                DrawSegment(vh, br, tr, tipHalf, tipBorder);
                DrawSegment(vh, tr, tl, tipHalf, tipBorder);
                DrawSegment(vh, tl, bl, tipHalf, tipBorder);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _panning = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (!ContainsScreenPoint(eventData.position)) return;

            _panning = true;
            _panStartScreen = eventData.position;
            _panStartView = _viewStart;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_panning || eventData == null || !HasPlot) return;

            float dx = eventData.position.x - _panStartScreen.x;
            float shift = -dx / Mathf.Max(1f, PlotRect.width) * ViewSpan;
            SetView(_panStartView + shift, ViewSpan);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _panning = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData == null || !ContainsScreenPoint(eventData.position)) return;

            float wheel = eventData.scrollDelta.y;
            if (Mathf.Approximately(wheel, 0f)) return;

            ZoomAt(eventData.position, wheel > 0f ? 0.85f : 1f / 0.85f);
        }

        private bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (rectTransform == null || !HasPlot) return false;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPosition, null, out local))
                return false;

            return local.x >= PlotRect.xMin && local.x <= PlotRect.xMax
                && local.y >= PlotRect.yMin && local.y <= PlotRect.yMax;
        }

        private void ZoomAt(Vector2 screenPosition, float factor)
        {
            if (_data == null || _data.HitCount <= 0 || !HasPlot) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPosition, null, out local))
                return;

            float t = Mathf.Clamp01((local.x - PlotRect.xMin) / Mathf.Max(1e-4f, PlotRect.width));
            float span = ViewSpan;
            float anchor = _viewStart + span * t;
            float total = Mathf.Max(1f, _data.HitCount);
            float newSpan = Mathf.Clamp(span * factor, Mathf.Min(MinVisibleHits, total), total);
            float newStart = anchor - (anchor - _viewStart) * (newSpan / span);
            SetView(newStart, newSpan);
        }

        private void SetView(float start, float span)
        {
            if (_data == null || _data.HitCount <= 0) return;

            float total = Mathf.Max(1f, _data.HitCount);
            float minSpan = Mathf.Min(MinVisibleHits, total);
            if (span < minSpan) span = minSpan;
            if (span > total) span = total;
            if (start < 0f) start = 0f;
            if (start + span > total) start = total - span;

            if (Mathf.Approximately(start, _viewStart) && Mathf.Approximately(start + span, _viewEnd))
                return;

            _viewStart = start;
            _viewEnd = start + span;
            BuildRenderSegments();
            TextDirty = true;
            SetVerticesDirty();
        }

        private float IndexToPx(float index)
        {
            float t = (index - _viewStart) / Mathf.Max(1e-4f, ViewSpan);
            return PlotRect.xMin + Mathf.Clamp01(t) * PlotRect.width;
        }
    }
}
