using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TimingShow.Logging;

namespace TimingShow.HUD
{
    public class LogOverviewDrawer : LogChartDrawer
    {
        private static readonly int[] Game34Order = { 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15 };
        private static readonly int[] LegacyOrder = { 9, 0, 1, 2, 4, 3, 6, 7, 8, 10, 11, 12, 13 };
        private static readonly string[] MarginLabels =
        {
            "TooEarly", "VeryEarly", "EarlyPerfect", "Perfect", "XPerfect", "PerfectPlus",
            "LatePerfect", "VeryLate", "TooLate", "Multipress", "FailMiss", "FailOverload",
            "Auto", "OverPress", "Midspin", "FailedFloor"
        };
        private static readonly Color[] MarginColors =
        {
            new Color(1f, 0f, 0f, 1f),
            new Color(1f, 0.435f, 0.306f, 1f),
            new Color(0.627f, 1f, 0.306f, 1f),
            new Color(0.376f, 1f, 0.306f, 1f),
            new Color(0.302f, 0.800f, 1f, 1f),
            new Color(0.376f, 1f, 0.306f, 1f),
            new Color(0.627f, 1f, 0.306f, 1f),
            new Color(1f, 0.435f, 0.306f, 1f),
            new Color(1f, 0f, 0f, 1f),
            new Color(0f, 1f, 0.929f, 1f),
            new Color(0.851f, 0.345f, 1f, 1f),
            new Color(0.851f, 0.345f, 1f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(0.851f, 0.345f, 1f, 1f),
            new Color(0.533f, 0.533f, 0.533f, 1f),
            new Color(0.851f, 0.345f, 1f, 1f)
        };
        private static readonly Color[] CardColors =
        {
            new Color(0.129f, 0.588f, 0.953f, 1f),
            new Color(1f, 0.718f, 0.302f, 1f),
            new Color(0.851f, 0.345f, 1f, 1f),
            new Color(0.376f, 1f, 0.306f, 1f)
        };
        private static readonly Color PanelColor = new Color(1f, 1f, 1f, 0.035f);
        private static readonly Color PanelBorderColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color MutedTextColor = new Color(0.60f, 0.64f, 0.70f, 1f);
        private static readonly Color ValueTextColor = new Color(0.96f, 0.98f, 1f, 1f);
        private static readonly Color XaccColor = new Color(0.376f, 1f, 0.306f, 1f);
        private static readonly Color XaccBgColor = new Color(0.376f, 1f, 0.306f, 0.10f);
        private static readonly Color XaccBorderColor = new Color(0.376f, 1f, 0.306f, 0.25f);

        private const int CardCount = 4;
        private const int MaxMargins = 16;
        private const int MaxMetaEntries = 16;

        private struct MetaEntry
        {
            public string Label;
            public string Value;
            public bool Warning;
        }

        private OverviewData _data;
        private TimingLogData _log;
        private readonly int[] _visibleCards = new int[CardCount];
        private int _visibleCardCount;
        private readonly Rect[] _cardRects = new Rect[CardCount];
        private readonly Color[] _cardAccent = new Color[CardCount];
        private Rect _marginRect;
        private Rect _xaccRect;
        private Rect _metaRect;
        private readonly List<int> _marginCodes = new List<int>(MaxMargins);
        private readonly List<MetaEntry> _metaEntries = new List<MetaEntry>(MaxMetaEntries);

        private TextMeshProUGUI[] _cardTitles;
        private TextMeshProUGUI[] _cardValues;
        private TextMeshProUGUI[] _cardSubs;
        private TextMeshProUGUI[] _marginLabelTexts;
        private TextMeshProUGUI[] _marginValueTexts;
        private TextMeshProUGUI[] _metaColumnLabels;
        private TextMeshProUGUI _xaccLabel;
        private TextMeshProUGUI _emptyLabel;
        private const int MaxMetaColumns = 4;
        private readonly StringBuilder _metaBuilder = new StringBuilder(512);

        protected override void CreateTextComponents()
        {
            _cardTitles = new TextMeshProUGUI[CardCount];
            _cardValues = new TextMeshProUGUI[CardCount];
            _cardSubs = new TextMeshProUGUI[CardCount];
            for (int i = 0; i < CardCount; i++)
            {
                _cardTitles[i] = CreateText("CardTitle" + i, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
                _cardValues[i] = CreateText("CardValue" + i, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
                _cardSubs[i] = CreateText("CardSub" + i, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f));
            }

            _marginLabelTexts = new TextMeshProUGUI[MaxMargins];
            _marginValueTexts = new TextMeshProUGUI[MaxMargins];
            for (int i = 0; i < MaxMargins; i++)
            {
                _marginLabelTexts[i] = CreateText("MarginLabel" + i, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
                _marginValueTexts[i] = CreateText("MarginValue" + i, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
            }

            _metaColumnLabels = new TextMeshProUGUI[MaxMetaColumns];
            for (int i = 0; i < MaxMetaColumns; i++)
            {
                _metaColumnLabels[i] = CreateText("MetaColumn" + i, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
                _metaColumnLabels[i].richText = true;
                _metaColumnLabels[i].textWrappingMode = TextWrappingModes.Normal;
                _metaColumnLabels[i].overflowMode = TextOverflowModes.Truncate;
            }

            _xaccLabel = CreateText("XaccLabel", TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
            _emptyLabel = CreateText("EmptyLabel", TextAlignmentOptions.Midline, new Vector2(0.5f, 0.5f));
        }

        internal void SetData(OverviewData data, TimingLogData log)
        {
            if (ReferenceEquals(_data, data) && ReferenceEquals(_log, log)) return;
            _data = data;
            _log = log;
            InvalidateLayout();
        }

        protected override void ComputeLayout(float width, float height)
        {
            HasPlot = width > 20f && height > 20f;
        }

        protected override void ApplyTextLayout(float width, float height)
        {
            if (!HasPlot) return;

            bool hasData = _data != null && _data.HasData;
            _emptyLabel.gameObject.SetActive(!hasData);
            if (!hasData)
            {
                SetText(_emptyLabel, i18n.T("LogGraph_OverviewNoData"));
                _emptyLabel.color = AxisTextColor;
                _emptyLabel.fontSize = FontSize;
                Place(_emptyLabel, new Vector2(width * 0.5f, height * 0.5f),
                    new Vector2(Mathf.Max(40f, width - 20f), FontSize * 2f), new Vector2(0.5f, 0.5f));
                HideAllContent();
                return;
            }

            LayoutCards(width, height);
            LayoutMarginBox(width, height);
            LayoutMetaPanel(width, height);
        }

        private void HideAllContent()
        {
            for (int i = 0; i < CardCount; i++)
            {
                SetActive(_cardTitles[i], false);
                SetActive(_cardValues[i], false);
                SetActive(_cardSubs[i], false);
            }
            for (int i = 0; i < MaxMargins; i++)
            {
                SetActive(_marginLabelTexts[i], false);
                SetActive(_marginValueTexts[i], false);
            }
            SetActive(_xaccLabel, false);
            for (int i = 0; i < MaxMetaColumns; i++)
                SetActive(_metaColumnLabels[i], false);
        }

        private void LayoutCards(float width, float height)
        {
            float pad = 12f * UiScale;
            float gap = 8f * UiScale;
            float cardHeight = 68f * UiScale;

            _visibleCards[0] = 0;
            _visibleCards[1] = 1;
            _visibleCards[2] = -1;
            _visibleCards[3] = 3;
            _visibleCardCount = 4;
            if (!_data.UrAvailable)
            {
                _visibleCards[2] = 3;
                _visibleCardCount = 3;
            }
            else
            {
                _visibleCards[2] = 2;
            }

            float cardWidth = (width - pad * 2f - gap * (_visibleCardCount - 1)) / _visibleCardCount;
            float top = height - pad;

            for (int i = 0; i < CardCount; i++)
            {
                int slot = -1;
                for (int k = 0; k < _visibleCardCount; k++)
                {
                    if (_visibleCards[k] == i) slot = k;
                }

                TextMeshProUGUI title = _cardTitles[i];
                TextMeshProUGUI value = _cardValues[i];
                TextMeshProUGUI sub = _cardSubs[i];

                if (slot < 0)
                {
                    SetActive(title, false);
                    SetActive(value, false);
                    SetActive(sub, false);
                    _cardRects[i] = new Rect(0f, 0f, 0f, 0f);
                    continue;
                }

                float x = pad + slot * (cardWidth + gap);
                Rect rect = new Rect(x, top - cardHeight, cardWidth, cardHeight);
                _cardRects[i] = rect;
                _cardAccent[i] = CardColors[i];

                SetActive(title, true);
                SetActive(value, true);
                SetActive(sub, i == 3);

                title.fontSize = FontSize * 0.78f;
                title.color = MutedTextColor;
                Place(title, new Vector2(rect.x + 10f * UiScale, rect.yMax - 5f * UiScale),
                    new Vector2(cardWidth - 20f * UiScale, 16f * UiScale), new Vector2(0f, 1f));

                value.fontSize = FontSize * 1.35f;
                value.color = ValueTextColor;
                Place(value, new Vector2(rect.x + 10f * UiScale, rect.yMax - 24f * UiScale),
                    new Vector2(cardWidth - 20f * UiScale, 30f * UiScale), new Vector2(0f, 1f));

                if (i == 3 && sub != null)
                {
                    sub.fontSize = FontSize * 0.72f;
                    sub.color = MutedTextColor;
                    Place(sub, new Vector2(rect.x + 10f * UiScale, rect.y + 5f * UiScale),
                        new Vector2(cardWidth - 20f * UiScale, 14f * UiScale), new Vector2(0f, 0f));
                }
            }

            SetText(_cardTitles[0], i18n.T("LogGraph_OverviewTotalHits"));
            SetText(_cardValues[0], _data.TotalHits.ToString("#,0", CultureInfo.InvariantCulture));

            SetText(_cardTitles[1], i18n.T("LogGraph_OverviewMaxCombo"));
            SetText(_cardValues[1], _data.MaxCombo.ToString(CultureInfo.InvariantCulture));

            if (_data.UrAvailable)
            {
                SetText(_cardTitles[2], i18n.T("LogGraph_OverviewUR"));
                SetText(_cardValues[2], _data.Ur.ToString("F2", CultureInfo.InvariantCulture));
            }

            SetText(_cardTitles[3], i18n.T("LogGraph_OverviewRatio"));
            SetText(_cardValues[3], FormatRatio(_data.RatioNumerator, _data.RatioDenominator));
            SetText(_cardSubs[3], i18n.T("LogGraph_OverviewXPerfectRatio") + "  "
                + FormatRatio(_data.XPerfectNumerator, _data.XPerfectDenominator));
        }

        private void LayoutMarginBox(float width, float height)
        {
            float pad = 12f * UiScale;
            float gap = 10f * UiScale;
            float cardsBottom = height - pad - 68f * UiScale;
            float boxTop = cardsBottom - gap * 1.5f;
            float xaccWidth = 160f * UiScale;
            float itemWidth = 78f * UiScale;

            BuildMarginCodes();
            int columns = Mathf.Max(1, Mathf.FloorToInt((width - pad * 2f - xaccWidth - gap) / itemWidth));
            int rows = Mathf.Max(1, (Mathf.Max(1, _marginCodes.Count) + columns - 1) / columns);
            float rowHeight = 46f * UiScale;
            float boxHeight = Mathf.Max(72f * UiScale, rows * rowHeight + 20f * UiScale);

            _marginRect = new Rect(pad, boxTop - boxHeight, width - pad * 2f, boxHeight);
            _xaccRect = new Rect(_marginRect.xMax - xaccWidth - 12f * UiScale,
                _marginRect.y + 10f * UiScale, xaccWidth, boxHeight - 20f * UiScale);

            for (int i = 0; i < MaxMargins; i++)
            {
                if (i >= _marginCodes.Count)
                {
                    SetActive(_marginLabelTexts[i], false);
                    SetActive(_marginValueTexts[i], false);
                    continue;
                }

                int code = _marginCodes[i];
                int col = i % columns;
                int row = i / columns;
                float x = _marginRect.x + 12f * UiScale + col * itemWidth;
                float top = _marginRect.yMax - 12f * UiScale - row * rowHeight;

                TextMeshProUGUI label = _marginLabelTexts[i];
                TextMeshProUGUI value = _marginValueTexts[i];
                SetActive(label, true);
                SetActive(value, true);

                Color color = MarginColors[code];
                label.fontSize = FontSize * 0.72f;
                label.color = color;
                Place(label, new Vector2(x, top),
                    new Vector2(itemWidth - 6f * UiScale, 15f * UiScale), new Vector2(0f, 1f));
                SetText(label, MarginLabel(code));

                value.fontSize = FontSize * 1.2f;
                value.color = color;
                Place(value, new Vector2(x, top - 16f * UiScale),
                    new Vector2(itemWidth - 6f * UiScale, 22f * UiScale), new Vector2(0f, 1f));
                SetText(value, _data.Counts[code].ToString(CultureInfo.InvariantCulture));
            }

            SetActive(_xaccLabel, true);
            _xaccLabel.fontSize = FontSize * 1.35f;
            _xaccLabel.color = XaccColor;
            Place(_xaccLabel, new Vector2(_xaccRect.center.x, _xaccRect.center.y),
                new Vector2(_xaccRect.width, _xaccRect.height), new Vector2(0.5f, 0.5f));
            SetText(_xaccLabel, "XACC: " + _data.Xacc.ToString("F2", CultureInfo.InvariantCulture) + "%");
        }

        private void LayoutMetaPanel(float width, float height)
        {
            float pad = 12f * UiScale;
            float gap = 10f * UiScale;
            float metaTop = _marginRect.yMin - gap;
            _metaRect = new Rect(pad, pad, width - pad * 2f, Mathf.Max(1f, metaTop - pad));

            BuildMetaEntries();

            int count = Mathf.Min(_metaEntries.Count, MaxMetaEntries);
            if (count == 0)
            {
                for (int i = 0; i < MaxMetaColumns; i++)
                    SetActive(_metaColumnLabels[i], false);
                return;
            }

            float innerWidth = Mathf.Max(1f, _metaRect.width - 24f * UiScale);
            float minColumnWidth = 240f * UiScale;
            float columnGap = 22f * UiScale;
            int columns = Mathf.Clamp(
                Mathf.FloorToInt((innerWidth + columnGap) / (minColumnWidth + columnGap)),
                1, MaxMetaColumns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            float columnWidth = Mathf.Max(1f,
                (innerWidth - columnGap * (columns - 1)) / columns);

            float availableHeight = Mathf.Max(1f, _metaRect.height - 20f * UiScale);
            float fontSize = Mathf.Clamp(
                Mathf.Min(FontSize * 0.9f, availableHeight / Mathf.Max(1f, rows * 1.3f)),
                8f, FontSize);
            string keyHex = ColorToHex(MutedTextColor);
            string valueHex = ColorToHex(ValueTextColor);
            string warningHex = ColorToHex(new Color(1f, 0.78f, 0.32f, 1f));

            for (int column = 0; column < columns; column++)
            {
                int start = column * rows;
                if (start >= count) break;

                _metaBuilder.Length = 0;
                int end = Mathf.Min(count, start + rows);
                for (int i = start; i < end; i++)
                {
                    if (_metaBuilder.Length > 0) _metaBuilder.Append('\n');

                    MetaEntry entry = _metaEntries[i];
                    _metaBuilder.Append("<color=#");
                    _metaBuilder.Append(keyHex);
                    _metaBuilder.Append('>');
                    _metaBuilder.Append(entry.Label);
                    _metaBuilder.Append("</color>  <color=#");
                    _metaBuilder.Append(entry.Warning ? warningHex : valueHex);
                    _metaBuilder.Append('>');
                    _metaBuilder.Append(entry.Value);
                    _metaBuilder.Append("</color>");
                }

                TextMeshProUGUI label = _metaColumnLabels[column];
                SetActive(label, true);
                label.fontSize = fontSize;
                label.color = AxisTextColor;
                Place(label, new Vector2(_metaRect.x + 12f * UiScale + column * (columnWidth + columnGap),
                        _metaRect.yMax - 10f * UiScale),
                    new Vector2(columnWidth, availableHeight), new Vector2(0f, 1f));
                SetText(label, _metaBuilder.ToString());
            }

            for (int i = columns; i < MaxMetaColumns; i++)
                SetActive(_metaColumnLabels[i], false);
        }

        private void BuildMarginCodes()
        {
            _marginCodes.Clear();
            if (_data == null) return;

            int[] order = IsGame34Log() ? Game34Order : LegacyOrder;
            for (int i = 0; i < order.Length && _marginCodes.Count < MaxMargins; i++)
            {
                int code = order[i];
                int count = _data.Counts[code];
                if ((code == 12 || code == 10) && count == 0) continue;
                _marginCodes.Add(code);
            }
        }

        private bool IsGame34Log()
        {
            if (_log != null && !string.IsNullOrEmpty(_log.HitMarginVersionText))
                return _log.HitMarginVersionText.IndexOf("Game34", StringComparison.OrdinalIgnoreCase) >= 0;
            return HitMarginCompat.IsGame34;
        }

        private string MarginLabel(int code)
        {
            if (code == 3 && IsGame34Log()) return "PerfectMinus";
            return MarginLabels[code];
        }

        private void BuildMetaEntries()
        {
            _metaEntries.Clear();
            if (_log == null)
            {
                ModLog.UnityLog("[LogOverview] metadata unavailable: log data is null");
                return;
            }

            AddMeta(i18n.T("LogGraph_MetaSong"), _log.SongName);
            AddMeta(i18n.T("LogGraph_MetaLevelPath"), _log.LevelPath);
            if (!string.IsNullOrEmpty(_log.FilePath))
            {
                string file;
                try { file = Path.GetFileName(_log.FilePath); }
                catch { file = _log.FilePath; }
                AddMeta(i18n.T("LogGraph_MetaFile"), file);
            }
            AddMeta(i18n.T("LogGraph_MetaFormatVersion"),
                _log.FormatVersion > 0 ? "v" + _log.FormatVersion.ToString(CultureInfo.InvariantCulture) : "Unknown");
            AddMeta(i18n.T("LogGraph_MetaAnalysisTime"), FormatTimestamp(_log.Timestamp));
            AddMeta(i18n.T("LogGraph_MetaJudgeVersion"),
                _log.JudgeCodeVersion > 0 ? "v" + _log.JudgeCodeVersion.ToString(CultureInfo.InvariantCulture) : "Unknown");
            AddMeta(i18n.T("LogGraph_MetaHitMarginVersion"),
                string.IsNullOrEmpty(_log.HitMarginVersionText) ? "Unknown" : _log.HitMarginVersionText);
            AddMeta(i18n.T("LogGraph_MetaBpm"), FormatNumber(_log.Bpm, "F2"));
            AddMeta(i18n.T("LogGraph_MetaSpeed"), FormatNumber(_log.Speed, "F2"));
            AddMeta(i18n.T("LogGraph_MetaPitch"), FormatNumber(_log.Pitch, "F3"));
            AddMeta(i18n.T("LogGraph_MetaValueMode"),
                i18n.T(_log.IsAngle ? "LogGraph_MetaAngle" : "LogGraph_MetaTiming"));
            AddMeta(i18n.T("LogGraph_MetaPlayerCount"), _log.PlayerCount.ToString(CultureInfo.InvariantCulture));
            AddMeta(i18n.T("LogGraph_MetaRecordCount"),
                _data != null ? _data.TotalHits.ToString("#,0", CultureInfo.InvariantCulture) : "0");
            if (_log.Truncated)
                AddMeta(i18n.T("LogGraph_MetaTruncated"), "⚠", true);
        }

        private void AddMeta(string label, string value, bool warning = false)
        {
            if (_metaEntries.Count >= MaxMetaEntries) return;

            MetaEntry entry = new MetaEntry();
            entry.Label = label;
            entry.Value = string.IsNullOrEmpty(value) ? "Unknown" : value;
            entry.Warning = warning;
            _metaEntries.Add(entry);
        }

        private static string FormatTimestamp(long timestamp)
        {
            if (timestamp <= 0) return "Unknown";
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string FormatNumber(double value, string format)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value == 0.0) return "-";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string FormatRatio(int numerator, int denominator)
        {
            if (numerator <= 0) return "0:1";
            if (denominator <= 0) return "∞:1";
            double ratio = (double)numerator / denominator;
            double rounded = Math.Round(ratio);
            string text = Math.Abs(ratio - rounded) < 1e-9
                ? rounded.ToString(CultureInfo.InvariantCulture)
                : ratio.ToString("F2", CultureInfo.InvariantCulture);
            return text + ":1";
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect local = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
            float width = local.width;
            float height = local.height;
            if (!(width > 0f) || !(height > 0f)) return;

            EnsureLayout();
            if (TextDirty)
            {
                TextDirty = false;
                ApplyTextLayout(LastWidth, LastHeight);
            }

            DrawQuad(vh, Vector2.zero, new Vector2(width, height), BgColor);

            if (_data == null || !_data.HasData || !HasPlot) return;

            for (int i = 0; i < CardCount; i++)
            {
                Rect rect = _cardRects[i];
                if (rect.width <= 0f || rect.height <= 0f) continue;

                DrawRect(vh, rect, PanelColor, PanelBorderColor, 1f);
                float accentWidth = 3f * UiScale;
                DrawQuad(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin + accentWidth, rect.yMax), _cardAccent[i]);
            }

            DrawRect(vh, _marginRect, PanelColor, PanelBorderColor, 1f);
            DrawRect(vh, _xaccRect, XaccBgColor, XaccBorderColor, 1f);
            DrawRect(vh, _metaRect, PanelColor, PanelBorderColor, 1f);
        }

        private void DrawRect(VertexHelper vh, Rect rect, Color fill, Color border, float borderWidth)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;

            DrawQuad(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMax), fill);
            DrawQuad(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin + borderWidth), border);
            DrawQuad(vh, new Vector2(rect.xMin, rect.yMax - borderWidth), new Vector2(rect.xMax, rect.yMax), border);
            DrawQuad(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin + borderWidth, rect.yMax), border);
            DrawQuad(vh, new Vector2(rect.xMax - borderWidth, rect.yMin), new Vector2(rect.xMax, rect.yMax), border);
        }
    }
}
