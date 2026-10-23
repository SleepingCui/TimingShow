using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TimingShow.HUD.LogGraphSettingsUI
{
    public sealed partial class LogGraphSettingsUI
    {
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
            
            float rowFontSize = Mathf.Clamp(Mathf.Min(fontSize * 0.9f, rowHeight / LineHeightRatio), 8f, fontSize);

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

            rows.Add(Slider("LogGraph_CornerScale", 0f, 2f, false, "F2",
                () => _draft.CornerScale,
                v => _draft.CornerScale = v));

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
            rows.Add(Toggle("Toggle_DistinguishPlayers",
                () => _draft.DistinguishPlayers,
                v => _draft.DistinguishPlayers = v));
            rows.Add(Toggle("Toggle_ShowZeroLine",
                () => _draft.ShowZeroLine,
                v => _draft.ShowZeroLine = v));
            rows.Add(Toggle("Toggle_ShowAvgLine",
                () => _draft.ShowAvgLine,
                v => _draft.ShowAvgLine = v));
            rows.Add(Toggle("Toggle_ShowGrid",
                () => _draft.ShowGrid,
                v => _draft.ShowGrid = v));
            rows.Add(Toggle("Toggle_ShowJudgeBands",
                () => _draft.ShowJudgeBands,
                v => _draft.ShowJudgeBands = v));
            rows.Add(Toggle("Toggle_ShowXpBand",
                () => _draft.ShowXpBand,
                v => _draft.ShowXpBand = v));
            rows.Add(Slider("Label_BandThresholdBpm", 100f, 600f, false, "F0",
                () => _draft.BandThresholdBpm,
                v => _draft.BandThresholdBpm = v));
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
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            if (right)
            {
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(rect.xMax, rect.yMin);
            }
            else
            {
                rt.pivot = Vector2.zero;
                rt.anchoredPosition = new Vector2(rect.xMin, rect.yMin);
            }

            rt.sizeDelta = new Vector2(Mathf.Max(1f, rect.width), Mathf.Max(1f, rect.height));
            text.ForceMeshUpdate();
        }

        private TextMeshProUGUI CreateText(string name, string content, TextAlignmentOptions alignment,
            float fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
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
        

    }
}
