using TMPro;
using UnityEngine;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private enum GraphView
        {
            Scatter = 0,
            Distribution = 1,
            Xacc = 2,
            Delta = 3,
            Rolling = 4,
            ReturnMap = 5,
            Combo = 6,
            Acf = 7,
            Overview = 8
        }

        private const int ViewCount = 9;

        private static readonly string[] ViewLabelKeys =
        {
            "LogGraph_ViewScatter",
            "LogGraph_ViewDistribution",
            "LogGraph_ViewXacc",
            "LogGraph_ViewDelta",
            "LogGraph_ViewRolling",
            "LogGraph_ViewReturn",
            "LogGraph_ViewCombo",
            "LogGraph_ViewAcf",
            "LogGraph_ViewOverview"
        };

        private GraphView _view = GraphView.Overview;
        private RectTransform[] _viewAreas;

        private LogXaccDrawer _xaccDrawer;
        private RectTransform _xaccRect;
        private XaccData _xaccData;

        private LogDistributionDrawer _deltaDrawer;
        private RectTransform _deltaRect;
        private DeltaData _deltaData;

        private LogRollingDrawer _rollingDrawer;
        private RectTransform _rollingRect;
        private RollingData _rollingData;
        private int _rollingDataWindow = -1;

        private LogReturnDrawer _returnDrawer;
        private RectTransform _returnRect;
        private ReturnData _returnData;

        private LogComboDrawer _comboDrawer;
        private RectTransform _comboRect;
        private ComboData _comboData;

        private LogAcfDrawer _acfDrawer;
        private RectTransform _acfRect;
        private AcfData _acfData;

        private LogOverviewDrawer _overviewDrawer;
        private RectTransform _overviewRect;
        private OverviewData _overviewData;

        private LogPillButton _chartButton;
        private LogPillButton _playerButton;
        private LogChartMenu _chartMenu;
        private LogChartMenu _playerMenu;
        private readonly string[] _chartMenuItems = new string[ViewCount];
        private readonly string[] _playerMenuItems = new string[Players.MaxPlayers];
        private bool _chartMenuOpen;
        private bool _playerMenuOpen;
        private bool _ignoreNextClick;

        private int EffectivePlayerId
        {
            get
            {
                int playerId = _drawer != null ? _drawer.SelectedPlayerId : -1;
                return playerId >= 0 ? playerId : 0;
            }
        }

        private void SetView(GraphView view)
        {
            CloseMenus();
            if (_view == view) return;
            _view = view;

            if (_viewAreas != null)
            {
                for (int i = 0; i < _viewAreas.Length; i++)
                {
                    if (_viewAreas[i] != null)
                        _viewAreas[i].gameObject.SetActive(i == (int)view);
                }
            }

            if (view != GraphView.Scatter && _graphPanActive)
            {
                _graphPanActive = false;
                _graphPanRunning = false;
                if (_drawer != null) _drawer.EndPan();
            }

            PushViewData();
            RefreshStats();
            UpdateChartButtonLabel();
            SyncSettingsInteraction();

            if (_windowRect != null) RefreshButtonHover(Input.mousePosition);
        }

        private void PushViewData()
        {
            bool isAngle = _data != null && _data.IsAngle;

            switch (_view)
            {
                case GraphView.Distribution:
                    if (_distributionDrawer != null)
                        _distributionDrawer.SetData(_distributionStats, isAngle);
                    break;
                case GraphView.Xacc:
                    PushXaccData();
                    break;
                case GraphView.Delta:
                    EnsureDeltaData();
                    if (_deltaDrawer != null)
                        _deltaDrawer.SetDeltaData(_deltaData != null ? _deltaData.Histogram : null, isAngle);
                    break;
                case GraphView.Rolling:
                    EnsureRollingData();
                    if (_rollingDrawer != null)
                        _rollingDrawer.SetData(_rollingData, isAngle);
                    break;
                case GraphView.ReturnMap:
                    EnsureReturnData();
                    if (_returnDrawer != null)
                        _returnDrawer.SetData(_returnData, isAngle);
                    break;
                case GraphView.Combo:
                    EnsureComboData();
                    if (_comboDrawer != null)
                        _comboDrawer.SetData(_comboData);
                    break;
                case GraphView.Acf:
                    EnsureAcfData();
                    if (_acfDrawer != null)
                        _acfDrawer.SetData(_acfData);
                    break;
                case GraphView.Overview:
                    EnsureOverviewData();
                    if (_overviewDrawer != null)
                        _overviewDrawer.SetData(_overviewData, _data);
                    break;
            }
        }

        private void ClearChartCaches()
        {
            _deltaData = null;
            _rollingData = null;
            _rollingDataWindow = -1;
            _returnData = null;
            _comboData = null;
            _acfData = null;
            _overviewData = null;
        }

        private void EnsureXaccData()
        {
            if (_xaccData != null) return;

            int players = _playerCount > 0 ? _playerCount : 1;
            _xaccData = XaccData.Compute(_data != null ? _data.Samples : null, players);
        }

        private void PushXaccData()
        {
            if (_xaccDrawer == null) return;

            EnsureXaccData();
            _xaccDrawer.SetData(_xaccData, -1);
        }

        private void EnsureDeltaData()
        {
            if (_deltaData != null) return;
            _deltaData = DeltaData.Compute(_data != null ? _data.Samples : null, EffectivePlayerId);
        }

        private void EnsureRollingData()
        {
            int window = _rollingDrawer != null ? _rollingDrawer.WindowSize : 50;
            if (_rollingData != null && _rollingDataWindow == window) return;

            _rollingData = RollingData.Compute(
                _data != null ? _data.Samples : null, EffectivePlayerId, window);
            _rollingDataWindow = window;
        }

        private void EnsureReturnData()
        {
            if (_returnData != null) return;
            _returnData = ReturnData.Compute(_data != null ? _data.Samples : null, EffectivePlayerId);
        }

        private void EnsureComboData()
        {
            if (_comboData != null) return;
            _comboData = ComboData.Compute(_data != null ? _data.Samples : null, EffectivePlayerId);
        }

        private void EnsureAcfData()
        {
            if (_acfData != null) return;
            _acfData = AcfData.Compute(_data != null ? _data.Samples : null, EffectivePlayerId);
        }

        private void EnsureOverviewData()
        {
            if (_overviewData != null) return;
            _overviewData = OverviewData.Compute(
                _data != null ? _data.Samples : null,
                EffectivePlayerId,
                _data != null && _data.IsAngle);
        }

        private void OnRollingWindowChanged(int size)
        {
            _rollingData = null;
            _rollingDataWindow = -1;
            PushViewData();
            RefreshStats();
        }

        private void CreateChartUi(TMP_FontAsset font)
        {
            for (int i = 0; i < ViewCount; i++) _chartMenuItems[i] = i18n.T(ViewLabelKeys[i]);

            GameObject chartButtonGo = new GameObject("ChartSelector", typeof(RectTransform));
            chartButtonGo.transform.SetParent(_windowRect, false);
            _chartButton = chartButtonGo.AddComponent<LogPillButton>();
            _chartButton.Initialize(font, ToggleChartMenu);

            GameObject playerButtonGo = new GameObject("PlayerSelector", typeof(RectTransform));
            playerButtonGo.transform.SetParent(_windowRect, false);
            _playerButton = playerButtonGo.AddComponent<LogPillButton>();
            _playerButton.Initialize(font, TogglePlayerMenu);

            _chartMenu = CreateMenu("ChartMenu", font, OnChartMenuItemSelected);
            _playerMenu = CreateMenu("PlayerMenu", font, OnPlayerMenuItemSelected);
            _chartMenu.gameObject.SetActive(false);
            _playerMenu.gameObject.SetActive(false);

            UpdateChartButtonLabel();
            UpdatePlayerButtonLabel();
        }

        private LogChartMenu CreateMenu(string name, TMP_FontAsset font, System.Action<int> onSelect)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_windowRect, false);
            LogChartMenu menu = go.AddComponent<LogChartMenu>();
            menu.Initialize(font, onSelect);
            return menu;
        }

        private void ToggleChartMenu()
        {
            if (_chartMenuOpen)
            {
                CloseMenus();
                return;
            }

            ClosePlayerMenu();
            _chartMenuOpen = true;
            _chartMenu.SetItems(_chartMenuItems, (int)_view);
            PositionSelectorAndMenus(_windowRect != null ? _windowRect.sizeDelta : Vector2.zero);
            _chartMenu.gameObject.SetActive(true);
            _chartButton.SetActiveState(true);
        }

        private void TogglePlayerMenu()
        {
            if (_playerMenuOpen)
            {
                CloseMenus();
                return;
            }

            CloseChartMenu();
            int count = Mathf.Clamp(_playerCount, 0, Players.MaxPlayers);
            for (int i = 0; i < count; i++) _playerMenuItems[i] = Coop.Label(i);
            _playerMenuOpen = true;
            _playerMenu.SetItems(_playerMenuItems, count, _drawer != null ? _drawer.SelectedPlayerId : -1);
            PositionSelectorAndMenus(_windowRect != null ? _windowRect.sizeDelta : Vector2.zero);
            _playerMenu.gameObject.SetActive(true);
            _playerButton.SetActiveState(true);
        }

        private void CloseChartMenu()
        {
            if (!_chartMenuOpen) return;
            _chartMenuOpen = false;
            if (_chartMenu != null) _chartMenu.gameObject.SetActive(false);
            if (_chartButton != null) _chartButton.SetActiveState(false);
        }

        private void ClosePlayerMenu()
        {
            if (!_playerMenuOpen) return;
            _playerMenuOpen = false;
            if (_playerMenu != null) _playerMenu.gameObject.SetActive(false);
            if (_playerButton != null) _playerButton.SetActiveState(false);
        }

        private void CloseMenus()
        {
            CloseChartMenu();
            ClosePlayerMenu();
        }

        private void OnChartMenuItemSelected(int index)
        {
            CloseMenus();
            SetView((GraphView)index);
        }

        private void OnPlayerMenuItemSelected(int index)
        {
            CloseMenus();
            if (_drawer != null) _drawer.SetPlayerFilter(index);
            UpdatePlayerButtonLabel();
        }

        private void UpdateChartButtonLabel()
        {
            if (_chartButton == null) return;
            _chartButton.SetLabel(i18n.T(ViewLabelKeys[(int)_view]) + " ▾");
        }

        private void UpdatePlayerButtonLabel()
        {
            if (_playerButton == null) return;
            int playerId = _drawer != null ? _drawer.SelectedPlayerId : -1;
            _playerButton.SetLabel((playerId >= 0 ? Coop.Label(playerId) : i18n.T("LogGraph_PlayerAll")) + " ▾");
        }

        private void PositionSelectorAndMenus(Vector2 size)
        {
            Rect chartRect;
            Rect playerRect;
            bool hasPlayer = _playerCount > 1;
            SelectorRects(size, hasPlayer, out chartRect, out playerRect);

            PlacePillButton(_chartButton, chartRect);
            if (_playerButton != null)
            {
                bool active = hasPlayer && playerRect.width > 1f;
                if (_playerButton.gameObject.activeSelf != active) _playerButton.gameObject.SetActive(active);
                if (active) PlacePillButton(_playerButton, playerRect);
            }

            if (_chartMenuOpen && _chartMenu != null)
            {
                float width = Mathf.Max(chartRect.width + 50f * UiScale, 190f * UiScale);
                _chartMenu.SetLayout(width, 20f * UiScale, 4f * UiScale);
                PlaceMenu(_chartMenu, chartRect, width, size);
            }

            if (_playerMenuOpen && _playerMenu != null)
            {
                float width = Mathf.Max(playerRect.width + 40f * UiScale, 90f * UiScale);
                _playerMenu.SetLayout(width, 20f * UiScale, 4f * UiScale);
                PlaceMenu(_playerMenu, playerRect, width, size);
            }
        }

        private static void PlacePillButton(LogPillButton button, Rect rect)
        {
            if (button == null) return;

            RectTransform transform = button.rectTransform;
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.zero;
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = new Vector2(rect.x, rect.y);
            transform.sizeDelta = new Vector2(rect.width, rect.height);
            button.SetFontSize(Mathf.Clamp(11f * UiScale, 8f, 28f));
        }

        private void PlaceMenu(LogChartMenu menu, Rect anchor, float width, Vector2 size)
        {
            if (menu == null) return;

            float padding = 4f * UiScale;
            float height = menu.MenuHeight;
            float x = Mathf.Clamp(anchor.xMin, padding, Mathf.Max(padding, size.x - width - padding));
            float y = Mathf.Max(padding, size.y - HeaderHeight - padding - height);

            RectTransform transform = menu.rectTransform;
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.zero;
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = new Vector2(x, y);
            transform.sizeDelta = new Vector2(width, height);
        }
    }
}
