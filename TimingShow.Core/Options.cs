using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityFileDialog;

namespace TimingShow
{
    public static class Options
    {
        #region Fields & State Variables

        private static string _bufferSizeText;
        private static string _maxPointsText;
        private static bool _showAdvancedSettings;
        
        private static int _activeTabIndex;
        private static readonly string[] TabNames = { "通用设置", "HUD 与 视觉", "高级与主题" };

        private static bool _foldoutTitleSettings = true;
        private static bool _foldoutPlanetSettings;
        private static bool _foldoutDeathSettings; 
        private static bool _foldoutWinSettings;
        private static bool _foldoutTimingHUD = true;
        private static bool _foldoutURHUD;
        private static bool _foldoutRatioHUD;
        private static bool _foldoutLogging = true;
        private static bool _foldoutXACCGraph;
        private static bool _foldoutThemeSettings;

        #endregion

        #region UI Style

        private static bool _stylesInitialized;

        private static Texture2D _texOuterCardBg;
        private static Texture2D _texSubCardBg;
        private static Texture2D _texSwitchBgOff;
        private static Texture2D _texSwitchBgOn;
        private static Texture2D _texSwitchBgOffHover;
        private static Texture2D _texSwitchBgOnHover;
        private static Texture2D _texSwitchThumb;
        private static Texture2D _texWarningIcon;
        
        private static GUIStyle _outerCardStyle;
        private static GUIStyle _subCardStyle;
        private static GUIStyle _tabBtnStyle;
        private static GUIStyle _tabBtnActiveStyle;
        private static GUIStyle _btnStyle;
        private static GUIStyle _btnActiveStyle;
        private static GUIStyle _textFieldStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _headerLabelStyle;
        private static GUIStyle _warningLabelStyle;

        private static readonly List<Texture2D> GeneratedTextures = new List<Texture2D>();

        public static void RebuildStyles()
        {
            foreach (var tex in GeneratedTextures)
            {
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            GeneratedTextures.Clear();
            _stylesInitialized = false;
        }

        private static void InitStyles()
        {
            if (_stylesInitialized) return;

            var cfg = ModContext.Settings;
            int radiusOuter = 16;
            int radiusSub = 10;
            
            _texOuterCardBg = CreateRoundedTex(128, 128, cfg.Theme_CardBg, radiusOuter);
            _texSubCardBg = CreateRoundedTex(128, 128, cfg.Theme_SubCardBg, radiusSub);

            _outerCardStyle = new GUIStyle
            {
                padding = new RectOffset(16, 16, 16, 16),
                margin = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(radiusOuter, radiusOuter, radiusOuter, radiusOuter)
            };
            _outerCardStyle.normal.background = _texOuterCardBg;

            _subCardStyle = new GUIStyle
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 6, 6),
                border = new RectOffset(radiusSub, radiusSub, radiusSub, radiusSub)
            };
            _subCardStyle.normal.background = _texSubCardBg;
            
            Color hoverPrimary = cfg.Theme_Primary * 1.15f;
            Color hoverDark = cfg.Theme_BgDark * 1.15f;
            _texSwitchBgOff = CreateRoundedTex(36, 20, cfg.Theme_BgDark, 10);
            _texSwitchBgOffHover = CreateRoundedTex(36, 20, hoverDark, 10);
            _texSwitchBgOn = CreateRoundedTex(36, 20, cfg.Theme_Primary, 10);
            _texSwitchBgOnHover = CreateRoundedTex(36, 20, hoverPrimary, 10);
            _texSwitchThumb = CreateCircleTex(14, Color.white);
            _texWarningIcon = CreateWarningIconTex(20, cfg.Theme_Warning);
            
