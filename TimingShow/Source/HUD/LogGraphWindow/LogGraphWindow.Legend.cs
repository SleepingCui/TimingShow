using TMPro;
using UnityEngine;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private const float JudgeLegendHeightBase = 18f;
        private const float JudgeLegendPaddingBase = 4f;
        private const float JudgeLegendGapBase = 6f;

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

        private void PositionJudgeLegend(Vector2 size)
        {
            if (_judgeLegendBar == null) return;

            float scale = Mathf.Max(0.01f, UiScale);
            float height = JudgeLegendHeightBase * scale;
            float pad = JudgeLegendPaddingBase * scale;
            float gap = JudgeLegendGapBase * scale;
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
            float y = Mathf.Clamp(plot.yMax + RangeStripHeight() + pad, 0f, Mathf.Max(0f, size.y - height));
            _judgeLegendBar.anchoredPosition = new Vector2(plot.x, y);
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
