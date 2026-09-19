using UnityEngine;

namespace TimingShow
{
    internal static class OptionsWidgets
    {
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

        public static void FoldoutToggle(string label, ref bool toggle, ref bool foldout)
        {
            GUILayout.BeginHorizontal();
            toggle = GUILayout.Toggle(toggle, label, GUILayout.ExpandWidth(false));

            if (toggle)
            {
                GUILayout.Space(10);
                string arrow = foldout ? "▲" : "▼";
                if (GUILayout.Button(arrow, GUILayout.Width(28), GUILayout.Height(18)))
                {
                    foldout = !foldout;
                }
            }
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

        public static bool ToggleWithDescription(bool value, string labelKey, string descKey, string extraDescriptionHtml = "", float indent = 20, string extraLabelHtml = "")
        {
            bool newValue = GUILayout.Toggle(value, i18n.T(labelKey) + extraLabelHtml, RichToggleStyle);
            IndentedLabel($"<color=#888888>{i18n.T(descKey)}</color>{extraDescriptionHtml}", indent);
            return newValue;
        }

        public static void IndentedLabel(string text, float indent = 20)
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
            GUILayout.Space(20);
            GUILayout.Label(i18n.T("Label_Format"), GUILayout.Width(100));
            format = GUILayout.TextField(format, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            SliderInt("Label_Precision", ref prec, 0, 5);
        }

        public static void SliderFloat(string labelKey, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T(labelKey) + $"{value:F2}", GUILayout.Width(120));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }

        public static void SliderInt(string labelKey, ref int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T(labelKey) + $"{value}", GUILayout.Width(120));
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(100)));
            GUILayout.EndHorizontal();
        }

        public static void Toggle(ref bool value, string labelKey, float indent = 20)
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);
            value = GUILayout.Toggle(value, i18n.T(labelKey));
            GUILayout.EndHorizontal();
        }

        public static void IntField(string labelKey, ref string text, ref int value, int min, int max, int fallback, float labelWidth = 120)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
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

        public static void RatioModeButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
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
            GUILayout.Space(20);
            GUILayout.Label(i18n.T("Label_Align"), GUILayout.Width(100));

            string[] labels = { i18n.T("Btn_Left"), i18n.T("Btn_Center"), i18n.T("Btn_Right") };
            for (int i = 0; i < 3; i++)
            {
                if (ActiveButton(labels[i], align == i, 60))
                    align = i;
            }
            GUILayout.EndHorizontal();
        }

        public static void ColorPicker(string label, ref Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(label, GUILayout.Width(100));
            GUILayout.Label("R", GUILayout.Width(15));
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("G", GUILayout.Width(15));
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("B", GUILayout.Width(15));
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("A", GUILayout.Width(15));
            color.a = GUILayout.HorizontalSlider(color.a, 0f, 1f, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }
    }
}
