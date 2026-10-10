using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public interface ILogGraphSettingsHost
    {
        void OnLogGraphSettingsChanged();
    }
    
    public sealed class LogGraphSettingsUI : MonoBehaviour
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

        private enum RowKind
        {
            Title,
            Tab,
            Slider,
            Toggle,
            ColorPick,
            ColorPreview,
            Text,
            Button
        }
        
        private sealed class Row
        {
            public RowKind Kind;
            public string LabelKey;
            public Rect Rect;
            public Rect[] Bars;        
            public float Min;
            public float Max;
            public bool LogScale;
            public string Format = "F1";
            public Func<float> Get;
            public Action<float> Set;
            public Func<bool> GetToggle;
            public Action<bool> SetToggle;
            public int TabIndex;
            public Func<Color> GetColor;
            public int ColorIndex;
            public Rect ResetRect;
            public TextMeshProUGUI ResetLabel;
            public bool ByteDisplay;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Value;
        }
        
        private sealed class Draft
        {
            public float UiScale = 1.25f;
            public float PointSize = 3f;
            public int MaxRenderPoints = 800;
            public bool IgnoreOutliers;
            public bool UseHitAxis;
            public bool UseJudgeColor = true;
            public bool ShowZeroLine;
            public bool ShowAvgLine;
            public bool ShowGrid = true;
            public bool UseCustomFont;
            public Color BgColor = Color.black;
            public Color GridColor = Color.white;
            public Color PointColor = Color.white;
            public Color ZeroLineColor = Color.white;
            public Color AvgLineColor = Color.yellow;
            public Color AxisTextColor = Color.gray;

            public static Draft FromSettings(Settings s)
            {
                Draft d = new Draft();
                if (s == null) return d;

                d.UiScale = s.LogGraph_UIScale;
                d.PointSize = s.LogGraph_PointSize;
                d.MaxRenderPoints = s.LogGraph_MaxRenderPoints;
                d.IgnoreOutliers = s.LogGraph_IgnoreOutliers;
                d.UseHitAxis = s.LogGraph_UseHitAxis;
                d.UseJudgeColor = s.LogGraph_UseJudgeColor;
                d.ShowZeroLine = s.LogGraph_ShowZeroLine;
                d.ShowAvgLine = s.LogGraph_ShowAvgLine;
                d.ShowGrid = s.LogGraph_ShowGrid;
                d.UseCustomFont = s.LogGraph_UseCustomFont;
                d.BgColor = s.LogGraph_BgColor;
                d.GridColor = s.LogGraph_GridColor;
                d.PointColor = s.LogGraph_PointColor;
                d.ZeroLineColor = s.LogGraph_ZeroLineColor;
                d.AvgLineColor = s.LogGraph_AvgLineColor;
                d.AxisTextColor = s.LogGraph_AxisTextColor;
                return d;
            }

            public void ApplyTo(Settings s)
            {
                if (s == null) return;

                s.LogGraph_UIScale = UiScale;
                s.LogGraph_PointSize = PointSize;
                s.LogGraph_MaxRenderPoints = MaxRenderPoints;
                s.LogGraph_IgnoreOutliers = IgnoreOutliers;
                s.LogGraph_UseHitAxis = UseHitAxis;
                s.LogGraph_UseJudgeColor = UseJudgeColor;
                s.LogGraph_ShowZeroLine = ShowZeroLine;
                s.LogGraph_ShowAvgLine = ShowAvgLine;
                s.LogGraph_ShowGrid = ShowGrid;
                s.LogGraph_UseCustomFont = UseCustomFont;
                s.LogGraph_BgColor = BgColor;
                s.LogGraph_GridColor = GridColor;
                s.LogGraph_PointColor = PointColor;
                s.LogGraph_ZeroLineColor = ZeroLineColor;
                s.LogGraph_AvgLineColor = AvgLineColor;
                s.LogGraph_AxisTextColor = AxisTextColor;
            }
        }

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

        private const int ColorSlotCount = 6;

        private static readonly string[] ColorSlotKeys =
        {
            "Label_BgColor",
            "Label_GridColor",
            "Label_PointColor",
            "Label_ZeroLineColor",
            "Label_AvgLineColor",
            "Label_AxisTextColor"
        };

        private static readonly string[] ColorChannelKeys =
        {
            "LogGraph_ColorChannelR",
            "LogGraph_ColorChannelG",
            "LogGraph_ColorChannelB"
        };

        private Color GetColorSlot(int index)
        {
            switch (index)
            {
                case 1: return _draft.GridColor;
                case 2: return _draft.PointColor;
                case 3: return _draft.ZeroLineColor;
                case 4: return _draft.AvgLineColor;
                case 5: return _draft.AxisTextColor;
                default: return _draft.BgColor;
            }
        }

        private void SetColorSlot(int index, Color color)
        {
            switch (index)
            {
                case 1: _draft.GridColor = color; break;
                case 2: _draft.PointColor = color; break;
                case 3: _draft.ZeroLineColor = color; break;
                case 4: _draft.AvgLineColor = color; break;
                case 5: _draft.AxisTextColor = color; break;
                default: _draft.BgColor = color; break;
            }
        }

        private float GetColorChannel(int slot, int channel)
        {
            Color color = GetColorSlot(slot);
            return channel == 0 ? color.r : channel == 1 ? color.g : color.b;
        }

        private void SetColorChannel(int slot, int channel, float value)
        {
            Color color = GetColorSlot(slot);
            color.r = channel == 0 ? value : color.r;
            color.g = channel == 1 ? value : color.g;
            color.b = channel == 2 ? value : color.b;
            SetColorSlot(slot, color);
        }

        private void ResetColorSlot(int index)
        {
            Color color = DefaultColorSlot(index);
            color.a = GetColorSlot(index).a;
            SetColorSlot(index, color);

            _draftDirty = true;
            _activeRow = null;
            _hoverRow = null;
            RebuildRows();
            MarkDirty();
        }

        private static Color DefaultColorSlot(int index)
        {
            switch (index)
            {
                case 1: return Settings.LogGraph_DefaultGridColor;
                case 2: return Settings.LogGraph_DefaultPointColor;
                case 3: return Settings.LogGraph_DefaultZeroLineColor;
                case 4: return Settings.LogGraph_DefaultAvgLineColor;
                case 5: return Settings.LogGraph_DefaultAxisTextColor;
                default: return Settings.LogGraph_DefaultBgColor;
            }
        }

        private static string ColorPreviewText(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color) + "    A " + color.a.ToString("F2");
        }

        private static Color ColorPreviewTextColor(Color color)
        {
            float luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            return luminance > 0.55f ? new Color(0.06f, 0.07f, 0.10f, 1f) : new Color(0.94f, 0.95f, 0.98f, 1f);
        }

        private void RefreshColorPreview()
        {
            if (_colorPreviewRow == null || _colorPreviewRow.Value == null) return;

            Color color = GetColorSlot(_colorIndex);
            _colorPreviewRow.Value.text = ColorPreviewText(color);
            _colorPreviewRow.Value.color = ColorPreviewTextColor(color);
        }

        internal static float ColorSwatchSize(float rowHeight, float scale)
        {
            return Mathf.Max(6f, Mathf.Min(rowHeight * 0.62f, 14f * scale));
        }

        internal static Rect ColorResetRect(Rect chipRect, float scale)
        {
            float resetWidth = Mathf.Min(Mathf.Max(1f, chipRect.width) * 0.30f, 76f * scale);
            float resetInset = Mathf.Max(1f, 3f * scale);

            return new Rect(
                chipRect.xMax - resetWidth - resetInset,
                chipRect.y + resetInset,
                Mathf.Max(1f, resetWidth),
                Mathf.Max(1f, chipRect.height - resetInset * 2f));
        }

        internal static float ColorChipLabelWidth(Rect chipRect, float scale)
        {
            float swatch = ColorSwatchSize(chipRect.height, scale);
            float reserved = ColorResetRect(chipRect, scale).width + 4f * scale;
            return chipRect.width - swatch - 11f * scale - reserved;
        }


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


        private void RebuildRows()
        {
            ClearTexts();
            _rows.Clear();

            float w = _panelWidth;
            float h = _panelHeight;
            if (w <= 1f || h <= 1f) return;

            float scale = UiScale;
            float pad = BasePadding * scale;
            float gap = BaseRowGap * scale;
            float fontSize = Mathf.Max(8f, Mathf.Round(BaseFontSize * scale));
            float innerWidth = Mathf.Max(1f, w - pad * 2f);
            float contentWidth = Mathf.Min(innerWidth, BaseContentMaxWidth * scale);
            float left = pad + Mathf.Max(0f, (innerWidth - contentWidth) * 0.5f);

            float top = h - pad;
            
            float titleHeight = BaseTitleHeight * scale;
            Rect titleRect = new Rect(left, top - titleHeight, contentWidth, titleHeight);
            _rows.Add(new Row { Kind = RowKind.Title, Rect = titleRect });
            _titleText = CreateText("SettingsTitle", i18n.T("LogGraph_SettingsTitle"), TextAlignmentOptions.MidlineLeft, fontSize, TextColor);
            PlaceText(_titleText, titleRect, w, h, false);

            top -= titleHeight + gap;
            
            float tabHeight = BaseTabHeight * scale;
            float tabWidth = contentWidth * 0.5f;
            Row tabBasic = new Row
            {
                Kind = RowKind.Tab,
                LabelKey = "LogGraph_TabBasic",
                TabIndex = 0,
                Rect = new Rect(left, top - tabHeight, tabWidth - gap * 0.5f, tabHeight)
            };
            Row tabColors = new Row
            {
                Kind = RowKind.Tab,
                LabelKey = "LogGraph_TabColors",
                TabIndex = 1,
                Rect = new Rect(left + tabWidth + gap * 0.5f, top - tabHeight, tabWidth - gap * 0.5f, tabHeight)
            };
            AddTab(tabBasic, fontSize);
            AddTab(tabColors, fontSize);
            top -= tabHeight + gap * 1.5f;
            
            float buttonHeight = BaseTabHeight * scale;
            Row applyRow = new Row
            {
                Kind = RowKind.Button,
                LabelKey = "LogGraph_Apply",
                Rect = new Rect(left, top - buttonHeight, contentWidth, buttonHeight)
            };
            AddButton(applyRow, fontSize);
            top -= buttonHeight + gap * 1.5f;

            if (_tab == 1)
            {
                BuildColorPicker(left, contentWidth, scale, gap, fontSize, ref top);
                BuildColorPreview(left, contentWidth, scale, gap, fontSize, ref top);
            }

            List<Row> content = _tab == 0 ? BuildBasicRows() : BuildColorRows();
            if (content.Count == 0) return;
            
            float available = Mathf.Max(1f, top - pad);
            
            float rowGap = Mathf.Min(gap, Mathf.Max(1f, available / content.Count * 0.15f));
            float rowHeight = Mathf.Min(BaseRowHeight * scale, available / content.Count - rowGap);
            rowHeight = Mathf.Max(12f, rowHeight);
            
            float rowFontSize = Mathf.Clamp(Mathf.Min(fontSize, rowHeight / LineHeightRatio), 8f, fontSize);

            float y = top;
            for (int i = 0; i < content.Count; i++)
            {
                Row row = content[i];
                row.Rect = new Rect(left, y - rowHeight, contentWidth, rowHeight);
                LayoutRowBars(row, scale, rowHeight);
                AddRowText(row, rowFontSize, w, h);
                _rows.Add(row);
                y -= rowHeight + rowGap;
            }
        }

        private List<Row> BuildBasicRows()
        {
            List<Row> rows = new List<Row>(12);
            
            rows.Add(Slider("LogGraph_UiScale", 0.75f, 3f, false, "F2",
                () => _draft.UiScale,
                v => _draft.UiScale = v));

            rows.Add(Slider("Label_PointSize", 1f, 12f, false, "F1",
                () => _draft.PointSize,
                v => _draft.PointSize = v));

            rows.Add(Slider("Label_MaxRenderPoints", 20f, 8000f, true, "F0",
                () => _draft.MaxRenderPoints,
                v => _draft.MaxRenderPoints = Mathf.RoundToInt(v)));

            rows.Add(Toggle("Toggle_IgnoreOutliers",
                () => _draft.IgnoreOutliers,
                v => _draft.IgnoreOutliers = v));
            rows.Add(Toggle("Toggle_UseHitAxis",
                () => _draft.UseHitAxis,
                v => _draft.UseHitAxis = v));
            rows.Add(Toggle("Toggle_UseJudgeColor",
                () => _draft.UseJudgeColor,
                v => _draft.UseJudgeColor = v));
            rows.Add(Toggle("Toggle_ShowZeroLine",
                () => _draft.ShowZeroLine,
                v => _draft.ShowZeroLine = v));
            rows.Add(Toggle("Toggle_ShowAvgLine",
                () => _draft.ShowAvgLine,
                v => _draft.ShowAvgLine = v));
            rows.Add(Toggle("Toggle_ShowGrid",
                () => _draft.ShowGrid,
                v => _draft.ShowGrid = v));
            rows.Add(Toggle("Toggle_CustomFont",
                () => _draft.UseCustomFont,
                v => _draft.UseCustomFont = v));

            rows.Add(Text("LogGraph_FontHint"));
            return rows;
        }

        private List<Row> BuildColorRows()
        {
            List<Row> rows = new List<Row>(3);

            for (int channel = 0; channel < 3; channel++)
            {
                int c = channel;
                rows.Add(new Row
                {
                    Kind = RowKind.Slider,
                    LabelKey = ColorChannelKeys[c],
                    Min = 0f,
                    Max = 1f,
                    Format = "F2",
                    ByteDisplay = true,
                    Get = () => GetColorChannel(_colorIndex, c),
                    Set = v => SetColorChannel(_colorIndex, c, v)
                });
            }

            return rows;
        }

        private void BuildColorPicker(float left, float contentWidth, float scale, float gap, float fontSize, ref float top)
        {
            const int columns = 2;

            float chipHeight = Mathf.Min(BaseRowHeight * scale, 26f * scale);
            float chipGap = Mathf.Max(1f, gap * 0.5f);
            float cellWidth = Mathf.Max(1f, (contentWidth - chipGap * (columns - 1)) / columns);

            for (int i = 0; i < ColorSlotCount; i++)
            {
                int slot = i;
                int column = i % columns;
                int line = i / columns;

                Rect chipRect = new Rect(
                    left + column * (cellWidth + chipGap),
                    top - chipHeight - line * (chipHeight + chipGap),
                    cellWidth,
                    chipHeight);

                Row chip = new Row
                {
                    Kind = RowKind.ColorPick,
                    LabelKey = ColorSlotKeys[slot],
                    ColorIndex = slot,
                    GetColor = () => GetColorSlot(slot),
                    Rect = chipRect,
                    ResetRect = ColorResetRect(chipRect, scale)
                };

                AddColorChip(chip, Mathf.Max(8f, fontSize * 0.9f));
            }

            int lines = (ColorSlotCount + columns - 1) / columns;
            top -= lines * chipHeight + Mathf.Max(0, lines - 1) * chipGap + gap * 1.5f;
        }

        private void BuildColorPreview(float left, float contentWidth, float scale, float gap, float fontSize, ref float top)
        {
            float freeHeight = Mathf.Max(1f, top - BasePadding * scale);
            float previewHeight = Mathf.Min(freeHeight * 0.38f, BaseRowHeight * scale * 4f);
            if (previewHeight < 20f * scale)
            {
                previewHeight = Mathf.Min(freeHeight * 0.8f, 20f * scale);
            }

            Row preview = new Row
            {
                Kind = RowKind.ColorPreview,
                GetColor = () => GetColorSlot(_colorIndex),
                Rect = new Rect(left, top - previewHeight, contentWidth, previewHeight)
            };

            AddColorPreview(preview, fontSize);
            top -= previewHeight + gap * 1.5f;
        }

        private static Row Slider(string key, float min, float max, bool logScale, string format,
            Func<float> get, Action<float> set)
        {
            return new Row
            {
                Kind = RowKind.Slider,
                LabelKey = key,
                Min = min,
                Max = max,
                LogScale = logScale,
                Format = format,
                Get = get,
                Set = set
            };
        }

        private static Row Toggle(string key, Func<bool> get, Action<bool> set)
        {
            return new Row
            {
                Kind = RowKind.Toggle,
                LabelKey = key,
                GetToggle = get,
                SetToggle = set
            };
        }

        private static Row Text(string key)
        {
            return new Row { Kind = RowKind.Text, LabelKey = key };
        }

        private void LayoutRowBars(Row row, float scale, float rowHeight)
        {
            Rect r = row.Rect;

            if (row.Kind == RowKind.Slider)
            {
                float labelWidth = r.width * BaseLabelRatio;
                float valueWidth = 48f * scale;
                float barLeft = r.x + labelWidth;
                float barRight = r.x + r.width - valueWidth - 4f * scale;
                float trackHeight = Mathf.Max(6f, Mathf.Min(BaseTrackHeight * scale, rowHeight * 0.7f));
                float barY = r.y + (rowHeight - trackHeight) * 0.5f;

                row.Bars = new[] { new Rect(barLeft, barY, Mathf.Max(1f, barRight - barLeft), trackHeight) };
            }
            else if (row.Kind == RowKind.Toggle)
            {
                float toggleSize = Mathf.Min(BaseToggleSize * scale, rowHeight * 0.8f);
                row.Bars = new[]
                {
                    new Rect(r.x + r.width - toggleSize - 2f * scale,
                        r.y + (rowHeight - toggleSize) * 0.5f, toggleSize, toggleSize)
                };
            }
        }

        private void AddRowText(Row row, float fontSize, float w, float h)
        {
            if (row.Kind == RowKind.Text)
            {
                row.Label = CreateText("RowText" + _rows.Count, i18n.T(row.LabelKey),
                    TextAlignmentOptions.MidlineLeft, Mathf.Max(8f, fontSize * 0.9f), TextColor);
                PlaceText(row.Label, row.Rect, w, h, false);
                return;
            }

            if (string.IsNullOrEmpty(row.LabelKey)) return;

            row.Label = CreateText("RowLabel" + _rows.Count, i18n.T(row.LabelKey),
                TextAlignmentOptions.MidlineLeft, fontSize, TextColor);

            float labelWidth = row.Rect.width * BaseLabelRatio;

            Rect labelRect = new Rect(row.Rect.x, row.Rect.y, labelWidth, row.Rect.height);
            PlaceText(row.Label, labelRect, w, h, false);

            if (row.Kind == RowKind.Slider)
            {
                row.Value = CreateText("RowValue" + _rows.Count, FormatValue(row),
                    TextAlignmentOptions.MidlineRight, fontSize, ValueColor);
                Rect valueRect = new Rect(row.Rect.x + row.Rect.width - 48f * UiScale, row.Rect.y,
                    48f * UiScale, row.Rect.height);
                PlaceText(row.Value, valueRect, w, h, true);
            }
        }

        private void AddColorChip(Row row, float fontSize)
        {
            row.Label = CreateText("ColorChip" + row.ColorIndex, i18n.T(row.LabelKey),
                TextAlignmentOptions.MidlineLeft, fontSize, TextColor);

            float scale = UiScale;
            float swatch = ColorSwatchSize(row.Rect.height, scale);
            Rect textRect = new Rect(
                row.Rect.x + 4f * scale + swatch + 4f * scale,
                row.Rect.y,
                Mathf.Max(1f, ColorChipLabelWidth(row.Rect, scale)),
                row.Rect.height);

            PlaceText(row.Label, textRect, _panelWidth, _panelHeight, false);

            if (row.ResetRect.width > 0f)
            {
                row.ResetLabel = CreateText("ColorReset" + row.ColorIndex, i18n.T("Btn_ResetColor"),
                    TextAlignmentOptions.Center, Mathf.Max(8f, fontSize), TextColor);
                PlaceText(row.ResetLabel, row.ResetRect, _panelWidth, _panelHeight, false);
            }

            _rows.Add(row);
        }

        private void AddColorPreview(Row row, float fontSize)
        {
            Color color = GetColorSlot(_colorIndex);

            row.Value = CreateText("ColorPreviewValue", ColorPreviewText(color),
                TextAlignmentOptions.Center, Mathf.Max(8f, fontSize), ColorPreviewTextColor(color));
            PlaceText(row.Value, row.Rect, _panelWidth, _panelHeight, false);

            _colorPreviewRow = row;
            _rows.Add(row);
        }

        private void AddTab(Row row, float fontSize)
        {
            row.Label = CreateText("Tab" + row.TabIndex, i18n.T(row.LabelKey),
                TextAlignmentOptions.Center, fontSize, TextColor);
            PlaceText(row.Label, row.Rect, _panelWidth, _panelHeight, false);
            _rows.Add(row);
        }

        private void AddButton(Row row, float fontSize)
        {
            row.Label = CreateText("Button" + row.LabelKey, i18n.T(row.LabelKey),
                TextAlignmentOptions.Center, fontSize, TextColor);
            PlaceText(row.Label, row.Rect, _panelWidth, _panelHeight, false);
            _rows.Add(row);
        }
        
        private static void PlaceText(TextMeshProUGUI text, Rect rect, float w, float h, bool right)
        {
            if (text == null) return;

            RectTransform rt = text.rectTransform;
            if (right)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(rect.x + rect.width - w, rect.y + rect.height - h);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(rect.x, rect.y + rect.height - h);
            }

            rt.sizeDelta = new Vector2(Mathf.Max(1f, rect.width), Mathf.Max(1f, rect.height));
        }

        private TextMeshProUGUI CreateText(string name, string content, TextAlignmentOptions alignment,
            float fontSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            if (_font != null) text.font = _font;

            _texts.Add(text);
            return text;
        }

        private void ClearTexts()
        {
            for (int i = 0; i < _texts.Count; i++)
            {
                if (_texts[i] != null) Destroy(_texts[i].gameObject);
            }

            _texts.Clear();
            _titleText = null;
            _colorPreviewRow = null;
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

        private string FormatValue(Row row)
        {
            float value = row.Get != null ? row.Get() : row.Min;
            if (row.ByteDisplay) return Mathf.RoundToInt(Mathf.Clamp01(value) * 255f).ToString();
            return value.ToString(row.Format);
        }

        private void NotifyChanged()
        {
            if (_host != null) _host.OnLogGraphSettingsChanged();
        }


        private void ApplyDraft()
        {
            Settings settings = S;
            if (settings == null) return;

            _draft.ApplyTo(settings);
            settings.Sanitize();
            _draft = Draft.FromSettings(settings);   
            _draftDirty = false;

            SaveSettings();
            NotifyChanged();                   
            MarkDirty();
        }

        private static void SaveSettings()
        {
            try
            {
                Settings settings = S;
                if (settings != null && !string.IsNullOrEmpty(ModContext.ModPath))
                    settings.Save(ModContext.ModPath);
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("[LogGraphSettingsUI] Failed to save settings: " + e.Message);
            }
        }

        private void MarkDirty()
        {
            if (_graphic != null) _graphic.MarkDirty();
        }


        internal sealed class SettingsPanelGraphic : MaskableGraphic
        {
            private LogGraphSettingsUI _owner;

            protected override void Awake()
            {
                base.Awake();
                color = Color.white;
                raycastTarget = false;     
                useLegacyMeshGeneration = false;
            }

            internal void Bind(LogGraphSettingsUI owner)
            {
                _owner = owner;
            }

            internal void MarkDirty()
            {
                SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                if (_owner == null) return;

                RectTransform rect = rectTransform;
                if (rect == null) return;

                Rect area = rect.rect;
                float w = area.width;
                float h = area.height;
                if (w <= 0f || h <= 0f) return;

                float x0 = area.xMin;
                float y0 = area.yMin;
                float x1 = area.xMax;
                float y1 = area.yMax;

                float scale = UiScale;
                float border = Mathf.Max(1f, 1.5f * scale);

                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x1, y1), PanelColor);
                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x0 + border, y1), BorderColor);
                DrawQuad(vh, new Vector2(x1 - border, y0), new Vector2(x1, y1), BorderColor);

                List<Row> rows = _owner._rows;
                for (int i = 0; i < rows.Count; i++)
                {
                    Row row = rows[i];

                    if (ReferenceEquals(row, _owner._hoverRow) && row.Kind != RowKind.Title)
                        DrawQuad(vh, row.Rect.min, row.Rect.max, RowHoverColor);

                    switch (row.Kind)
                    {
                        case RowKind.Tab:
                            DrawQuad(vh, row.Rect.min, row.Rect.max,
                                _owner._tab == row.TabIndex ? TabActiveColor : TabColor);
                            break;

                        case RowKind.Button:
                            DrawButton(vh, row, scale, _owner._draftDirty);
                            break;

                        case RowKind.Slider:
                            DrawSlider(vh, row, scale);
                            break;

                        case RowKind.ColorPick:
                            DrawColorChip(vh, row, scale, _owner._colorIndex == row.ColorIndex);
                            break;

                        case RowKind.ColorPreview:
                            DrawColorPreview(vh, row, scale);
                            break;

                        case RowKind.Toggle:
                            DrawToggle(vh, row, scale);
                            break;
                    }
                }
            }

            private static void DrawSlider(VertexHelper vh, Row row, float scale)
            {
                if (row.Bars == null || row.Bars.Length == 0 || row.Get == null) return;

                Rect bar = row.Bars[0];
                DrawQuad(vh, bar.min, bar.max, TrackColor);

                float t = Mathf.InverseLerp(row.Min, row.Max, row.Get());
                if (row.LogScale && row.Min > 0f && row.Max > row.Min)
                    t = Mathf.Log(Mathf.Max(row.Min, row.Get()) / row.Min) / Mathf.Log(row.Max / row.Min);

                t = Mathf.Clamp01(t);
                float fillRight = bar.x + bar.width * t;
                if (fillRight > bar.x)
                    DrawQuad(vh, new Vector2(bar.x, bar.y), new Vector2(fillRight, bar.yMax), AccentColor);

                float knobWidth = Mathf.Max(3f, 3f * scale);
                float knobCenter = Mathf.Clamp(fillRight, bar.x + knobWidth * 0.5f, bar.xMax - knobWidth * 0.5f);
                DrawQuad(vh,
                    new Vector2(knobCenter - knobWidth * 0.5f, bar.y - 2f * scale),
                    new Vector2(knobCenter + knobWidth * 0.5f, bar.yMax + 2f * scale),
                    ValueColor);
            }

            private static void DrawColorChip(VertexHelper vh, Row row, float scale, bool selected)
            {
                if (row.GetColor == null) return;

                DrawQuad(vh, row.Rect.min, row.Rect.max,
                    selected ? TabActiveColor : new Color(1f, 1f, 1f, 0.035f));

                float swatch = ColorSwatchSize(row.Rect.height, scale);
                float sx = row.Rect.x + 4f * scale;
                float sy = row.Rect.y + (row.Rect.height - swatch) * 0.5f;
                Color color = row.GetColor();

                DrawQuad(vh, new Vector2(sx, sy), new Vector2(sx + swatch, sy + swatch),
                    new Color(0.5f, 0.5f, 0.5f, 1f));
                DrawQuad(vh, new Vector2(sx, sy), new Vector2(sx + swatch, sy + swatch), color);

                if (row.ResetRect.width > 0f)
                {
                    DrawQuad(vh, row.ResetRect.min, row.ResetRect.max, new Color(1f, 1f, 1f, 0.10f));

                    float bt = Mathf.Max(1f, 1.2f * scale);
                    DrawQuad(vh, row.ResetRect.min, new Vector2(row.ResetRect.xMax, row.ResetRect.yMin + bt), BorderColor);
                    DrawQuad(vh, new Vector2(row.ResetRect.xMin, row.ResetRect.yMax - bt), row.ResetRect.max, BorderColor);
                    DrawQuad(vh, row.ResetRect.min, new Vector2(row.ResetRect.xMin + bt, row.ResetRect.yMax), BorderColor);
                    DrawQuad(vh, new Vector2(row.ResetRect.xMax - bt, row.ResetRect.yMin), row.ResetRect.max, BorderColor);
                }

                float thickness = selected ? Mathf.Max(1.5f, 1.6f * scale) : Mathf.Max(1f, 1.2f * scale);
                Color edge = selected ? AccentColor : BorderColor;
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMax, row.Rect.yMin + thickness), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMin, row.Rect.yMax - thickness), row.Rect.max, edge);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMin + thickness, row.Rect.yMax), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMax - thickness, row.Rect.yMin), row.Rect.max, edge);
            }

            private static void DrawColorPreview(VertexHelper vh, Row row, float scale)
            {
                if (row.GetColor == null) return;

                DrawQuad(vh, row.Rect.min, row.Rect.max, PanelColor);
                DrawQuad(vh, row.Rect.min, row.Rect.max, row.GetColor());

                float t = Mathf.Max(1f, 1.2f * scale);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMax, row.Rect.yMin + t), BorderColor);
                DrawQuad(vh, new Vector2(row.Rect.xMin, row.Rect.yMax - t), row.Rect.max, BorderColor);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMin + t, row.Rect.yMax), BorderColor);
                DrawQuad(vh, new Vector2(row.Rect.xMax - t, row.Rect.yMin), row.Rect.max, BorderColor);
            }
            
            private static void DrawButton(VertexHelper vh, Row row, float scale, bool dirty)
            {
                Color fill = dirty ? new Color(0.20f, 0.52f, 0.88f, 0.95f) : new Color(1f, 1f, 1f, 0.10f);
                Color edge = dirty ? new Color(0.55f, 0.82f, 1f, 0.95f) : BorderColor;

                DrawQuad(vh, row.Rect.min, row.Rect.max, fill);

                float t = Mathf.Max(1f, 1.2f * scale);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMax, row.Rect.yMin + t), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMin, row.Rect.yMax - t), row.Rect.max, edge);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMin + t, row.Rect.yMax), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMax - t, row.Rect.yMin), row.Rect.max, edge);
            }

            private static void DrawToggle(VertexHelper vh, Row row, float scale)
            {
                if (row.Bars == null || row.Bars.Length == 0 || row.GetToggle == null) return;

                Rect box = row.Bars[0];
                bool on = row.GetToggle();

                DrawQuad(vh, box.min, box.max, on ? AccentColor : TrackColor);
                DrawQuad(vh,
                    new Vector2(box.x, box.y),
                    new Vector2(box.xMax, box.yMax),
                    new Color(0f, 0f, 0f, on ? 0f : 0.25f));

                if (on)
                {
                    float inset = Mathf.Max(1f, 3f * scale);
                    DrawQuad(vh,
                        new Vector2(box.x + inset, box.y + inset),
                        new Vector2(box.xMax - inset, box.yMax - inset),
                        new Color(0.05f, 0.06f, 0.09f, 1f));
                }
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
        }
    }
}
