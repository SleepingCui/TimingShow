using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TimingShow.HUD.LogGraphSettingsUI
{
    public sealed partial class LogGraphSettingsUI : MonoBehaviour
    {
        private const float BasePadding = 10f;
        private const float BaseContentMaxWidth = 420f;
        private const float BaseTitleHeight = 20f;
        private const float BaseTabHeight = 22f;
        private const float BaseRowHeight = 22f;
        private const float BaseRowGap = 4f;
        private const float BaseFontSize = 12f;
        private const float BaseTrackHeight = 8f;
        private const float BaseToggleSize = 14f;
        private const float BaseLabelRatio = 0.42f;
        private const float LineHeightRatio = 1.4f;
        
        private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.12f, 1f);
        private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.28f);
        private static readonly Color RowHoverColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color TabColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color TabActiveColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color AccentColor = new Color(0.30f, 0.76f, 1f, 1f);
        private static readonly Color TextColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        private static readonly Color ValueColor = new Color(0.97f, 0.97f, 0.99f, 1f);

        private ILogGraphSettingsHost _host;
        private TMP_FontAsset _font;
        private SettingsPanelGraphic _graphic;
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
        private TextMeshProUGUI _titleText;

        private bool _open;
        private int _tab;
        private int _colorIndex;
        private Row _colorPreviewRow;
        
        private Draft _draft = new Draft();
        
        private bool _draftDirty;
        private Row _activeRow;
        private int _activeBar = -1;
        private Row _hoverRow;
        private float _lastLeft = float.NaN;
        private float _lastBottom = float.NaN;
        private float _lastWidth = float.NaN;
        private float _lastHeight = float.NaN;
        private float _lastScale = float.NaN;
        private float _panelWidth;
        private float _panelHeight;

        private static Settings S => ModContext.Settings;
        private RectTransform Rect => (RectTransform)transform;

        private static float UiScale
        {
            get
            {
                Settings settings = S;
                return settings != null ? Mathf.Clamp(settings.LogGraph_UIScale, 0.75f, 3f) : 1.25f;
            }
        }
        public bool IsOpen => _open;

        public static LogGraphSettingsUI Create(RectTransform parent, ILogGraphSettingsHost host, TMP_FontAsset font)
        {
            if (parent == null) return null;

            GameObject go = new GameObject("LogGraphSettingsPanel");
            go.transform.SetParent(parent, false);
            if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();

            LogGraphSettingsUI ui = go.AddComponent<LogGraphSettingsUI>();
            ui._host = host;
            ui._font = font != null ? font : TMP_Settings.defaultFontAsset;
            ui._graphic = go.AddComponent<SettingsPanelGraphic>();
            ui._graphic.Bind(ui);
            ui._open = false;
            go.SetActive(false);
            return ui;
        }

     
        public void SetFont(TMP_FontAsset font)
        {
            TMP_FontAsset target = font != null ? font : TMP_Settings.defaultFontAsset;
            if (target == null || target == _font) return;

            _font = target;
            for (int i = 0; i < _texts.Count; i++)
            {
                if (_texts[i] != null) _texts[i].font = target;
            }

            MarkDirty();
        }

        public void SetOpen(bool open)
        {
            if (_open == open) return;

            _open = open;
            gameObject.SetActive(open);

            if (open)
            {
                _lastWidth = float.NaN;  
                _draft = Draft.FromSettings(S);  
                _draftDirty = false;
                RebuildRows();
            }
            else
            {
                _activeRow = null;
                _activeBar = -1;
                _hoverRow = null;
                _draftDirty = false;  
            }
        }

        public void Toggle()
        {
            SetOpen(!_open);
        }


        
        public void ApplyLayout(float left, float bottom, float width, float height)
        {
            float scale = UiScale;
            if (left == _lastLeft && bottom == _lastBottom && width == _lastWidth
                && height == _lastHeight && scale == _lastScale)
            {
                return;
            }

            _lastLeft = left;
            _lastBottom = bottom;
            _lastWidth = width;
            _lastHeight = height;
            _lastScale = scale;

            RectTransform rect = Rect;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);

            _panelWidth = width;
            _panelHeight = height;

            RebuildRows();
            MarkDirty();
        }
        
        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (!_open) return false;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screenPosition, null, out local))
                return false;

            return new Rect(0f, 0f, _panelWidth, _panelHeight).Contains(local);
        }


        private void Update()
        {
            if (!_open) return;

            Vector2 mouse = Input.mousePosition;
            Vector2 local;
            bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Rect, mouse, null, out local) && new Rect(0f, 0f, _panelWidth, _panelHeight).Contains(local);

            if (Input.GetMouseButtonDown(0))
            {
                _activeRow = inside ? FindRow(local, out _activeBar) : null;
                if (_activeRow != null) HandlePress(_activeRow, local);
            }
            else if (_activeRow != null && Input.GetMouseButton(0))
            {
                HandleDrag(_activeRow, local);
            }

            if (Input.GetMouseButtonUp(0))
            {
                _activeRow = null;
                _activeBar = -1;
            }

            Row hover = inside ? FindRow(local, out _) : null;
            if (!ReferenceEquals(hover, _hoverRow))
            {
                _hoverRow = hover;
                MarkDirty();
            }
        }

        private Row FindRow(Vector2 local, out int barIndex)
        {
            barIndex = -1;

            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (!row.Rect.Contains(local)) continue;

                if (row.Bars != null)
                {
                    for (int b = 0; b < row.Bars.Length; b++)
                    {
                        if (row.Bars[b].Contains(local))
                        {
                            barIndex = b;
                            break;
                        }
                    }
                }

                return row;
            }

            return null;
        }

        private void HandlePress(Row row, Vector2 local)
        {
            switch (row.Kind)
            {
                case RowKind.Tab:
                    if (_tab != row.TabIndex)
                    {
                        _tab = row.TabIndex;
                        _activeRow = null;
                        RebuildRows();
                        MarkDirty();
                    }
                    return;
                case RowKind.Button:
                    ApplyDraft();
                    return;
                case RowKind.Toggle:
                    bool value = !(row.GetToggle != null && row.GetToggle());
                    row.SetToggle?.Invoke(value);
                    _draftDirty = true;
                    MarkDirty();
                    return;
                case RowKind.ColorPick:
                    if (row.ResetRect.width > 0f && row.ResetRect.Contains(local))
                    {
                        ResetColorSlot(row.ColorIndex);
                        return;
                    }

                    if (_colorIndex != row.ColorIndex)
                    {
                        _colorIndex = row.ColorIndex;
                        _activeRow = null;
                        _hoverRow = null;
                        RebuildRows();
                        MarkDirty();
                    }

                    return;
                case RowKind.Slider:
                    HandleDrag(row, local);
                    return;
            }
        }

        private void HandleDrag(Row row, Vector2 local)
        {
            if (row.Bars == null || row.Bars.Length == 0) return;

            int bar = _activeBar;
            if (bar < 0 || bar >= row.Bars.Length)
            {
  
                bar = 0;
                float best = float.MaxValue;
                for (int i = 0; i < row.Bars.Length; i++)
                {
                    float distance = Mathf.Abs(local.x - row.Bars[i].center.x);
                    if (distance < best)
                    {
                        best = distance;
                        bar = i;
                    }
                }

                if (row.Kind == RowKind.Slider) bar = 0;
            }

            float t = Mathf.Clamp01(row.Bars[bar].width > 0f
                ? (local.x - row.Bars[bar].x) / row.Bars[bar].width
                : 0f);

            if (row.Kind == RowKind.Slider)
            {
                float value = row.LogScale
                    ? Mathf.Round(row.Min * Mathf.Pow(row.Max / row.Min, t))
                    : Mathf.Lerp(row.Min, row.Max, t);

                if (row.Max <= 20f && row.Min >= 1f) value = Mathf.Round(value * 10f) / 10f;

                row.Set?.Invoke(value);
                if (row.Value != null) row.Value.text = FormatValue(row);
                RefreshColorPreview();
            }

            MarkDirty();
            _draftDirty = true;   
        }

    }
}
