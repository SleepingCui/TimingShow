using UnityEngine;
using TMPro;

namespace TimingShow.HUD.ScatterDrawerBase
{
    public abstract partial class ScatterDrawerBase
    {
        private TMP_Text _xStartText;
        private TMP_Text _xEndText;
        private TMP_Text _avgValueText;
        private TMP_Text[] _xTickTexts;
        private TMP_Text[] _yTickTexts;

        protected override void CreateTextComponents()
        {
            base.CreateTextComponents();

            _xStartText = CreateText("XStartLabel", CurrentFontAsset, TextAnchor.UpperLeft);
            _xEndText = CreateText("XEndLabel", CurrentFontAsset, TextAnchor.UpperRight);
            _avgValueText = CreateText("AvgValueLabel", CurrentFontAsset, TextAnchor.MiddleLeft);
            _detailText = CreateText("DetailLabel", CurrentFontAsset, TextAnchor.LowerLeft);
            _detailText.richText = true;
            _detailText.overflowMode = TextOverflowModes.Overflow;
            _detailText.gameObject.SetActive(false);
            _hintText = CreateText("GestureHintLabel", CurrentFontAsset, TextAnchor.LowerCenter);
            _hintText.alignment = TextAlignmentOptions.Bottom;
            _hintText.gameObject.SetActive(false);

            CreateAxisTickTexts();
            CreatePlayerLegendTexts();

            TMP_Text[] children = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < children.Length; i++) children[i].raycastTarget = false;
        }

        private void CreateAxisTickTexts()
        {
            _xTickTexts = new TMP_Text[MaxXAxisTicks];
            for (int i = 0; i < _xTickTexts.Length; i++)
            {
                TMP_Text text = CreateText("XAxisTick" + i, CurrentFontAsset, TextAnchor.LowerCenter);
                text.gameObject.SetActive(false);
                _xTickTexts[i] = text;
            }

            _yTickTexts = new TMP_Text[MaxYAxisTicks];
            for (int i = 0; i < _yTickTexts.Length; i++)
            {
                TMP_Text text = CreateText("YAxisTick" + i, CurrentFontAsset, TextAnchor.MiddleLeft);
                text.gameObject.SetActive(false);
                _yTickTexts[i] = text;
            }
        }

        protected override void ApplyFontToTexts(TMP_FontAsset font)
        {
            base.ApplyFontToTexts(font);
            ApplyFontToText(_xStartText, font);
            ApplyFontToText(_xEndText, font);
            ApplyFontToText(_avgValueText, font);
            ApplyFontToText(_detailText, font);
            ApplyFontToText(_hintText, font);
            if (_xTickTexts != null) for (int i = 0; i < _xTickTexts.Length; i++) ApplyFontToText(_xTickTexts[i], font);
            if (_yTickTexts != null) for (int i = 0; i < _yTickTexts.Length; i++) ApplyFontToText(_yTickTexts[i], font);
            ApplyPlayerLegendFont(font);
        }

        protected override void ToggleTexts(bool active)
        {
            base.ToggleTexts(active);

            bool showXAxisLabels = active && !UsesDynamicAxisTicks;
            if (_xStartText != null && _xStartText.gameObject.activeSelf != showXAxisLabels) _xStartText.gameObject.SetActive(showXAxisLabels);
            if (_xEndText != null && _xEndText.gameObject.activeSelf != showXAxisLabels) _xEndText.gameObject.SetActive(showXAxisLabels);

            bool showTicks = active && UsesDynamicAxisTicks;
            if (_xTickTexts != null) for (int i = 0; i < _xTickTexts.Length; i++)
            {
                if (_xTickTexts[i] != null && _xTickTexts[i].gameObject.activeSelf != showTicks) _xTickTexts[i].gameObject.SetActive(showTicks);
            }
            if (_yTickTexts != null) for (int i = 0; i < _yTickTexts.Length; i++)
            {
                if (_yTickTexts[i] != null && _yTickTexts[i].gameObject.activeSelf != showTicks) _yTickTexts[i].gameObject.SetActive(showTicks);
            }

            if (_avgValueText != null)
            {
                bool wantAvg = active && _avgLabelVisible;
                if (_avgValueText.gameObject.activeSelf != wantAvg) _avgValueText.gameObject.SetActive(wantAvg);
            }

            if (_detailText != null)
            {
                bool wantDetail = active && _detailVisible;
                if (_detailText.gameObject.activeSelf != wantDetail) _detailText.gameObject.SetActive(wantDetail);
            }

            TogglePlayerLegend(active);
        }

