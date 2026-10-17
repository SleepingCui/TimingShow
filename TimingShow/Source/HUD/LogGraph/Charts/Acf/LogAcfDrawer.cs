using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogAcfDrawer : LogChartDrawer
    {
        private const int YTickCount = 5;
        private const int XTickCount = 5;
        private const int GridDivisions = 4;

        private static readonly Color BarColor = new Color(0f, 0.737f, 0.831f, 0.75f);
        private static readonly Color BarBorderColor = new Color(0f, 0.737f, 0.831f, 0.95f);
        private static readonly Color BarHoverColor = new Color(0.35f, 0.88f, 0.94f, 0.95f);
        private static readonly Color BandColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color BandEdgeColor = new Color(1f, 1f, 1f, 0.35f);

        private AcfData _data;
        private int _hoverLag;
        private bool _tipLayoutDirty = true;
        private Vector2 _tipMin;
        private Vector2 _tipMax;
        private float _ySpan = 0.2f;

        private TextMeshProUGUI[] _yLabels;
        private TextMeshProUGUI[] _xLabels;
        private TextMeshProUGUI _xAxisTitle;
        private TextMeshProUGUI _yAxisTitle;
        private TextMeshProUGUI _legendLabel;
        private TextMeshProUGUI _emptyLabel;
        private TextMeshProUGUI _tipLabel;

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
        }

        internal void SetData(AcfData data)
        {
            if (ReferenceEquals(_data, data)) return;
            _data = data;
            _ySpan = data != null ? (float)data.YSpan : 0.2f;
            if (!(_ySpan > 0.01f)) _ySpan = 0.01f;
            InvalidateLayout();
        }

        protected override void ComputeLayout(float width, float height)
        {
            float pad = 6f * UiScale;
            float gap = 6f * UiScale;
            float margin = 3f * UiScale;

            float labelWidth = 0f;
            for (int i = 0; i < YTickCount; i++)
                labelWidth = Mathf.Max(labelWidth, EstimateTextWidth(YLabelContent(i), FontSize * 0.95f));

            float left = labelWidth + gap + margin;
            float bottom = FontSize * 1.30f + FontSize * 1.30f + margin * 2f;
            float topPad = FontSize * 1.85f;
            float right = gap + margin;

            float plotWidth = Mathf.Max(1f, width - pad * 2f - left - right);
            float plotHeight = Mathf.Max(1f, height - pad * 2f - bottom - topPad);
            PlotRect = new Rect(pad + left, pad + bottom, plotWidth, plotHeight);
            HasPlot = plotWidth > 4f && plotHeight > 4f;
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
            SetActive(_tipLabel, hasData && _hoverLag > 0);

            if (!hasData)
            {
                _hoverLag = 0;
                SetText(_emptyLabel, i18n.T("LogGraph_AcfNoData"));
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
                    new Vector2(Mathf.Max(30f, EstimateTextWidth(YLabelContent(i), FontSize) + 8f), FontSize * 1.6f),
                    new Vector2(1f, 0.5f));
                SetText(text, YLabelContent(i));
                text.color = AxisTextColor;
                text.fontSize = FontSize * 0.95f;
            }

            for (int i = 0; i < XTickCount; i++)
            {
                TextMeshProUGUI text = _xLabels[i];
                float t = i / (float)(XTickCount - 1);
                float x = PlotRect.xMin + PlotRect.width * t;
                Place(text, new Vector2(x, PlotRect.yMin - 1f * UiScale),
                    new Vector2(PlotRect.width, FontSize * 1.5f), new Vector2(0.5f, 1f));
                SetText(text, XLabelContent(t));
                text.color = AxisTextColor;
                text.fontSize = FontSize * 0.95f;
            }

            SetText(_yAxisTitle, i18n.T("LogGraph_AcfAxisY"));
            _yAxisTitle.color = AxisTextColor;
            _yAxisTitle.fontSize = FontSize * 0.95f;
            Place(_yAxisTitle, new Vector2(PlotRect.xMin, PlotRect.yMax + 2f * UiScale),
                new Vector2(Mathf.Max(40f, PlotRect.width * 0.6f), FontSize * 1.5f), new Vector2(0f, 0f));

            SetText(_xAxisTitle, i18n.T("LogGraph_AcfAxisX"));
            _xAxisTitle.color = AxisTextColor;
            _xAxisTitle.fontSize = FontSize * 0.95f;
            Place(_xAxisTitle, new Vector2(PlotRect.center.x, PlotRect.yMin - FontSize * 1.45f),
                new Vector2(PlotRect.width, FontSize * 1.5f), new Vector2(0.5f, 1f));

            SetText(_legendLabel,
                "<color=#" + ColorToHex(BarColor) + ">" + i18n.T("LogGraph_AcfTitle") + "</color>  " +
                "<color=#" + ColorToHex(BandEdgeColor) + ">" + i18n.T("LogGraph_AcfBand") + "</color>");
            _legendLabel.color = AxisTextColor;
            _legendLabel.fontSize = FontSize * 0.95f;
            Place(_legendLabel, new Vector2(PlotRect.xMax, PlotRect.yMax + 2f * UiScale),
                new Vector2(Mathf.Max(1f, PlotRect.width * 0.8f), FontSize * 1.5f), new Vector2(1f, 0f));
        }

        private string YLabelContent(int index)
        {
            float value = -_ySpan + (_ySpan * 2f) * index / (YTickCount - 1);
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private string XLabelContent(float t)
        {
            int lag = Mathf.Clamp(Mathf.RoundToInt(1f + (AcfData.MaxLag - 1) * t), 1, AcfData.MaxLag);
            return lag.ToString(CultureInfo.InvariantCulture);
        }

        protected override void UpdateInteraction()
        {
            int lag = 0;
            if (Initialized && HasPlot && _data != null && _data.HasData && rectTransform != null)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out local)
                    && local.x >= PlotRect.xMin && local.x <= PlotRect.xMax
                    && local.y >= PlotRect.yMin && local.y <= PlotRect.yMax)
                {
                    float step = PlotRect.width / AcfData.MaxLag;
                    lag = Mathf.FloorToInt((local.x - PlotRect.xMin) / Mathf.Max(1e-4f, step)) + 1;
                    if (lag < 1) lag = 1;
                    if (lag > AcfData.MaxLag) lag = AcfData.MaxLag;
                }
            }

            if (lag == _hoverLag && !_tipLayoutDirty) return;

            _hoverLag = lag;
            _tipLayoutDirty = false;
            RefreshTip();
            SetVerticesDirty();
        }

        private void RefreshTip()
        {
            if (_tipLabel == null) return;

            if (_hoverLag <= 0 || _data == null || !_data.HasData || !HasPlot)
            {
                SetActive(_tipLabel, false);
                _tipMin = Vector2.zero;
                _tipMax = Vector2.zero;
                return;
            }

            AcfEntry entry = _data.Entries[_hoverLag - 1];
            string content = "lag " + _hoverLag;
            content += "\n" + i18n.T("LogGraph_ReturnCorr") + "  "
                + (entry != null && !double.IsNaN(entry.R) ? entry.R.ToString("R", CultureInfo.InvariantCulture) : "-");
            content += "\n" + i18n.T("LogGraph_AcfPairs") + "  " + (entry != null ? entry.Pairs : 0);
            if (entry != null && !double.IsNaN(entry.Band))
                content += "\n" + i18n.T("LogGraph_AcfBand") + "  ±" + entry.Band.ToString("R", CultureInfo.InvariantCulture);

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

            float x = LagCenterX(_hoverLag);
            float minX = x + 10f * UiScale;
            float minY = PlotRect.yMax - boxHeight - 8f * UiScale;

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

            float zeroY = YToPx(0f);
            DrawSegment(vh, new Vector2(PlotRect.xMin, zeroY), new Vector2(PlotRect.xMax, zeroY), 0.8f * UiScale,
                new Color(1f, 1f, 1f, 0.45f));

            DrawBars(vh);

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
            for (int i = 0; i < AcfData.MaxLag - 1; i++)
            {
                AcfEntry a = _data.Entries[i];
                AcfEntry b = _data.Entries[i + 1];
                if (a == null || b == null) continue;
                if (double.IsNaN(a.Band) || double.IsNaN(b.Band)) continue;

                float x0 = LagCenterX(i + 1);
                float x1 = LagCenterX(i + 2);
                float u0 = YToPx((float)a.Band);
                float l0 = YToPx((float)-a.Band);
                float u1 = YToPx((float)b.Band);
                float l1 = YToPx((float)-b.Band);
                DrawTriangleQuad(vh, new Vector2(x0, u0), new Vector2(x1, u1), new Vector2(x1, l1), new Vector2(x0, l0), BandColor);
            }

            float half = 0.6f * UiScale;
            Vector2 prevUpper = Vector2.zero;
            Vector2 prevLower = Vector2.zero;
            bool hasPrevious = false;
            for (int i = 0; i < AcfData.MaxLag; i++)
            {
                AcfEntry entry = _data.Entries[i];
                if (entry == null || double.IsNaN(entry.Band))
                {
                    hasPrevious = false;
                    continue;
                }

                float x = LagCenterX(entry.Lag);
                Vector2 upper = new Vector2(x, YToPx((float)entry.Band));
                Vector2 lower = new Vector2(x, YToPx((float)-entry.Band));
                if (hasPrevious)
                {
                    DrawSegment(vh, prevUpper, upper, half, BandEdgeColor);
                    DrawSegment(vh, prevLower, lower, half, BandEdgeColor);
                }
                prevUpper = upper;
                prevLower = lower;
                hasPrevious = true;
            }
        }

        private void DrawBars(VertexHelper vh)
        {
            float step = PlotRect.width / AcfData.MaxLag;
            float barWidth = Mathf.Max(1f, step * 0.7f);
            float zeroY = YToPx(0f);

            for (int i = 0; i < AcfData.MaxLag; i++)
            {
                AcfEntry entry = _data.Entries[i];
                if (entry == null || double.IsNaN(entry.R)) continue;

                float x = LagCenterX(entry.Lag);
                float y = YToPx((float)entry.R);
                float top = Mathf.Max(zeroY, y);
                float bottom = Mathf.Min(zeroY, y);
                if (top - bottom < 0.5f) top = bottom + 0.5f;

                Color color = entry.Lag == _hoverLag ? BarHoverColor : BarColor;
                DrawQuad(vh, new Vector2(x - barWidth * 0.5f, bottom),
                    new Vector2(x + barWidth * 0.5f, top), color);

                Color border = entry.Lag == _hoverLag ? Color.white : BarBorderColor;
                float half = 0.5f * UiScale;
                DrawSegment(vh, new Vector2(x - barWidth * 0.5f, bottom), new Vector2(x + barWidth * 0.5f, bottom), half, border);
                DrawSegment(vh, new Vector2(x - barWidth * 0.5f, top), new Vector2(x + barWidth * 0.5f, top), half, border);
            }
        }

        private void DrawHover(VertexHelper vh)
        {
            if (_hoverLag <= 0) return;

            float x = LagCenterX(_hoverLag);
            Color cross = AxisTextColor;
            cross.a *= 0.55f;
            DrawDashedVertical(vh, x, 5f * UiScale, 4f * UiScale, 0.8f * UiScale, cross);

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

        private float LagCenterX(int lag)
        {
            float step = PlotRect.width / AcfData.MaxLag;
            return PlotRect.xMin + (lag - 0.5f) * step;
        }

        private float YToPx(float value)
        {
            if (!IsFinite(value) || !(_ySpan > 1e-6f)) return PlotRect.center.y;
            float t = (value + _ySpan) / (_ySpan * 2f);
            return PlotRect.yMin + Mathf.Clamp01(t) * PlotRect.height;
        }
    }
}
