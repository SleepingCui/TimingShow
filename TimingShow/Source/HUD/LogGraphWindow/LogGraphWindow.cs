using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TimingShow.Logging;
using GraphSettingsUI = TimingShow.HUD.LogGraphSettingsUI.LogGraphSettingsUI;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow : MonoBehaviour, LogGraphWindow.ILogGraphPanelInteraction, IPointerExitHandler, ILogGraphSettingsHost
    {
        private const string RootObjectName = "TimingShow_LogGraphCanvas";
        private const string WindowObjectName = "LogGraphWindow";
        private const string GraphContainerObjectName = "GraphContainer";
        private const string PlotAreaObjectName = "PlotArea";
        private const string GraphAreaObjectName = "GraphArea";
        private const string DistributionAreaObjectName = "DistributionArea";
        private const string XaccAreaObjectName = "XaccArea";
        private const string DeltaAreaObjectName = "DeltaArea";
        private const string RollingAreaObjectName = "RollingArea";
        private const string ReturnAreaObjectName = "ReturnArea";
        private const string ComboAreaObjectName = "ComboArea";
        private const string AcfAreaObjectName = "AcfArea";
        private const string OverviewAreaObjectName = "OverviewArea";

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
        private const float BaseAxisLabelGap = 6f;
        private const float BaseAxisLabelMargin = 3f;
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
        private static float AxisLabelGap => BaseAxisLabelGap * UiScale;
        private static float AxisLabelMargin => BaseAxisLabelMargin * UiScale;
        private static float TitleRightPadding => CloseButtonSize + SettingsButtonSize + CloseButtonInset * 3f + ButtonGap;
        
        private static float ContentWidth(float windowWidth) => Mathf.Max(0f, windowWidth - ContentInset * 2f);
        
        private static float ContentHeight(float windowHeight) =>
            Mathf.Max(0f, windowHeight - HeaderHeight - ContentInset * 2f);
        

        private const int StatsRowCount = 4;
        
        private static Rect HeaderRect(float windowWidth, float windowHeight) => new Rect(0f, Mathf.Max(0f, windowHeight - HeaderHeight), Mathf.Max(0f, windowWidth), HeaderHeight);
        private static Rect ContentRect(float windowWidth, float windowHeight) => ContentRectCore(windowWidth, windowHeight, UiScale);

        internal static Rect ContentRectCore(float windowWidth, float windowHeight, float uiScale)
        {
            float inset = BaseContentInset * uiScale;
            float width = Mathf.Max(0f, windowWidth - inset * 2f);
            float height = Mathf.Max(0f, windowHeight - BaseHeaderHeight * uiScale - inset * 2f);
            return new Rect(inset, inset, width, height);
        }
        
        private static float StatsColumnWidth(float windowWidth)
        {
            float max = Mathf.Max(StatsMinWidth, ContentWidth(windowWidth) * 0.25f);
            return Mathf.Clamp(StatsPanelWidth, StatsMinWidth, max);
        }
        
        private static Rect WindowRect(float windowWidth, float windowHeight) => new Rect(0f, 0f, Mathf.Max(0f, windowWidth), Mathf.Max(0f, windowHeight));
        
        private static Rect GraphContainerRect(float windowWidth, float windowHeight) => GraphContainerRectCore(windowWidth, windowHeight, UiScale);

        internal static Rect GraphContainerRectCore(float windowWidth, float windowHeight, float uiScale)
        {
            Rect content = ContentRectCore(windowWidth, windowHeight, uiScale);
            float pad = BaseGraphPadding * uiScale;
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
        
        private static void GraphRects(float windowWidth, float windowHeight, float yAxisLabelWidth, float footerHeight,
            out Rect container, out Rect yAxis, out Rect plot, out Rect xAxis, out Rect topGutter, out Rect rightBand)
        {
            GraphRectsCore(windowWidth, windowHeight, yAxisLabelWidth, footerHeight, UiScale,
                out container, out yAxis, out plot, out xAxis, out topGutter, out rightBand);
        }

        internal static void GraphRectsCore(float windowWidth, float windowHeight, float yAxisLabelWidth,
            float footerHeight, float uiScale,
            out Rect container, out Rect yAxis, out Rect plot, out Rect xAxis, out Rect topGutter, out Rect rightBand)
        {
            container = GraphContainerRectCore(windowWidth, windowHeight, uiScale);

            float pad = BaseChartPadding * uiScale;
            float innerX = container.x + pad;
            float innerY = container.y + pad;
            float innerWidth = Mathf.Max(1f, container.width - pad * 2f);
            float innerHeight = Mathf.Max(1f, container.height - pad * 2f);

            float labelGap = BaseAxisLabelGap * uiScale;
            float labelMargin = BaseAxisLabelMargin * uiScale;
            float gutterRight = Mathf.Min(BaseAxisGutterRight * uiScale, innerWidth * 0.32f);
            float maxGutterLeft = Mathf.Max(1f, innerWidth * 0.35f);
            float leftLabelWidth = yAxisLabelWidth > 1f
                ? yAxisLabelWidth
                : Mathf.Max(1f, BaseAxisGutterLeft * uiScale - labelGap - labelMargin);
            float gutterLeft = Mathf.Clamp(leftLabelWidth + labelGap + labelMargin, 1f, maxGutterLeft);
            float gutterTop = Mathf.Min(BaseAxisGutterTop * uiScale, innerHeight * 0.10f);

            float axisFontSize = Mathf.Clamp(Mathf.RoundToInt(11f * uiScale), 8f, 32f);
            float fallbackFooter = 3f * uiScale + 16f * (axisFontSize / 12f) + 1f;
            float footer = footerHeight > 1f ? footerHeight : fallbackFooter;
            float gutterBottom = Mathf.Clamp(footer, Mathf.Min(BaseAxisGutterBottom * uiScale, innerHeight * 0.16f), Mathf.Max(1f, innerHeight * 0.40f));

            plot = new Rect(innerX, innerY, innerWidth, innerHeight);
            yAxis = new Rect(innerX, innerY, Mathf.Max(1f, gutterLeft), innerHeight);
            xAxis = new Rect(innerX, innerY, innerWidth, Mathf.Max(1f, gutterBottom));
            topGutter = new Rect(innerX, innerY + innerHeight - Mathf.Max(1f, gutterTop), innerWidth, Mathf.Max(1f, gutterTop));
            rightBand = new Rect(innerX + innerWidth - Mathf.Max(1f, gutterRight), innerY, Mathf.Max(1f, gutterRight), innerHeight);
        }
        
        private Rect _resolvedContainerRect;
        private Rect _resolvedYAxisRect;
        private Rect _resolvedPlotRect;
        private Rect _resolvedXAxisRect;
        private Rect _resolvedTopGutterRect;
        private Rect _resolvedRightBandRect;
        private bool _graphLayoutResolved;

        private void ResolveGraphLayout(float windowWidth, float windowHeight)
        {
            float yAxisLabelWidth = _drawer != null ? _drawer.AxisLabelWidth : 0f;
            float footerHeight = _drawer != null ? _drawer.AxisFooterHeight : 0f;

            Rect container, yAxis, plot, xAxis, topGutter, rightBand;
            GraphRects(windowWidth, windowHeight, yAxisLabelWidth, footerHeight,
                out container, out yAxis, out plot, out xAxis, out topGutter, out rightBand);

            _resolvedContainerRect = container;
            _resolvedYAxisRect = yAxis;
            _resolvedPlotRect = plot;
            _resolvedXAxisRect = xAxis;
            _resolvedTopGutterRect = topGutter;
            _resolvedRightBandRect = rightBand;
            _graphLayoutResolved = true;

            if (_drawer == null) return;

            float bandLeft = _resolvedYAxisRect.width;
            float bandBottom = _resolvedXAxisRect.height;
            float bandRight = _resolvedRightBandRect.width;
            float bandTop = _resolvedTopGutterRect.height;
            _drawer.SetAxisBands(bandLeft, bandBottom, bandRight, bandTop);
        }

        private Rect ResolvedPlotRect()
        {
            if (!_graphLayoutResolved && _windowRect != null)
            {
                Vector2 size = _windowRect.sizeDelta;
                ResolveGraphLayout(size.x, size.y);
            }

            return _resolvedPlotRect;
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
        private static readonly Color GraphContainerColor = new Color(1f, 1f, 1f, 0.02f);

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
        private bool _pendingTextRefresh;
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
        private int _playerCount = 1;
        private readonly int[] _statTotalByPlayer = new int[4];
        private readonly int[] _statNormalPerfectByPlayer = new int[4];
        private readonly int[] _statPerfectFamilyByPlayer = new int[4];
        private readonly int[] _statXPerfectByPlayer = new int[4];
        private readonly double[] _statUrByPlayer = new double[4];
        private GraphSettingsUI _settingsUI;
        private bool _dragging;
        private bool _resizing;
        private bool _updateFallbackDrag;
        private Vector2 _dragStartScreen;
        private Vector2 _windowAtDragStart;
        private Vector2 _sizeAtDragStart;
        private bool _graphPanActive;
        private bool _graphPanRunning;
        private LogDistributionDrawer _distributionDrawer;
        private RectTransform _distributionRect;
        private DistributionStats _distributionStats;
        private TMP_FontAsset _graphFont;
        private enum PendingButton { None, Close, Settings }
        private PendingButton _pendingButton = PendingButton.None;
        
        private static bool DiagnoseInput
        {
            get
            {
                Settings settings = ModContext.Settings;
                return settings != null && settings.Diag_InputTrace;
            }
        }

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
            LogRoundedRect.SyncFromSettings();

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
            if (drawer != null) drawer.AxisMetricsChanged = window.OnGraphAxisMetricsChanged;
            if (drawer != null) drawer.PlayerFilterChanged = window.OnPlayerFilterChanged;
            window._graphContainerRect = graphContainerRect;
            window._plotAreaRect = plotAreaRect;
            window._graphRect = graphRect;

            GameObject distributionGo = new GameObject(DistributionAreaObjectName);
            distributionGo.transform.SetParent(graphContainerGo.transform, false);
            RectTransform distributionRect = distributionGo.GetComponent<RectTransform>();
            if (distributionRect == null) distributionRect = distributionGo.AddComponent<RectTransform>();
            distributionRect.anchorMin = Vector2.zero;
            distributionRect.anchorMax = Vector2.one;
            distributionRect.pivot = Vector2.zero;
            distributionRect.offsetMin = Vector2.zero;
            distributionRect.offsetMax = Vector2.zero;

            LogDistributionDrawer distributionDrawer = distributionGo.AddComponent<LogDistributionDrawer>();
            window._distributionDrawer = distributionDrawer;
            window._distributionRect = distributionRect;

            RectTransform xaccRect;
            GameObject xaccGo = CreateChartArea(graphContainerGo.transform, XaccAreaObjectName,
                out xaccRect);
            LogXaccDrawer xaccDrawer = xaccGo.AddComponent<LogXaccDrawer>();
            window._xaccDrawer = xaccDrawer;
            window._xaccRect = xaccRect;

            GameObject deltaGo = CreateChartArea(graphContainerGo.transform, DeltaAreaObjectName,
                out window._deltaRect);
            window._deltaDrawer = deltaGo.AddComponent<LogDistributionDrawer>();

            GameObject rollingGo = CreateChartArea(graphContainerGo.transform, RollingAreaObjectName,
                out window._rollingRect);
            window._rollingDrawer = rollingGo.AddComponent<LogRollingDrawer>();
            window._rollingDrawer.WindowSizeChanged = window.OnRollingWindowChanged;

            GameObject returnGo = CreateChartArea(graphContainerGo.transform, ReturnAreaObjectName,
                out window._returnRect);
            window._returnDrawer = returnGo.AddComponent<LogReturnDrawer>();

            GameObject comboGo = CreateChartArea(graphContainerGo.transform, ComboAreaObjectName,
                out window._comboRect);
            window._comboDrawer = comboGo.AddComponent<LogComboDrawer>();

            GameObject acfGo = CreateChartArea(graphContainerGo.transform, AcfAreaObjectName,
                out window._acfRect);
            window._acfDrawer = acfGo.AddComponent<LogAcfDrawer>();

            GameObject overviewGo = CreateChartArea(graphContainerGo.transform, OverviewAreaObjectName,
                out window._overviewRect);
            window._overviewDrawer = overviewGo.AddComponent<LogOverviewDrawer>();

            TMP_FontAsset graphFont = null;
            if (drawer != null)
            {
                drawer.LoadLog(data);
                graphFont = drawer.GraphFontAsset;
            }

            window._graphFont = graphFont;
            if (window._distributionDrawer != null) window._distributionDrawer.Initialize(graphFont);
            if (window._xaccDrawer != null) window._xaccDrawer.Initialize(graphFont);
            if (window._deltaDrawer != null) window._deltaDrawer.Initialize(graphFont);
            if (window._rollingDrawer != null) window._rollingDrawer.Initialize(graphFont);
            if (window._returnDrawer != null) window._returnDrawer.Initialize(graphFont);
            if (window._comboDrawer != null) window._comboDrawer.Initialize(graphFont);
            if (window._acfDrawer != null) window._acfDrawer.Initialize(graphFont);
            if (window._overviewDrawer != null) window._overviewDrawer.Initialize(graphFont);

            window._viewAreas = new RectTransform[ViewCount];
            window._viewAreas[(int)GraphView.Scatter] = plotAreaRect;
            window._viewAreas[(int)GraphView.Distribution] = distributionRect;
            window._viewAreas[(int)GraphView.Xacc] = xaccRect;
            window._viewAreas[(int)GraphView.Delta] = window._deltaRect;
            window._viewAreas[(int)GraphView.Rolling] = window._rollingRect;
            window._viewAreas[(int)GraphView.ReturnMap] = window._returnRect;
            window._viewAreas[(int)GraphView.Combo] = window._comboRect;
            window._viewAreas[(int)GraphView.Acf] = window._acfRect;
            window._viewAreas[(int)GraphView.Overview] = window._overviewRect;

            for (int i = 0; i < window._viewAreas.Length; i++)
            {
                if (window._viewAreas[i] != null)
                    window._viewAreas[i].gameObject.SetActive(i == (int)window._view);
            }

            window.ApplyHeaderFonts(graphFont);

            window._data = data;
            window.CreateStatsTexts(windowGo.transform, graphFont);
            window.RefreshStats();
            window._settingsUI = GraphSettingsUI.Create(windowRect, window, graphFont);
            window.CreateChartUi(graphFont);
            window.ApplyLayout(size, position);
            window._pendingTextRefresh = true;
        }

        private static GameObject CreateChartArea(Transform parent, string name, out RectTransform rect)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
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

            if (_pendingTextRefresh)
            {
                _pendingTextRefresh = false;
                RefreshAllTextMeshes();
            }
            
            if ((_chartMenuOpen || _playerMenuOpen) && Input.GetKeyDown(KeyCode.Escape))
                CloseMenus();

            SyncSettingsInteraction();
            UpdateBoundsDiagnostics();
            if (!_dragging) RefreshButtonHover(Input.mousePosition);
            UpdateInputFallback();
        }

        private void RefreshAllTextMeshes()
        {
            if (_root == null) return;
            Canvas.ForceUpdateCanvases();

            TMP_Text[] texts = _root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null) texts[i].ForceMeshUpdate();
            }
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

            if (ModContext.Settings == null || !ModContext.Settings.Diag_TrackMeshBounds)
                GraphDrawerBase.TrackMeshBounds = false;
        }


    }
}
