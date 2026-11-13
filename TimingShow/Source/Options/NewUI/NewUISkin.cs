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
        private static readonly Dictionary<long, Texture2D> BorderedTextures = new Dictionary<long, Texture2D>();
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
        internal static Color WarningFill;

        internal static readonly Color Warning = new Color(0.9686f, 0.5647f, 0.0353f, 1f);
        internal static readonly Color WarningBorder = new Color(0.8627f, 0.4078f, 0.0118f, 1f);

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
        internal static GUIStyle WarningStyle;
        internal static GUIStyle WarningBoxStyle;
        internal static Texture2D WarningIcon;

        private const int WarningIconResolution = 64;
        private static int _warningIconKey;
        internal static Texture2D FoldArrowOpen;
        internal static Texture2D FoldArrowClosed;

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
            WarningFill = Lighten(Color.Lerp(PanelAlt, Warning, 0.12f), 0.02f);
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

            WarningStyle = TextStyle(_fontSize, Text, TextAnchor.MiddleLeft);
            WarningStyle.wordWrap = true;
            WarningStyle.clipping = TextClipping.Clip;

            WarningIcon = EnsureWarningIcon();
            FoldArrowOpen = BuildFoldArrow(16, true, TextMuted);
            FoldArrowClosed = BuildFoldArrow(16, false, TextMuted);
            WarningBoxStyle = new GUIStyle
            {
                normal = { background = BorderedRounded(8, 1, WarningBorder, WarningFill) },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(40, 12, 9, 9),
                margin = new RectOffset(0, 0, 0, 4)
            };

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

        private static Texture2D EnsureWarningIcon()
        {
            int key = ToColorKey(Text);
            if (WarningIcon != null && _warningIconKey == key) return WarningIcon;

            _warningIconKey = key;
            return BuildWarningIcon(WarningIconResolution);
        }

        private static Texture2D BorderedRounded(int radius, int thickness, Color border, Color fill)
        {
            long key = ((long)radius << 48) ^ ((long)thickness << 40) ^ ((long)ToColorKey(border) << 16) ^ (uint)ToColorKey(fill);

            Texture2D texture;
            if (BorderedTextures.TryGetValue(key, out texture) && texture != null) return texture;

            int size = radius * 2 + 2;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            int innerSize = size - thickness * 2;
            int innerRadius = Mathf.Max(1, radius - thickness);
            Color32 borderColor = border;
            Color32 fillColor = fill;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float outer = Coverage(x + 0.5f, y + 0.5f, size, radius);
                    if (outer <= 0f) continue;

                    float inner = Coverage(x + 0.5f - thickness, y + 0.5f - thickness, innerSize, innerRadius);
                    float fillCoverage = Mathf.Clamp01(inner);
                    float borderCoverage = Mathf.Clamp01(outer - fillCoverage);

                    float r = (fillColor.r * fillCoverage + borderColor.r * borderCoverage) / outer;
                    float g = (fillColor.g * fillCoverage + borderColor.g * borderCoverage) / outer;
                    float b = (fillColor.b * fillCoverage + borderColor.b * borderCoverage) / outer;

                    pixels[y * size + x] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(r), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(g), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(b), 0, 255),
                        (byte)Mathf.RoundToInt(outer * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            BorderedTextures[key] = texture;
            return texture;
        }

        private static Texture2D BuildWarningIcon(int size)
        {
            float unit = size / 20f;
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float outerRadius = 10f * unit;
            float innerRadius = 8f * unit;
            float stroke = 1.5f * unit;

            Vector2[] triangle =
            {
                WarningPoint(cx, cy, 30f, 9f * unit),
                WarningPoint(cx, cy, 150f, 9f * unit),
                WarningPoint(cx, cy, 270f, 9f * unit)
            };
            Vector2 barStart = new Vector2(cx, cy - 4f * unit);
            Vector2 barEnd = new Vector2(cx, cy - 0.5f * unit);
            Vector2 dotCenter = new Vector2(cx, cy + 2f * unit);
            float dotRadius = 0.75f * unit;
            Vector2 center = new Vector2(cx, cy);

            Color32 fill = Warning;
            Color32 ring = WarningBorder;
            Color32 strokeColor = Text;

            const int Samples = 4;
            const int SampleCount = Samples * Samples;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = 0f;
                    float g = 0f;
                    float b = 0f;
                    int hits = 0;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            Vector2 point = new Vector2(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples);
                            Color32 color;
                            if (IsWarningStroke(point, triangle, barStart, barEnd, dotCenter, dotRadius, stroke)) color = strokeColor;
                            else if ((point - center).sqrMagnitude <= innerRadius * innerRadius) color = fill;
                            else if ((point - center).sqrMagnitude <= outerRadius * outerRadius) color = ring;
                            else continue;

                            r += color.r;
                            g += color.g;
                            b += color.b;
                            hits++;
                        }
                    }

                    if (hits == 0) continue;

                    pixels[(size - 1 - y) * size + x] = new Color32(
                        (byte)Mathf.RoundToInt(r / hits),
                        (byte)Mathf.RoundToInt(g / hits),
                        (byte)Mathf.RoundToInt(b / hits),
                        (byte)Mathf.RoundToInt(hits * 255f / SampleCount));
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D BuildFoldArrow(int size, bool pointDown, Color color)
        {
            Vector2 a = pointDown ? new Vector2(1.5f, 4f) : new Vector2(4f, 1.5f);
            Vector2 b = pointDown ? new Vector2(size - 1.5f, 4f) : new Vector2(size - 3f, size * 0.5f);
            Vector2 c = pointDown ? new Vector2(size * 0.5f, size - 3f) : new Vector2(4f, size - 1.5f);
            Color32 fill = color;

            const int Samples = 4;
            const int SampleCount = Samples * Samples;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            Vector2 point = new Vector2(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples);
                            if (TriangleContains(point, a, b, c)) hits++;
                        }
                    }

                    if (hits == 0) continue;
                    pixels[(size - 1 - y) * size + x] = new Color32(fill.r, fill.g, fill.b, (byte)Mathf.RoundToInt(hits * 255f / SampleCount));
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static bool TriangleContains(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y);
            float d2 = (point.x - c.x) * (b.y - c.y) - (b.x - c.x) * (point.y - c.y);
            float d3 = (point.x - a.x) * (c.y - a.y) - (c.x - a.x) * (point.y - a.y);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static bool IsWarningStroke(Vector2 point, Vector2[] triangle, Vector2 barStart, Vector2 barEnd, Vector2 dotCenter, float dotRadius, float stroke)
        {
            float halfStroke = stroke * 0.5f;
            for (int i = 0; i < triangle.Length; i++)
            {
                if (SegmentDistance(point, triangle[i], triangle[(i + 1) % triangle.Length]) <= halfStroke) return true;
            }

            if (SegmentDistance(point, barStart, barEnd) <= halfStroke) return true;
            return (point - dotCenter).sqrMagnitude <= dotRadius * dotRadius;
        }

        private static float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            float t = lengthSquared <= 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return (point - (start + segment * t)).magnitude;
        }

        private static Vector2 WarningPoint(float cx, float cy, float angle, float radius)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(cx + radius * Mathf.Cos(radians), cy + radius * Mathf.Sin(radians));
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