        protected override void UpdateTextLayoutAndValues()
        {
            base.UpdateTextLayoutAndValues();

            if (_xStartText == null || _xEndText == null) return;

            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(11 * scale), 8, 32);
            float labelHeight = 16f * (fontSize / 12f);
            Color color = ScatterAxisTextColor;

            bool labelsInside = AxisLabelsInside;
            float axisY;
            if (labelsInside)
            {
                axisY = 3f * scale;
                if (AxisBandBottom > 1f) axisY = Mathf.Min(axisY, Mathf.Max(0f, AxisBandBottom - labelHeight));
            }
            else
            {
                axisY = -3f * scale;
            }

            if (UsesDynamicAxisTicks)
            {
                if (_xStartText.gameObject.activeSelf) _xStartText.gameObject.SetActive(false);
                if (_xEndText.gameObject.activeSelf) _xEndText.gameObject.SetActive(false);
                UpdateAxisTickLabels(w, h, scale, labelsInside, axisY, color);
            }
            else
            {
                string startText = string.Empty;
                string endText = string.Empty;

                if (_hasData)
                {
                    if (_useTimeAxis)
                    {
                        startText = _manualViewActive ? FormatDetailTime(_viewXMin) : $"{_viewXMin / 1000f:F1}s";
                        endText = _manualViewActive ? FormatDetailTime(_viewXMax) : $"{_viewXMax / 1000f:F1}s";
                    }
                    else
                    {
                        startText = $"#{Mathf.Max(0, Mathf.RoundToInt(_viewXMin)) + 1}";
                        endText = $"#{Mathf.Max(0, Mathf.RoundToInt(_viewXMax)) + 1}";
                    }
                }

                Vector2 startPivot = labelsInside ? new Vector2(0f, 0f) : new Vector2(0f, 1f);
                Vector2 endPivot = labelsInside ? new Vector2(1f, 0f) : new Vector2(1f, 1f);
                TextAlignmentOptions startAlign = labelsInside ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.TopLeft;
                TextAlignmentOptions endAlign = labelsInside ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.TopRight;

                float xStartX;
                float xEndX;
                float xLabelMaxWidth;
                if (labelsInside)
                {
                    xStartX = AxisBandLeft > 1f ? Mathf.Min(AxisBandLeft, w * 0.5f) : 2f * scale;
                    xEndX = w - 2f * scale;
                    xLabelMaxWidth = Mathf.Max(1f, (xEndX - xStartX) * 0.5f);
                }
                else
                {
                    xStartX = 2f * scale;
                    xEndX = w - 2f * scale;
                    xLabelMaxWidth = Mathf.Max(1f, w - 4f * scale);
                }

                SetupXAxisText(_xStartText, startText, new Vector2(xStartX, axisY),
                    startPivot, startAlign, fontSize, labelHeight, color, xLabelMaxWidth);
                SetupXAxisText(_xEndText, endText, new Vector2(xEndX, axisY),
                    endPivot, endAlign, fontSize, labelHeight, color, xLabelMaxWidth);

                SetAxisTickTextsActive(false);
            }

            ReportAxisFooterHeight(Mathf.Abs(axisY) + labelHeight + 1f);

            UpdatePlayerLegendLayout(w, h);

