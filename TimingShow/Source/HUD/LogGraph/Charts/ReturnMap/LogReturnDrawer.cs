using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class LogReturnDrawer : LogChartDrawer
    {
        private const int TickCount = 5;
        private const int GridDivisions = 4;

        private static readonly Color PointDrawColor = new Color(0.31f, 0.76f, 0.97f, 0.55f);
        private static readonly Color PointHoverColor = new Color(0.55f, 0.88f, 1f, 1f);
        private static readonly Color DiagonalColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color ZeroColor = new Color(1f, 1f, 1f, 0.20f);

        private ReturnData _data;
        private bool _isAngle;
        private float _limit = 1f;
        private int _hoverPair = -1;
        private bool _tipLayoutDirty = true;
        private Vector2 _tipMin;
        private Vector2 _tipMax;

        private TextMeshProUGUI[] _yLabels;
        private TextMeshProUGUI[] _xLabels;
        private TextMeshProUGUI _xAxisTitle;
        private TextMeshProUGUI _yAxisTitle;
        private TextMeshProUGUI _emptyLabel;
        private TextMeshProUGUI _tipLabel;

        protected override void CreateTextComponents()
        {
            _yLabels = new TextMeshProUGUI[TickCount];
            for (int i = 0; i < TickCount; i++)
                _yLabels[i] = CreateText("YLabel" + i, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f));

            _xLabels = new TextMeshProUGUI[TickCount];
            for (int i = 0; i < TickCount; i++)
                _xLabels[i] = CreateText("XLabel" + i, TextAlignmentOptions.Top, new Vector2(0.5f, 1f));

            _yAxisTitle = CreateText("YAxisTitle", TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f));
            _xAxisTitle = CreateText("XAxisTitle", TextAlignmentOptions.Top, new Vector2(0.5f, 1f));
            _emptyLabel = CreateText("EmptyLabel", TextAlignmentOptions.Midline, new Vector2(0.5f, 0.5f));
            _tipLabel = CreateText("TipLabel", TextAlignmentOptions.TopLeft, new Vector2(0f, 0f));
            _tipLabel.richText = true;
            if (_tipLabel != null) _tipLabel.gameObject.SetActive(false);
        }

        internal void SetData(ReturnData data, bool isAngle)
        {
            if (ReferenceEquals(_data, data) && _isAngle == isAngle) return;
            _data = data;
            _isAngle = isAngle;
            _limit = data != null && data.Limit > 0f ? data.Limit : 1f;
            InvalidateLayout();
        }

        protected override void ComputeLayout(float width, float height)
        {
            float pad = 6f * UiScale;
            float gap = 6f * UiScale;
            float margin = 3f * UiScale;

            float labelWidth = 0f;
            for (int i = 0; i < TickCount; i++)
                labelWidth = Mathf.Max(labelWidth, EstimateTextWidth(TickLabel(i), FontSize * 0.95f));

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
            for (int i = 0; i < TickCount; i++) SetActive(_yLabels[i], hasData);
            for (int i = 0; i < TickCount; i++) SetActive(_xLabels[i], hasData);
            SetActive(_tipLabel, hasData && _hoverPair >= 0);

            if (!hasData)
            {
                _hoverPair = -1;
                SetText(_emptyLabel, i18n.T("LogGraph_ReturnNoData"));
                _emptyLabel.color = AxisTextColor;
                _emptyLabel.fontSize = FontSize;
                Place(_emptyLabel, new Vector2(width * 0.5f, height * 0.5f),
                    new Vector2(Mathf.Max(40f, width - 20f), FontSize * 2f), new Vector2(0.5f, 0.5f));
                return;
            }

            for (int i = 0; i < TickCount; i++)
            {
                TextMeshProUGUI text = _yLabels[i];
                float y = PlotRect.yMin + PlotRect.height * i / (TickCount - 1);
                Place(text, new Vector2(PlotRect.xMin - 6f * UiScale, y),
                    new Vector2(Mathf.Max(30f, EstimateTextWidth(TickLabel(i), FontSize) + 8f), FontSize * 1.6f),
                    new Vector2(1f, 0.5f));
                SetText(text, TickLabel(i));
                text.color = AxisTextColor;
                text.fontSize = FontSize * 0.95f;
            }

            for (int i = 0; i < TickCount; i++)
            {
                TextMeshProUGUI text = _xLabels[i];
                float t = i / (float)(TickCount - 1);
                float x = PlotRect.xMin + PlotRect.width * t;
                Place(text, new Vector2(x, PlotRect.yMin - 1f * UiScale),
                    new Vector2(PlotRect.width, FontSize * 1.5f), new Vector2(0.5f, 1f));
                SetText(text, TickLabel(i));
                text.color = AxisTextColor;
                text.fontSize = FontSize * 0.95f;
            }

            SetText(_yAxisTitle, string.Format(i18n.T("LogGraph_ReturnAxisY"), UnitSuffix));
            _yAxisTitle.color = AxisTextColor;
            _yAxisTitle.fontSize = FontSize * 0.95f;
            Place(_yAxisTitle, new Vector2(PlotRect.xMin, PlotRect.yMax + 2f * UiScale),
                new Vector2(Mathf.Max(40f, PlotRect.width * 0.6f), FontSize * 1.5f), new Vector2(0f, 0f));

            SetText(_xAxisTitle, string.Format(i18n.T("LogGraph_ReturnAxisX"), UnitSuffix));
            _xAxisTitle.color = AxisTextColor;
            _xAxisTitle.fontSize = FontSize * 0.95f;
            Place(_xAxisTitle, new Vector2(PlotRect.center.x, PlotRect.yMin - FontSize * 1.45f),
                new Vector2(PlotRect.width, FontSize * 1.5f), new Vector2(0.5f, 1f));
        }

        private string TickLabel(int index)
        {
            float value = -_limit + (_limit * 2f) * index / (TickCount - 1);
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private string UnitSuffix { get { return _isAngle ? "°" : "ms"; } }

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
                    float best = float.MaxValue;
                    float radius = 14f * UiScale;
                    for (int i = 0; i < _data.RenderPairs.Count; i++)
                    {
                        ReturnPair pair = _data.RenderPairs[i];
                        if (Math.Abs(pair.Previous) > _limit || Math.Abs(pair.Current) > _limit) continue;

                        Vector2 point = new Vector2(XToPx(pair.Previous), YToPx(pair.Current));
                        float dx = point.x - local.x;
                        float dy = point.y - local.y;
                        float distance = dx * dx + dy * dy;
                        if (distance <= radius * radius && distance < best)
                        {
                            best = distance;
                            index = i;
                        }
                    }
                }
            }

            if (index == _hoverPair && !_tipLayoutDirty) return;

            _hoverPair = index;
            _tipLayoutDirty = false;
            RefreshTip();
            SetVerticesDirty();
        }

        private void RefreshTip()
        {
            if (_tipLabel == null) return;

            if (_hoverPair < 0 || _data == null || !_data.HasData || !HasPlot
                || _hoverPair >= _data.RenderPairs.Count)
            {
                SetActive(_tipLabel, false);
                _tipMin = Vector2.zero;
                _tipMax = Vector2.zero;
                return;
            }

            ReturnPair pair = _data.RenderPairs[_hoverPair];
            string content = string.Format(i18n.T("LogGraph_XaccTipHit"), pair.Hit);
            content += "\n" + FormatValue(pair.Previous) + "  →  " + FormatValue(pair.Current);

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

            float minX = XToPx(pair.Previous) + 10f * UiScale;
            float minY = YToPx(pair.Current) + 10f * UiScale;

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

        private string FormatValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return "-";
            return value.ToString("R", CultureInfo.InvariantCulture) + UnitSuffix;
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
                float t = i / (float)GridDivisions;
                float y = PlotRect.yMin + PlotRect.height * t;
                float x = PlotRect.xMin + PlotRect.width * t;
                DrawSegment(vh, new Vector2(PlotRect.xMin, y), new Vector2(PlotRect.xMax, y), gridHalf, grid);
                DrawSegment(vh, new Vector2(x, PlotRect.yMin), new Vector2(x, PlotRect.yMax), gridHalf, grid);
            }

            float centerX = XToPx(0f);
            float centerY = YToPx(0f);
            DrawSegment(vh, new Vector2(centerX, PlotRect.yMin), new Vector2(centerX, PlotRect.yMax), 0.7f * UiScale, ZeroColor);
            DrawSegment(vh, new Vector2(PlotRect.xMin, centerY), new Vector2(PlotRect.xMax, centerY), 0.7f * UiScale, ZeroColor);

            DrawDashedLine(vh, new Vector2(PlotRect.xMin, PlotRect.yMin),
                new Vector2(PlotRect.xMax, PlotRect.yMax), 5f * UiScale, 5f * UiScale, 0.8f * UiScale, DiagonalColor);

            DrawPoints(vh);

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

        private void DrawPoints(VertexHelper vh)
        {
            float half = 2.2f * UiScale;
            for (int i = 0; i < _data.RenderPairs.Count; i++)
            {
                ReturnPair pair = _data.RenderPairs[i];
                if (Math.Abs(pair.Previous) > _limit || Math.Abs(pair.Current) > _limit) continue;

                float x = XToPx(pair.Previous);
                float y = YToPx(pair.Current);
                Color color = i == _hoverPair ? PointHoverColor : PointDrawColor;
                float radius = i == _hoverPair ? half * 1.5f : half;
                DrawCircle(vh, new Vector2(x, y), radius, color, 12);
            }
        }

        private void DrawHover(VertexHelper vh)
        {
            if (_hoverPair < 0 || _hoverPair >= _data.RenderPairs.Count) return;

            ReturnPair pair = _data.RenderPairs[_hoverPair];
            float x = XToPx(pair.Previous);
            float y = YToPx(pair.Current);
            Color cross = AxisTextColor;
            cross.a *= 0.45f;
            DrawSegment(vh, new Vector2(x, PlotRect.yMin), new Vector2(x, PlotRect.yMax), 0.6f * UiScale, cross);
            DrawSegment(vh, new Vector2(PlotRect.xMin, y), new Vector2(PlotRect.xMax, y), 0.6f * UiScale, cross);

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

        private void DrawDashedLine(VertexHelper vh, Vector2 from, Vector2 to, float dash, float gap, float halfWidth, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (!(length > 0.01f)) return;

            Vector2 direction = delta / length;
            float step = dash + gap;
            int guard = 0;
            for (float t = 0f; t < length && guard++ < 2000; t += step)
            {
                float end = Mathf.Min(t + dash, length);
                DrawSegment(vh, from + direction * t, from + direction * end, halfWidth, color);
            }
        }

        private float XToPx(float value)
        {
            if (!IsFinite(value) || !(_limit > 1e-6f)) return PlotRect.center.x;
            float t = (value + _limit) / (_limit * 2f);
            return PlotRect.xMin + Mathf.Clamp01(t) * PlotRect.width;
        }

        private float YToPx(float value)
        {
            if (!IsFinite(value) || !(_limit > 1e-6f)) return PlotRect.center.y;
            float t = (value + _limit) / (_limit * 2f);
            return PlotRect.yMin + Mathf.Clamp01(t) * PlotRect.height;
        }
    }
}
