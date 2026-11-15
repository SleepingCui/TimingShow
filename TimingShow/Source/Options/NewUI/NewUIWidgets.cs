using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TimingShow.Options
{
    internal static class NewUIWidgets
    {
        internal const float RowHeight = 26f;
        internal const float RowGap = 3f;
        internal const float Indent = 16f;
        internal const float SwitchWidth = 34f;
        internal const float SwitchHeight = 18f;
        internal const float LabelWidth = 150f;

        private static readonly Dictionary<string, bool> SectionStates = new Dictionary<string, bool>();
        private static readonly Dictionary<int, string> HexTexts = new Dictionary<int, string>();
        private static readonly Dictionary<string, string> ChannelTexts = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> SliderTexts = new Dictionary<string, string>();

        private static int _colorIndex;
        private static int _fieldIndex;
        private static int _sliderIndex;
        private static int _expandedColor = -1;
        private static bool _capturingKey;
        private static int _keySuppressFrame = -1;
        private static string _activeField;

        internal static void BeginFrame()
        {
            _colorIndex = 0;
            _fieldIndex = 0;
            _sliderIndex = 0;

            Event current = Event.current;
            if (current != null && current.type == EventType.MouseDown) _activeField = null;
        }

        internal static bool IsCapturingKey
        {
            get { return _capturingKey || _keySuppressFrame == Time.frameCount; }
        }

        internal static Rect Row(float height)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            GUILayout.Space(RowGap);
            return rect;
        }

        internal static void Space(float height)
        {
            GUILayout.Space(height);
        }

        internal static bool Section(string titleKey, string id, bool defaultOpen = true)
        {
            return FoldHeader(i18n.T(titleKey), id, defaultOpen);
        }

        internal static bool FoldHeader(string title, string id, bool defaultOpen = true)
        {
            bool open = FoldState(id, defaultOpen);
            Rect row = Row(24f);
            bool hover = row.Contains(Event.current.mousePosition);
            if (hover) NewUISkin.DrawRounded(new Rect(row.x - 4f, row.y - 1f, row.width + 8f, row.height + 2f), 5f, NewUISkin.Fade(NewUISkin.Element, 0.5f));

            NewUISkin.DrawRounded(new Rect(row.x + 2f, row.y + 4f, 3f, row.height - 8f), 1.5f, NewUISkin.Accent);
            Rect arrow = new Rect(row.x + 10f, row.y + (row.height - NewUISkin.FoldArrowSize) * 0.5f, NewUISkin.FoldArrowSize, NewUISkin.FoldArrowSize);
            NewUISkin.DrawFoldArrow(arrow, open);
            GUI.Label(new Rect(arrow.xMax + 8f, row.y, row.xMax - arrow.xMax - 16f, row.height), title, NewUISkin.SectionStyle);

            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                open = !open;
                SectionStates[id] = open;
            }
            return open;
        }

        private static bool FoldState(string id, bool defaultOpen = true)
        {
            bool open;
            if (!SectionStates.TryGetValue(id, out open))
            {
                open = defaultOpen;
                SectionStates[id] = open;
            }
            return open;
        }

        internal static bool Toggle(string labelKey, bool value, string descKey = null, float indent = 0f, bool enabled = true)
        {
            bool hasDesc = !string.IsNullOrEmpty(descKey);
            float height = hasDesc ? RowHeight + 15f : RowHeight;
            Rect row = Row(height);
            Rect content = new Rect(row.x + indent, row.y, row.width - indent, row.height);
            bool hover = enabled && row.Contains(Event.current.mousePosition);
            if (hover) NewUISkin.DrawRounded(new Rect(content.x - 4f, row.y - 1f, content.width + 8f, row.height + 2f), 5f, NewUISkin.Fade(NewUISkin.Element, 0.45f));

            float switchX = content.xMax - SwitchWidth - 2f;
            float textWidth = Mathf.Max(10f, switchX - content.x - 16f);
            GUIStyle labelStyle = !enabled ? NewUISkin.MutedLabelStyle : (value ? NewUISkin.LabelStyle : NewUISkin.DimLabelStyle);
            GUI.Label(new Rect(content.x + 8f, content.y, textWidth, hasDesc ? 18f : RowHeight), i18n.T(labelKey), labelStyle);
            if (hasDesc) GUI.Label(new Rect(content.x + 8f, content.y + 17f, textWidth, 15f), i18n.T(descKey), NewUISkin.MutedLabelStyle);

            DrawSwitch(new Rect(switchX, content.y + (RowHeight - SwitchHeight) * 0.5f, SwitchWidth, SwitchHeight), value, hover, enabled);
            if (enabled && GUI.Button(row, GUIContent.none, GUIStyle.none)) value = !value;
            return value;
        }

        internal static bool ToggleFold(string labelKey, string id, ref bool value, string descKey = null, bool enabled = true)
        {
            bool open = FoldState(id);
            bool hasDesc = !string.IsNullOrEmpty(descKey);
            float height = hasDesc ? RowHeight + 15f : RowHeight;
            Rect row = Row(height);
            bool hover = enabled && row.Contains(Event.current.mousePosition);
            if (hover) NewUISkin.DrawRounded(new Rect(row.x - 4f, row.y - 1f, row.width + 8f, row.height + 2f), 5f, NewUISkin.Fade(NewUISkin.Element, 0.45f));

            Rect arrowRect = new Rect(row.x + 6f, row.y + (RowHeight - NewUISkin.FoldArrowSize) * 0.5f, NewUISkin.FoldArrowSize, NewUISkin.FoldArrowSize);
            float switchX = row.xMax - SwitchWidth - 2f;
            float textX = arrowRect.xMax + 8f;
            float textWidth = Mathf.Max(10f, switchX - textX - 8f);
            GUIStyle labelStyle = !enabled ? NewUISkin.MutedLabelStyle : (value ? NewUISkin.LabelStyle : NewUISkin.DimLabelStyle);
            GUI.Label(new Rect(textX, row.y, textWidth, hasDesc ? 18f : RowHeight), i18n.T(labelKey), labelStyle);
            if (hasDesc) GUI.Label(new Rect(textX, row.y + 17f, textWidth, 15f), i18n.T(descKey), NewUISkin.MutedLabelStyle);

            DrawSwitch(new Rect(switchX, row.y + (RowHeight - SwitchHeight) * 0.5f, SwitchWidth, SwitchHeight), value, hover, enabled);
            bool foldable = enabled && value;
            NewUISkin.DrawFoldArrow(arrowRect, foldable && open, foldable);

            if (foldable && GUI.Button(arrowRect, GUIContent.none, GUIStyle.none))
            {
                open = !open;
                SectionStates[id] = open;
            }
            else if (enabled && GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }
            return open;
        }

        internal static void DrawSwitch(Rect rect, bool on, bool hover, bool enabled = true)
        {
            Color track = on
                ? (hover ? NewUISkin.AccentHover : NewUISkin.Accent)
                : (hover ? NewUISkin.ElementHover : NewUISkin.ElementActive);
            if (!enabled) track = NewUISkin.Fade(track, 0.5f);

            NewUISkin.DrawCapsule(rect, track);
            float size = rect.height - 6f;
            float x = on ? rect.xMax - size - 3f : rect.x + 3f;
            Color knob = on ? Color.white : NewUISkin.TextDim;
            if (!enabled) knob = NewUISkin.Fade(knob, 0.5f);
            NewUISkin.DrawCapsule(new Rect(x, rect.y + 3f, size, size), knob);
        }

        internal static bool Button(string text, float width = 0f, bool selected = false, float height = 28f, bool enabled = true)
        {
            Rect rect = width > 0f
                ? GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height))
                : GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            GUILayout.Space(RowGap);
            return ButtonAt(rect, text, selected, enabled);
        }

        internal static bool ButtonAt(Rect rect, string text, bool selected = false, bool enabled = true, Color? selectedColor = null)
        {
            Event current = Event.current;
            bool hover = enabled && rect.Contains(current.mousePosition);
            bool pressed = hover && current.type == EventType.MouseDown;

            Color background;
            if (!enabled) background = NewUISkin.Fade(NewUISkin.Element, 0.65f);
            else if (selected)
            {
                Color accent = selectedColor ?? NewUISkin.Accent;
                background = pressed ? NewUISkin.Lighten(accent, -0.15f) : (hover ? NewUISkin.Lighten(accent, 0.12f) : accent);
            }
            else background = pressed ? NewUISkin.ElementActive : (hover ? NewUISkin.ElementHover : NewUISkin.Element);

            NewUISkin.DrawRounded(rect, NewUISkin.ElementRadius, background);
            GUIStyle labelStyle = !enabled ? NewUISkin.CenterMutedStyle : (selected ? NewUISkin.CenterStyle : NewUISkin.CenterDimStyle);
            GUI.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), text, labelStyle);

            return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        internal static int Choice(string labelKey, int value, string[] labels, float height = 28f)
        {
            Rect row = Row(height);
            float labelWidth = string.IsNullOrEmpty(labelKey) ? 0f : LabelWidth;
            float gap = 6f;
            if (labelWidth > 0f)
            {
                GUI.Label(new Rect(row.x + 8f, row.y, labelWidth, row.height), i18n.T(labelKey), NewUISkin.LabelStyle);
                labelWidth += 12f;
            }

            Rect area = new Rect(row.x + labelWidth, row.y, row.width - labelWidth, row.height);
            float cellWidth = (area.width - gap * (labels.Length - 1)) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect cell = new Rect(area.x + i * (cellWidth + gap), area.y, cellWidth, area.height);
                if (ButtonAt(cell, labels[i], value == i)) value = i;
            }
            return value;
        }

        internal static float Slider(string labelKey, float value, float min, float max, string format = "0.###")
        {
            Rect row = Row(RowHeight);
            GUI.Label(new Rect(row.x + 8f, row.y, LabelWidth, row.height), i18n.T(labelKey), NewUISkin.LabelStyle);

            float trackX = row.x + LabelWidth + 16f;
            float trackWidth = Mathf.Max(40f, row.xMax - 78f - trackX);
            Rect track = new Rect(trackX, row.center.y - 8f, trackWidth, 16f);
            float dragged = SliderCore(track, value, min, max);

            Rect fieldRect = new Rect(row.xMax - 72f, row.y + 2f, 64f, RowHeight - 4f);
            return SliderValueField(fieldRect, labelKey, dragged, min, max, format, dragged != value);
        }

        private static float SliderValueField(Rect rect, string labelKey, float value, float min, float max, string format, bool forceText)
        {
            string key = labelKey + "#" + _sliderIndex++.ToString(CultureInfo.InvariantCulture);
            bool focused = FieldFocused(key, rect);
            string fallback = value.ToString(format, CultureInfo.InvariantCulture);
            string text;
            if (!focused || forceText || !SliderTexts.TryGetValue(key, out text) || text == null) text = fallback;

            DrawFieldFrame(rect, focused);
            GUI.SetNextControlName(key);
            string edited = GUI.TextField(rect, text, NewUISkin.FieldStyle);
            if (focused && !forceText && !IsNumberDraft(edited, format == "0")) edited = fallback;
            SliderTexts[key] = edited;

            if (!focused || forceText) return value;

            float parsed;
            if (float.TryParse(edited, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed >= min && parsed <= max) return parsed;
            return value;
        }

        internal static int IntSlider(string labelKey, int value, int min, int max)
        {
            return Mathf.RoundToInt(Slider(labelKey, value, min, max, "0"));
        }

        internal static float SliderCore(Rect track, float value, float min, float max)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            bool hover = track.Contains(current.mousePosition);

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (hover && current.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        value = ValueAt(track, current.mousePosition.x, min, max);
                        current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = ValueAt(track, current.mousePosition.x, min, max);
                        current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;
            }

            float barY = track.y + 5f;
            float barHeight = 6f;
            NewUISkin.DrawRounded(new Rect(track.x, barY, track.width, barHeight), 3f, NewUISkin.Field);

            float ratio = max > min ? Mathf.Clamp01((value - min) / (max - min)) : 0f;
            float filled = track.width * ratio;
            if (filled > 1f) NewUISkin.DrawRounded(new Rect(track.x, barY, filled, barHeight), 3f, NewUISkin.Accent);

            float knobSize = hover ? 12f : 10f;
            float knobX = Mathf.Lerp(track.x, track.xMax, ratio);
            NewUISkin.DrawRounded(new Rect(knobX - knobSize * 0.5f, track.center.y - knobSize * 0.5f, knobSize, knobSize), knobSize * 0.5f, hover ? Color.white : NewUISkin.Text);
            return value;
        }

        private static float ValueAt(Rect track, float mouseX, float min, float max)
        {
            float ratio = track.width > 0f ? Mathf.Clamp01((mouseX - track.x) / track.width) : 0f;
            return Mathf.Lerp(min, max, ratio);
        }

        internal static int IntField(string labelKey, ref string text, int value, int min, int max)
        {
            Rect row = Row(RowHeight);
            GUI.Label(new Rect(row.x + 8f, row.y, LabelWidth, row.height), i18n.T(labelKey), NewUISkin.LabelStyle);

            if (text == null) text = value.ToString(CultureInfo.InvariantCulture);
            Rect fieldRect = new Rect(row.xMax - 72f, row.y + 2f, 64f, RowHeight - 4f);
            bool focused;
            string edited = TextField(fieldRect, text, out focused);
            if (edited != text && IsNumberDraft(edited, true))
            {
                text = edited;
                int parsed;
                if (int.TryParse(edited, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed >= min && parsed <= max) value = parsed;
            }
            if (!focused && text != null)
            {
                string canonical = value.ToString(CultureInfo.InvariantCulture);
                if (text != canonical) text = canonical;
            }
            return value;
        }

        internal static float FloatField(string labelKey, ref string text, float value, float min, float max)
        {
            Rect row = Row(RowHeight);
            GUI.Label(new Rect(row.x + 8f, row.y, LabelWidth, row.height), i18n.T(labelKey), NewUISkin.LabelStyle);

            if (text == null) text = value.ToString("0.###", CultureInfo.InvariantCulture);
            Rect fieldRect = new Rect(row.xMax - 72f, row.y + 2f, 64f, RowHeight - 4f);
            bool focused;
            string edited = TextField(fieldRect, text, out focused);
            if (edited != text && IsNumberDraft(edited, false))
            {
                text = edited;
                float parsed;
                if (float.TryParse(edited, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed >= min && parsed <= max) value = parsed;
            }
            if (!focused && text != null)
            {
                string canonical = value.ToString("0.###", CultureInfo.InvariantCulture);
                if (text != canonical) text = canonical;
            }
            return value;
        }

        internal static string TextRow(string labelKey, string text, float fieldWidth = 160f)
        {
            Rect row = Row(RowHeight);
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - fieldWidth - 24f, row.height), i18n.T(labelKey), NewUISkin.LabelStyle);
            Rect fieldRect = new Rect(row.xMax - fieldWidth - 8f, row.y + 3f, fieldWidth, RowHeight - 6f);
            return TextField(fieldRect, text);
        }

        internal static string TextField(Rect rect, string text)
        {
            bool focused;
            return TextField(rect, text, out focused);
        }

        internal static string TextField(Rect rect, string text, out bool focused)
        {
            string key = "TimingShowNewUIField" + _fieldIndex++;
            focused = FieldFocused(key, rect);

            DrawFieldFrame(rect, focused);

            GUI.SetNextControlName(key);
            return GUI.TextField(rect, text, NewUISkin.FieldStyle);
        }

        private static bool FieldFocused(string key, Rect rect)
        {
            Event current = Event.current;
            if (current != null && current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition)) _activeField = key;
            return _activeField == key;
        }

        internal static bool IsNumberDraft(string text, bool integer)
        {
            if (string.IsNullOrEmpty(text)) return true;

            int start = text[0] == '-' || text[0] == '+' ? 1 : 0;
            if (text.Length == start) return true;

            bool dot = false;
            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.')
                {
                    if (integer || dot) return false;
                    dot = true;
                    continue;
                }
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        private static bool IsHexDraft(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            int start = text[0] == '#' ? 1 : 0;
            if (text.Length - start > 8) return false;

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        private static void DrawFieldFrame(Rect rect, bool focused)
        {
            NewUISkin.DrawBorder(rect, NewUISkin.ElementRadius, 1f, focused ? NewUISkin.Accent : NewUISkin.Border, NewUISkin.Field);
        }

        internal static string SearchField(Rect rect, string text, string placeholder)
        {
            string key = "TimingShowNewUIField" + _fieldIndex++;
            bool focused = FieldFocused(key, rect);

            DrawFieldFrame(rect, focused);
            if (!focused && string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(placeholder))
                GUI.Label(new Rect(rect.x + 10f, rect.y, Mathf.Max(10f, rect.width - 20f), rect.height), placeholder, NewUISkin.MutedLabelStyle);

            GUI.SetNextControlName(key);
            return GUI.TextField(rect, text, NewUISkin.FieldStyle);
        }

        internal static void Label(string text)
        {
            Rect row = Row(RowHeight - 4f);
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 16f, row.height), text, NewUISkin.LabelStyle);
        }

        internal static void Hint(string text)
        {
            Rect row = Row(RowHeight - 6f);
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 16f, row.height), text, NewUISkin.MutedLabelStyle);
        }

        internal static void Warning(string text)
        {
            const float IconSize = 20f;
            const float IconInset = 12f;

            GUILayout.BeginVertical(NewUISkin.WarningBoxStyle);
            GUILayout.Label(text, NewUISkin.WarningStyle);
            Rect textRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndVertical();

            Rect icon = new Rect(textRect.x - 40f + IconInset, textRect.y + (textRect.height - IconSize) * 0.5f, IconSize, IconSize);
            GUI.DrawTexture(icon, NewUISkin.WarningIcon, ScaleMode.ScaleToFit, true);
        }

        internal static void Divider()
        {
            Rect row = Row(9f);
            NewUISkin.DrawRect(new Rect(row.x + 4f, row.center.y, row.width - 8f, 1f), NewUISkin.Fade(NewUISkin.Border, 0.7f));
        }

        internal static void CardTitle(string titleKey)
        {
            Rect row = Row(24f);
            NewUISkin.DrawRounded(new Rect(row.x + 2f, row.y + 4f, 3f, row.height - 8f), 1.5f, NewUISkin.Accent);
            GUI.Label(new Rect(row.x + 12f, row.y, row.width - 12f, row.height), i18n.T(titleKey), NewUISkin.SectionStyle);
        }

        internal static void Hint(string text, Color color)
        {
            Rect row = Row(RowHeight - 6f);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 16f, row.height), text, NewUISkin.StatusLabelStyle);
            GUI.color = previous;
        }

        internal static Color ColorRow(string labelKey, Color color, Color defaultColor)
        {
            int index = _colorIndex++;
            Rect row = Row(RowHeight + 4f);
            float controlsWidth = 34f + 6f + 90f + 6f + 62f;
            GUI.Label(new Rect(row.x + 8f, row.y, Mathf.Max(24f, row.width - controlsWidth - 24f), row.height), i18n.T(labelKey), NewUISkin.LabelStyle);

            bool expanded = _expandedColor == index;
            Rect swatch = new Rect(row.xMax - controlsWidth, row.y + 5f, 34f, RowHeight - 6f);
            NewUISkin.DrawRounded(swatch, 5f, NewUISkin.Field);
            Color shown = color;
            shown.a = 1f;
            NewUISkin.DrawRounded(new Rect(swatch.x + 2f, swatch.y + 2f, swatch.width - 4f, swatch.height - 4f), 4f, shown);
            if (GUI.Button(swatch, GUIContent.none, GUIStyle.none)) _expandedColor = expanded ? -1 : index;

            Rect hexRect = new Rect(swatch.xMax + 6f, row.y + 4f, 90f, RowHeight - 4f);
            color = HexField(hexRect, index, color);

            Rect resetRect = new Rect(hexRect.xMax + 6f, row.y + 4f, 62f, RowHeight - 4f);
            if (ButtonAt(resetRect, i18n.T("Btn_ResetColor"))) color = defaultColor;

            if (expanded)
            {
                color.r = ChannelSlider(index, "R", color.r);
                color.g = ChannelSlider(index, "G", color.g);
                color.b = ChannelSlider(index, "B", color.b);
                color.a = ChannelSlider(index, "A", color.a);
            }
            return color;
        }

        private static float ChannelSlider(int index, string channel, float value)
        {
            Rect row = Row(RowHeight - 4f);
            GUI.Label(new Rect(row.x + 24f, row.y, 20f, row.height), channel, NewUISkin.MutedLabelStyle);

            string key = "TimingShowNewUIChannel" + channel + index.ToString(CultureInfo.InvariantCulture);
            string storeKey = channel + index.ToString(CultureInfo.InvariantCulture);
            Rect fieldRect = new Rect(row.x + 48f, row.y + 2f, 46f, row.height - 4f);
            bool focused = FieldFocused(key, fieldRect);
            string text;
            if (!focused || !ChannelTexts.TryGetValue(storeKey, out text) || text == null) text = ChannelText(value);

            DrawFieldFrame(fieldRect, focused);
            GUI.SetNextControlName(key);
            string edited = GUI.TextField(fieldRect, text, NewUISkin.FieldStyle);
            if (focused && !IsNumberDraft(edited, true)) edited = ChannelText(value);
            ChannelTexts[storeKey] = edited;

            if (focused)
            {
                int parsed;
                if (int.TryParse(edited, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed >= 0 && parsed <= 255)
                    value = parsed / 255f;
            }

            Rect track = new Rect(row.x + 102f, row.center.y - 8f, Mathf.Max(30f, row.width - 102f - 10f), 16f);
            float dragged = Mathf.Clamp01(SliderCore(track, value, 0f, 1f));
            if (dragged != value)
            {
                value = dragged;
                if (!focused) ChannelTexts[storeKey] = ChannelText(value);
            }
            return value;
        }

        private static string ChannelText(float value)
        {
            return Mathf.RoundToInt(value * 255f).ToString(CultureInfo.InvariantCulture);
        }

        private static Color HexField(Rect rect, int index, Color color)
        {
            string derived = OptionsWidgets.ColorToHex(color);
            string key = "TimingShowNewUIHex" + index.ToString(CultureInfo.InvariantCulture);
            bool focused = FieldFocused(key, rect);
            string text;
            if (!focused || !HexTexts.TryGetValue(index, out text) || text == null) text = derived;

            DrawFieldFrame(rect, focused);

            GUI.SetNextControlName(key);
            string edited = GUI.TextField(rect, text, NewUISkin.FieldStyle);
            if (focused && !IsHexDraft(edited)) edited = derived;
            HexTexts[index] = edited;
            if (focused)
            {
                Color parsed;
                if (OptionsWidgets.TryParseHex(edited, color, out parsed)) return parsed;
            }
            return color;
        }

        internal static KeyCode KeyRow(string labelKey, KeyCode key)
        {
            Rect row = Row(RowHeight + 2f);
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 130f, row.height), i18n.T(labelKey), NewUISkin.LabelStyle);

            string display = _capturingKey
                ? i18n.T("Btn_PressKey")
                : (key == KeyCode.None ? i18n.T("Status_Disabled") : key.ToString());

            Rect buttonRect = new Rect(row.xMax - 122f, row.y + 3f, 116f, RowHeight - 4f);
            if (ButtonAt(buttonRect, display, _capturingKey))
            {
                _capturingKey = true;
                _keySuppressFrame = Time.frameCount;
            }

            if (!_capturingKey) return key;

            Event current = Event.current;
            if (current == null || current.type != EventType.KeyDown || current.keyCode == KeyCode.None) return key;
            if (Time.frameCount == _keySuppressFrame) return key;

            if (current.keyCode != KeyCode.Escape) key = current.keyCode;
            _capturingKey = false;
            _keySuppressFrame = Time.frameCount;
            current.Use();
            return key;
        }
    }
}
