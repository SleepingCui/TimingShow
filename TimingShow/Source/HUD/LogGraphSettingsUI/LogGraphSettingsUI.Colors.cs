using TMPro;
using UnityEngine;

namespace TimingShow.HUD.LogGraphSettingsUI
{
    public sealed partial class LogGraphSettingsUI 
    {
        private const int ColorSlotCount = 6;

        private static readonly string[] ColorSlotKeys =
        {
            "Label_BgColor",
            "Label_GridColor",
            "Label_PointColor",
            "Label_ZeroLineColor",
            "Label_AvgLineColor",
            "Label_AxisTextColor"
        };

        private static readonly string[] ColorChannelKeys =
        {
            "LogGraph_ColorChannelR",
            "LogGraph_ColorChannelG",
            "LogGraph_ColorChannelB"
        };

        private Color GetColorSlot(int index)
        {
            switch (index)
            {
                case 1: return _draft.GridColor;
                case 2: return _draft.PointColor;
                case 3: return _draft.ZeroLineColor;
                case 4: return _draft.AvgLineColor;
                case 5: return _draft.AxisTextColor;
                default: return _draft.BgColor;
            }
        }

        private void SetColorSlot(int index, Color color)
        {
            switch (index)
            {
                case 1: _draft.GridColor = color; break;
                case 2: _draft.PointColor = color; break;
                case 3: _draft.ZeroLineColor = color; break;
                case 4: _draft.AvgLineColor = color; break;
                case 5: _draft.AxisTextColor = color; break;
                default: _draft.BgColor = color; break;
            }
        }

        private float GetColorChannel(int slot, int channel)
        {
            Color color = GetColorSlot(slot);
            return channel == 0 ? color.r : channel == 1 ? color.g : color.b;
        }

        private void SetColorChannel(int slot, int channel, float value)
        {
            Color color = GetColorSlot(slot);
            color.r = channel == 0 ? value : color.r;
            color.g = channel == 1 ? value : color.g;
            color.b = channel == 2 ? value : color.b;
            SetColorSlot(slot, color);
        }

        private void ResetColorSlot(int index)
        {
            Color color = DefaultColorSlot(index);
            color.a = GetColorSlot(index).a;
            SetColorSlot(index, color);

            _draftDirty = true;
            _activeRow = null;
            _hoverRow = null;
            RebuildRows();
            MarkDirty();
        }

        private static Color DefaultColorSlot(int index)
        {
            switch (index)
            {
                case 1: return Settings.LogGraph_DefaultGridColor;
                case 2: return Settings.LogGraph_DefaultPointColor;
                case 3: return Settings.LogGraph_DefaultZeroLineColor;
                case 4: return Settings.LogGraph_DefaultAvgLineColor;
                case 5: return Settings.LogGraph_DefaultAxisTextColor;
                default: return Settings.LogGraph_DefaultBgColor;
            }
        }

        private static string ColorPreviewText(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color) + "    A " + color.a.ToString("F2");
        }

        private static Color ColorPreviewTextColor(Color color)
        {
            float luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            return luminance > 0.55f ? new Color(0.06f, 0.07f, 0.10f, 1f) : new Color(0.94f, 0.95f, 0.98f, 1f);
        }

        private void RefreshColorPreview()
        {
            if (_colorPreviewRow == null || _colorPreviewRow.Value == null) return;

            Color color = GetColorSlot(_colorIndex);
            _colorPreviewRow.Value.text = ColorPreviewText(color);
            _colorPreviewRow.Value.color = ColorPreviewTextColor(color);
        }

        internal static float ColorSwatchSize(float rowHeight, float scale)
        {
            return Mathf.Max(6f, Mathf.Min(rowHeight * 0.62f, 14f * scale));
        }

        internal static Rect ColorResetRect(Rect chipRect, float scale)
        {
            float resetWidth = Mathf.Min(Mathf.Max(1f, chipRect.width) * 0.30f, 76f * scale);
            float resetInset = Mathf.Max(1f, 3f * scale);

            return new Rect(
                chipRect.xMax - resetWidth - resetInset,
                chipRect.y + resetInset,
                Mathf.Max(1f, resetWidth),
                Mathf.Max(1f, chipRect.height - resetInset * 2f));
        }

        internal static float ColorChipLabelWidth(Rect chipRect, float scale)
        {
            float swatch = ColorSwatchSize(chipRect.height, scale);
            float reserved = ColorResetRect(chipRect, scale).width + 4f * scale;
            return chipRect.width - swatch - 11f * scale - reserved;
        }


        private void AddColorChip(Row row, float fontSize)
        {
            row.Label = CreateText("ColorChip" + row.ColorIndex, i18n.T(row.LabelKey),
                TextAlignmentOptions.MidlineLeft, fontSize, TextColor);

            float scale = UiScale;
            float swatch = ColorSwatchSize(row.Rect.height, scale);
            Rect textRect = new Rect(
                row.Rect.x + 4f * scale + swatch + 4f * scale,
                row.Rect.y,
                Mathf.Max(1f, ColorChipLabelWidth(row.Rect, scale)),
                row.Rect.height);

            PlaceText(row.Label, textRect, _panelWidth, _panelHeight, false);

            if (row.ResetRect.width > 0f)
            {
                row.ResetLabel = CreateText("ColorReset" + row.ColorIndex, i18n.T("Btn_ResetColor"),
                    TextAlignmentOptions.Center, Mathf.Max(8f, fontSize), TextColor);
                PlaceText(row.ResetLabel, row.ResetRect, _panelWidth, _panelHeight, false);
            }

            _rows.Add(row);
        }

        private void AddColorPreview(Row row, float fontSize)
        {
            Color color = GetColorSlot(_colorIndex);

            row.Value = CreateText("ColorPreviewValue", ColorPreviewText(color),
                TextAlignmentOptions.Center, Mathf.Max(8f, fontSize), ColorPreviewTextColor(color));
            PlaceText(row.Value, row.Rect, _panelWidth, _panelHeight, false);

            _colorPreviewRow = row;
            _rows.Add(row);
        }

    }
}
