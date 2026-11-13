using UnityEngine;

namespace TimingShow.Options
{
    public static class SettingsWindow
    {
        public const float TitleBarHeight = 34f;

        private const float WindowRadius = 8f;
        private const float ContentPadding = 12f;
        private const float CloseSize = 20f;

        private static readonly Color CloseHover = new Color(0.855f, 0.267f, 0.325f, 1f);
        private static readonly Color ClosePressed = new Color(0.702f, 0.204f, 0.259f, 1f);

        private static GUIStyle _windowStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _glyphStyle;
        private static GUIStyle _labelStyle;
        private static int _styleRevision = -1;

        public static GUIStyle WindowStyle
        {
            get
            {
                if (_windowStyle != null) return _windowStyle;

                _windowStyle = new GUIStyle
                {
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };
                return _windowStyle;
            }
        }

        public static bool DrawFrame(Rect rect, string title)
        {
            EnsureStyles();

            Rect inner = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            NewUISkin.DrawRounded(rect, WindowRadius, NewUISkin.Border);
            NewUISkin.DrawRounded(inner, WindowRadius - 1f, NewUISkin.Background);

            Rect titleBar = new Rect(inner.x, inner.y, inner.width, TitleBarHeight);
            NewUISkin.DrawRounded(new Rect(titleBar.x, titleBar.y, titleBar.width, TitleBarHeight + WindowRadius - 1f), WindowRadius - 1f, NewUISkin.Element);
            NewUISkin.DrawRect(new Rect(inner.x, titleBar.yMax, inner.width, WindowRadius), NewUISkin.Background);
            NewUISkin.DrawRect(new Rect(inner.x + 1f, titleBar.yMax, inner.width - 2f, 1f), NewUISkin.Fade(NewUISkin.Border, 0.6f));

            GUI.Label(new Rect(inner.x + 40f, titleBar.y, inner.width - 80f, TitleBarHeight), title, _titleStyle);

            Rect close = new Rect(inner.xMax - 10f - CloseSize, titleBar.y + (TitleBarHeight - CloseSize) * 0.5f, CloseSize, CloseSize);
            bool hover = close.Contains(Event.current.mousePosition);
            if (hover) NewUISkin.DrawRounded(close, CloseSize * 0.5f, Event.current.type == EventType.MouseDown ? ClosePressed : CloseHover);

            Color previous = GUI.contentColor;
            GUI.contentColor = hover ? Color.white : NewUISkin.TextDim;
            GUI.Label(close, "\u00D7", _glyphStyle);
            GUI.contentColor = previous;

            bool closed = GUI.Button(close, GUIContent.none, GUIStyle.none);
            GUI.DragWindow(new Rect(inner.x, inner.y, inner.width - CloseSize - 20f, TitleBarHeight));
            return closed;
        }

        public static Rect ContentRect(Rect rect)
        {
            float top = rect.y + 1f + TitleBarHeight + 8f;
            float height = Mathf.Max(0f, rect.yMax - 1f - ContentPadding - top);
            return new Rect(rect.x + 1f + ContentPadding, top, rect.width - 2f - ContentPadding * 2f, height);
        }

        public static bool Button(Rect rect, string text, bool primary = false)
        {
            EnsureStyles();
            return NewUIWidgets.ButtonAt(rect, text, primary);
        }

        public static void Label(Rect rect, string text)
        {
            EnsureStyles();
            GUI.Label(rect, text, _labelStyle);
        }

        public static void Divider(Rect rect)
        {
            EnsureStyles();
            NewUISkin.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), NewUISkin.Fade(NewUISkin.Border, 0.6f));
        }

        private static void EnsureStyles()
        {
            NewUISkin.Ensure();
            if (_styleRevision == NewUISkin.Revision) return;

            _styleRevision = NewUISkin.Revision;
            Font font = GUI.skin != null ? GUI.skin.font : null;
            int size = NewUISkin.FontSize;

            _titleStyle = TextStyle(font, size + 1, FontStyle.Bold, TextAnchor.MiddleCenter, NewUISkin.Text);
            _glyphStyle = TextStyle(font, size + 4, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            _labelStyle = TextStyle(font, size, FontStyle.Normal, TextAnchor.MiddleLeft, NewUISkin.TextDim);
        }

        private static GUIStyle TextStyle(Font font, int size, FontStyle fontStyle, TextAnchor anchor, Color color)
        {
            GUIStyle style = new GUIStyle
            {
                font = font,
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
    }
}
