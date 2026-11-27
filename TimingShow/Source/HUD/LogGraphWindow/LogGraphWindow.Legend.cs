using TMPro;
using UnityEngine;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private const float JudgeLegendHeightBase = 22f;
        private const float JudgeLegendPaddingBase = 4f;
        private const float JudgeLegendGapBase = 3f;

        private RectTransform _judgeLegendBar;
        private readonly LogJudgeLegendChip[] _judgeLegendChips = new LogJudgeLegendChip[LogScatterJudgeLegend.Count];

        private bool JudgeLegendVisible
        {
            get
            {
                if (_view != GraphView.Scatter) return false;
                if (_settingsUI != null && _settingsUI.IsOpen) return false;
                return _drawer != null && _drawer.ShowJudgeLegend;
            }
        }

        private float JudgeLegendStripHeight()
        {
            if (!JudgeLegendVisible) return 0f;
            return (JudgeLegendHeightBase + JudgeLegendPaddingBase * 2f) * Mathf.Max(0.01f, UiScale);
        }

        private void CreateJudgeLegend(TMP_FontAsset font)
        {
            GameObject barGo = new GameObject("JudgeLegend", typeof(RectTransform));
            barGo.transform.SetParent(_windowRect, false);

            _judgeLegendBar = (RectTransform)barGo.transform;
            _judgeLegendBar.anchorMin = Vector2.zero;
            _judgeLegendBar.anchorMax = Vector2.zero;
            _judgeLegendBar.pivot = Vector2.zero;

            TMP_FontAsset chipFont = font != null ? font : TMP_Settings.defaultFontAsset;
            for (int i = 0; i < _judgeLegendChips.Length; i++)
            {
                int index = i;
                string label = i18n.T(LogScatterJudgeLegend.LabelKey(i));
                LogGraphFont.EnsureGlyphs(chipFont, label);

                GameObject chipGo = new GameObject("Chip" + index, typeof(RectTransform));
                chipGo.transform.SetParent(_judgeLegendBar, false);

                LogJudgeLegendChip chip = chipGo.AddComponent<LogJudgeLegendChip>();
                chip.Initialize(chipFont, LogScatterJudgeLegend.SwatchColor(i), label, () => ToggleJudgeLegend(index));
                _judgeLegendChips[i] = chip;
            }

            barGo.SetActive(false);
        }

        private void ApplyJudgeLegendFont(TMP_FontAsset font)
        {
            if (font == null) return;

            for (int i = 0; i < _judgeLegendChips.Length; i++)
            {
                if (_judgeLegendChips[i] != null) _judgeLegendChips[i].SetFont(font);
            }
        }

        private void SyncJudgeLegendVisible()
        {
            if (_judgeLegendBar == null) return;

            bool visible = _root != null && JudgeLegendVisible;
            bool wasVisible = _judgeLegendBar.gameObject.activeSelf;
            if (wasVisible != visible) _judgeLegendBar.gameObject.SetActive(visible);
            if (!visible) return;

            PositionJudgeLegend(_windowRect != null ? _windowRect.sizeDelta : Vector2.zero);
            RefreshJudgeLegendState();

            if (!wasVisible)
            {
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < _judgeLegendChips.Length; i++)
                {
                    if (_judgeLegendChips[i] != null) _judgeLegendChips[i].RefreshLabel();
                }
            }
        }

        private float ToolbarStripHeight()
        {
            float range = RangeStripHeight();
            float legend = JudgeLegendStripHeight();
            if (legend <= 0f) return range;
            if (range <= 0f) return legend;
            return ToolbarFitsOneRow() ? Mathf.Max(range, legend) : range + legend;
        }

        private bool ToolbarFitsOneRow()
        {
            float scale = Mathf.Max(0.01f, UiScale);
            float legendWidth = JudgeLegendVisible ? JudgeLegendRowWidth() : 0f;
            if (legendWidth <= 0f) return true;
            if (!RangeFilterVisible) return true;

            float needed = legendWidth + RangeControlsWidth() + JudgeLegendPaddingBase * 2f * scale;
            return needed <= ResolvedPlotRect().width;
        }

        private float JudgeLegendRowWidth()
        {
            float scale = Mathf.Max(0.01f, UiScale);
            float gap = JudgeLegendGapBase * scale;
            float total = 0f;
            for (int i = 0; i < _judgeLegendChips.Length; i++)
            {
                LogJudgeLegendChip chip = _judgeLegendChips[i];
                if (chip == null) continue;
                chip.SetScale(scale);
                total += chip.MeasureWidth() + gap;
            }

            return Mathf.Max(0f, total - gap);
        }

        private void PositionJudgeLegend(Vector2 size)
        {
            if (_judgeLegendBar == null) return;

            float scale = Mathf.Max(0.01f, UiScale);
            float height = JudgeLegendHeightBase * scale;
            float gap = JudgeLegendGapBase * scale;
            float inset = RangeBarPaddingBase * scale;
            Rect plot = ResolvedPlotRect();

            float cursor = 0f;
            for (int i = 0; i < _judgeLegendChips.Length; i++)
            {
                LogJudgeLegendChip chip = _judgeLegendChips[i];
                if (chip == null) continue;

                chip.SetScale(scale);
                float width = chip.MeasureWidth();
                PlaceLegendChild(chip.rectTransform, cursor, 0f, width, height);
                cursor += width + gap;
            }

            float barWidth = Mathf.Max(0f, cursor - gap);
            float y;
            if (RangeFilterVisible && !ToolbarFitsOneRow())
            {
                y = plot.yMax + RangeStripHeight() + JudgeLegendPaddingBase * scale;
            }
            else
            {
                float centering = RangeFilterVisible ? (RangeBarHeightBase - JudgeLegendHeightBase) * 0.5f * scale : 0f;
                y = plot.yMax + inset + centering;
            }
            y = Mathf.Clamp(y, 0f, Mathf.Max(0f, size.y - height));
            _judgeLegendBar.anchoredPosition = new Vector2(plot.x + inset, y);
            _judgeLegendBar.sizeDelta = new Vector2(barWidth, height);
        }

        private static void PlaceLegendChild(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void RefreshJudgeLegendState()
        {
            if (_drawer == null) return;

            for (int i = 0; i < _judgeLegendChips.Length; i++)
            {
                if (_judgeLegendChips[i] != null) _judgeLegendChips[i].SetChecked(_drawer.IsJudgeClassVisible(i));
            }
        }

        private void ToggleJudgeLegend(int index)
        {
            if (_drawer == null) return;

            _drawer.ToggleJudgeClass(index);
            RefreshJudgeLegendState();
        }
    }
}
