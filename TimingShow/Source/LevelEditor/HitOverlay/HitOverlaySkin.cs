using UnityEngine;

namespace TimingShow.LevelEditor
{
    internal static class HitOverlaySkin
    {
        internal const float FontSize = 12f;
        internal const float RowHeight = 18f;
        internal const float Padding = 6f;
        internal const float GroupGap = 6f;
        internal const float PanelWidth = 280f;
        internal const float HeaderHeight = 20f;
        internal const float SectionHeight = 16f;
        internal const float ChipHeight = 16f;
        internal const float ChipGap = 4f;
        internal const float ChipPaddingX = 6f;
        internal const float SliderLabelWidth = 58f;
        internal const float SliderValueWidth = 44f;
        internal const float FooterHeight = 16f;

        internal static readonly Color Panel = Rgb(0x262626);
        internal static readonly Color Header = Rgb(0x2E2E2E);
        internal static readonly Color Section = Rgb(0x212121);
        internal static readonly Color Border = Rgb(0x4A4A4A);
        internal static readonly Color Separator = Rgb(0x383838);
        internal static readonly Color RowHover = Rgb(0x3A3A3A);
        internal static readonly Color DangerHover = Rgb(0x7A2E2E);
        internal static readonly Color Accent = Rgb(0x4274B3);
        internal static readonly Color TextPrimary = Rgb(0xD8D8D8);
        internal static readonly Color TextSecondary = Rgb(0x9A9A9A);
        internal static readonly Color TextDisabled = Rgb(0x6A6A6A);
        internal static readonly Color ChipBackground = Rgb(0x333333);
        internal static readonly Color ChipBorder = Rgb(0x4A4A4A);
        internal static readonly Color FieldBackground = Rgb(0x1B1B1B);
        internal static readonly Color Track = Rgb(0x1B1B1B);
        internal static readonly Color Knob = Rgb(0xB4B4B4);

        private static readonly Color OffsetPerfect = new Color(0.35f, 0.85f, 0.45f, 1f);
        private static readonly Color OffsetLate = new Color(0.95f, 0.85f, 0.30f, 1f);
        private static readonly Color OffsetMiss = new Color(0.95f, 0.35f, 0.30f, 1f);

        private static readonly Color[] PlayerPalette =
        {
            new Color(0.36f, 0.72f, 0.98f, 1f),
            new Color(0.98f, 0.62f, 0.30f, 1f),
            new Color(0.55f, 0.86f, 0.45f, 1f),
            new Color(0.92f, 0.45f, 0.62f, 1f),
            new Color(0.72f, 0.60f, 0.96f, 1f),
            new Color(0.95f, 0.84f, 0.38f, 1f),
            new Color(0.45f, 0.86f, 0.82f, 1f),
            new Color(0.86f, 0.86f, 0.86f, 1f)
        };

        internal static Color Rgb(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        internal static Color OffsetColor(float offsetMs)
        {
            float magnitude = Mathf.Abs(offsetMs);
            if (magnitude <= 15f) return Color.Lerp(OffsetPerfect, OffsetLate, magnitude / 15f);
            return Color.Lerp(OffsetLate, OffsetMiss, Mathf.Clamp01((magnitude - 15f) / 45f));
        }

        internal static Color PlayerColor(int playerId)
        {
            if (playerId < 0) return TextSecondary;
            return PlayerPalette[playerId % PlayerPalette.Length];
        }

        internal static float EstimateTextWidth(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= 0x2E80) width += fontSize;
                else if (c == ' ' || c == '.' || c == ',' || c == ':' || c == ';' || c == '|' || c == '!' || c == 'i' || c == 'l' || c == 'I')
                    width += fontSize * 0.34f;
                else
                    width += fontSize * 0.58f;
            }

            return width;
        }
    }
}
