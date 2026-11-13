using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TimingShow.Options
{
    internal static class OptionsWidgets
    {
        public const float ContentIndent = 40f;

        private static GUIStyle _activeButtonStyle;
        private static GUIStyle _richToggleStyle;

        private static GUIStyle ActiveButtonStyle
        {
            get
            {
                if (_activeButtonStyle == null) _activeButtonStyle = new GUIStyle(GUI.skin.button);
                return _activeButtonStyle;
            }
        }

        private static GUIStyle RichToggleStyle
        {
            get
            {
                if (_richToggleStyle == null)
                {
                    _richToggleStyle = new GUIStyle(GUI.skin.toggle);
                    _richToggleStyle.richText = true;
                }
                return _richToggleStyle;
            }
        }

        
        
        public static bool ActiveButton(string label, bool active, float width)
        {
            GUIStyle style = ActiveButtonStyle;
            style.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            return GUILayout.Button(label, style, GUILayout.Width(width));
        }

        public static void ChoiceRow(string labelKey, ref int value, string[] optionKeys, float labelWidth = 120f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(i18n.T(labelKey), GUILayout.Width(labelWidth));
            for (int i = 0; i < optionKeys.Length; i++)
            {
                if (ActiveButton(i18n.T(optionKeys[i]), value == i, 76f)) value = i;
            }
            GUILayout.EndHorizontal();
        }

        private static GUIStyle _expandStyle;

        private const string ArrowExpanded = "◢";
        private const string ArrowCollapsed = "▶";
        
        private const float ArrowWidth = 13f;
        private const int ArrowFontSize = 13;
        private const int ArrowExpandedFontSize = 18;
        private const int ArrowRightMargin = 2;

        private static GUIStyle ExpandStyle
        {
            get
            {
                if (_expandStyle == null)
                {
                    _expandStyle = new GUIStyle
                    {
                        fixedWidth = ArrowWidth,
                        fontSize = ArrowFontSize,
                        alignment = TextAnchor.MiddleCenter,
                        margin = new RectOffset(0, ArrowRightMargin, 1, 1)
                    };

                    Color color = Color.white;
                    _expandStyle.normal.textColor = color;
                    _expandStyle.hover.textColor = color;
                    _expandStyle.active.textColor = color;
                    _expandStyle.focused.textColor = color;
                    _expandStyle.onNormal.textColor = color;
                    _expandStyle.onHover.textColor = color;
                    _expandStyle.onActive.textColor = color;
                    _expandStyle.onFocused.textColor = color;
                }
                return _expandStyle;
            }
        }

        private static GUIStyle _expandExpandedStyle;

        private static GUIStyle ExpandExpandedStyle
        {
            get
            {
                if (_expandExpandedStyle == null)
                {
                    _expandExpandedStyle = new GUIStyle(ExpandStyle);
                    _expandExpandedStyle.fontSize = ArrowExpandedFontSize;
                }
                return _expandExpandedStyle;
            }
        }

        
        public static void FoldoutToggle(string label, ref bool toggle, ref bool foldout)
        {
            GUILayout.BeginHorizontal();

            if (toggle)
            {
                GUIStyle arrowStyle = foldout ? ExpandExpandedStyle : ExpandStyle;
                bool expandedNext = GUILayout.Toggle(foldout, foldout ? ArrowExpanded : ArrowCollapsed, arrowStyle);
                if (expandedNext != foldout) foldout = expandedNext;
            }
            else
            {
                // Reserve the exact same layout footprint as the arrow toggle so
                // every checkbox in the list starts at the same x.
                GUILayout.Toggle(false, string.Empty, ExpandStyle);
            }

            toggle = GUILayout.Toggle(toggle, label, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public static void ToggleFold(string label, ref bool toggle, ref bool foldout)
        {
            FoldoutToggle(label, ref toggle, ref foldout);
        }

        public static void SettingFold(string label, ref bool toggle, ref int precision, ref bool foldout)
        {
            FoldoutToggle(label, ref toggle, ref foldout);

            if (toggle)
            {
                SliderInt("Label_Precision", ref precision, 0, 5);
            }
        }

        public static void SettingRow(string label, ref bool toggle, ref int precision)
        {
            toggle = GUILayout.Toggle(toggle, label);
            if (toggle)
            {
                SliderInt("Label_Precision", ref precision, 0, 5);
            }
        }

        public static bool ToggleWithDescription(bool value, string labelKey, string descKey, string extraDescriptionHtml = "", float indent = ContentIndent, string extraLabelHtml = "")
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);
            bool newValue = GUILayout.Toggle(value, i18n.T(labelKey) + extraLabelHtml, RichToggleStyle);
            GUILayout.EndHorizontal();
            IndentedLabel($"<color=#888888>{i18n.T(descKey)}</color>{extraDescriptionHtml}", indent);
            return newValue;
        }

        public static void IndentedLabel(string text, float indent = ContentIndent)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
        }

        public static void HUDBase(ref float x, ref float y, ref float scale, ref bool bold, ref int align, ref string format, ref int prec)
        {
            SliderFloat("Label_XOffset", ref x, -0.5f, 0.5f);
            SliderFloat("Label_YOffset", ref y, -0.5f, 0.5f);
            SliderFloat("Label_Scale", ref scale, 0.2f, 3.0f);
            Toggle(ref bold, "Toggle_Bold");
            AlignButtons(ref align);

            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(i18n.T("Label_Format"), GUILayout.Width(100));
            format = GUILayout.TextField(format, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            SliderInt("Label_Precision", ref prec, 0, 5);
        }

        public static void SliderFloat(string labelKey, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(i18n.T(labelKey) + $"{value:F2}", GUILayout.ExpandWidth(false));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }

        public static void SliderInt(string labelKey, ref int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(i18n.T(labelKey) + $"{value}", GUILayout.ExpandWidth(false));
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(100)));
            GUILayout.EndHorizontal();
        }

        public static void Toggle(ref bool value, string labelKey, float indent = ContentIndent)
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);
            value = GUILayout.Toggle(value, i18n.T(labelKey));
            GUILayout.EndHorizontal();
        }

        public static void IntField(string labelKey, ref string text, ref int value, int min, int max, int fallback, float labelWidth = 120, float indent = ContentIndent)
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);
            GUILayout.Label(i18n.T(labelKey), GUILayout.Width(labelWidth));
            if (text == null) text = value.ToString();
            string newText = GUILayout.TextField(text, GUILayout.Width(80));
            if (newText != text)
            {
                if (int.TryParse(newText, out int parsed) && parsed >= min && parsed <= max)
                {
                    text = newText;
                    value = parsed;
                }
                else
                {
                    text = fallback.ToString();
                    value = fallback;
                }
            }
            GUILayout.EndHorizontal();
        }

        public static void FloatField(string labelKey, ref string text, ref float value, float min, float max, float labelWidth = 190f, float indent = ContentIndent)
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);
            GUILayout.Label(i18n.T(labelKey), GUILayout.Width(labelWidth));
            if (text == null) text = value.ToString("0.###", CultureInfo.InvariantCulture);
            string newText = GUILayout.TextField(text, GUILayout.Width(80));
            if (newText != text)
            {
                text = newText;
                if (float.TryParse(newText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                    value = Mathf.Clamp(parsed, min, max);
            }
            GUILayout.EndHorizontal();
        }

        public static void RatioModeButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(i18n.T("Label_RatioMode"), GUILayout.Width(120));

            string[] labels =
            {
                i18n.T("Btn_RatioNormalPerfect"),
                i18n.T("Btn_RatioPerfectFamily"),
                i18n.T("Btn_RatioXPerfect")
            };
            int[] modes =
            {
                Settings.RatioMode_NormalPerfect,
                Settings.RatioMode_PerfectFamily,
                Settings.RatioMode_XPerfect
            };

            for (int i = 0; i < labels.Length; i++)
            {
                if (ActiveButton(labels[i], ModContext.Settings.Ratio_Mode == modes[i], 110))
                    ModContext.Settings.Ratio_Mode = modes[i];
            }
            GUILayout.EndHorizontal();
        }

        public static void AlignButtons(ref int align)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(i18n.T("Label_Align"), GUILayout.Width(100));

            string[] labels = { i18n.T("Btn_Left"), i18n.T("Btn_Center"), i18n.T("Btn_Right") };
            for (int i = 0; i < 3; i++)
            {
                if (ActiveButton(labels[i], align == i, 60))
                    align = i;
            }
            GUILayout.EndHorizontal();
        }

        private const float ColorColumnGap = 18f;
        private const float MaxColorColumnWidth = 330f;
        private const float ColorTitleMinWidth = 150f;
        private const int ColorColumnsPerRow = 2;

        private static int _colorColumn;

        private static readonly Dictionary<int, string> HexFieldTexts = new Dictionary<int, string>();
        private static int _colorPickerIndex;
        private static int _focusedHexPicker = -1;
        private static GUIStyle _valueLabelStyle;

        private static GUIStyle ValueLabelStyle
        {
            get
            {
                if (_valueLabelStyle == null)
                {
                    _valueLabelStyle = new GUIStyle(GUI.skin.label);
                    _valueLabelStyle.alignment = TextAnchor.MiddleRight;
                }
                return _valueLabelStyle;
            }
        }

        public static void BeginFrame()
        {
            _colorPickerIndex = 0;
        }

        public static void BeginColorPickerGrid()
        {
            _colorColumn = 0;
        }

        public static void EndColorPickerGrid()
        {
            if (_colorColumn != 0) GUILayout.EndHorizontal();
            _colorColumn = 0;
        }

        public static void ColorPicker(string label, ref Color color, Color defaultColor)
        {
            if (_colorColumn == 0) GUILayout.BeginHorizontal();
            else GUILayout.Space(ColorColumnGap);

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.MaxWidth(MaxColorColumnWidth));
            int index = _colorPickerIndex++;

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(label, GUILayout.MinWidth(ColorTitleMinWidth));
            GUILayout.EndHorizontal();
            ChannelRow("R:", ref color.r);
            ChannelRow("G:", ref color.g);
            ChannelRow("B:", ref color.b);
            ChannelRow("A:", ref color.a);

            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label("Hex:", GUILayout.Width(34));
            color = DrawHexField(index, color);
            if (GUILayout.Button(i18n.T("Btn_ResetColor"), GUILayout.Width(60)))
            {
                color = defaultColor;
                HexFieldTexts[index] = ColorToHex(defaultColor);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (++_colorColumn >= ColorColumnsPerRow)
            {
                GUILayout.EndHorizontal();
                _colorColumn = 0;
            }
        }

        private static void ChannelRow(string channelLabel, ref float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(ContentIndent);
            GUILayout.Label(channelLabel, GUILayout.Width(34));
            value = GUILayout.HorizontalSlider(Mathf.Clamp01(value), 0f, 1f);
            GUILayout.Label(Mathf.RoundToInt(value * 255f).ToString(), ValueLabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();
        }

        private static Color DrawHexField(int index, Color current)
        {
            string controlName = "TimingShowHexField" + index;
            GUI.SetNextControlName(controlName);
            bool editing = GUI.GetNameOfFocusedControl() == controlName;

            if (editing && _focusedHexPicker != index)
            {
                HexFieldTexts[index] = ColorToHex(current);
                _focusedHexPicker = index;
            }
            else if (!editing && _focusedHexPicker == index)
            {
                _focusedHexPicker = -1;
            }

            string shown = editing && HexFieldTexts.TryGetValue(index, out string buffered) ? buffered : ColorToHex(current);
            string edited = GUILayout.TextField(shown, GUILayout.Width(96));
            if (!editing || edited == shown) return current;

            HexFieldTexts[index] = edited;
            return TryParseHex(edited, current, out Color parsed) ? parsed : current;
        }

        internal static string ColorToHex(Color color)
        {
            return ChannelToHex(color.r) + ChannelToHex(color.g) + ChannelToHex(color.b) + ChannelToHex(color.a);
        }

        private static string ChannelToHex(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 255f).ToString("X2");
        }

        internal static bool TryParseHex(string text, Color current, out Color color)
        {
            color = current;
            if (string.IsNullOrEmpty(text)) return false;

            string digits = text.Trim().TrimStart('#');
            if (digits.Length != 6 && digits.Length != 8) return false;

            if (!TryParseHexByte(digits, 0, out byte r) || !TryParseHexByte(digits, 2, out byte g) || !TryParseHexByte(digits, 4, out byte b))
                return false;

            byte a = 255;
            if (digits.Length == 8 && !TryParseHexByte(digits, 6, out a)) return false;

            float alpha = digits.Length == 8 ? a / 255f : current.a;
            color = new Color(r / 255f, g / 255f, b / 255f, alpha);
            return true;
        }

        private static bool TryParseHexByte(string digits, int start, out byte value)
        {
            return byte.TryParse(digits.Substring(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
    }
}