            bool showAvgLabel = _avgAvailable && ScatterShowAvgLine;
            UpdateAvgValueLabel(showAvgLabel, settings, scale, w, h);
        }

        private void SetAxisTickTextsActive(bool active)
        {
            if (_xTickTexts != null) for (int i = 0; i < _xTickTexts.Length; i++)
            {
                if (_xTickTexts[i] != null && _xTickTexts[i].gameObject.activeSelf != active) _xTickTexts[i].gameObject.SetActive(active);
            }
            if (_yTickTexts != null) for (int i = 0; i < _yTickTexts.Length; i++)
            {
                if (_yTickTexts[i] != null && _yTickTexts[i].gameObject.activeSelf != active) _yTickTexts[i].gameObject.SetActive(active);
            }
        }

        private void UpdateAxisTickLabels(float w, float h, float scale, bool labelsInside, float axisY, Color color)
        {
            ComputeAxisTicks(w, h, scale);

            int xFontSize = Mathf.Clamp(Mathf.RoundToInt(10f * scale), 8, 24);
            int yFontSize = Mathf.Clamp(Mathf.RoundToInt(10f * scale), 8, 24);
            float xLabelHeight = 16f * (xFontSize / 12f);
            float yBoxHeight = 18f * (yFontSize / 12f);
            Color tickColor = color;
            tickColor.a *= 0.85f;

            for (int i = 0; i < _xTickTexts.Length; i++)
            {
                TMP_Text text = _xTickTexts[i];
                if (text == null) continue;

                bool show = i < _xTickLabels.Count;
                if (text.gameObject.activeSelf != show) text.gameObject.SetActive(show);
                if (!show) continue;

                string label = _xTickLabels[i];
                text.text = label;
                text.fontSize = xFontSize;
                text.color = tickColor;

                float fraction = _xTickFractions[i];
                bool leftEdge = fraction <= 0.0001f;
                bool rightEdge = fraction >= 0.9999f;
                float x = leftEdge ? 1f : rightEdge ? Mathf.Max(1f, w - 1f) : fraction * w;

                Vector2 pivot;
                TextAlignmentOptions alignment;
                if (labelsInside)
                {
                    pivot = leftEdge ? new Vector2(0f, 0f) : rightEdge ? new Vector2(1f, 0f) : new Vector2(0.5f, 0f);
                    alignment = leftEdge ? TextAlignmentOptions.BottomLeft : rightEdge ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.Bottom;
                }
                else
                {
                    pivot = leftEdge ? new Vector2(0f, 1f) : rightEdge ? new Vector2(1f, 1f) : new Vector2(0.5f, 1f);
                    alignment = leftEdge ? TextAlignmentOptions.TopLeft : rightEdge ? TextAlignmentOptions.TopRight : TextAlignmentOptions.Top;
                }

                text.alignment = alignment;
                float measured = MeasureTextWidth(text, label, xFontSize);
                if (!leftEdge && !rightEdge)
                    x = Mathf.Clamp(x, measured * 0.5f + 1f, Mathf.Max(measured * 0.5f + 1f, w - measured * 0.5f - 1f));

                float labelY = labelsInside ? axisY + 2f * scale : axisY;
                RectTransform rt = text.rectTransform;
                rt.pivot = pivot;
                rt.anchoredPosition = new Vector2(x, labelY);
                rt.sizeDelta = new Vector2(measured, xLabelHeight);
            }

            float maxYWidth = 0f;
            for (int i = 0; i < _yTickTexts.Length; i++)
            {
                TMP_Text text = _yTickTexts[i];
                if (text == null) continue;

                bool show = i < _yTickLabels.Count;
                if (text.gameObject.activeSelf != show) text.gameObject.SetActive(show);
                if (!show) continue;

                string label = _yTickLabels[i];
                text.text = label;
                text.fontSize = yFontSize;
                text.color = tickColor;
                text.alignment = labelsInside ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;

                float fraction = _yTickFractions[i];
                bool bottomEdge = fraction <= 0.0001f;
                bool topEdge = fraction >= 0.9999f;
                float y = bottomEdge ? 1f : topEdge ? Mathf.Max(1f, h - 1f) : fraction * h;
                RectTransform rt = text.rectTransform;
                rt.pivot = new Vector2(labelsInside ? 0f : 1f, bottomEdge ? 0f : topEdge ? 1f : 0.5f);
                rt.anchoredPosition = new Vector2(labelsInside ? AxisLabelGap : -AxisLabelGap, y);

                float width = MeasureTextWidth(text, label, yFontSize);
                rt.sizeDelta = new Vector2(width, yBoxHeight);
                if (width > maxYWidth) maxYWidth = width;
            }

            if (maxYWidth > 0f) ReportAxisLabelWidth(maxYWidth);
        }

        private void UpdateAvgValueLabel(bool visible, Settings settings, float scale, float w, float h)
        {
            if (_avgValueText == null) return;

            _avgLabelVisible = visible;
            if (_avgValueText.gameObject.activeSelf != visible) _avgValueText.gameObject.SetActive(visible);
            if (!visible) return;

            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            float currentAvg = _avgSmoothValid ? _avgSmoothValue : _avgCurrent;
            float ny = Mathf.Clamp01((currentAvg - _minY) / rangeY);

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 8, 32);
            _avgValueText.fontSize = fontSize;
            string avgText = "Avg: " + FormatYLabel(currentAvg);
            if (_avgValueText.text != avgText) _avgValueText.text = avgText;
            _avgValueText.color = ScatterAvgLineColor;
            _avgValueText.alignment = AxisLabelsInside ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

            float textWidth = Mathf.Max(24f, MeasureTextWidth(_avgValueText, avgText, fontSize));
            float labelWidth = textWidth;
            if (AvgLabelMaxWidthRatio < 1f)
            {
                float ratioWidth = Mathf.Max(24f, w * Mathf.Clamp01(AvgLabelMaxWidthRatio));
                if (labelWidth > ratioWidth) labelWidth = ratioWidth;
            }
            if (AxisBandRight > 1f)
            {
                float bandWidth = Mathf.Max(24f, AxisBandRight - 6f * scale);
                if (labelWidth > bandWidth) labelWidth = bandWidth;
            }
            if (AxisLabelsInside) labelWidth = Mathf.Min(labelWidth, Mathf.Max(1f, w - 6f * scale));
            _avgValueText.overflowMode = labelWidth < textWidth ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;

            float boxHeight = 24f * (fontSize / 12f);
            float centerY = ny * h;
            if (AxisLabelsInside)
            {
                boxHeight = Mathf.Min(boxHeight, Mathf.Max(1f, h));
                float half = boxHeight * 0.5f;
                centerY = Mathf.Clamp(centerY, half, Mathf.Max(half, h - half));
            }
            else if (AxisBandTop > 1f && AxisBandBottom > 1f)
            {
                float up = (h - ny * h) + AxisBandTop;
                float down = ny * h + AxisBandBottom;
                boxHeight = Mathf.Min(boxHeight, Mathf.Max(1f, 2f * Mathf.Min(up, down)));
            }

            RectTransform rt = _avgValueText.rectTransform;
            rt.pivot = AxisLabelsInside ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(AxisLabelsInside ? w - 6f * scale : w + 6f * scale, centerY);
            rt.sizeDelta = new Vector2(labelWidth, boxHeight);
        }

        private void SetupXAxisText(TMP_Text t, string content, Vector2 anchoredPosition, Vector2 pivot,
            TextAlignmentOptions alignment, int fontSize, float height, Color color, float maxWidth = 0f)
        {
            if (t == null) return;

            t.fontSize = fontSize;
            if (t.text != content) t.text = content;
            t.color = color;
            t.alignment = alignment;

            float measured = Mathf.Max(1f, MeasureTextWidth(t, content, fontSize));
            float boxWidth = measured;
            if (maxWidth > 0f) boxWidth = Mathf.Min(boxWidth, Mathf.Max(1f, maxWidth));
            t.overflowMode = boxWidth < measured ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;

            float boxHeight = Mathf.Max(1f, height);
            if (AxisBandBottom > 1f) boxHeight = Mathf.Min(boxHeight, Mathf.Max(1f, AxisBandBottom - Mathf.Abs(anchoredPosition.y)));

            RectTransform rt = t.rectTransform;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(boxWidth, boxHeight);
        }

    }
}
