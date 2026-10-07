using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace TimingShow
{
    public class LogGraphWindow : MonoBehaviour, LogGraphWindow.ILogGraphPanelInteraction, IPointerExitHandler, ILogGraphSettingsHost
    {
        private const string RootObjectName = "TimingShow_LogGraphCanvas";
        private const string WindowObjectName = "LogGraphWindow";
        private const string GraphContainerObjectName = "GraphContainer";
        private const string PlotAreaObjectName = "PlotArea";
        private const string GraphAreaObjectName = "GraphArea";

        private const int CanvasSortingOrder = 320;

        private const string TitleSeparator = " · ";
        
        private const float DefaultWindowWidth = 1440f;
        private const float DefaultWindowHeight = 670f;
        private const float MinWindowWidth = 480f;
        private const float MinWindowHeight = 260f;
        private const float MaxWindowWidth = 1920f;
        private const float MaxWindowHeight = 1080f;
        
        private const float BaseHeaderHeight = 44f;
        private const float BaseHeaderPadding = 4f;
        private const float BaseBorderThickness = 1.5f;
        private const float BaseContentInset = 10f;
        private const float BaseTitleLeftPadding = 8f;
        private const float BaseSecondaryTitleFontSize = 10f;
        private const float BaseMetaFontSize = 10.5f;
        private const float BaseMetaWidth = 160f;
        private const float BaseTitleMetaGap = 10f;
        private const float BaseCloseButtonSize = 26f;
        private const float BaseCloseButtonInset = 2f;
        private const float BaseCloseGlyphThickness = 2f;
        private const float BaseCloseGlyphInset = 7f;
        private const float BaseSettingsButtonSize = 24f;
        private const float BaseButtonGap = 4f;
        private const float BaseGripSize = 14f;
        private const float BaseGripInset = 2f;
        private const float BaseGripLineThickness = 1.25f;
        private const float BaseTitleFontSize = 14f;
        private const float BaseStatsPanelWidth = 190f;
        private const float BaseStatsMinWidth = 130f;
        private const float BaseStatsRowHeight = 46f;
        private const float BaseStatsLabelFontSize = 10f;
        private const float BaseStatsValueFontSize = 17f;
        private const float BaseStatsHeaderHeight = 24f;
        private const float BaseGraphPadding = 5f;
        private const float BaseChartPadding = 5f;
        private const float BaseAxisGutterLeft = 62f;
        private const float BaseAxisGutterRight = 96f;
        private const float BaseAxisGutterBottom = 22f;
        private const float BaseAxisGutterTop = 8f;
        private const float TextLineRatio = 1.35f;
        
        private static float UiScale
        {
            get
            {
                Settings settings = ModContext.Settings;
                return settings != null ? Mathf.Clamp(settings.LogGraph_UIScale, 0.75f, 3f) : 1.25f;
            }
        }

        private static float HeaderHeight => BaseHeaderHeight * UiScale;
        private static float HeaderPadding => BaseHeaderPadding * UiScale;
        private static float BorderThickness => BaseBorderThickness * UiScale;
        private static float ContentInset => BaseContentInset * UiScale;
        private static float TitleLeftPadding => BaseTitleLeftPadding * UiScale;
        private static float TitleFontSize => BaseTitleFontSize * UiScale;
        private static float SecondaryTitleFontSize => BaseSecondaryTitleFontSize * UiScale;
        private static float MetaFontSize => BaseMetaFontSize * UiScale;
        private static float MetaWidth => BaseMetaWidth * UiScale;
        private static float TitleMetaGap => BaseTitleMetaGap * UiScale;
        private static float CloseButtonSize => BaseCloseButtonSize * UiScale;
        private static float CloseButtonInset => BaseCloseButtonInset * UiScale;
        private static float CloseGlyphThickness => BaseCloseGlyphThickness * UiScale;
        private static float CloseGlyphInset => BaseCloseGlyphInset * UiScale;
        private static float SettingsButtonSize => BaseSettingsButtonSize * UiScale;
        private static float ButtonGap => BaseButtonGap * UiScale;
        private static float GripSize => BaseGripSize * UiScale;
        private static float GripInset => BaseGripInset * UiScale;
        private static float GripLineThickness => BaseGripLineThickness * UiScale;
        private static float StatsPanelWidth => BaseStatsPanelWidth * UiScale;
        private static float StatsMinWidth => BaseStatsMinWidth * UiScale;
        private static float StatsPanelGap => BaseButtonGap * UiScale;
        private static float StatsRowHeight => BaseStatsRowHeight * UiScale;
        private static float StatsLabelFontSize => BaseStatsLabelFontSize * UiScale;
        private static float StatsValueFontSize => BaseStatsValueFontSize * UiScale;
        private static float StatsHeaderHeight => BaseStatsHeaderHeight * UiScale;
        private static float GraphPadding => BaseGraphPadding * UiScale;
        private static float ChartPadding => BaseChartPadding * UiScale;
        private static float AxisGutterLeft => BaseAxisGutterLeft * UiScale;
        private static float AxisGutterRight => BaseAxisGutterRight * UiScale;
        private static float AxisGutterBottom => BaseAxisGutterBottom * UiScale;
        private static float AxisGutterTop => BaseAxisGutterTop * UiScale;
        
        private static float TitleRightPadding => CloseButtonSize + SettingsButtonSize + CloseButtonInset * 3f + ButtonGap;
        
        private static float ContentWidth(float windowWidth) => Mathf.Max(0f, windowWidth - ContentInset * 2f);
        
        private static float ContentHeight(float windowHeight) =>
            Mathf.Max(0f, windowHeight - HeaderHeight - ContentInset * 2f);
        

        private const int StatsRowCount = 4;
        
        private static Rect HeaderRect(float windowWidth, float windowHeight) => new Rect(0f, Mathf.Max(0f, windowHeight - HeaderHeight), Mathf.Max(0f, windowWidth), HeaderHeight);
        private static Rect ContentRect(float windowWidth, float windowHeight) => new Rect(ContentInset, ContentInset, ContentWidth(windowWidth), ContentHeight(windowHeight));
        
        private static float StatsColumnWidth(float windowWidth)
        {
            float max = Mathf.Max(StatsMinWidth, ContentWidth(windowWidth) * 0.25f);
            return Mathf.Clamp(StatsPanelWidth, StatsMinWidth, max);
        }
        
        private static Rect WindowRect(float windowWidth, float windowHeight) => new Rect(0f, 0f, Mathf.Max(0f, windowWidth), Mathf.Max(0f, windowHeight));
        
        private static Rect GraphContainerRect(float windowWidth, float windowHeight)
        {
            Rect content = ContentRect(windowWidth, windowHeight);
            float pad = GraphPadding;
            return new Rect(content.x + pad, content.y + pad,
                Mathf.Max(1f, content.width - pad * 2f), Mathf.Max(1f, content.height - pad * 2f));
        }
        
        private static Rect SettingsRect(float windowWidth, float windowHeight)
        {
            Rect content = ContentRect(windowWidth, windowHeight);
            float width = Mathf.Max(1f, content.width - StatsColumnWidth(windowWidth) - StatsPanelGap);
            return new Rect(content.x, content.y, width, content.height);
        }
        
        private static Rect SidebarRect(float windowWidth, float windowHeight)
        {
            float width = StatsColumnWidth(windowWidth);
            return new Rect(Mathf.Max(0f, windowWidth - ContentInset - width), ContentInset, width, ContentHeight(windowHeight));
        }
        
        private static Rect FooterRect(float windowWidth, float windowHeight)
        {
            Rect card = GraphContainerRect(windowWidth, windowHeight);
            Rect plot = PlotRect(windowWidth, windowHeight);
            float pad = ChartPadding;
            float bottom = card.y + pad;
            return new Rect(card.x + pad, bottom, Mathf.Max(1f, card.width - pad * 2f), Mathf.Max(1f, plot.y - bottom));
        }

        private static void StatsRowMetrics(float contentHeight, out float headerHeight, out float rowsTop,
            out float rowHeight, out float labelHeight, out float valueHeight)
        {
            headerHeight = Mathf.Min(StatsHeaderHeight, Mathf.Max(1f, contentHeight * 0.3f));
            rowsTop = headerHeight;
            float rowsHeight = Mathf.Max(1f, contentHeight - headerHeight);
            rowHeight = Mathf.Min(StatsRowHeight, Mathf.Max(1f, rowsHeight / StatsRowCount));
            labelHeight = Mathf.Min(rowHeight * 0.45f, StatsLabelFontSize * 1.6f);
            valueHeight = Mathf.Max(1f, rowHeight - labelHeight);
        }
        
        private static void GraphRects(float windowWidth, float windowHeight,
            out Rect container, out Rect yAxis, out Rect plot, out Rect xAxis, out Rect topGutter)
        {
            container = GraphContainerRect(windowWidth, windowHeight);

            float pad = ChartPadding;
            float innerX = container.x + pad;
            float innerY = container.y + pad;
            float innerWidth = Mathf.Max(1f, container.width - pad * 2f);
            float innerHeight = Mathf.Max(1f, container.height - pad * 2f);

            float gutterLeft = Mathf.Min(AxisGutterLeft, innerWidth * 0.28f);
            float gutterRight = Mathf.Min(AxisGutterRight, innerWidth * 0.32f);
            float gutterTop = Mathf.Min(AxisGutterTop, innerHeight * 0.10f);
            
            float axisFontSize = Mathf.Clamp(Mathf.RoundToInt(11f * UiScale), 8f, 32f);
            float needBottom = 3f * UiScale + 16f * (axisFontSize / 12f) + 1f;
            float gutterBottom = Mathf.Clamp(needBottom, Mathf.Min(AxisGutterBottom, innerHeight * 0.16f), Mathf.Max(1f, innerHeight * 0.40f));

            float plotX = innerX + gutterLeft;
            float plotY = innerY + gutterBottom;
            float plotWidth = Mathf.Max(1f, innerWidth - gutterLeft - gutterRight);
            float plotHeight = Mathf.Max(1f, innerHeight - gutterBottom - gutterTop);

            yAxis = new Rect(innerX, plotY, Mathf.Max(1f, gutterLeft), plotHeight);
            plot = new Rect(plotX, plotY, plotWidth, plotHeight);
            xAxis = new Rect(plotX, innerY, plotWidth, Mathf.Max(1f, gutterBottom));
            topGutter = new Rect(plotX, plotY + plotHeight, plotWidth, Mathf.Max(1f, gutterTop));
        }
        
        private static Rect PlotRect(float windowWidth, float windowHeight)
        {
            Rect container, yAxis, plot, xAxis, topGutter;
            GraphRects(windowWidth, windowHeight, out container, out yAxis, out plot, out xAxis, out topGutter);
            return plot;
        }
        
        private static readonly Color PanelColor = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color TitleBarColor = new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color CloseNormalColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color CloseHoverColor = new Color(1f, 0.35f, 0.35f, 0.9f);
        private static readonly Color GripColor = new Color(1f, 1f, 1f, 0.20f);
        private static readonly Color SettingsButtonHoverColor = new Color(1f, 1f, 1f, 0.28f);
        private static readonly Color TitlePrimaryColor = new Color(1f, 1f, 1f, 0.90f);
        private static readonly Color TitleSecondaryColor = new Color(1f, 1f, 1f, 0.42f);
        private static readonly Color TitleMetaColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color SettingsButtonActiveColor = new Color(0.30f, 0.76f, 1f, 0.55f);
        private static readonly Color StatsPanelColor = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color StatsDividerColor = new Color(1f, 1f, 1f, 0.09f);
        private static readonly Color StatsLabelColor = new Color(0.60f, 0.64f, 0.70f, 1f);
        private static readonly Color StatsValueColor = new Color(0.96f, 0.98f, 1f, 1f);
        private static readonly Color StatsTitleColor = new Color(0.88f, 0.91f, 0.95f, 1f);
        private static readonly Color ChartCardColor = new Color(1f, 1f, 1f, 0.035f);
        private static readonly Color ChartCardBorderColor = new Color(1f, 1f, 1f, 0.10f);

        private static LogGraphWindow _instance;
        private static GameObject _root;
        private RectTransform _windowRect;
        private LogGraphWindowPanel _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _fileText;
        private TextMeshProUGUI _metaText;
        private string _baseTitle;
        private LogScatterDrawer _drawer;
        private RectTransform _graphContainerRect;
        private RectTransform _plotAreaRect;
        private RectTransform _graphRect;
        private TimingLogData _data;
        private bool _closed;
        private TextMeshProUGUI _statsTitleText;
        private readonly TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[4];
        private readonly TextMeshProUGUI[] _statValues = new TextMeshProUGUI[4];
        private readonly string[] _statTexts = new string[4];
        private bool _statsComputed;
        private int _statTotal;
        private int _statNormalPerfect;
        private int _statPerfectFamily;
        private int _statXPerfect;
        private double _statUr;
        private LogGraphSettingsUI _settingsUI;
        private bool _dragging;
        private bool _resizing;
        private bool _updateFallbackDrag;
        private Vector2 _dragStartScreen;
        private Vector2 _windowAtDragStart;
        private Vector2 _sizeAtDragStart;
        private bool _graphPanActive;
        private bool _graphPanRunning;
        private enum PendingButton { None, Close, Settings }
        private PendingButton _pendingButton = PendingButton.None;
        
        private static readonly bool DiagnoseInput = false;

        private static void Trace(string message)
        {
            if (!DiagnoseInput) return;
            ModContext.Logger?.Log("[LogGraphWindow] " + message);
        }
        
        public static bool IsOpen
        {
            get { return _instance != null && _root != null; }
        }


        
        public static void Open(TimingLogData data)
        {
            if (data == null)
            {
                ModContext.Logger?.Error("[LogGraphWindow] Cannot open scatter plot window: log data is null");
                return;
            }

            Close();

            try
            {
                Build(data);
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Error("[LogGraphWindow] Failed to create scatter plot window: " + e);
                Close();
            }
        }
        
        public static void Close()
        {
            GameObject root = _root;

            _root = null;
            _instance = null;
            _dragProxy = null;
            _hoverProxy = null;
            _exitProxy = null;

            if (root != null) Destroy(root);
        }
        

        private static void Build(TimingLogData data)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            Vector2 savedSize = SavedWindowSize();
            float width = Mathf.Clamp(savedSize.x, MinWindowWidth, Mathf.Max(MinWindowWidth, screenWidth));
            float height = Mathf.Clamp(savedSize.y, MinWindowHeight, Mathf.Max(MinWindowHeight, screenHeight));
            Vector2 size = ClampSize(new Vector2(width, height), screenWidth, screenHeight);
            Vector2 position = ClampPosition(
                new Vector2((screenWidth - size.x) * 0.5f, (screenHeight - size.y) * 0.5f),
                size, screenWidth, screenHeight);
            
            GameObject root = new GameObject(RootObjectName);
            root.transform.SetParent(null, false);
            if (root.GetComponent<RectTransform>() == null) root.AddComponent<RectTransform>();

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            root.AddComponent<GraphicRaycaster>();
            
            GameObject windowGo = new GameObject(WindowObjectName);
            windowGo.transform.SetParent(root.transform, false);
            RectTransform windowRect = windowGo.GetComponent<RectTransform>();
            if (windowRect == null) windowRect = windowGo.AddComponent<RectTransform>();

            LogGraphWindow window = root.AddComponent<LogGraphWindow>();
            _root = root;
            _instance = window;
            window._windowRect = windowRect;
            window._closed = false;
            
            _dragProxy = window;
            _hoverProxy = window;
            _exitProxy = window;

            LogGraphWindowPanel panel = windowGo.AddComponent<LogGraphWindowPanel>();
            window._panel = panel;
            ApplyProxy();
            
            window.CreateHeaderTexts(windowGo.transform, data);

            GameObject graphContainerGo = new GameObject(GraphContainerObjectName);
            graphContainerGo.transform.SetParent(windowGo.transform, false);
            RectTransform graphContainerRect = graphContainerGo.GetComponent<RectTransform>();
            if (graphContainerRect == null) graphContainerRect = graphContainerGo.AddComponent<RectTransform>();
            graphContainerRect.anchorMin = Vector2.zero;
            graphContainerRect.anchorMax = Vector2.zero;
            graphContainerRect.pivot = Vector2.zero;
            graphContainerRect.offsetMin = Vector2.zero;
            graphContainerRect.offsetMax = Vector2.zero;
            graphContainerGo.AddComponent<RectMask2D>();


            GameObject plotAreaGo = new GameObject(PlotAreaObjectName);
            plotAreaGo.transform.SetParent(graphContainerGo.transform, false);
            RectTransform plotAreaRect = plotAreaGo.GetComponent<RectTransform>();
            if (plotAreaRect == null) plotAreaRect = plotAreaGo.AddComponent<RectTransform>();
            plotAreaRect.anchorMin = Vector2.zero;
            plotAreaRect.anchorMax = Vector2.zero;
            plotAreaRect.pivot = Vector2.zero;
            plotAreaRect.offsetMin = Vector2.zero;
            plotAreaRect.offsetMax = Vector2.zero;

            GameObject graphAreaGo = new GameObject(GraphAreaObjectName);
            graphAreaGo.transform.SetParent(plotAreaGo.transform, false);
            RectTransform graphRect = graphAreaGo.GetComponent<RectTransform>();
            if (graphRect == null) graphRect = graphAreaGo.AddComponent<RectTransform>();
            graphRect.anchorMin = Vector2.zero;
            graphRect.anchorMax = Vector2.one;
            graphRect.pivot = Vector2.zero;
            graphRect.offsetMin = Vector2.zero;
            graphRect.offsetMax = Vector2.zero;

            LogScatterDrawer drawer = graphAreaGo.AddComponent<LogScatterDrawer>();
            window._drawer = drawer;
            window._graphContainerRect = graphContainerRect;
            window._plotAreaRect = plotAreaRect;
            window._graphRect = graphRect;

            TMP_FontAsset graphFont = null;
            if (drawer != null)
            {
                drawer.LoadLog(data);
                graphFont = drawer.GraphFontAsset;  
            }

            window.ApplyHeaderFonts(graphFont);
            
            window._data = data;
            window.CreateStatsTexts(windowGo.transform, graphFont);
            window.RefreshStats();
            window._settingsUI = LogGraphSettingsUI.Create(windowRect, window, graphFont);
            window.ApplyLayout(size, position);
        }
        
        private static readonly string[] StatLabelKeys =
        {
            "LogGraph_StatTotalHits",
            "LogGraph_StatUR",
            "LogGraph_StatRatio",
            "LogGraph_StatXAcc"
        };

        private void CreateStatsTexts(Transform parent, TMP_FontAsset font)
        {
            _statsTitleText = CreateStatsText(parent, "StatsTitle", i18n.T("LogGraph_StatsTitle"), StatsLabelFontSize, StatsTitleColor, font);

            for (int i = 0; i < StatsRowCount; i++)
            {
                _statLabels[i] = CreateStatsText(parent, "StatLabel" + i, (i18n.T(StatLabelKeys[i]) ?? "").ToUpperInvariant(), StatsLabelFontSize, StatsLabelColor, font);
                _statValues[i] = CreateStatsText(parent, "StatValue" + i, "", StatsValueFontSize, StatsValueColor, font);
            }
        }

        private static TextMeshProUGUI CreateStatsText(Transform parent, string name, string content,
            float fontSize, Color color, TMP_FontAsset font)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            if (font != null) text.font = font;
            return text;
        }


        private void RefreshStats()
        {
            if (!_statsComputed && _data != null)
            {
                ComputeStats();
            }

            UpdateStatTexts();
        }
        
        private void ComputeStats()
        {
            _statTotal = 0;
            _statNormalPerfect = 0;
            _statPerfectFamily = 0;
            _statXPerfect = 0;
            _statUr = 0.0;

            List<TimingScatterSample> samples = _data != null ? _data.Samples : null;
            if (samples != null)
            {
                List<double> offsets = new List<double>(samples.Count);
                for (int i = 0; i < samples.Count; i++)
                {
                    TimingScatterSample sample = samples[i];
                    if (!sample.HasJudge) continue;

                    _statTotal++;
                    if (!float.IsNaN(sample.OffsetMs) && !float.IsInfinity(sample.OffsetMs))
                    {
                        offsets.Add(sample.OffsetMs);
                    }
                    if (HitMarginCompat.IsPerfectFamily(sample.Judge)) _statPerfectFamily++;
                    if (HitMarginCompat.IsNormalPerfect(sample.Judge)) _statNormalPerfect++;
                    if (sample.IsXPerfect) _statXPerfect++;
                }

                if (offsets.Count > 0) _statUr = CalcUR.calc(offsets);
            }

            _statsComputed = true;
        }
        
        private void UpdateStatTexts()
        {
            Settings settings = ModContext.Settings;
            int percentDigits = settings != null ? settings.Perc3 : 1;
            int ratioDigits = settings != null ? settings.PercRatioHUD : 1;
            int ratioMode = settings != null ? settings.Ratio_Mode : Settings.RatioMode_NormalPerfect;

            _statTexts[0] = _statTotal.ToString();
            _statTexts[1] = _statUr.ToString("F" + percentDigits);

            int target;
            switch (ratioMode)
            {
                case Settings.RatioMode_PerfectFamily:
                    target = _statPerfectFamily;
                    break;
                case Settings.RatioMode_XPerfect:
                    target = _statXPerfect;
                    break;
                default:
                    target = _statNormalPerfect;
                    break;
            }

            int other = _statTotal - target;
            if (_statTotal == 0) _statTexts[2] = "0:1";
            else if (other <= 0) _statTexts[2] = "infinity";
            else _statTexts[2] = ((double)target / other).ToString("F" + ratioDigits) + ":1";

            float xAcc = _statTotal > 0 ? (float)_statXPerfect / _statTotal * 100f : 0f;
            _statTexts[3] = xAcc.ToString("F" + percentDigits) + "%";

            for (int i = 0; i < 4; i++)
            {
                if (_statValues[i] != null) _statValues[i].text = _statTexts[i];
            }
        }


        private void CreateHeaderTexts(Transform parent, TimingLogData data)
        {
            _titleText = CreateHeaderText(parent, "TitleText", BuildTitle(data),
                TitleFontSize, TitlePrimaryColor, TextAlignmentOptions.Left);
            _baseTitle = _titleText != null ? _titleText.text : "";

            string file = BuildFileTitle(data);
            _fileText = CreateHeaderText(parent, "TitleFileText", file,
                SecondaryTitleFontSize, TitleSecondaryColor, TextAlignmentOptions.Left);
            if (_fileText != null && string.IsNullOrEmpty(file)) _fileText.gameObject.SetActive(false);

            string meta = BuildMetaTitle(data);
            _metaText = CreateHeaderText(parent, "TitleMetaText", meta,
                MetaFontSize, TitleMetaColor, TextAlignmentOptions.MidlineRight);
            if (_metaText != null && string.IsNullOrEmpty(meta)) _metaText.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI CreateHeaderText(Transform parent, string name, string content,
            float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private void ApplyHeaderFonts(TMP_FontAsset asset)
        {
            ApplyTitleFont(_titleText, asset);
            ApplyTitleFont(_fileText, asset);
            ApplyTitleFont(_metaText, asset);
        }

        private static void ApplyTitleFont(TextMeshProUGUI text, TMP_FontAsset asset)
        {
            // 只接受显式传入的（系统）字体，不主动改用游戏字体
            if (text == null || asset == null) return;
            if (text.font == asset) return;

            text.font = asset;
            text.SetVerticesDirty();
            text.SetLayoutDirty();
        }
        
        private static string BuildTitle(TimingLogData data)
        {
            if (data == null) return "";
            return string.IsNullOrEmpty(data.SongName) ? "?" : data.SongName;
        }
        
        private static string BuildFileTitle(TimingLogData data)
        {
            if (data == null || string.IsNullOrEmpty(data.FilePath)) return "";

            try { return Path.GetFileName(data.FilePath); }
            catch { return data.FilePath; }
        }
        
        private static string BuildMetaTitle(TimingLogData data)
        {
            if (data == null) return "";

            StringBuilder sb = new StringBuilder(48);
            if (data.Bpm > 0.0)
            {
                sb.Append("BPM ");
                sb.Append(data.Bpm.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("  x");
                sb.Append(data.Speed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            }
            
            if (data.IsAngle)
            {
                if (sb.Length > 0) sb.Append("   ");
                sb.Append("Angle");
            }

            return sb.ToString();
        }



        private void ApplyLayout(Vector2 size, Vector2 position)
        {
            if (_windowRect == null)
            {
                Close();
                return;
            }

            _windowRect.anchorMin = Vector2.zero;
            _windowRect.anchorMax = Vector2.zero;
            _windowRect.pivot = Vector2.zero;
            _windowRect.sizeDelta = size;
            _windowRect.anchoredPosition = position;
            
            ApplyHeaderLayout(size);
            RefreshTitle();
            
            bool settingsOpen = _settingsUI != null && _settingsUI.IsOpen;
            
            Rect container, yAxisRect, plot, xAxisRect, topGutterRect;
            GraphRects(size.x, size.y, out container, out yAxisRect, out plot, out xAxisRect, out topGutterRect);
            
            if (_graphContainerRect != null)
            {
                _graphContainerRect.anchorMin = Vector2.zero;
                _graphContainerRect.anchorMax = Vector2.zero;
                _graphContainerRect.pivot = Vector2.zero;
                _graphContainerRect.anchoredPosition = new Vector2(container.x, container.y);
                _graphContainerRect.sizeDelta = new Vector2(container.width, container.height);
            }
            
            if (_plotAreaRect != null)
            {
                _plotAreaRect.anchorMin = Vector2.zero;
                _plotAreaRect.anchorMax = Vector2.zero;
                _plotAreaRect.pivot = Vector2.zero;
                _plotAreaRect.anchoredPosition = new Vector2(plot.x - container.x, plot.y - container.y);
                _plotAreaRect.sizeDelta = new Vector2(plot.width, plot.height);
            }
            
            if (_graphRect != null)
            {
                _graphRect.anchorMin = Vector2.zero;
                _graphRect.anchorMax = Vector2.one;
                _graphRect.pivot = Vector2.zero;
                _graphRect.offsetMin = Vector2.zero;
                _graphRect.offsetMax = Vector2.zero;
            }
            
            if (settingsOpen)
            {
                ApplyStatsLayout(size, ContentHeight(size.y), StatsColumnWidth(size.x));
                ApplySettingsLayout(size);
            }
            
            SetStatsSidebarVisible(settingsOpen);
            SetGraphPageVisible(!settingsOpen);

            if (_panel != null)
            {
                _panel.SetSettingsActive(settingsOpen);
                _panel.MarkDirty();
            }
        }
        
        private void ApplyHeaderLayout(Vector2 size)
        {
            if (_titleText == null) return;

            float pad = HeaderPadding;
            float innerHeight = Mathf.Max(1f, HeaderHeight - pad * 2f);

            bool hasFile = _fileText != null && _fileText.gameObject.activeSelf && !string.IsNullOrEmpty(_fileText.text);
            bool hasMeta = _metaText != null && _metaText.gameObject.activeSelf && !string.IsNullOrEmpty(_metaText.text);

            float titleLine = TitleFontSize * TextLineRatio;
            float fileLine = SecondaryTitleFontSize * TextLineRatio;
            float fit = hasFile ? Mathf.Min(1f, innerHeight / Mathf.Max(1f, titleLine + fileLine)) : 1f;

            float titleHeight = hasFile ? titleLine * fit : Mathf.Min(innerHeight, titleLine);
            float fileHeight = hasFile ? fileLine * fit : 0f;
            float titleFontSize = hasFile ? Mathf.Max(8f, TitleFontSize * fit) : TitleFontSize;
            float fileFontSize = Mathf.Max(8f, SecondaryTitleFontSize * fit);
            
            float reserved = TitleLeftPadding + TitleRightPadding;
            float metaWidth = Mathf.Min(MetaWidth, Mathf.Max(0f, (size.x - reserved) * 0.5f));
            float titleWidth = Mathf.Max(0f, size.x - reserved - (hasMeta ? metaWidth + TitleMetaGap : 0f));

            if (!Mathf.Approximately(_titleText.fontSize, titleFontSize)) _titleText.fontSize = titleFontSize;
            _titleText.alignment = hasFile ? TextAlignmentOptions.Left : TextAlignmentOptions.MidlineLeft;

            RectTransform titleRect = _titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(TitleLeftPadding,
                hasFile ? -pad : -pad - (innerHeight - titleHeight) * 0.5f);
            titleRect.sizeDelta = new Vector2(titleWidth, titleHeight);

            if (_fileText != null)
            {
                if (!Mathf.Approximately(_fileText.fontSize, fileFontSize)) _fileText.fontSize = fileFontSize;

                RectTransform rect = _fileText.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(TitleLeftPadding, -pad - titleHeight);
                rect.sizeDelta = new Vector2(titleWidth, fileHeight);
            }

            if (_metaText != null)
            {
                float metaFontSize = MetaFontSize;
                if (!Mathf.Approximately(_metaText.fontSize, metaFontSize)) _metaText.fontSize = metaFontSize;

                RectTransform rect = _metaText.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(size.x - TitleRightPadding, -pad);
                rect.sizeDelta = new Vector2(metaWidth, titleHeight);
            }
        }
        
        private void ApplyStatsLayout(Vector2 size, float contentHeight, float statsWidth)
        {
            float panelX = size.x - ContentInset - statsWidth;
            float textX = panelX + 2f * UiScale;
            float top = HeaderHeight + ContentInset;
            
            float headerHeight, rowsOffset, rowHeight, labelHeight, valueHeight;
            StatsRowMetrics(contentHeight, out headerHeight, out rowsOffset, out rowHeight, out labelHeight,
                out valueHeight);
            float rowsTop = top + rowsOffset;
            
            float labelFontSize = Mathf.Clamp(Mathf.Min(StatsLabelFontSize, labelHeight / TextLineRatio), 8f, StatsLabelFontSize);
            float valueFontSize = Mathf.Clamp(Mathf.Min(StatsValueFontSize, valueHeight / TextLineRatio), 8f, StatsValueFontSize);

            if (_statsTitleText != null)
            {
                float titleFontSize = Mathf.Clamp(
                    Mathf.Min(StatsValueFontSize * 0.85f, headerHeight / TextLineRatio), 8f, StatsValueFontSize);
                if (!Mathf.Approximately(_statsTitleText.fontSize, titleFontSize)) _statsTitleText.fontSize = titleFontSize;

                RectTransform rect = _statsTitleText.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(textX, -top);
                rect.sizeDelta = new Vector2(statsWidth, headerHeight);
            }

            for (int i = 0; i < StatsRowCount; i++)
            {
                if (_statLabels[i] != null)
                {
                    if (!Mathf.Approximately(_statLabels[i].fontSize, labelFontSize)) _statLabels[i].fontSize = labelFontSize;

                    RectTransform rect = _statLabels[i].rectTransform;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(textX, -(rowsTop + i * rowHeight));
                    rect.sizeDelta = new Vector2(statsWidth, labelHeight);
                }

                if (_statValues[i] != null)
                {
                    if (!Mathf.Approximately(_statValues[i].fontSize, valueFontSize)) _statValues[i].fontSize = valueFontSize;

                    RectTransform rect = _statValues[i].rectTransform;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(textX, -(rowsTop + i * rowHeight + labelHeight));
                    rect.sizeDelta = new Vector2(statsWidth, valueHeight);
                }
            }
        }
        
        private void ApplySettingsLayout(Vector2 size)
        {
            if (_settingsUI == null) return;

            Rect settings = SettingsRect(size.x, size.y);
            _settingsUI.ApplyLayout(settings.x, settings.y, settings.width, settings.height);
        }

        private static Vector2 ClampSize(Vector2 size, float screenWidth, float screenHeight)
        {
            float maxWidth = Mathf.Max(MinWindowWidth, Mathf.Min(MaxWindowWidth, screenWidth));
            float maxHeight = Mathf.Max(MinWindowHeight, Mathf.Min(MaxWindowHeight, screenHeight));

            size.x = Mathf.Clamp(size.x, MinWindowWidth, maxWidth);
            size.y = Mathf.Clamp(size.y, MinWindowHeight, maxHeight);
            return size;
        }

        private static Vector2 ClampPosition(Vector2 position, Vector2 size, float screenWidth, float screenHeight)
        {
            float maxX = Mathf.Max(0f, screenWidth - size.x);
            float maxY = Mathf.Max(0f, screenHeight - size.y);

            position.x = Mathf.Clamp(position.x, 0f, maxX);
            position.y = Mathf.Clamp(position.y, 0f, maxY);
            return position;
        }



        private static LogGraphWindow _dragProxy;
        private static ILogGraphPanelInteraction _hoverProxy;
        private static IPointerExitHandler _exitProxy;

        private static void ApplyProxy()
        {
            LogGraphWindowPanel panel = _instance != null ? _instance._panel : null;
            if (panel == null) return;

            panel.SetDragProxy(_dragProxy);
            panel.SetHoverProxy(_hoverProxy, _exitProxy);
        }

        public void OnPanelPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_windowRect == null) return;

            Vector2 screen = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            Vector2 local;
            if (!ScreenToLocal(screen, out local)) return;

            Vector2 size = _windowRect.sizeDelta;
            bool onClose = HitCloseButton(local, size);
            bool onSettings = !onClose && HitSettingsButton(local, size);
            bool onGrip = !onClose && !onSettings && HitResizeGrip(local, size);
            bool onTitleBar = !onClose && !onSettings && !onGrip && local.y >= size.y - HeaderHeight;
            
            if (onClose || onSettings) return;
            if (IsPointerInsideGraphArea(screen)) return;
            if (!onGrip && !onTitleBar) return;

            _resizing = onGrip;
            _updateFallbackDrag = false;
            _dragStartScreen = screen;
            _windowAtDragStart = _windowRect.anchoredPosition;
            _sizeAtDragStart = size;
            _dragging = true;
        }

        public void OnPanelDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_windowRect == null) return;

            Vector2 screen = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            ApplyDrag(screen);
        }

        public void OnPanelPointerUp(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            EndDrag();
        }

        public void OnPanelPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_windowRect == null) return;
        }

        public void OnPanelPointerMove(PointerEventData eventData)
        {
            if (eventData == null) return;
            RefreshButtonHover(eventData.position);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            OnPanelPointerExit(eventData);
        }

        internal void OnPanelPointerExit(PointerEventData eventData)
        {
            if (_panel != null)
            {
                _panel.SetHover(false);
                _panel.SetSettingsHover(false);
            }
        }
        
        private void ToggleSettingsPanel()
        {
            if (_settingsUI == null) return;

            _settingsUI.Toggle();
            if (_windowRect != null) ApplyLayout(_windowRect.sizeDelta, _windowRect.anchoredPosition);

            SyncSettingsInteraction();
            RefreshTitle();

            Trace(_settingsUI.IsOpen ? "settings page opened" : "settings page closed");
        }
        
        private void SetGraphPageVisible(bool visible)
        {
            RectTransform target = _graphContainerRect != null ? _graphContainerRect : _graphRect;
            if (target == null) return;
            if (target.gameObject.activeSelf != visible) target.gameObject.SetActive(visible);
        }
        
        private void SetStatsSidebarVisible(bool visible)
        {
            if (_statsTitleText != null && _statsTitleText.gameObject.activeSelf != visible)
                _statsTitleText.gameObject.SetActive(visible);

            for (int i = 0; i < StatsRowCount; i++)
            {
                if (_statLabels[i] != null && _statLabels[i].gameObject.activeSelf != visible)
                    _statLabels[i].gameObject.SetActive(visible);

                if (_statValues[i] != null && _statValues[i].gameObject.activeSelf != visible)
                    _statValues[i].gameObject.SetActive(visible);
            }
        }
        
        private void RefreshTitle()
        {
            if (_titleText == null) return;

            string title = _baseTitle ?? "";
            if (_settingsUI != null && _settingsUI.IsOpen)
            {
                title = title + TitleSeparator + i18n.T("LogGraph_SettingsTitle");
            }

            if (_titleText.text != title) _titleText.text = title;
        }
        
        private void SyncSettingsInteraction()
        {
            bool suspended = _settingsUI != null && _settingsUI.IsOpen;
            if (_drawer != null) _drawer.InteractionSuspended = suspended;

            if (suspended && _graphPanActive)
            {
                _graphPanActive = false;
                if (_drawer != null) _drawer.EndPan();
            }
        }
        
        public void OnLogGraphSettingsChanged()
        {
            if (_windowRect == null) return;

            ApplyLayout(_windowRect.sizeDelta, _windowRect.anchoredPosition);
            RefreshStats();

            if (_drawer != null) _drawer.ReloadAppearance();
            if (_panel != null) _panel.MarkDirty();
            if (_settingsUI != null) _settingsUI.SetFont(_drawer != null ? _drawer.GraphFontAsset : null);
        }
        
        private void ApplyDrag(Vector2 screen)
        {
            if (_windowRect == null) { EndDrag(); return; }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (_resizing)
            {
                Vector2 size = new Vector2(_sizeAtDragStart.x + (screen.x - _dragStartScreen.x), _sizeAtDragStart.y - (screen.y - _dragStartScreen.y));
                size = ClampSize(size, screenWidth, screenHeight);
                Vector2 position = new Vector2(_windowAtDragStart.x + _sizeAtDragStart.x - size.x, _windowAtDragStart.y + _sizeAtDragStart.y - size.y);
                ApplyLayout(size, ClampPosition(position, size, screenWidth, screenHeight));
                return;
            }

            Vector2 moved = new Vector2(_windowAtDragStart.x + (screen.x - _dragStartScreen.x), _windowAtDragStart.y + (screen.y - _dragStartScreen.y));
            ApplyLayout(_windowRect.sizeDelta, ClampPosition(moved, _windowRect.sizeDelta, screenWidth, screenHeight));
        }

        private void EndDrag()
        {
            bool hasWindow = _windowRect != null;
            bool wasResizing = _resizing;
            Vector2 size = hasWindow ? _windowRect.sizeDelta : Vector2.zero;
            _dragging = false;
            _resizing = false;
            _updateFallbackDrag = false;

            if (wasResizing && hasWindow && size.x > 0f && size.y > 0f) SaveWindowSize(size);
        }
        
        private static Vector2 SavedWindowSize()
        {
            Settings settings = ModContext.Settings;
            if (settings == null) return new Vector2(DefaultWindowWidth, DefaultWindowHeight);

            float width = settings.LogGraph_WindowWidth;
            float height = settings.LogGraph_WindowHeight;

            if (!(width > 0f)) width = DefaultWindowWidth;    
            if (!(height > 0f)) height = DefaultWindowHeight;

            return new Vector2(width, height);
        }
        
        private void SaveWindowSize(Vector2 size)
        {
            Settings settings = ModContext.Settings;
            if (settings == null || string.IsNullOrEmpty(ModContext.ModPath)) return;

            if (Mathf.Approximately(settings.LogGraph_WindowWidth, size.x) &&
                Mathf.Approximately(settings.LogGraph_WindowHeight, size.y))
            {
                return;
            }

            settings.LogGraph_WindowWidth = size.x;
            settings.LogGraph_WindowHeight = size.y;
            settings.Save(ModContext.ModPath);   
        }

        private bool ScreenToLocal(Vector2 screenPosition, out Vector2 local)
        {
            if (_windowRect == null)
            {
                local = Vector2.zero;
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRect, screenPosition, null, out local);
        }

        private bool HitCloseButton(Vector2 local, Vector2 size)
        {
            float maxX = size.x - CloseButtonInset;
            float maxY = size.y - CloseButtonInset;
            float minX = maxX - CloseButtonSize;
            float minY = maxY - CloseButtonSize;

            return local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
        }
        
        private bool HitResizeGrip(Vector2 local, Vector2 size)
        {
            float maxX = size.x - GripInset;
            float minX = maxX - GripSize;
            float minY = GripInset;
            float maxY = minY + GripSize;

            return local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
        }
        
        private bool HitSettingsButton(Vector2 local, Vector2 size)
        {
            float maxX = size.x - CloseButtonInset - CloseButtonSize - ButtonGap;
            float maxY = size.y - CloseButtonInset;
            float minX = maxX - SettingsButtonSize;
            float minY = maxY - SettingsButtonSize;

            return local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
        }
        
        private void RefreshButtonHover(Vector2 screenPosition)
        {
            if (_panel == null || _windowRect == null)
            {
                if (_panel != null)
                {
                    _panel.SetHover(false);
                    _panel.SetSettingsHover(false);
                }
                return;
            }

            Vector2 local;
            if (!ScreenToLocal(screenPosition, out local))
            {
                _panel.SetHover(false);
                _panel.SetSettingsHover(false);
                return;
            }

            Vector2 size = _windowRect.sizeDelta;
            bool onClose = HitCloseButton(local, size);
            _panel.SetHover(onClose);
            _panel.SetSettingsHover(!onClose && HitSettingsButton(local, size));
        }

        private void Update()
        {
            if (_root == null)
            {
                if (!_closed)
                {
                    _closed = true;
                    _instance = null;
                    _dragProxy = null;
                    _hoverProxy = null;
                    _exitProxy = null;
                    _dragging = false;
                    _resizing = false;
                    _updateFallbackDrag = false;
                    _graphPanActive = false;
                }
                return;
            }

            if (_windowRect == null)
            {
                Close();
                return;
            }
            
            SyncSettingsInteraction();
            if (!_dragging) RefreshButtonHover(Input.mousePosition);
            UpdateInputFallback();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            _closed = true;

            if (_graphPanActive)
            {
                _graphPanActive = false;
                if (_drawer != null) _drawer.EndPan();
            }

            _dragging = false;
            _resizing = false;
            _updateFallbackDrag = false;

            if (_root != null && _root == gameObject)
            {
                _root = null;
                _dragProxy = null;
                _hoverProxy = null;
                _exitProxy = null;
            }
        }


        private void UpdateInputFallback()
        {
            Vector2 screen = Input.mousePosition;
            if (!Application.isFocused)
            {
                if (_dragging) EndDrag();
                _pendingButton = PendingButton.None;
                if (_graphPanActive)
                {
                    _graphPanActive = false;
                    if (_graphPanRunning) Trace("Unfocused");
                    _graphPanRunning = false;
                    if (_drawer != null) _drawer.EndPan();
                }
                return;
            }
            
            if (_graphPanActive)
            {
                if (Input.GetMouseButton(0))
                {
                    if (_drawer != null)
                    {
                        _drawer.MovePan(screen);
                        if (!_graphPanRunning && _drawer.IsGesturePanning)
                        {
                            _graphPanRunning = true;
                            Trace("Graph pan in progress");
                        }
                    }
                }
                else
                {
                    if (_graphPanRunning) Trace("Graph gesture ended");;
                    _graphPanRunning = false;
                    _graphPanActive = false;
                    if (_drawer != null) _drawer.EndPan();
                }
                return;
            }
            
            if (_dragging)
            {
                if (!Input.GetMouseButton(0))
                {
                    EndDrag();
                    return;
                }

                if (_windowRect == null)
                {
                    EndDrag();
                    return;
                }

                if (!_updateFallbackDrag) return;  
                ApplyDrag(screen);
                return;
            }

            if (_windowRect == null) return;
            
            float wheel = Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(wheel, 0f)
                && (_settingsUI == null || !_settingsUI.IsOpen)
                && IsPointerInsideGraphArea(screen))
            {
                if (_drawer != null && _drawer.ZoomAt(wheel, screen))
                {
                    return;
                }
            }
            
            if (Input.GetMouseButtonUp(0) && _pendingButton != PendingButton.None)
            {
                PendingButton pending = _pendingButton;
                _pendingButton = PendingButton.None;

                Vector2 upLocal;
                if (ScreenToLocal(screen, out upLocal) && HitRect(upLocal, _windowRect.sizeDelta))
                {
                    if (pending == PendingButton.Close && HitCloseButton(upLocal, _windowRect.sizeDelta))
                    {
                        Close();
                        return;
                    }

                    if (pending == PendingButton.Settings && HitSettingsButton(upLocal, _windowRect.sizeDelta))
                    {
                        ToggleSettingsPanel();
                        return;
                    }
                }
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;

            Vector2 local;
            if (!ScreenToLocal(screen, out local)) return;

            Vector2 size = _windowRect.sizeDelta;
            if (!HitRect(local, size)) return;
            
            if (HitCloseButton(local, size))
            {
                _pendingButton = PendingButton.Close;
                return;
            }

            if (HitSettingsButton(local, size))
            {
                _pendingButton = PendingButton.Settings;
                return;
            }
            
            if (HitResizeGrip(local, size))
            {
                _resizing = true;
                _updateFallbackDrag = true;
                _dragStartScreen = screen;
                _windowAtDragStart = _windowRect.anchoredPosition;
                _sizeAtDragStart = size;
                _dragging = true;
                return;
            }
            
            if (_settingsUI != null && _settingsUI.IsOpen && _settingsUI.ContainsScreenPoint(screen))
            {
                return;
            }
            
            if (local.y >= size.y - HeaderHeight)
            {
                Trace("按下 → 标题栏");
                _resizing = false;
                _updateFallbackDrag = true;
                _dragStartScreen = screen;
                _windowAtDragStart = _windowRect.anchoredPosition;
                _sizeAtDragStart = size;
                _dragging = true;
                return;
            }
            
            if (_drawer != null && IsPointerInsideGraphArea(screen))
            {
                _graphPanActive = true;
                _drawer.BeginPan(screen);
                return;
            }
            
        }


        private bool IsPointerInsideGraphArea(Vector2 screenPosition)
        {
            if (_windowRect == null) return false;

            Vector2 local;
            if (!ScreenToLocal(screenPosition, out local)) return false;

            Vector2 size = _windowRect.sizeDelta;
            Rect graph = GraphContainerRect(size.x, size.y);
            return local.x >= graph.xMin && local.x <= graph.xMax && local.y >= graph.yMin && local.y <= graph.yMax;
        }

        private static bool HitRect(Vector2 local, Vector2 size)
        {
            return local.x >= 0f && local.x <= size.x && local.y >= 0f && local.y <= size.y;
        }


        
        public interface ILogGraphPanelInteraction
        {
            void OnPanelPointerDown(PointerEventData eventData);
            void OnPanelDrag(PointerEventData eventData);
            void OnPanelPointerUp(PointerEventData eventData);
            void OnPanelPointerClick(PointerEventData eventData);
            void OnPanelPointerMove(PointerEventData eventData);
        }
        
        internal class LogGraphWindowPanel : MaskableGraphic,
            IPointerDownHandler, IDragHandler, IPointerUpHandler,
            IPointerClickHandler, IPointerMoveHandler,
            IPointerExitHandler
        {
            private ILogGraphPanelInteraction _dragProxy;
            private ILogGraphPanelInteraction _hoverProxy;
            private IPointerExitHandler _exitProxy;
            private bool _hovered;
            private bool _settingsHovered;
            private bool _settingsActive;

            protected override void Awake()
            {
                base.Awake();

                color = Color.white;       
                raycastTarget = true;     
                useLegacyMeshGeneration = false;
            }

            internal void SetDragProxy(ILogGraphPanelInteraction proxy)
            {
                _dragProxy = proxy;
            }

            internal void SetHoverProxy(ILogGraphPanelInteraction proxy, IPointerExitHandler exitProxy)
            {
                _hoverProxy = proxy;
                _exitProxy = exitProxy;
            }

            internal void SetHover(bool hovered)
            {
                if (_hovered == hovered) return;

                _hovered = hovered;
                SetVerticesDirty();
            }

            internal void SetSettingsHover(bool hovered)
            {
                if (_settingsHovered == hovered) return;

                _settingsHovered = hovered;
                SetVerticesDirty();
            }
            
            internal void SetSettingsActive(bool active)
            {
                if (_settingsActive == active) return;

                _settingsActive = active;
                SetVerticesDirty();
            }
            
            internal void MarkDirty()
            {
                SetVerticesDirty();
            }
            
            

            public void OnPointerDown(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelPointerDown(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelDrag(eventData);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelPointerUp(eventData);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelPointerClick(eventData);
            }

            public void OnPointerMove(PointerEventData eventData)
            {
                if (eventData == null) return;

                if (_hoverProxy != null) _hoverProxy.OnPanelPointerMove(eventData);
                else RefreshHover(eventData.position);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (_exitProxy != null) _exitProxy.OnPointerExit(eventData);
                else
                {
                    SetHover(false);
                    SetSettingsHover(false);
                }
            }

            private void RefreshHover(Vector2 screenPosition)
            {
                RectTransform rt = rectTransform;
                if (rt == null) return;

                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPosition, null, out local))
                {
                    SetHover(false);
                    SetSettingsHover(false);
                    return;
                }

                Rect rect = rt.rect;
                float size = Mathf.Min(CloseButtonSize, Mathf.Min(rect.width, rect.height));
                float maxX = rect.xMax - CloseButtonInset;
                float maxY = rect.yMax - CloseButtonInset;
                float minX = maxX - size;
                float minY = maxY - size;

                bool onClose = local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
                SetHover(onClose);

                float setMaxX = rect.xMax - CloseButtonInset - CloseButtonSize - ButtonGap;
                float setMaxY = rect.yMax - CloseButtonInset;
                float setMinX = setMaxX - SettingsButtonSize;
                float setMinY = setMaxY - SettingsButtonSize;
                SetSettingsHover(!onClose && local.x >= setMinX && local.x <= setMaxX
                    && local.y >= setMinY && local.y <= setMaxY);
            }
            
            protected override void OnRectTransformDimensionsChange()
            {
                base.OnRectTransformDimensionsChange();
                SetVerticesDirty();
            }
            
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
                float w = rect.width;
                float h = rect.height;
                if (w <= 0f || h <= 0f) return;

                float x0 = rect.xMin;
                float y0 = rect.yMin;
                float x1 = rect.xMax;
                float y1 = rect.yMax;
                float border = Mathf.Min(BorderThickness, Mathf.Min(w, h) * 0.25f);
                float titleTop = y1 - border;
                float titleBottom = Mathf.Max(y0 + border, titleTop - HeaderHeight);
                
                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x1, y1), PanelColor);
                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x1, y0 + border), BorderColor);
                DrawQuad(vh, new Vector2(x0, y1 - border), new Vector2(x1, y1), BorderColor);
                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x0 + border, y1), BorderColor);
                DrawQuad(vh, new Vector2(x1 - border, y0), new Vector2(x1, y1), BorderColor);
                
                if (titleTop > titleBottom)
                {
                    DrawQuad(vh, new Vector2(x0 + border, titleBottom), new Vector2(x1 - border, titleTop), TitleBarColor);

                    float headerEdge = Mathf.Max(1f, border * 0.5f);
                    Color headerEdgeColor = BorderColor;
                    headerEdgeColor.a *= 0.45f;
                    DrawQuad(vh, new Vector2(x0 + border, titleBottom), new Vector2(x1 - border, titleBottom + headerEdge), headerEdgeColor);
                }
                
                
                float closeMaxX = x1 - CloseButtonInset;
                float closeMaxY = y1 - CloseButtonInset;
                float closeMinX = closeMaxX - CloseButtonSize;
                float closeMinY = closeMaxY - CloseButtonSize;
                if (closeMinX > x0 && closeMinY > y0)
                {
                    DrawQuad(vh, new Vector2(closeMinX, closeMinY), new Vector2(closeMaxX, closeMaxY), _hovered ? CloseHoverColor : CloseNormalColor);

                    float glyphThickness = Mathf.Max(1f, CloseGlyphThickness);
                    float glyphInset = CloseGlyphInset;
                    Vector2 gMin = new Vector2(closeMinX + glyphInset, closeMinY + glyphInset);
                    Vector2 gMax = new Vector2(closeMaxX - glyphInset, closeMaxY - glyphInset);
                    if (gMax.x > gMin.x && gMax.y > gMin.y)
                    {
                        DrawThickLine(vh, new Vector2(gMin.x, gMin.y), new Vector2(gMax.x, gMax.y), glyphThickness, Color.white);
                        DrawThickLine(vh, new Vector2(gMin.x, gMax.y), new Vector2(gMax.x, gMin.y), glyphThickness, Color.white);
                    }
                }
                
                float setMaxX = closeMinX - ButtonGap;
                float setMaxY = y1 - CloseButtonInset;
                float setMinX = setMaxX - SettingsButtonSize;
                float setMinY = setMaxY - SettingsButtonSize;
                if (setMinX > x0 && setMinY > y0)
                {
                    Color setColor = _settingsActive ? SettingsButtonActiveColor : (_settingsHovered ? SettingsButtonHoverColor : CloseNormalColor);
                    DrawQuad(vh, new Vector2(setMinX, setMinY), new Vector2(setMaxX, setMaxY), setColor);

                    float barThickness = Mathf.Max(1f, CloseGlyphThickness);
                    float barLeft = setMinX + CloseGlyphInset * 0.75f;
                    float barRight = setMaxX - CloseGlyphInset * 0.75f;
                    float barCenter = (setMinY + setMaxY) * 0.5f;
                    float barSpacing = Mathf.Max(2f, (setMaxY - setMinY) * 0.22f);
                    if (barRight > barLeft)
                    {
                        DrawThickLine(vh, new Vector2(barLeft, barCenter - barSpacing), new Vector2(barRight, barCenter - barSpacing), barThickness, Color.white);
                        DrawThickLine(vh, new Vector2(barLeft, barCenter), new Vector2(barLeft + (barRight - barLeft) * 0.75f, barCenter), barThickness, Color.white);
                        DrawThickLine(vh, new Vector2(barLeft, barCenter + barSpacing), new Vector2(barRight, barCenter + barSpacing), barThickness, Color.white);
                    }
                }
                
                if (!_settingsActive)
                {
                    Rect graph = GraphContainerRect(w, h);
                    if (graph.width > 2f && graph.height > 2f)
                    {
                        DrawQuad(vh, new Vector2(graph.xMin, graph.yMin), new Vector2(graph.xMax, graph.yMax), ChartCardColor);

                        float cardBorder = Mathf.Max(1f, border * 0.6f);
                        DrawQuad(vh, new Vector2(graph.xMin, graph.yMin), new Vector2(graph.xMax, graph.yMin + cardBorder), ChartCardBorderColor);
                        DrawQuad(vh, new Vector2(graph.xMin, graph.yMax - cardBorder), new Vector2(graph.xMax, graph.yMax), ChartCardBorderColor);
                        DrawQuad(vh, new Vector2(graph.xMin, graph.yMin), new Vector2(graph.xMin + cardBorder, graph.yMax), ChartCardBorderColor);
                        DrawQuad(vh, new Vector2(graph.xMax - cardBorder, graph.yMin), new Vector2(graph.xMax, graph.yMax), ChartCardBorderColor);
                    }
                }
                
                Rect sidebar = SidebarRect(w, h);
                if (_settingsActive && sidebar.width > 0f && sidebar.height > 0f)
                {
                    DrawQuad(vh, new Vector2(sidebar.xMin, sidebar.yMin), new Vector2(sidebar.xMax, sidebar.yMax), StatsPanelColor);

                    float dividerThickness = Mathf.Max(1f, border * 0.5f);
                    float dividerX = sidebar.xMin - StatsPanelGap * 0.5f;
                    DrawQuad(vh, new Vector2(dividerX, sidebar.yMin), new Vector2(dividerX + dividerThickness, sidebar.yMax), StatsDividerColor);

                    float rowHeaderHeight, rowsOffset, rowHeight, rowLabelHeight, rowValueHeight;
                    StatsRowMetrics(sidebar.height, out rowHeaderHeight, out rowsOffset, out rowHeight, out rowLabelHeight, out rowValueHeight);
                    
                    float headerLineY = sidebar.yMax - rowHeaderHeight;
                    if (headerLineY > sidebar.yMin + dividerThickness * 2f)
                    {
                        DrawQuad(vh, new Vector2(sidebar.xMin, headerLineY), new Vector2(sidebar.xMax, headerLineY + dividerThickness), StatsDividerColor);
                    }
                    
                    for (int i = 1; i < StatsRowCount; i++)
                    {
                        float lineY = headerLineY - i * rowHeight;
                        if (lineY <= sidebar.yMin + dividerThickness * 2f) break;
                        DrawQuad(vh, new Vector2(sidebar.xMin, lineY), new Vector2(sidebar.xMax, lineY + dividerThickness), StatsDividerColor);
                    }
                }
                
                float gripThickness = Mathf.Max(1f, GripLineThickness);
                float gripMax = GripSize;
                Color grip = GripColor;
                DrawThickLine(vh, new Vector2(x1 - GripInset - gripMax * 0.375f, y0 + GripInset), new Vector2(x1 - GripInset, y0 + GripInset + gripMax * 0.375f), gripThickness, grip);
                DrawThickLine(vh, new Vector2(x1 - GripInset - gripMax * 0.6875f, y0 + GripInset), new Vector2(x1 - GripInset, y0 + GripInset + gripMax * 0.6875f), gripThickness, grip);
                DrawThickLine(vh, new Vector2(x1 - GripInset - gripMax, y0 + GripInset), new Vector2(x1 - GripInset, y0 + GripInset + gripMax), gripThickness, grip);
            }

            private static void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
            {
                if (max.x <= min.x || max.y <= min.y) return;

                int baseIndex = vh.currentVertCount;
                vh.AddVert(new Vector3(min.x, min.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(min.x, max.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(max.x, max.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(max.x, min.y, 0f), color, Vector2.zero);
                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            }

            private static void DrawThickLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color color)
            {
                float dx = to.x - from.x;
                float dy = to.y - from.y;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                if (length <= 0.0001f) return;

                Vector2 direction = new Vector2(dx / length, dy / length);
                Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

                int baseIndex = vh.currentVertCount;
                vh.AddVert(new Vector3(from.x - normal.x, from.y - normal.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(from.x + normal.x, from.y + normal.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(to.x + normal.x, to.y + normal.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(to.x - normal.x, to.y - normal.y, 0f), color, Vector2.zero);
                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            }
        }
    }
}