            int radiusTab = 8;
            _tabBtnStyle = new GUIStyle
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 4, 0, 0),
                padding = new RectOffset(14, 14, 6, 6),
                border = new RectOffset(radiusTab, radiusTab, radiusTab, radiusTab)
            };

            Texture2D texTabNormal = CreateRoundedTex(64, 64, cfg.Theme_BgDark, radiusTab);
            Texture2D texTabHover = CreateRoundedTex(64, 64, hoverDark, radiusTab);
            Texture2D texTabActive = CreateRoundedTex(64, 64, cfg.Theme_Primary, radiusTab);

            _tabBtnStyle.normal.background = texTabNormal;
            _tabBtnStyle.normal.textColor = cfg.Theme_Text * 0.7f;
            _tabBtnStyle.hover.background = texTabHover;
            _tabBtnStyle.hover.textColor = cfg.Theme_Text;
            _tabBtnStyle.active.background = texTabActive;
            _tabBtnStyle.active.textColor = Color.white;

            _tabBtnActiveStyle = new GUIStyle(_tabBtnStyle)
            {
                border = new RectOffset(radiusTab, radiusTab, radiusTab, radiusTab)
            };
            _tabBtnActiveStyle.normal.background = texTabActive;
            _tabBtnActiveStyle.normal.textColor = Color.white;
            _tabBtnActiveStyle.fontStyle = FontStyle.Bold;
            _tabBtnActiveStyle.hover.background = texTabActive;
            _tabBtnActiveStyle.active.background = texTabActive;
            
            int radiusBtn = 6;
            _btnStyle = new GUIStyle
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(12, 12, 6, 6),
                border = new RectOffset(radiusBtn, radiusBtn, radiusBtn, radiusBtn)
            };

            Texture2D texBtnNormal = CreateRoundedTex(64, 64, cfg.Theme_BgDark, radiusBtn);
            Texture2D texBtnHover = CreateRoundedTex(64, 64, hoverDark, radiusBtn);
            Texture2D texBtnActive = CreateRoundedTex(64, 64, cfg.Theme_Primary, radiusBtn);

            _btnStyle.normal.background = texBtnNormal;
            _btnStyle.normal.textColor = cfg.Theme_Text;
            _btnStyle.hover.background = texBtnHover;
            _btnStyle.hover.textColor = Color.white;
            _btnStyle.active.background = texBtnActive;
            _btnStyle.active.textColor = Color.white;

            _btnActiveStyle = new GUIStyle(_btnStyle)
            {
                border = new RectOffset(radiusBtn, radiusBtn, radiusBtn, radiusBtn)
            };
            _btnActiveStyle.normal.background = texBtnActive;
            _btnActiveStyle.normal.textColor = Color.white;
            _btnActiveStyle.fontStyle = FontStyle.Bold;
            _btnActiveStyle.hover.background = texBtnActive;
            _btnActiveStyle.active.background = texBtnActive;
            
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 2, 2)
            };
            _textFieldStyle.normal.background = CreateRoundedTex(16, 16, cfg.Theme_BgDark, 3);
            _textFieldStyle.normal.textColor = cfg.Theme_Text;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = cfg.Theme_Text;

            _headerLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            _headerLabelStyle.normal.textColor = Color.white;

            _warningLabelStyle = new GUIStyle(_labelStyle)
            {
                wordWrap = true
            };
            _warningLabelStyle.normal.textColor = cfg.Theme_Warning;

            _stylesInitialized = true;
        }

        private static Texture2D CreateRoundedTex(int width, int height, Color col, int cornerRadius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] cols = new Color[width * height];
            float r = cornerRadius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, width - r);
                    float cy = Mathf.Clamp(y + 0.5f, r, height - r);
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 1.0f;
                    if (dist > r) alpha = 0.0f;
                    else if (dist > r - 1.5f) alpha = (r - dist) / 1.5f;

                    cols[y * width + x] = new Color(col.r, col.g, col.b, col.a * alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            GeneratedTextures.Add(tex);
            return tex;
        }

        private static Texture2D CreateCircleTex(int size, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] cols = new Color[size * size];
            float radius = size / 2.0f;
            float center = radius - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 1.0f;
                    if (dist > radius) alpha = 0.0f;
                    else if (dist > radius - 1.5f) alpha = (radius - dist) / 1.5f;

                    cols[y * size + x] = new Color(col.r, col.g, col.b, col.a * alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            GeneratedTextures.Add(tex);
            return tex;
        }

        private static Texture2D CreateWarningIconTex(int size, Color iconColor)
        {
            int renderSize = size * 4;
            using (var bitmap = new System.Drawing.Bitmap(renderSize, renderSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.Clear(System.Drawing.Color.Transparent);
                
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    float margin = renderSize * 0.1f;
                    float w = renderSize - margin * 2;
                    float h = renderSize - margin * 2;
                    float topX = renderSize / 2f;
                    float topY = margin;
                    float rightX = renderSize - margin;
                    float rightY = renderSize - margin;
                    float leftX = margin;
                    float leftY = renderSize - margin;

                    path.AddLine(topX, topY + 10, rightX - 5, rightY);
                    path.AddLine(rightX - 5, rightY, leftX + 5, leftY);
                    path.AddLine(leftX + 5, leftY, topX, topY + 10);
                    path.CloseFigure();

                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(
                        (int)(iconColor.a * 255), 
                        (int)(iconColor.r * 255), 
                        (int)(iconColor.g * 255), 
                        (int)(iconColor.b * 255))))
                    {
                        graphics.FillPath(brush, path);
                    }
                }
                using (var exclamationPath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    float cx = renderSize / 2f;
                    float barWidth = renderSize * 0.12f;
                    float barTop = renderSize * 0.32f;
                    float barHeight = renderSize * 0.32f;
                    
                    exclamationPath.AddRectangle(new System.Drawing.RectangleF(cx - barWidth / 2f, barTop, barWidth, barHeight));
                    
                    float dotTop = renderSize * 0.70f;
                    float dotSize = renderSize * 0.13f;
                    exclamationPath.AddEllipse(cx - dotSize / 2f, dotTop, dotSize, dotSize);
                    
                    using (var clearBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 30, 32, 35)))
                    {
                        graphics.FillPath(clearBrush, exclamationPath);
                    }
                }
                
                Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;

                Color[] cols = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var sysCol = bitmap.GetPixel(x * 4 + 2, (size - 1 - y) * 4 + 2);
                        cols[y * size + x] = new Color(sysCol.R / 255f, sysCol.G / 255f, sysCol.B / 255f, sysCol.A / 255f);
                    }
                }

                tex.SetPixels(cols);
                tex.Apply();
                GeneratedTextures.Add(tex);
                return tex;
            }
        }

        #endregion

        #region UI Components

        private static bool DrawCapsuleSwitch(bool value, string labelText)
        {
            GUILayout.BeginHorizontal();
            
            Rect switchRect = GUILayoutUtility.GetRect(36, 20, GUILayout.Width(36), GUILayout.Height(20));
            bool isHovered = switchRect.Contains(Event.current.mousePosition);
            
            Texture2D bgTex = value 
                ? (isHovered ? _texSwitchBgOnHover : _texSwitchBgOn) 
                : (isHovered ? _texSwitchBgOffHover : _texSwitchBgOff);

            GUI.DrawTexture(switchRect, bgTex, ScaleMode.StretchToFill, true);

            float thumbX = value ? switchRect.x + 19 : switchRect.x + 3;
            float thumbY = switchRect.y + 3;
            Rect thumbRect = new Rect(thumbX, thumbY, 14, 14);
            GUI.DrawTexture(thumbRect, _texSwitchThumb, ScaleMode.StretchToFill, true);

            GUILayout.Space(10);
            
            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(labelText), _labelStyle, GUILayout.ExpandWidth(false));
            GUI.Label(labelRect, labelText, _labelStyle);

            Rect totalClickRect = new Rect(switchRect.x, switchRect.y, switchRect.width + 10 + labelRect.width, switchRect.height);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && totalClickRect.Contains(Event.current.mousePosition))
            {
                value = !value;
                Event.current.Use();
            }

            GUILayout.EndHorizontal();
            return value;
        }

        private static void DrawWarningBox(string message)
        {
            GUILayout.BeginVertical(_subCardStyle);
            GUILayout.BeginHorizontal();
            
            Rect iconRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
            if (_texWarningIcon != null)
            {
                GUI.DrawTexture(iconRect, _texWarningIcon, ScaleMode.ScaleToFit, true);
            }

            GUILayout.Space(6);
            GUILayout.Label(message, _warningLabelStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawHeader(string title)
        {
            GUILayout.Space(6);
            GUILayout.Label(title, _headerLabelStyle);
            GUILayout.Space(6);
        }

        #endregion

        #region Main Entry Point

        public static void OnGUI()
        {
            InitStyles();

            GUILayout.BeginVertical(_outerCardStyle);
            {
                DrawTabHeader();
                GUILayout.Space(12);

                switch (_activeTabIndex)
                {
                    case 0:
                        DrawGeneralTab();
                        break;
                    case 1:
                        DrawVisualHUDTab();
                        break;
                    case 2:
                        DrawLoggingAndAdvancedTab();
                        break;
                }
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region Tab Rendering Modules

        private static void DrawTabHeader()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < TabNames.Length; i++)
            {
                bool isSelected = (_activeTabIndex == i);
                GUIStyle style = isSelected ? _tabBtnActiveStyle : _tabBtnStyle;

                if (GUILayout.Button(TabNames[i], style, GUILayout.Height(30)))
                {
                    _activeTabIndex = i;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawGeneralTab()
        {
            DrawHeader("语言设置");
            DrawLanguageSettings();

            DrawHeader("核心提示与判定展示");
            DrawTitleSettings();
            DrawPlanetSettings();
            DrawDeathAndWinSettings();

            GUILayout.Space(10);
            DrawSessionControls();
        }

        private static void DrawVisualHUDTab()
        {
            DrawHeader("实时 HUD 渲染");
            DrawTimingHUD();
            DrawURHUD();
            DrawRatioHUD();

            DrawHeader("XACC 图表");
            DrawXACCGraphSettings();
        }

        private static void DrawLoggingAndAdvancedTab()
        {
            DrawHeader("UI 主题自定义");
            DrawThemeSettings();

            DrawHeader("日志记录器");
            DrawLoggingSettings();

            DrawHeader("高级与 Hooks 选项");
            DrawAdvancedSettings();

            GUILayout.Space(10);
            DrawWarningBox("注意：开启 Hook 模式可能与部分同类型模组（如 custom judge 类）产生冲突，请谨慎开启。");
        }

        #endregion

        #region Sub Modules

        private static void DrawLanguageSettings()
        {
            GUILayout.BeginHorizontal();
            foreach (string langCode in LangMan.AvailableLanguages)
            {
                bool isSelected = (ModContext.Settings.Language == langCode);
                GUIStyle style = isSelected ? _btnActiveStyle : _btnStyle;

                if (GUILayout.Button(langCode, style, GUILayout.Width(100), GUILayout.Height(24)))
                    ModContext.Settings.Language = langCode;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        private static void DrawThemeSettings()
        {
            var cfg = ModContext.Settings;
            string arrow = _foldoutThemeSettings ? "▲" : "▼";
            if (GUILayout.Button($"自定义主题配色 {arrow}", _btnStyle, GUILayout.Width(150), GUILayout.Height(26)))
                _foldoutThemeSettings = !_foldoutThemeSettings;

            if (!_foldoutThemeSettings) return;

            GUILayout.BeginVertical(_subCardStyle);
            {
                bool changed = false;

                changed |= ColorPicker("外层大卡片底色", ref cfg.Theme_CardBg);
                changed |= ColorPicker("子卡片底色", ref cfg.Theme_SubCardBg);
                changed |= ColorPicker("品牌主色调 (Primary)", ref cfg.Theme_Primary);
                changed |= ColorPicker("未选中背景色", ref cfg.Theme_BgDark);
                changed |= ColorPicker("主文本颜色", ref cfg.Theme_Text);
                changed |= ColorPicker("警告区域颜色", ref cfg.Theme_Warning);

                if (changed)
                {
                    RebuildStyles();
                }

                GUILayout.Space(6);
                if (GUILayout.Button("重置主题为默认配色", _btnStyle, GUILayout.Width(160), GUILayout.Height(24)))
                {
                    cfg.Theme_CardBg = new Color(0.12f, 0.13f, 0.15f, 0.95f);
                    cfg.Theme_SubCardBg = new Color(0.16f, 0.17f, 0.20f, 1.00f);
                    cfg.Theme_Primary = new Color(0.18f, 0.53f, 0.94f, 1.0f);
                    cfg.Theme_BgDark = new Color(0.22f, 0.24f, 0.28f, 1.0f);
                    cfg.Theme_Text = new Color(0.92f, 0.94f, 0.96f, 1.0f);
                    cfg.Theme_Warning = new Color(0.96f, 0.62f, 0.14f, 1.0f);
                    RebuildStyles();
                }
            }
            GUILayout.EndVertical();
        }

        private static void DrawTitleSettings()
        {
            FoldoutToggle(LangMan.T("Toggle_Title"), ref ModContext.Settings.ShowInSongTitle, ref _foldoutTitleSettings);
            if (ModContext.Settings.ShowInSongTitle && _foldoutTitleSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc1, 0, 5);
                Toggle(ref ModContext.Settings.Title_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.Title_UseJudgeColor)
                {
                    Toggle(ref ModContext.Settings.Title_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.Title_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawPlanetSettings()
        {
            bool oldShowOnPlanet = ModContext.Settings.ShowOnPlanet;
            FoldoutToggle(LangMan.T("Toggle_Planet"), ref ModContext.Settings.ShowOnPlanet, ref _foldoutPlanetSettings);
            
            if (oldShowOnPlanet != ModContext.Settings.ShowOnPlanet && ModContext.Settings.AutoReloadInEditor)
            {
                if (!ADOBase.isLevelEditor) return;
                ADOBase.RestartScene();
            }

            if (ModContext.Settings.ShowOnPlanet && _foldoutPlanetSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc3, 0, 5);
                Toggle(ref ModContext.Settings.Planet_ShowAngle, "Toggle_ShowAngle");
                Toggle(ref ModContext.Settings.Planet_EnableXPerfect, "Enable_XP");

                GUILayout.Label(LangMan.T("Setting_Title"), _labelStyle);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    GUILayout.BeginVertical();
                    {
                        Toggle(ref ModContext.Settings.ReplaceFailOverload, "Toggle_FailOverload", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooEarly, "Toggle_TooEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryEarly, "Toggle_VeryEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceEarlyPerfect, "Toggle_EarlyPerfect", 0);
                        Toggle(ref ModContext.Settings.ReplacePerfect, "Toggle_Perfect", 0);
                        Toggle(ref ModContext.Settings.ReplaceLatePerfect, "Toggle_LatePerfect", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryLate, "Toggle_VeryLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooLate, "Toggle_TooLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceFailMiss, "Toggle_FailMiss", 0);
                        Toggle(ref ModContext.Settings.ReplaceMultipress, "Toggle_Multipress", 0);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawDeathAndWinSettings()
        {
            FoldoutToggle(LangMan.T("Toggle_Death"), ref ModContext.Settings.ShowOnDeath, ref _foldoutDeathSettings);
            if (ModContext.Settings.ShowOnDeath && _foldoutDeathSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc3, 0, 5);
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowAvgTiming, "Toggle_Death_AvgTiming");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowUR, "Toggle_Death_UR");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowXACC, "Toggle_Death_XACC");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowRatio, "Toggle_Death_Ratio");
                SliderInt("Label_FontSize", ref ModContext.Settings.ShowOnDeath_FontSize, 20, 200);
            }
            
            FoldoutToggle(LangMan.T("Toggle_Win"), ref ModContext.Settings.ShowInWinPage, ref _foldoutWinSettings);
            if (ModContext.Settings.ShowInWinPage && _foldoutWinSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc4, 0, 5);
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowAvgTiming, "Toggle_Death_AvgTiming");
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowUR, "Toggle_Death_UR");
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowRatio, "Toggle_Death_Ratio");
                SliderInt("Label_FontSize", ref ModContext.Settings.ShowInWinPage_FontSize, 20, 200);
            }
        }

        private static void DrawTimingHUD()
        {
            ToggleFold(LangMan.T("Toggle_TimingHUD"), ref ModContext.Settings.ShowTimingHUD, ref _foldoutTimingHUD);
            if (ModContext.Settings.ShowTimingHUD && _foldoutTimingHUD)
            {
                HUDBase(
                    ref ModContext.Settings.HUD_x, ref ModContext.Settings.HUD_y, ref ModContext.Settings.HUD_scale,
                    ref ModContext.Settings.HUD_bold, ref ModContext.Settings.HUD_align, ref ModContext.Settings.HUD_Format,
                    ref ModContext.Settings.PercHUD
                );
                Toggle(ref ModContext.Settings.HUD_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.HUD_UseJudgeColor)
                {
                    Toggle(ref ModContext.Settings.HUD_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.HUD_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawURHUD()
        {
            ToggleFold(LangMan.T("Toggle_URHUD"), ref ModContext.Settings.ShowURHUD, ref _foldoutURHUD);
            if (ModContext.Settings.ShowURHUD && _foldoutURHUD)
            {
                HUDBase(
                    ref ModContext.Settings.URHUD_x, ref ModContext.Settings.URHUD_y, ref ModContext.Settings.URHUD_scale,
                    ref ModContext.Settings.URHUD_bold, ref ModContext.Settings.URHUD_align, ref ModContext.Settings.URHUD_Format,
                    ref ModContext.Settings.PercURHUD
                );
            }
        }

        private static void DrawRatioHUD()
        {
            ToggleFold(LangMan.T("Toggle_RatioHUD"), ref ModContext.Settings.ShowRatioHUD, ref _foldoutRatioHUD);
            if (ModContext.Settings.ShowRatioHUD && _foldoutRatioHUD)
            {
                HUDBase(
                    ref ModContext.Settings.RatioHUD_x, ref ModContext.Settings.RatioHUD_y, ref ModContext.Settings.RatioHUD_scale,
                    ref ModContext.Settings.RatioHUD_bold, ref ModContext.Settings.RatioHUD_align, ref ModContext.Settings.RatioHUD_Format,
                    ref ModContext.Settings.PercRatioHUD
                );
                Toggle(ref ModContext.Settings.Ratio_UseXPerfect, "Enable_XP");
            }
        }

        private static void DrawXACCGraphSettings()
        {
            ToggleFold(LangMan.T("Toggle_XACCGraph"), ref ModContext.Settings.ShowXACCGraph, ref _foldoutXACCGraph);
            if (ModContext.Settings.ShowXACCGraph && _foldoutXACCGraph)
            {
                Toggle(ref ModContext.Settings.XACCGraph_ShowEnd, "Toggle_ShowEnd");
                SliderFloat("Label_XOffset", ref ModContext.Settings.XACCGraph_X, 0.0f, 1.0f);
                SliderFloat("Label_YOffset", ref ModContext.Settings.XACCGraph_Y, 0.0f, 1.0f);
                SliderFloat("Label_Scale", ref ModContext.Settings.XACCGraph_Scale, 0.2f, 3.0f);
                IntField("Label_MaxPoints", ref _maxPointsText, ref ModContext.Settings.XACCGraph_MaxPoints, 20, 5000, 250);

                ColorPicker(LangMan.T("Label_BgColor"), ref ModContext.Settings.XACCGraph_BgColor);
                ColorPicker(LangMan.T("Label_LineColor"), ref ModContext.Settings.XACCGraph_LineColor);
                ColorPicker(LangMan.T("Label_GridColor"), ref ModContext.Settings.XACCGraph_GridColor);
                ColorPicker(LangMan.T("Label_AxisTextColor"), ref ModContext.Settings.XACCGraph_AxisTextColor);
                ColorPicker(LangMan.T("Label_InfoTextColor"), ref ModContext.Settings.XACCGraph_ValueTextColor);
            }
        }

        private static void DrawLoggingSettings()
        {
            GUILayout.BeginVertical();
            {
                ToggleFold(LangMan.T("Toggle_Logging"), ref ModContext.Settings.EnableLogging, ref _foldoutLogging);

                if (ModContext.Settings.EnableLogging && _foldoutLogging)
                {
                    SliderInt("Label_Precision", ref ModContext.Settings.PercLog, 0, 5);
                    Toggle(ref ModContext.Settings.Logger_EnableXPerfect, "Enable_XP");
                    Toggle(ref ModContext.Settings.Logger_ShowAngle, "Toggle_ShowAngle");
                    Toggle(ref ModContext.Settings.LogAutoplay, "Toggle_LogAutoplay");
                    Toggle(ref ModContext.Settings.UseJsonWriter, "Toggle_UseJsonWriter");

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_LogDir"), _labelStyle, GUILayout.Width(140));
                    string absolutePath = AbsLogPath(ModContext.Settings.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "None" : absolutePath;
                    GUILayout.Label(displayPath, _labelStyle, GUILayout.MinWidth(280), GUILayout.MaxWidth(480));
                    
                    if (GUILayout.Button(LangMan.T("Btn_Browse"), _btnStyle, GUILayout.Width(70), GUILayout.Height(22)))
                    {
                        string defaultDir = GetLogDirectory();
                        string selectedFolder = FileBrowser.PickFolder(defaultDir, "Folder", new string[0], LangMan.T("Label_LogDir"));
                        if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            ModContext.Settings.LogDirectory = Path.GetFullPath(selectedFolder);
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_BufferSize"), _labelStyle, GUILayout.Width(140));

                    if (_bufferSizeText == null) _bufferSizeText = ModContext.Settings.LogBufferSizeKB.ToString();
                    string newBufferSizeText = GUILayout.TextField(_bufferSizeText, _textFieldStyle, GUILayout.Width(80));
                    if (newBufferSizeText != _bufferSizeText)
                    {
                        if (int.TryParse(newBufferSizeText, out int parsedVal) && parsedVal >= 8 && parsedVal <= 102400)
                        {
                            _bufferSizeText = newBufferSizeText;
                            ModContext.Settings.LogBufferSizeKB = parsedVal;
                        }
                        else
                        {
                            _bufferSizeText = "64";
                            ModContext.Settings.LogBufferSizeKB = 64;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(6);
                if (GUILayout.Button(LangMan.T("Btn_OpenLogs"), _btnStyle, GUILayout.Width(150), GUILayout.Height(26)))
                {
                    try
                    {
                        string logDir = GetLogDirectory();
                        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                        Process.Start(new ProcessStartInfo() { FileName = logDir, UseShellExecute = true, Verb = "open" });
                    }
                    catch (Exception e)
                    {
                        ModContext.Logger.Error(e.Message);
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private static void DrawSessionControls()
        {
            if (GUILayout.Button(LangMan.T("Btn_Reset"), _btnActiveStyle, GUILayout.Width(160), GUILayout.Height(28)))
            {
                ModContext.SessionOffsets.Clear();
                ModContext.LastHitMargin = HitMargin.Perfect;
                ModContext.LastTiming = 0;
                ModContext.LastAngle = 0;
            }
        }

        private static void DrawAdvancedSettings()
        {
            string foldoutArrow = _showAdvancedSettings ? "▲" : "▼";
            if (GUILayout.Button($"{LangMan.T("Btn_Advanced")} {foldoutArrow}", _btnStyle, GUILayout.Width(150), GUILayout.Height(26)))
                _showAdvancedSettings = !_showAdvancedSettings;

            if (!_showAdvancedSettings) return;

            GUILayout.BeginVertical();
            {
                GUILayout.Space(5);
                
                bool newHookMode = ToggleWithDescription( ModContext.Settings.UseHookMode, "Toggle_HookMode", "Desc_HookMode" );
                if (newHookMode != ModContext.Settings.UseHookMode)
                {
                    ModContext.Settings.UseHookMode = newHookMode;
                    if (newHookMode) XPerfectBridge.TryInit(force: true);
                    else XPerfectBridge.UnloadHook();
                }

                XPerfectBridge.HookState currentState = XPerfectBridge.CurrentState;
                string statusDisplayText;
                switch (currentState)
                {
                    case XPerfectBridge.HookState.Success:
                        statusDisplayText = $"<color=#55FF55>{LangMan.T("Status_HookSuccess")}</color>";
                        break;
                    case XPerfectBridge.HookState.Failed:
                        statusDisplayText = $"<color=#FF5555>{LangMan.T("Status_HookFailed")}{XPerfectBridge.LastErrorMessage}</color>";
                        break;
                    case XPerfectBridge.HookState.Disabled:
                    default:
                        statusDisplayText = $"<color=#888888>{LangMan.T("Status_HookDisabled")}</color>";
                        break;
                }
                IndentedLabel($"{LangMan.T("Label_CurrentStatus")}{statusDisplayText}");

                GUILayout.Space(5);
                
                ModContext.Settings.DisplayCurrMode = ToggleWithDescription(
                    ModContext.Settings.DisplayCurrMode,
                    "Toggle_DisplayCurrMode",
                    "Desc_DisplayCurrMode",
                    extraDescriptionHtml: " <color=#FF96B4>#FF96B4</color>"
                );

                GUILayout.Space(5);

                bool previousGuiState = GUI.enabled;
                if (!ModContext.Settings.UseJsonWriter)
                    GUI.enabled = false;
                ModContext.Settings.UseOldJsonFormat = ToggleWithDescription(
                    ModContext.Settings.UseOldJsonFormat,
                    "Toggle_UseOldJsonFormat",
                    "Desc_UseOldJsonFormat"
                );
                GUI.enabled = previousGuiState;

                GUILayout.Space(5);
                
                ModContext.Settings.AutoReloadInEditor = ToggleWithDescription(
                    ModContext.Settings.AutoReloadInEditor,
                    "Toggle_AutoReloadInEditor",
                    "Desc_AutoReloadInEditor"
                );
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region Helpers & Layout Utils

        private static void FoldoutToggle(string label, ref bool toggle, ref bool foldout)
        {
            GUILayout.BeginHorizontal();
            
            toggle = DrawCapsuleSwitch(toggle, label);

            if (toggle)
            {
                GUILayout.Space(6);
                string arrow = foldout ? "▲" : "▼";
                if (GUILayout.Button(arrow, _btnStyle, GUILayout.Width(28), GUILayout.Height(20)))
                {
                    foldout = !foldout;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void ToggleFold(string label, ref bool toggle, ref bool foldout)
        {
            FoldoutToggle(label, ref toggle, ref foldout);
        }

        private static bool ToggleWithDescription(bool value, string labelKey, string descKey, string extraDescriptionHtml = "", float indent = 20)
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);

            bool newValue = DrawCapsuleSwitch(value, LangMan.T(labelKey));

            GUILayout.EndHorizontal();

            IndentedLabel($"<color=#888888>{LangMan.T(descKey)}</color>{extraDescriptionHtml}", indent);
            return newValue;
        }

        private static void IndentedLabel(string text, float indent = 20)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            GUILayout.Label(text, _labelStyle);
            GUILayout.EndHorizontal();
        }

        private static void HUDBase(ref float x, ref float y, ref float scale, ref bool bold, ref int align, ref string format, ref int prec)
        {
            SliderFloat("Label_XOffset", ref x, -0.5f, 0.5f);
            SliderFloat("Label_YOffset", ref y, -0.5f, 0.5f);
            SliderFloat("Label_Scale", ref scale, 0.2f, 3.0f);
            Toggle(ref bold, "Toggle_Bold");
            AlignButtons(ref align);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Format"), _labelStyle, GUILayout.Width(100));
            format = GUILayout.TextField(format, _textFieldStyle, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            SliderInt("Label_Precision", ref prec, 0, 5);
        }

        private static void SliderFloat(string labelKey, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T(labelKey) + $"{value:F2}", _labelStyle, GUILayout.Width(120));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }

        private static void SliderInt(string labelKey, ref int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T(labelKey) + $"{value}", _labelStyle, GUILayout.Width(120));
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(100)));
            GUILayout.EndHorizontal();
        }

        private static void Toggle(ref bool value, string labelKey, float indent = 20)
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);

            value = DrawCapsuleSwitch(value, LangMan.T(labelKey));

            GUILayout.EndHorizontal();
        }

        private static void IntField(string labelKey, ref string text, ref int value, int min, int max, int fallback, float labelWidth = 120)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T(labelKey), _labelStyle, GUILayout.Width(labelWidth));
            if (text == null) text = value.ToString();
            string newText = GUILayout.TextField(text, _textFieldStyle, GUILayout.Width(80));
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

        private static void AlignButtons(ref int align)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Align"), _labelStyle, GUILayout.Width(100));

            string[] labels = { LangMan.T("Btn_Left"), LangMan.T("Btn_Center"), LangMan.T("Btn_Right") };
            for (int i = 0; i < 3; i++)
            {
                GUIStyle style = (align == i) ? _btnActiveStyle : _btnStyle;
                if (GUILayout.Button(labels[i], style, GUILayout.Width(60), GUILayout.Height(22)))
                    align = i;
            }
            GUILayout.EndHorizontal();
        }

        private static bool ColorPicker(string label, ref Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(label, _labelStyle, GUILayout.Width(160));
            
            float r = color.r, g = color.g, b = color.b, a = color.a;

            GUILayout.Label("R", _labelStyle, GUILayout.Width(15));
            r = GUILayout.HorizontalSlider(r, 0f, 1f, GUILayout.Width(45));
            
            GUILayout.Label("G", _labelStyle, GUILayout.Width(15));
            g = GUILayout.HorizontalSlider(g, 0f, 1f, GUILayout.Width(45));
            
            GUILayout.Label("B", _labelStyle, GUILayout.Width(15));
            b = GUILayout.HorizontalSlider(b, 0f, 1f, GUILayout.Width(45));
            
            GUILayout.Label("A", _labelStyle, GUILayout.Width(15));
            a = GUILayout.HorizontalSlider(a, 0f, 1f, GUILayout.Width(45));

            bool changed = (r != color.r || g != color.g || b != color.b || a != color.a);
            if (changed)
            {
                color = new Color(r, g, b, a);
            }

            GUILayout.EndHorizontal();
            return changed;
        }

        #endregion

        #region Utils

        private static string GetDefaultLogDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs"));
        }

        private static string GetLogDirectory()
        {
            string abs = AbsLogPath(ModContext.Settings.LogDirectory);
            return string.IsNullOrWhiteSpace(abs) ? GetDefaultLogDirectory() : abs;
        }

        private static string AbsLogPath(string logDir)
        {
            if (string.IsNullOrWhiteSpace(logDir))
                return null;
            try
            {
                if (Path.IsPathRooted(logDir)) return Path.GetFullPath(logDir);
                string gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.GetFullPath(Path.Combine(gameRoot, logDir));
            }
            catch (Exception e)
            {
                ModContext.Logger.Log($"Err parsing path: {e.Message}");
                return logDir;
            }
        }

        #endregion
    }
}