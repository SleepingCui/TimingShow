using System.Collections.Generic;
using UnityEngine;

namespace TimingShow.Options
{
    internal static class NewUISkin
    {
        internal const float CardRadius = 10f;
        internal const float ElementRadius = 6f;

        private const int MaxRadius = 30;

        private static readonly Dictionary<int, Texture2D> WhiteTextures = new Dictionary<int, Texture2D>();
        private static readonly Dictionary<long, Texture2D> ColoredTextures = new Dictionary<long, Texture2D>();
        private static readonly Dictionary<int, GUIStyle> RoundedStyles = new Dictionary<int, GUIStyle>();

        internal static Color Background;
        internal static Color Panel;
        internal static Color PanelAlt;
        internal static Color Element;
        internal static Color ElementHover;
        internal static Color ElementActive;
        internal static Color Accent;
        internal static Color AccentHover;
        internal static Color AccentActive;
        internal static Color Text;
        internal static Color TextDim;
        internal static Color TextMuted;
        internal static Color Border;
        internal static Color Field;

        internal static GUIStyle PageStyle;
        internal static GUIStyle CardStyle;
        internal static GUIStyle LabelStyle;
        internal static GUIStyle DimLabelStyle;
        internal static GUIStyle MutedLabelStyle;
        internal static GUIStyle ValueStyle;
        internal static GUIStyle SectionStyle;
        internal static GUIStyle CenterStyle;
        internal static GUIStyle CenterDimStyle;
        internal static GUIStyle CenterMutedStyle;
        internal static GUIStyle FieldStyle;

        private static bool _built;
        private static GUISkin _skin;
        private static Color _sourceBackground;
        private static Color _sourceElement;
        private static Color _sourceAccent;
        private static Color _sourceText;
        private static int _fontSize = 12;
        private static int _revision;

        internal static int FontSize
        {
            get { return _fontSize; }
        }

        internal static int Revision
        {
            get { return _revision; }
        }

        internal static void Ensure()
        {
            Settings settings = ModContext.Settings;
            Color background = settings != null ? settings.Theme_Background : Settings.Theme_DefaultBackground;
            Color element = settings != null ? settings.Theme_Element : Settings.Theme_DefaultElement;
            Color accent = settings != null ? settings.Theme_Primary : Settings.Theme_DefaultPrimary;
            Color text = settings != null ? settings.Theme_Text : Settings.Theme_DefaultText;

            if (_built && _skin == GUI.skin &&
                SameColor(_sourceBackground, background) &&
                SameColor(_sourceElement, element) &&
                SameColor(_sourceAccent, accent) &&
                SameColor(_sourceText, text))
            {
                return;
            }

            _sourceBackground = background;
            _sourceElement = element;
            _sourceAccent = accent;
            _sourceText = text;
            _built = true;

            ApplyPalette(background, element, accent, text);
            Rebuild();
        }

        internal static void ApplyPalette(Color background, Color element, Color accent, Color text)
        {
            background.a = 1f;
            element.a = 1f;
            accent.a = 1f;
            text.a = 1f;

            Background = background;
            Panel = Lighten(background, 0.035f);
            PanelAlt = Lighten(background, 0.075f);
            Element = element;
            ElementHover = Lighten(element, 0.10f);
            ElementActive = Lighten(element, -0.10f);
            Accent = accent;
            AccentHover = Lighten(accent, 0.12f);
            AccentActive = Lighten(accent, -0.15f);
            Text = text;
            TextDim = Color.Lerp(text, background, 0.35f);
            TextMuted = Color.Lerp(text, background, 0.55f);
            Border = Lighten(element, 0.14f);
            Field = Lighten(background, -0.35f);
        }

        internal static Color Lighten(Color color, float amount)
        {
            Color result = amount >= 0f ? Color.Lerp(color, Color.white, amount) : Color.Lerp(color, Color.black, -amount);
            result.a = color.a;
            return result;
        }

        internal static Color Fade(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        private static bool SameColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.0005f && Mathf.Abs(a.g - b.g) < 0.0005f &&
                   Mathf.Abs(a.b - b.b) < 0.0005f && Mathf.Abs(a.a - b.a) < 0.0005f;
        }

