using System.Globalization;
using TMPro;
using UnityEngine;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private const float RangeBarHeightBase = 22f;
        private const float RangeBarPaddingBase = 4f;
        private const float RangeLabelWidthBase = 76f;
        private const float RangeFieldWidthBase = 54f;
        private const float RangeSeparatorWidthBase = 12f;
        private const float RangeUnitWidthBase = 22f;
        private const float RangeResetWidthBase = 64f;
        private const float RangeGapBase = 4f;
        private const float RangeFontSizeBase = 11f;

        private RectTransform _rangeBar;
        private TMP_Text _rangeLabel;
        private LogRangeField _rangeMinField;
        private LogRangeField _rangeMaxField;
        private TMP_Text _rangeSeparator;
        private TMP_Text _rangeUnitText;
        private LogPillButton _rangeResetButton;
        private TMP_Text _rangeStatusText;

        private bool RangeFilterVisible
        {
            get { return _view == GraphView.Scatter && (_settingsUI == null || !_settingsUI.IsOpen); }
        }

        private float RangeStripHeight()
        {
            if (!RangeFilterVisible) return 0f;
            return (RangeBarHeightBase + RangeBarPaddingBase * 2f) * Mathf.Max(0.01f, UiScale);
        }

        private void CreateRangeFilter(TMP_FontAsset font)
        {
            GameObject barGo = new GameObject("RangeFilter", typeof(RectTransform));
            barGo.transform.SetParent(_windowRect, false);

            _rangeBar = (RectTransform)barGo.transform;
            _rangeBar.anchorMin = Vector2.zero;
            _rangeBar.anchorMax = Vector2.zero;
            _rangeBar.pivot = Vector2.zero;
            barGo.AddComponent<LogRangeBar>();

            _rangeLabel = CreateRangeText("Label", i18n.T("LogGraph_RangeFilter"), font, TextAlignmentOptions.MidlineLeft);
            _rangeMinField = CreateRangeField("MinField", font);
            _rangeSeparator = CreateRangeText("Separator", "~", font, TextAlignmentOptions.Center);
            _rangeMaxField = CreateRangeField("MaxField", font);
            _rangeUnitText = CreateRangeText("Unit", "", font, TextAlignmentOptions.MidlineLeft);
            _rangeResetButton = CreateRangeButton(font);
            _rangeStatusText = CreateRangeText("Status", "", font, TextAlignmentOptions.MidlineRight);
            _rangeStatusText.overflowMode = TextOverflowModes.Ellipsis;

            barGo.SetActive(false);
        }

        private TMP_Text CreateRangeText(string name, string content, TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_rangeBar, false);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset textFont = font != null ? font : TMP_Settings.defaultFontAsset;
            LogGraphFont.EnsureGlyphs(textFont, content);
            text.font = textFont;
            text.text = content;
            text.color = new Color(1f, 1f, 1f, 0.62f);
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private LogRangeField CreateRangeField(string name, TMP_FontAsset font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_rangeBar, false);

            LogRangeField field = go.AddComponent<LogRangeField>();
            field.Initialize(font, RangeFontSizeBase * UiScale, OnRangeFieldChanged);
            return field;
        }

        private LogPillButton CreateRangeButton(TMP_FontAsset font)
        {
            GameObject go = new GameObject("Reset", typeof(RectTransform));
            go.transform.SetParent(_rangeBar, false);

            LogPillButton button = go.AddComponent<LogPillButton>();
            button.Initialize(font, ResetRangeFilter);
            string label = i18n.T("Btn_ResetColor");
            TMP_FontAsset labelFont = font != null ? font : TMP_Settings.defaultFontAsset;
            LogGraphFont.EnsureGlyphs(labelFont, label);
            button.SetLabelOverflow(TextOverflowModes.Overflow);
            button.SetLabel(label);
            return button;
        }

        private void ApplyRangeFilterFont(TMP_FontAsset font)
        {
            ApplyRangeFont(_rangeLabel, font);
            ApplyRangeFont(_rangeSeparator, font);
            ApplyRangeFont(_rangeUnitText, font);
            ApplyRangeFont(_rangeStatusText, font);
            if (_rangeMinField != null) _rangeMinField.SetFont(font);
            if (_rangeMaxField != null) _rangeMaxField.SetFont(font);
            if (_rangeResetButton != null) _rangeResetButton.SetFont(font);
        }

        private static void ApplyRangeFont(TMP_Text text, TMP_FontAsset font)
        {
            if (text == null || font == null || text.font == font) return;
            text.font = font;
            text.SetVerticesDirty();
        }

        private void SyncRangeFilterVisible()
        {
            if (_rangeBar == null) return;

            bool visible = _root != null && RangeFilterVisible;
            bool wasVisible = _rangeBar.gameObject.activeSelf;
            if (wasVisible != visible)
            {
                if (!visible) EndRangeEditing();
                _rangeBar.gameObject.SetActive(visible);
            }

            if (!visible) return;

            PositionRangeFilter(_windowRect != null ? _windowRect.sizeDelta : Vector2.zero);
            UpdateRangeFilterStatus();

            if (!wasVisible)
            {
                Canvas.ForceUpdateCanvases();
                if (_rangeResetButton != null) _rangeResetButton.RefreshLabel();
            }
        }

        private void PositionRangeFilter(Vector2 size)
        {
            if (_rangeBar == null) return;

            float scale = Mathf.Max(0.01f, UiScale);
            float height = RangeBarHeightBase * scale;
            float pad = RangeBarPaddingBase * scale;
            float gap = RangeGapBase * scale;
            float labelWidth = RangeLabelWidthBase * scale;
            float fieldWidth = RangeFieldWidthBase * scale;
            float separatorWidth = RangeSeparatorWidthBase * scale;
            float unitWidth = RangeUnitWidthBase * scale;
            float resetWidth = RangeResetWidthBase * scale;

            Rect plot = ResolvedPlotRect();
            float width = Mathf.Max(plot.width, pad * 2f + labelWidth + fieldWidth * 2f + separatorWidth + unitWidth + resetWidth);
            float x = plot.x;
            float y = Mathf.Clamp(plot.yMax + pad, 0f, Mathf.Max(0f, size.y - height));

            _rangeBar.anchoredPosition = new Vector2(x, y);
            _rangeBar.sizeDelta = new Vector2(width, height);

            float fontSize = RangeFontSizeBase * scale;
            float fieldHeight = Mathf.Max(12f * scale, height - pad * 1.2f);
            float fieldY = (height - fieldHeight) * 0.5f;

            float cursor = pad;
            PlaceRangeChild(_rangeLabel.rectTransform, cursor, 0f, labelWidth, height);
            cursor += labelWidth;
            PlaceRangeChild(_rangeMinField.rectTransform, cursor, fieldY, fieldWidth, fieldHeight);
            cursor += fieldWidth;
            PlaceRangeChild(_rangeSeparator.rectTransform, cursor, 0f, separatorWidth, height);
            cursor += separatorWidth;
            PlaceRangeChild(_rangeMaxField.rectTransform, cursor, fieldY, fieldWidth, fieldHeight);
            cursor += fieldWidth;
            PlaceRangeChild(_rangeUnitText.rectTransform, cursor, 0f, unitWidth, height);
            cursor += unitWidth + gap;
            PlaceRangeChild(_rangeResetButton.rectTransform, cursor, fieldY, resetWidth, fieldHeight);
            cursor += resetWidth + gap;

            float statusWidth = Mathf.Max(0f, width - cursor - pad);
            PlaceRangeChild(_rangeStatusText.rectTransform, cursor, 0f, statusWidth, height);

            _rangeLabel.fontSize = fontSize;
            _rangeSeparator.fontSize = fontSize;
            _rangeUnitText.fontSize = fontSize;
            _rangeStatusText.fontSize = fontSize;
            _rangeMinField.SetFontSize(fontSize);
            _rangeMaxField.SetFontSize(fontSize);
            _rangeResetButton.SetFontSize(Mathf.Clamp(fontSize, 8f, 28f));
        }

        private static void PlaceRangeChild(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void TickRangeFilter()
        {
            if (_rangeBar == null || !_rangeBar.gameObject.activeInHierarchy) return;

            if (_rangeMinField != null) _rangeMinField.Tick();
            if (_rangeMaxField != null) _rangeMaxField.Tick();
            UpdateRangeFilterStatus();
        }

        private void OnRangeFieldChanged(string value)
        {
            ApplyRangeFilter();
        }

        private void ApplyRangeFilter()
        {
            bool hasMin = TryParseRangeValue(_rangeMinField != null ? _rangeMinField.Value : null, out float min);
            bool hasMax = TryParseRangeValue(_rangeMaxField != null ? _rangeMaxField.Value : null, out float max);
            if (_drawer != null) _drawer.SetOffsetRange(hasMin, min, hasMax, max);
            UpdateRangeFilterStatus();
        }

        private void ResetRangeFilter()
        {
            if (_rangeMinField != null) _rangeMinField.SetValueSilent("");
            if (_rangeMaxField != null) _rangeMaxField.SetValueSilent("");
            ApplyRangeFilter();
            EndRangeEditing();
        }

        private void UpdateRangeFilterStatus()
        {
            if (_rangeStatusText != null)
            {
                string status = _drawer != null && _drawer.HasOffsetRange
                    ? string.Format(CultureInfo.InvariantCulture, i18n.T("LogGraph_RangeInRange"), _drawer.RangeKeptCount)
                    : "";
                if (_rangeStatusText.text != status) _rangeStatusText.text = status;
            }

            if (_rangeUnitText != null)
            {
                string unit = _data != null && _data.IsAngle ? "°" : "ms";
                if (_rangeUnitText.text != unit) _rangeUnitText.text = unit;
            }
        }

        private static bool TryParseRangeValue(string text, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(text)) return false;

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void EndRangeEditing()
        {
            if (_rangeMinField != null) _rangeMinField.EndEdit();
            if (_rangeMaxField != null) _rangeMaxField.EndEdit();
        }

        private void EndRangeEditingOnClick(Vector2 screenPosition)
        {
            if (_rangeMinField != null && _rangeMinField.IsEditing &&
                !IsPointerInsideField(_rangeMinField, screenPosition))
            {
                _rangeMinField.EndEdit();
            }

            if (_rangeMaxField != null && _rangeMaxField.IsEditing &&
                !IsPointerInsideField(_rangeMaxField, screenPosition))
            {
                _rangeMaxField.EndEdit();
            }
        }

        private bool IsPointerInsideRangeFilter(Vector2 screenPosition)
        {
            if (_rangeBar == null || !_rangeBar.gameObject.activeInHierarchy) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(_rangeBar, screenPosition, null);
        }

        private static bool IsPointerInsideField(LogRangeField field, Vector2 screenPosition)
        {
            if (field == null || !field.gameObject.activeInHierarchy) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(field.rectTransform, screenPosition, null);
        }
    }
}
