using System.Collections.Generic;
using System.Globalization;
using TMPro;
using TimingShow.HUD;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.LevelEditor
{
    internal sealed class HitOverlayPanel
    {
        private const float SliderTravelHeight = 4f;
        private const float SliderHitHeight = 14f;
        private const float ResizeGripSize = 14f;
        private const float MinPanelScale = 0.75f;
        private const float MaxPanelScale = 2.5f;
        private const float ScaleDragPixels = 300f;
        private const float DefaultPanelWidth = HitOverlaySkin.PanelWidth;
        private const float MinPanelWidth = 240f;
        private const float MaxPanelWidth = 460f;
        private const int MaxResultRows = 6;

        private readonly EditorHitMarkerController _owner;
        private readonly HitJudgeChip[] _chips;
        private readonly List<int> _results = new List<int>(MaxResultRows);

        private GameObject _canvasObject;
        private HitOverlayRenderer _renderer;
        private Image _blocker;

        private Settings _settings;
        private HitOverlayFilter _filter = HitOverlayFilter.CreateDefault();
        private HitOverlayQuery _query;
        private bool[] _searchMask;
        private bool _onlyMatches = true;
        private int _pinnedIndex = -1;

        private string _searchText = string.Empty;
        private bool _searchFocused;
        private bool _searchDirty;
        private int _resultCount;

        private bool _openFilters = true;
        private bool _openDisplay = false;
        private bool _openSearch = true;
        private bool _collapsed;
        private EditorHitSnapshotData _snapshotRef;
        private bool _preparingRef;

        private bool _inputPass = true;
        private float _scale = 1f;
        private float _width = DefaultPanelWidth;
        private float _originX;
        private float _originTop;
        private float _y;
        private float _contentHeight;
        private Rect _rect;

        private Vector2 _mouse;
        private bool _down;
        private bool _up;
        private bool _held;
        private bool _consumedDown;
        private string _dragSlider;
        private bool _draggingPanel;
        private Vector2 _dragGrab;
        private bool _positionDirty;
        private bool _scalingPanel;
        private float _scaleStartValue;
        private float _scaleStartMouseX;

        internal HitOverlayPanel(EditorHitMarkerController owner)
        {
            _owner = owner;
            _chips = HitOverlayCatalog.BuildChips();
        }

        internal bool Visible
        {
            get { return _canvasObject != null && _canvasObject.activeSelf && _settings != null && _settings.EditorHits_PanelVisible; }
        }

        internal Rect ScreenRect
        {
            get { return _rect; }
        }

        internal HitOverlayFilter Filter
        {
            get { return _filter; }
        }

        internal bool[] SearchMask
        {
            get { return _onlyMatches ? _searchMask : null; }
        }

        internal int PinnedIndex
        {
            get { return _pinnedIndex; }
        }

        internal bool ContainsMouse(Vector2 screenPoint)
        {
            return Visible && _rect.width > 0f && _rect.Contains(screenPoint);
        }

        internal void Tick()
        {
            InputGate.BeginBypass();
            try
            {
                TickCore();
            }
            finally
            {
                InputGate.EndBypass();
            }
        }

        private void TickCore()
        {
            Settings settings = ModContext.Settings;
            if (settings == null)
            {
                InputGate.Clear();
                SetShown(false);
                return;
            }

            _settings = settings;

            if (PanelKeyPressed(settings)) settings.EditorHits_PanelVisible = !settings.EditorHits_PanelVisible;

            if (!settings.EditorHits_PanelVisible)
            {
                _draggingPanel = false;
                _scalingPanel = false;
                _searchFocused = false;
                InputGate.Clear();
                SetShown(false);
                return;
            }

            EnsureUi();
            if (_renderer == null)
            {
                InputGate.Clear();
                return;
            }

            SetShown(true);
            UpdateMouse();

            UpdateWidth(settings);
            UpdateScale(settings);
            ResolveOrigin();
            ResolveSearch();

            _inputPass = true;
            _consumedDown = false;
            _y = 0f;
            Layout();
            float height = _y;
            _contentHeight = height;
            _rect = new Rect(_originX, _originTop - height * _scale, _width * _scale, height * _scale);
            UpdateScaleGrip();

            _inputPass = false;
            _renderer.Begin();
            _renderer.Fill(_rect, HitOverlaySkin.Panel);
            _renderer.Outline(_rect, HitOverlaySkin.Border);
            _y = 0f;
            Layout();
            DrawScaleGrip();
            _renderer.End();

            UpdateBlocker();
            ApplyToOwner();
            PersistPositionIfNeeded();
            InputGate.SetArea(settings.EditorHits_PanelVisible, _rect, _searchFocused || _draggingPanel || _scalingPanel);
        }

        internal void Dispose()
        {
            InputGate.Clear();
            if (_canvasObject != null) Object.Destroy(_canvasObject);

            _canvasObject = null;
            _renderer = null;
            _blocker = null;
            _rect = new Rect(0f, 0f, 0f, 0f);
        }

        private void SetShown(bool shown)
        {
            if (_canvasObject != null && _canvasObject.activeSelf != shown) _canvasObject.SetActive(shown);
        }

        private void UpdateScale(Settings settings)
        {
            if (_scalingPanel)
            {
                float delta = (_mouse.x - _scaleStartMouseX) / ScaleDragPixels;
                settings.EditorHits_PanelScale = Mathf.Clamp(_scaleStartValue + delta, MinPanelScale, MaxPanelScale);
            }

            _scale = Mathf.Clamp(settings.EditorHits_PanelScale, MinPanelScale, FitScaleLimit());
            if (_scale != settings.EditorHits_PanelScale) settings.EditorHits_PanelScale = _scale;
        }

        private float FitScaleLimit()
        {
            float widthLimit = Screen.width > 0f ? (Screen.width - 24f) / _width : MaxPanelScale;
            float heightLimit = _contentHeight > 1f && Screen.height > 0f ? (Screen.height - 24f) / _contentHeight : MaxPanelScale;
            return Mathf.Clamp(Mathf.Min(MaxPanelScale, Mathf.Min(widthLimit, heightLimit)), MinPanelScale, MaxPanelScale);
        }

        private void UpdateWidth(Settings settings)
        {
            _width = Mathf.Clamp(settings.EditorHits_PanelWidth, MinPanelWidth, MaxPanelWidth);
        }

        private void UpdateScaleGrip()
        {
            if (!_inputPass) return;

            Rect grip = GripRect();
            if (_down && !_consumedDown && grip.Contains(_mouse))
            {
                _consumedDown = true;
                _scalingPanel = true;
                _scaleStartValue = _scale;
                _scaleStartMouseX = _mouse.x;
            }

            if (_scalingPanel && !_held)
            {
                _scalingPanel = false;
                ModEntry.SaveSettings();
            }
        }

        private Rect GripRect()
        {
            float size = ResizeGripSize * _scale;
            return new Rect(_rect.xMax - size, _rect.yMin, size, size);
        }

        private void DrawScaleGrip()
        {
            Rect grip = GripRect();
            bool active = _scalingPanel || Hovered(grip);
            Color color = active ? HitOverlaySkin.TextPrimary : HitOverlaySkin.TextDisabled;
            for (int i = 0; i < 3; i++)
            {
                float length = (4f + i * 3f) * _scale;
                float bottom = (3f + i * 3f) * _scale;
                Fill(new Rect(grip.xMax - 3f * _scale - length, grip.yMin + bottom, length, 1f), color);
            }
        }

        private void EnsureUi()
        {
            if (_canvasObject != null) return;

            _canvasObject = new GameObject("TimingShow_EditorHitPanel", typeof(RectTransform));
            if (_owner != null) _canvasObject.transform.SetParent(_owner.transform, false);

            Canvas canvas = _canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32001;
            _canvasObject.AddComponent<GraphicRaycaster>();

            GameObject rendererObject = new GameObject("Surface", typeof(RectTransform));
            rendererObject.transform.SetParent(_canvasObject.transform, false);
            _renderer = rendererObject.AddComponent<HitOverlayRenderer>();
            _renderer.Initialize(LogGraphFont.Resolve());

            GameObject blockerObject = new GameObject("Blocker", typeof(RectTransform));
            blockerObject.transform.SetParent(_canvasObject.transform, false);
            RectTransform blockerRect = (RectTransform)blockerObject.transform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.zero;
            blockerRect.pivot = Vector2.zero;
            _blocker = blockerObject.AddComponent<Image>();
            _blocker.color = new Color(0f, 0f, 0f, 0f);
            _blocker.raycastTarget = true;
            blockerObject.transform.SetAsFirstSibling();
        }

        private void UpdateMouse()
        {
            _mouse = Input.mousePosition;
            _down = Input.GetMouseButtonDown(0);
            _up = Input.GetMouseButtonUp(0);
            _held = Input.GetMouseButton(0);
        }

        private void ResolveOrigin()
        {
            float scaleWidth = _width * _scale;
            float x = _settings.EditorHits_PanelX >= 0f ? _settings.EditorHits_PanelX * Screen.width : 12f;
            float top = _settings.EditorHits_PanelY >= 0f ? _settings.EditorHits_PanelY * Screen.height : Screen.height - 12f;

            if (_draggingPanel)
            {
                x = _mouse.x - _dragGrab.x;
                top = _mouse.y - _dragGrab.y;
            }

            float minTop = Mathf.Min(_rect.height, Screen.height - 40f);
            float maxTop = Mathf.Max(minTop, Screen.height - 12f);

            _originX = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - scaleWidth));
            _originTop = Mathf.Clamp(top, minTop, maxTop);
        }

        private void PersistPositionIfNeeded()
        {
            if (!_positionDirty || _settings == null) return;

            _positionDirty = false;
            _settings.EditorHits_PanelX = Screen.width > 0 ? _originX / Screen.width : 0f;
            _settings.EditorHits_PanelY = Screen.height > 0 ? _originTop / Screen.height : 0f;
        }

        private void UpdateBlocker()
        {
            if (_blocker == null) return;

            RectTransform rect = (RectTransform)_blocker.transform;
            rect.anchoredPosition = new Vector2(_rect.xMin, _rect.yMin);
            rect.sizeDelta = new Vector2(_rect.width, _rect.height);
            _blocker.raycastTarget = _rect.width > 0f && _rect.height > 0f;
        }

        private void ApplyToOwner()
        {
            if (_owner == null) return;

            _owner.ApplyPanelState(_filter, _searchMask, _onlyMatches, _pinnedIndex, _query.IsEmpty ? 0 : 1);
        }

        private void ResolveSearch()
        {
            if (_owner != null && !ReferenceEquals(_owner.Snapshot, _snapshotRef))
            {
                _snapshotRef = _owner.Snapshot;
                _searchDirty = true;
            }

            bool preparing = _owner != null && _owner.IsPreparing;
            if (preparing != _preparingRef)
            {
                _preparingRef = preparing;
                _searchDirty = true;
            }

            if (!_searchDirty) return;

            _searchDirty = false;
            _query = HitOverlayQuery.Parse(_searchText);

            if (_owner == null || _owner.Snapshot == null)
            {
                _searchMask = null;
                _results.Clear();
                _resultCount = 0;
                return;
            }

            _searchMask = HitOverlaySearch.BuildMask(_owner.Snapshot.Samples, _owner.ValidMask, _query);
            _resultCount = HitOverlaySearch.Collect(_owner.Snapshot.Samples, _owner.ValidMask, _query, _results, MaxResultRows);
        }

        private void Layout()
        {
            Header();
            if (!_collapsed)
            {
                if (_owner != null && _owner.Snapshot != null)
                {
                    FiltersSection();
                    DisplaySection();
                    SearchSection();
                }
                else
                {
                    EmptySection();
                }

                Footer();
            }

            _y += HitOverlaySkin.Padding;
        }

        private void EmptySection()
        {
            Label(R(HitOverlaySkin.Padding, _y + 8f, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight), i18n.T("HitPanel_Empty"), HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);
            _y += HitOverlaySkin.RowHeight + 8f;
        }

        private void Header()
        {
            Rect header = R(0f, 0f, _width, HitOverlaySkin.HeaderHeight);
            Fill(header, HitOverlaySkin.Header);
            Fill(R(0f, HitOverlaySkin.HeaderHeight - 1f, _width, 1f), HitOverlaySkin.Separator);

            Label(R(HitOverlaySkin.Padding, 0f, 110f, HitOverlaySkin.HeaderHeight), i18n.T("HitPanel_Title"), HitOverlaySkin.TextPrimary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize);

            int valid = _owner != null ? _owner.ValidCount : 0;
            int shown = _owner != null ? _owner.FilteredCount : 0;
            string count = shown.ToString(CultureInfo.InvariantCulture) + " / " + valid.ToString(CultureInfo.InvariantCulture);
            Label(R(_width - 108f, 0f, 58f, HitOverlaySkin.HeaderHeight), count, HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineRight, HitOverlaySkin.FontSize - 1f);

            Rect collapse = R(_width - 42f, 1f, 18f, 18f);
            Rect close = R(_width - 22f, 1f, 18f, 18f);

            DrawHeaderButton(collapse, HitOverlaySkin.ChipBackground, false);
            DrawCollapseIcon(collapse);
            if (Pressed(HitRect(collapse))) _collapsed = !_collapsed;

            DrawHeaderButton(close, HitOverlaySkin.ChipBackground, true);
            DrawCloseIcon(close);
            if (Pressed(HitRect(close)))
            {
                _draggingPanel = false;
                _scalingPanel = false;
                _settings.EditorHits_PanelVisible = false;
                SetShown(false);
            }

            if (_inputPass)
            {
                bool overButtons = HitRect(collapse).Contains(_mouse) || HitRect(close).Contains(_mouse);
                if (_down && !overButtons && header.Contains(_mouse))
                {
                    _draggingPanel = true;
                    _dragGrab = new Vector2(_mouse.x - _originX, _mouse.y - _originTop);
                }

                if (_draggingPanel && _up)
                {
                    _draggingPanel = false;
                    _positionDirty = true;
                }
            }

            _y = HitOverlaySkin.HeaderHeight;
        }

        private void DrawHeaderButton(Rect rect, Color background, bool danger)
        {
            if (_inputPass) return;

            bool hover = rect.Contains(_mouse);
            Fill(rect, hover ? (danger ? HitOverlaySkin.DangerHover : HitOverlaySkin.RowHover) : background);
            Outline(rect, hover ? HitOverlaySkin.Border : HitOverlaySkin.ChipBorder);
        }

        private void DrawCollapseIcon(Rect rect)
        {
            if (_inputPass) return;

            Vector2 center = rect.center;
            float arm = 4f * _scale;
            float thickness = Mathf.Max(1f, 1.5f * _scale);
            Line(new Vector2(center.x - arm, center.y), new Vector2(center.x + arm, center.y), thickness, HitOverlaySkin.TextPrimary);
            if (_collapsed) Line(new Vector2(center.x, center.y - arm), new Vector2(center.x, center.y + arm), thickness, HitOverlaySkin.TextPrimary);
        }

        private void DrawCloseIcon(Rect rect)
        {
            if (_inputPass) return;

            Vector2 center = rect.center;
            float arm = 3.5f * _scale;
            float thickness = Mathf.Max(1f, 1.5f * _scale);
            Line(new Vector2(center.x - arm, center.y - arm), new Vector2(center.x + arm, center.y + arm), thickness, HitOverlaySkin.TextPrimary);
            Line(new Vector2(center.x - arm, center.y + arm), new Vector2(center.x + arm, center.y - arm), thickness, HitOverlaySkin.TextPrimary);
        }

        private Rect HitRect(Rect rect)
        {
            float grow = 2f * _scale;
            return new Rect(rect.xMin - grow, rect.yMin - grow, rect.width + grow * 2f, rect.height + grow * 2f);
        }

        private void FiltersSection()
        {
            if (!SectionHeader("HitPanel_Filters", ref _openFilters)) return;

            ChipFlow();
            _y += 4f;

            SliderRow("HitPanel_OffsetMin", ref _filter.MinAbsOffset, 0f, 200f, "0", false);
            SliderRow("HitPanel_OffsetMax", ref _filter.MaxAbsOffset, 0f, 200f, "0", true);

            if (_filter.MaxAbsOffset < _filter.MinAbsOffset) _filter.MaxAbsOffset = _filter.MinAbsOffset;

            PlayerChips();

            float width = (_width - HitOverlaySkin.Padding * 2f - HitOverlaySkin.ChipGap * 2f) / 3f;
            PresetButton("HitPanel_PresetPerfect", HitOverlaySkin.Padding, width, PresetPerfect);
            PresetButton("HitPanel_PresetXp", HitOverlaySkin.Padding + width + HitOverlaySkin.ChipGap, width, PresetXp);
            PresetButton("HitPanel_PresetFail", HitOverlaySkin.Padding + (width + HitOverlaySkin.ChipGap) * 2f, width, PresetFail);
            _y += HitOverlaySkin.ChipHeight;

            _y += HitOverlaySkin.GroupGap;
        }

        private void DisplaySection()
        {
            if (!SectionHeader("HitPanel_Display", ref _openDisplay)) return;

            SliderRow("Label_PointSize", ref _settings.EditorHits_MarkerSize, 1f, 100f, "0", false);
            SliderRow("Label_AngleScale", ref _settings.EditorHits_AngleScale, 0f, 3f, "0.0", false);
            SliderRow("HitPanel_Opacity", ref _settings.EditorHits_MarkerOpacity, 0.1f, 1f, "0.00", false);

            SegmentRow("HitPanel_Color", ref _settings.EditorHits_ColorMode, new[] { "HitPanel_ColorJudge", "HitPanel_ColorOffset", "HitPanel_ColorPlayer" });
            SegmentRow("HitPanel_Shape", ref _settings.EditorHits_MarkerShape, new[] { "HitPanel_ShapeRing", "HitPanel_ShapeDot", "HitPanel_ShapeCross" });

            CheckRow("Toggle_EditorHitsTooltip", ref _settings.EditorHits_ShowTooltip);
            CheckRow("HitPanel_DimOthers", ref _settings.EditorHits_DimUnmatched);

            _y += HitOverlaySkin.GroupGap;
        }

        private void SearchSection()
        {
            if (!SectionHeader("HitPanel_Search", ref _openSearch)) return;

            Rect field = R(HitOverlaySkin.Padding, _y + 2f, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight);
            Fill(field, HitOverlaySkin.FieldBackground);
            Outline(field, _searchFocused ? HitOverlaySkin.Accent : HitOverlaySkin.Border);

            Rect textRect = new Rect(field.xMin + 5f * _scale, field.yMin, field.width - 10f * _scale, field.height);
            string display = string.IsNullOrEmpty(_searchText) ? i18n.T("HitPanel_SearchHint") : _searchText;
            Label(textRect, display, string.IsNullOrEmpty(_searchText) ? HitOverlaySkin.TextDisabled : HitOverlaySkin.TextPrimary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 0.5f);

            if (_searchFocused && Blink())
            {
                float caretX = textRect.xMin + HitOverlaySkin.EstimateTextWidth(_searchText, (HitOverlaySkin.FontSize - 0.5f) * _scale);
                Fill(new Rect(caretX, field.yMin + 3f * _scale, 1f, field.height - 6f * _scale), HitOverlaySkin.TextPrimary);
            }

            if (Pressed(field)) _searchFocused = true;
            if (_inputPass && _searchFocused) ReadInput();
            if (_inputPass && _down && !field.Contains(_mouse)) _searchFocused = false;

            _y += HitOverlaySkin.RowHeight + 4f;

            CheckRow("HitPanel_OnlyMatches", ref _onlyMatches, true);

            for (int i = 0; i < _results.Count; i++)
            {
                int index = _results[i];
                TimingScatterSample sample = _owner.Snapshot.Samples[index];
                Rect row = R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight - 2f);

                if (index == _pinnedIndex) Fill(row, HitOverlaySkin.Accent);
                else if (Hovered(row)) Fill(row, HitOverlaySkin.RowHover);

                string text = DescribeSample(sample);
                Label(new Rect(row.xMin + 4f * _scale, row.yMin, row.width - 8f * _scale, row.height), text, HitOverlaySkin.TextPrimary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);

                if (Pressed(row)) _pinnedIndex = index;
                _y += HitOverlaySkin.RowHeight - 2f;
            }

            if (_resultCount == 0 && !_query.IsEmpty)
            {
                Label(R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight), i18n.T("HitPanel_NoResults"), HitOverlaySkin.TextDisabled, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);
                _y += HitOverlaySkin.RowHeight;
            }
            else if (_resultCount > 0)
            {
                Label(R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight), i18n.T("HitPanel_Matches") + " " + _resultCount.ToString(CultureInfo.InvariantCulture), HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);
                _y += HitOverlaySkin.RowHeight;
            }

            if (_pinnedIndex >= 0 && _owner != null && _owner.Snapshot != null && _pinnedIndex < _owner.Snapshot.Samples.Length)
                PinnedDetails(_owner.Snapshot.Samples[_pinnedIndex]);

            _y += HitOverlaySkin.GroupGap;
        }

        private void PinnedDetails(TimingScatterSample sample)
        {
            Fill(R(0f, _y, _width, 1f), HitOverlaySkin.Separator);
            _y += 4f;

            string floor = sample.HasFloorId
                ? (sample.FloorIdEstimated ? "~#" : "#") + sample.FloorId.ToString(CultureInfo.InvariantCulture)
                : "-";
            DetailRow("Detail_Floor", floor);
            DetailRow("Detail_Timing", sample.TimeMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
            DetailRow("Detail_Player", Coop.Label(sample.PlayerId));
            DetailRow("Detail_Judge", i18n.T(HitOverlayCatalog.LabelKey(sample.Judge, sample.IsXPerfect)));
            DetailRow("Detail_Angle", sample.HasAngle ? sample.AngleDeg.ToString("0.0", CultureInfo.InvariantCulture) + "\u00b0" : "-");
        }

        private void DetailRow(string labelKey, string value)
        {
            Label(R(HitOverlaySkin.Padding, _y, 70f, HitOverlaySkin.RowHeight), i18n.T(labelKey), HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);
            Label(R(HitOverlaySkin.Padding + 70f, _y, _width - HitOverlaySkin.Padding * 2f - 70f, HitOverlaySkin.RowHeight), value, HitOverlaySkin.TextPrimary, TextAlignmentOptions.MidlineRight, HitOverlaySkin.FontSize - 1f);
            _y += HitOverlaySkin.RowHeight - 2f;
        }

        private static bool PanelKeyPressed(Settings settings)
        {
            KeyCode key = settings.EditorHits_PanelKey;
            if (key == KeyCode.None) return false;
            if (TimingShow.Options.Options.HitPanelHotkeySuppressed) return false;
            return Input.GetKeyDown(key);
        }

        private void Footer()
        {
            Fill(R(0f, _y, _width, 1f), HitOverlaySkin.Separator);
            _y += 4f;

            bool hasSnapshot = _owner != null && _owner.Snapshot != null;
            float buttonWidth = (_width - HitOverlaySkin.Padding * 2f - HitOverlaySkin.ChipGap) * 0.5f;
            Rect import = R(HitOverlaySkin.Padding, _y, buttonWidth, HitOverlaySkin.ChipHeight);
            Rect clear = R(HitOverlaySkin.Padding + buttonWidth + HitOverlaySkin.ChipGap, _y, buttonWidth, HitOverlaySkin.ChipHeight);

            if (Button(import, i18n.T("Btn_ImportHitLog"), false, true) && _owner != null) _owner.RequestImport();
            if (Button(clear, i18n.T("HitPanel_ClearDisplay"), true, hasSnapshot) && _owner != null) _owner.RequestClear();
            _y += HitOverlaySkin.ChipHeight + 4f;

            if (_owner != null && !string.IsNullOrEmpty(_owner.ImportStatus))
            {
                Label(R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight), _owner.ImportStatus, _owner.ImportStatusOk ? HitOverlaySkin.TextSecondary : HitOverlaySkin.TextDanger, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1.5f);
                _y += HitOverlaySkin.RowHeight;
            }

            string hint;
            if (_owner != null && _owner.IsPreparing)
            {
                hint = i18n.T("HitPanel_Preparing");
            }
            else
            {
                KeyCode key = _settings != null ? _settings.EditorHits_PanelKey : KeyCode.None;
                string keyName = key == KeyCode.None ? i18n.T("Status_Disabled") : key.ToString();
                hint = string.Format(CultureInfo.InvariantCulture, i18n.T("HitPanel_Hint"), keyName);
            }
            Label(R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.FooterHeight), hint, HitOverlaySkin.TextDisabled, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1.5f);
            _y += HitOverlaySkin.FooterHeight;
        }

        private bool Button(Rect rect, string label, bool danger, bool enabled)
        {
            bool hover = enabled && Hovered(rect);
            Color background = hover ? (danger ? HitOverlaySkin.DangerHover : HitOverlaySkin.RowHover) : HitOverlaySkin.ChipBackground;
            Fill(rect, background);
            Outline(rect, hover ? background : HitOverlaySkin.ChipBorder);
            Label(rect, label, enabled ? HitOverlaySkin.TextPrimary : HitOverlaySkin.TextDisabled, TextAlignmentOptions.Center, HitOverlaySkin.FontSize - 1f);
            return enabled && Pressed(rect);
        }

        private bool SectionHeader(string key, ref bool open)
        {
            Rect rect = R(0f, _y, _width, HitOverlaySkin.SectionHeight);
            Fill(rect, HitOverlaySkin.Section);

            Rect arrow = R(HitOverlaySkin.Padding, _y + 5f, 6f, 6f);
            Triangle(arrow, HitOverlaySkin.TextSecondary, !open);
            Label(R(HitOverlaySkin.Padding + 10f, _y, _width - 40f, HitOverlaySkin.SectionHeight), i18n.T(key), HitOverlaySkin.TextPrimary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize);

            if (Pressed(rect)) open = !open;

            _y += HitOverlaySkin.SectionHeight;
            return open;
        }

        private void ChipFlow()
        {
            float x = HitOverlaySkin.Padding;
            float y = _y;
            float maxWidth = _width - HitOverlaySkin.Padding;

            Chip(i18n.T("HitPanel_All"), null, !_filter.IsActive, HitOverlaySkin.TextSecondary, ref x, ref y, maxWidth);
            if (Pressed(_lastChip)) PresetAll();

            for (int i = 0; i < _chips.Length; i++)
            {
                HitJudgeChip chip = _chips[i];
                uint bit = chip.IsXp ? 0u : HitOverlayFilter.BitOf(chip.Judge);
                bool active = chip.IsXp ? _filter.XpOnly : (_filter.JudgeMask & bit) != 0u;
                int count = ChipCount(chip);
                Chip(i18n.T(chip.LabelKey), count, active, chip.Color, ref x, ref y, maxWidth);

                if (Pressed(_lastChip))
                {
                    if (chip.IsXp) _filter.XpOnly = !_filter.XpOnly;
                    else _filter.JudgeMask ^= bit;
                    MarkFilterDirty();
                }
            }

            _y = y + HitOverlaySkin.ChipHeight;
        }

        private Rect _lastChip;

        private void Chip(string label, int? count, bool active, Color accent, ref float x, ref float y, float maxWidth)
        {
            string text = count.HasValue ? label + " " + count.Value.ToString(CultureInfo.InvariantCulture) : label;
            float width = HitOverlaySkin.EstimateTextWidth(text, HitOverlaySkin.FontSize - 1f) + HitOverlaySkin.ChipPaddingX * 2f;

            if (x + width > maxWidth && x > HitOverlaySkin.Padding)
            {
                x = HitOverlaySkin.Padding;
                y += HitOverlaySkin.ChipHeight + HitOverlaySkin.ChipGap;
            }

            Rect rect = R(x, y, width, HitOverlaySkin.ChipHeight);
            _lastChip = rect;

            Fill(rect, active ? HitOverlaySkin.Accent : HitOverlaySkin.ChipBackground);
            Outline(rect, active ? HitOverlaySkin.Accent : HitOverlaySkin.ChipBorder);
            Label(new Rect(rect.xMin, rect.yMin, rect.width, rect.height), text, active ? HitOverlaySkin.TextPrimary : accent, TextAlignmentOptions.Center, HitOverlaySkin.FontSize - 1f);

            x += width + HitOverlaySkin.ChipGap;
        }

        private int ChipCount(HitJudgeChip chip)
        {
            if (_owner == null) return 0;

            int[] counts = _owner.JudgeCounts;
            if (counts == null) return 0;

            if (chip.IsXp) return _owner.XPerfectCount;

            int index = (int)chip.Judge + 1;
            return index >= 0 && index < counts.Length ? counts[index] : 0;
        }

        private void PlayerChips()
        {
            if (_owner == null || _owner.PlayerCount <= 1) return;

            float x = HitOverlaySkin.Padding;
            float y = _y;
            float maxWidth = _width - HitOverlaySkin.Padding;
            int count = Mathf.Min(8, _owner.PlayerCount);

            for (int i = 0; i < count; i++)
            {
                bool active = (_filter.PlayerMask & (1 << i)) != 0;
                Chip("P" + (i + 1).ToString(CultureInfo.InvariantCulture), null, active, HitOverlaySkin.PlayerColor(i), ref x, ref y, maxWidth);

                if (Pressed(_lastChip))
                {
                    _filter.PlayerMask ^= 1 << i;
                    MarkFilterDirty();
                }
            }

            _y = y + HitOverlaySkin.ChipHeight;
        }

        private void PresetButton(string labelKey, float x, float width, System.Action action)
        {
            Rect rect = R(x, _y, width, HitOverlaySkin.ChipHeight);
            Fill(rect, Hovered(rect) ? HitOverlaySkin.RowHover : HitOverlaySkin.ChipBackground);
            Outline(rect, HitOverlaySkin.ChipBorder);
            Label(rect, i18n.T(labelKey), HitOverlaySkin.TextSecondary, TextAlignmentOptions.Center, HitOverlaySkin.FontSize - 2f);

            if (Pressed(rect))
            {
                action();
                MarkFilterDirty();
            }
        }

        private void PresetAll()
        {
            _filter = HitOverlayFilter.CreateDefault();
        }

        private void PresetPerfect()
        {
            _filter.XpOnly = false;
            _filter.JudgeMask = HitOverlayFilter.BitOf(HitMan.EarlyPerfect) | HitOverlayFilter.BitOf(HitMan.PerfectMinus) |
                                HitOverlayFilter.BitOf(HitMan.XPerfect) | HitOverlayFilter.BitOf(HitMan.PerfectPlus) |
                                HitOverlayFilter.BitOf(HitMan.LatePerfect);
        }

        private void PresetXp()
        {
            _filter.JudgeMask = 0u;
            _filter.XpOnly = true;
        }

        private void PresetFail()
        {
            _filter.XpOnly = false;
            _filter.JudgeMask = HitOverlayFilter.BitOf(HitMan.FailMiss) | HitOverlayFilter.BitOf(HitMan.FailOverload) | HitOverlayFilter.BitOf(HitMan.OverPress);
        }

        private void SliderRow(string labelKey, ref float value, float min, float max, string format, bool unbounded)
        {
            Rect row = R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight);
            Label(new Rect(row.xMin, row.yMin, HitOverlaySkin.SliderLabelWidth * _scale, row.height), i18n.T(labelKey), HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);

            float trackX = row.xMin + HitOverlaySkin.SliderLabelWidth * _scale;
            float trackWidth = row.width - (HitOverlaySkin.SliderLabelWidth + HitOverlaySkin.SliderValueWidth) * _scale;
            Rect track = new Rect(trackX, row.yMin + row.height * 0.5f - SliderTravelHeight * 0.5f * _scale, trackWidth, SliderTravelHeight * _scale);
            Fill(track, HitOverlaySkin.Track);

            float ratio = Mathf.Approximately(max, min) ? 0f : Mathf.Clamp01((value - min) / (max - min));
            Fill(new Rect(track.xMin, track.yMin, track.width * ratio, track.height), HitOverlaySkin.Accent);
            Fill(new Rect(track.xMin + track.width * ratio - 3f * _scale, track.yMin - 2f * _scale, 6f * _scale, track.height + 4f * _scale), HitOverlaySkin.Knob);

            string text = unbounded && value >= max
                ? "\u221e"
                : Mathf.Clamp(value, min, max).ToString(format, CultureInfo.InvariantCulture);
            Label(new Rect(row.xMax - HitOverlaySkin.SliderValueWidth * _scale, row.yMin, HitOverlaySkin.SliderValueWidth * _scale, row.height), text, HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineRight, HitOverlaySkin.FontSize - 1f);

            Rect hitArea = new Rect(trackX, row.yMin + (row.height - SliderHitHeight * _scale) * 0.5f, trackWidth, SliderHitHeight * _scale);
            if (_inputPass)
            {
                bool inside = hitArea.Contains(_mouse);
                if (_down && inside)
                {
                    _dragSlider = labelKey;
                    _consumedDown = true;
                }

                if (_dragSlider == labelKey && _up) _dragSlider = null;

                if (_dragSlider == labelKey && _held)
                {
                    float t = Mathf.Clamp01((_mouse.x - trackX) / Mathf.Max(1f, trackWidth));
                    value = Mathf.Lerp(min, max, t);
                    MarkDisplayDirty();
                }
            }

            _y += HitOverlaySkin.RowHeight;
        }

        private void SegmentRow(string labelKey, ref int value, string[] options)
        {
            Rect row = R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight);
            Label(new Rect(row.xMin, row.yMin, HitOverlaySkin.SliderLabelWidth * _scale, row.height), i18n.T(labelKey), HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);

            float startX = row.xMin + HitOverlaySkin.SliderLabelWidth * _scale;
            float available = row.width - HitOverlaySkin.SliderLabelWidth * _scale;
            float gap = 2f * _scale;
            float width = (available - gap * (options.Length - 1)) / options.Length;

            for (int i = 0; i < options.Length; i++)
            {
                Rect cell = new Rect(startX + (width + gap) * i, row.yMin + 2f * _scale, width, row.height - 4f * _scale);
                bool active = value == i;
                Fill(cell, active ? HitOverlaySkin.Accent : (Hovered(cell) ? HitOverlaySkin.RowHover : HitOverlaySkin.ChipBackground));
                Label(cell, i18n.T(options[i]), active ? HitOverlaySkin.TextPrimary : HitOverlaySkin.TextSecondary, TextAlignmentOptions.Center, HitOverlaySkin.FontSize - 2f);

                if (Pressed(cell))
                {
                    value = i;
                    MarkDisplayDirty();
                }
            }

            _y += HitOverlaySkin.RowHeight;
        }

        private void CheckRow(string labelKey, ref bool value, bool triggerFilter = false)
        {
            Rect row = R(HitOverlaySkin.Padding, _y, _width - HitOverlaySkin.Padding * 2f, HitOverlaySkin.RowHeight);
            Rect box = new Rect(row.xMin, row.yMin + (row.height - 11f * _scale) * 0.5f, 11f * _scale, 11f * _scale);

            Fill(box, value ? HitOverlaySkin.Accent : HitOverlaySkin.FieldBackground);
            Outline(box, value ? HitOverlaySkin.Accent : HitOverlaySkin.Border);
            if (value)
            {
                float inset = box.width * 0.2f;
                Fill(new Rect(box.x + inset, box.y + inset,
                    Mathf.Max(0f, box.width - inset * 2f), Mathf.Max(0f, box.height - inset * 2f)), Color.white);
            }
            Label(new Rect(box.xMax + 6f * _scale, row.yMin, row.width - box.width - 6f * _scale, row.height), i18n.T(labelKey), HitOverlaySkin.TextSecondary, TextAlignmentOptions.MidlineLeft, HitOverlaySkin.FontSize - 1f);

            if (Pressed(row))
            {
                value = !value;
                if (triggerFilter) MarkFilterDirty();
                else MarkDisplayDirty();
            }

            _y += HitOverlaySkin.RowHeight;
        }

        private void ReadInput()
        {
            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed)) return;

            bool changed = false;
            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];
                if (c == '\b')
                {
                    if (_searchText.Length > 0) _searchText = _searchText.Substring(0, _searchText.Length - 1);
                    changed = true;
                }
                else if (c == '\n' || c == '\r')
                {
                    _searchFocused = false;
                }
                else if (c == 27)
                {
                    _searchText = string.Empty;
                    changed = true;
                }
                else if (c >= ' ' && c < 127)
                {
                    if (_searchText.Length < 48) _searchText += c;
                    changed = true;
                }
            }

            if (changed) _searchDirty = true;
        }

        private void MarkFilterDirty()
        {
            _searchDirty = true;
            if (_owner != null) _owner.RequestRebuild();
        }

        private void MarkDisplayDirty()
        {
            if (_owner != null) _owner.RequestRebuild();
        }

        private string DescribeSample(TimingScatterSample sample)
        {
            string floor = sample.HasFloorId ? "#" + sample.FloorId.ToString(CultureInfo.InvariantCulture) : "#?";
            string time = sample.TimeMs.ToString("0", CultureInfo.InvariantCulture) + "ms";
            string offset = sample.OffsetMs.ToString("+0.0;-0.0;0", CultureInfo.InvariantCulture);
            return floor + "  " + time + "  " + offset;
        }

        private bool Blink()
        {
            return Mathf.Repeat(Time.unscaledTime, 1f) < 0.55f;
        }

        private Rect R(float x, float y, float width, float height)
        {
            float left = _originX + x * _scale;
            float top = _originTop - y * _scale;
            return new Rect(left, top - height * _scale, width * _scale, height * _scale);
        }

        private void Fill(Rect rect, Color color)
        {
            if (_inputPass || _renderer == null) return;
            _renderer.Fill(rect, color);
        }

        private void Outline(Rect rect, Color color)
        {
            if (_inputPass || _renderer == null) return;
            _renderer.Outline(rect, color);
        }

        private void Triangle(Rect rect, Color color, bool pointsRight)
        {
            if (_inputPass || _renderer == null) return;
            _renderer.Triangle(rect, color, pointsRight);
        }

        private void Line(Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (_inputPass || _renderer == null) return;
            _renderer.Line(start, end, thickness, color);
        }

        private void Label(Rect rect, string text, Color color, TextAlignmentOptions alignment, float fontSize)
        {
            if (_inputPass || _renderer == null) return;
            _renderer.Text(rect, text, color, alignment, fontSize * _scale);
        }

        private bool Hovered(Rect rect)
        {
            return !_inputPass && rect.Contains(_mouse);
        }

        private bool Pressed(Rect rect)
        {
            if (!_inputPass || !_down || _consumedDown) return false;
            if (!rect.Contains(_mouse)) return false;

            _consumedDown = true;
            return true;
        }
    }
}