        private static void Rebuild()
        {
            _revision++;
            _skin = GUI.skin;
            int baseSize = _skin != null && _skin.label != null && _skin.label.fontSize > 0 ? _skin.label.fontSize : 12;
            _fontSize = Mathf.Max(11, baseSize);

            PageStyle = new GUIStyle();
            PageStyle.normal.background = ColoredRounded(14, Panel);
            PageStyle.border = new RectOffset(14, 14, 14, 14);
            PageStyle.padding = new RectOffset(14, 14, 12, 14);
            PageStyle.margin = new RectOffset(0, 0, 0, 0);

            CardStyle = new GUIStyle();
            CardStyle.normal.background = ColoredRounded(12, PanelAlt);
            CardStyle.border = new RectOffset(12, 12, 12, 12);
            CardStyle.padding = new RectOffset(10, 10, 8, 10);
            CardStyle.margin = new RectOffset(0, 0, 0, 6);

            LabelStyle = TextStyle(_fontSize, Text, TextAnchor.MiddleLeft);
            DimLabelStyle = TextStyle(_fontSize, TextDim, TextAnchor.MiddleLeft);
            MutedLabelStyle = TextStyle(_fontSize - 1, TextMuted, TextAnchor.MiddleLeft);
            ValueStyle = TextStyle(_fontSize, Text, TextAnchor.MiddleRight);
            SectionStyle = TextStyle(_fontSize + 2, Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            CenterStyle = TextStyle(_fontSize, Text, TextAnchor.MiddleCenter);
            CenterDimStyle = TextStyle(_fontSize, TextDim, TextAnchor.MiddleCenter);
            CenterMutedStyle = TextStyle(_fontSize, TextMuted, TextAnchor.MiddleCenter);

            FieldStyle = new GUIStyle
            {
                font = _skin != null ? _skin.font : null,
                fontSize = _fontSize,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(7, 7, 3, 3),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                richText = false
            };
            SetFieldColors(FieldStyle);
        }

        private static void SetFieldColors(GUIStyle style)
        {
            ClearBackgrounds(style.normal);
            ClearBackgrounds(style.hover);
            ClearBackgrounds(style.active);
            ClearBackgrounds(style.focused);
            ClearBackgrounds(style.onNormal);
            ClearBackgrounds(style.onHover);
            ClearBackgrounds(style.onActive);
            ClearBackgrounds(style.onFocused);

            style.normal.textColor = Text;
            style.hover.textColor = Text;
            style.active.textColor = Text;
            style.focused.textColor = Text;
            style.onNormal.textColor = Text;
            style.onHover.textColor = Text;
            style.onActive.textColor = Text;
            style.onFocused.textColor = Text;
        }

        private static void ClearBackgrounds(GUIStyleState state)
        {
            state.background = null;
            state.textColor = Color.white;
        }

        private static GUIStyle TextStyle(int size, Color color, TextAnchor anchor, FontStyle fontStyle = FontStyle.Normal)
        {
            GUIStyle style = new GUIStyle
            {
                font = _skin != null ? _skin.font : null,
                fontSize = size,
                fontStyle = fontStyle,
                alignment = anchor,
                richText = false,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
            return style;
        }

        internal static void DrawRect(Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f) return;
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        internal static void DrawRounded(Rect rect, float radius, Color color)
        {
            if (rect.width <= 0.5f || rect.height <= 0.5f || color.a <= 0f) return;

            int r = Mathf.Clamp(Mathf.RoundToInt(radius), 1, MaxRadius);
            if (rect.width < r * 2f + 1f || rect.height < r * 2f + 1f)
                r = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(rect.width, rect.height) * 0.5f));

            Color previous = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, RoundedStyle(r));
            GUI.color = previous;
        }

        private static GUIStyle RoundedStyle(int radius)
        {
            GUIStyle style;
            if (RoundedStyles.TryGetValue(radius, out style) && style != null) return style;

            style = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
            style.normal.background = WhiteRounded(radius);
            RoundedStyles[radius] = style;
            return style;
        }

        internal static void DrawBorder(Rect rect, float radius, float thickness, Color color, Color fill)
        {
            if (rect.width <= 1f || rect.height <= 1f) return;

            Rect inner = new Rect(rect.x + thickness, rect.y + thickness,
                rect.width - thickness * 2f, rect.height - thickness * 2f);

            DrawRounded(rect, radius, color);
            DrawRounded(inner, Mathf.Max(1f, radius - thickness), fill);
        }

        private static Texture2D WhiteRounded(int radius)
        {
            Texture2D texture;
            if (WhiteTextures.TryGetValue(radius, out texture) && texture != null) return texture;

            int size = radius * 2 + 2;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = Coverage(x + 0.5f, y + 0.5f, size, radius);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            WhiteTextures[radius] = texture;
            return texture;
        }

        private static Texture2D ColoredRounded(int radius, Color color)
        {
            int colorKey = ToColorKey(color);
            long key = ((long)radius << 32) ^ (uint)colorKey;

            Texture2D texture;
            if (ColoredTextures.TryGetValue(key, out texture) && texture != null) return texture;

            int size = radius * 2 + 2;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * 255f), 0, 255);

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float coverage = Coverage(x + 0.5f, y + 0.5f, size, radius);
                    pixels[y * size + x] = new Color32(r, g, b, (byte)Mathf.RoundToInt(coverage * a));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            ColoredTextures[key] = texture;
            return texture;
        }

        private static int ToColorKey(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            int a = Mathf.Clamp(Mathf.RoundToInt(color.a * 255f), 0, 255);
            return (r << 24) | (g << 16) | (b << 8) | a;
        }

        private static float Coverage(float x, float y, int size, int radius)
        {
            float cx = Mathf.Clamp(x, radius, size - radius);
            float cy = Mathf.Clamp(y, radius, size - radius);
            float dx = x - cx;
            float dy = y - cy;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - distance + 0.5f);
        }
    }
}
