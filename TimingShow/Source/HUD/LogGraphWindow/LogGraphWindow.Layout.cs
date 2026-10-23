using System.IO;
using System.Text;
using UnityEngine;
using TMPro;
using TimingShow.Logging;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
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
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private void ApplyHeaderFonts(TMP_FontAsset asset)
        {
            ApplyTitleFont(_titleText, asset);
            ApplyTitleFont(_fileText, asset);
            ApplyTitleFont(_metaText, asset);
            if (_chartButton != null) _chartButton.SetFont(asset);
            if (_playerButton != null) _playerButton.SetFont(asset);
            if (_chartMenu != null) _chartMenu.SetFont(asset);
            if (_playerMenu != null) _playerMenu.SetFont(asset);
        }

        private static void ApplyTitleFont(TextMeshProUGUI text, TMP_FontAsset asset)
        {
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
            PositionSelectorAndMenus(size);
            RefreshTitle();
            
            bool settingsOpen = _settingsUI != null && _settingsUI.IsOpen;
            
            ResolveGraphLayout(size.x, size.y);
            Rect container = _resolvedContainerRect;
            Rect plot = _resolvedPlotRect;
            
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
            
            bool hasPlayer = _playerCount > 1;
            Rect chartRect;
            Rect playerRect;
            SelectorRects(size, hasPlayer, out chartRect, out playerRect);
            float selectorsLeft = hasPlayer ? playerRect.xMin : chartRect.xMin;

            float metaWidth = Mathf.Min(MetaWidth, Mathf.Max(0f, (size.x - TitleLeftPadding - TitleRightPadding) * 0.4f));
            float titleWidth = Mathf.Max(0f,
                selectorsLeft - (hasMeta ? metaWidth + TitleMetaGap * 2f : TitleMetaGap) - TitleLeftPadding);

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
                rect.anchoredPosition = new Vector2(selectorsLeft - TitleMetaGap, -pad);
                rect.sizeDelta = new Vector2(metaWidth, titleHeight);
            }

            _titleText.ForceMeshUpdate();
            if (_fileText != null && _fileText.gameObject.activeSelf) _fileText.ForceMeshUpdate();
            if (_metaText != null && _metaText.gameObject.activeSelf) _metaText.ForceMeshUpdate();
        }

        private static float ChartSelectorWidth { get { return 150f * UiScale; } }
        private static float PlayerSelectorWidth { get { return 64f * UiScale; } }
        private static float SelectorGap { get { return 4f * UiScale; } }

        private static float SelectorHeight
        {
            get { return Mathf.Clamp(20f * UiScale, 12f, Mathf.Max(12f, HeaderHeight - HeaderPadding * 2f)); }
        }

        private static void SelectorRects(Vector2 size, bool hasPlayer, out Rect chart, out Rect player)
        {
            float height = SelectorHeight;
            float y = size.y - HeaderPadding - height;
            float right = size.x - TitleRightPadding;

            chart = new Rect(right - ChartSelectorWidth, y, ChartSelectorWidth, height);
            player = hasPlayer
                ? new Rect(chart.xMin - SelectorGap - PlayerSelectorWidth, y, PlayerSelectorWidth, height)
                : new Rect(0f, 0f, 0f, 0f);
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
            if (_playerCount > 1) valueFontSize = Mathf.Max(8f, valueFontSize * 0.8f);

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
        
        private void OnGraphAxisMetricsChanged()
        {
            if (_windowRect == null) return;

            ApplyLayout(_windowRect.sizeDelta, _windowRect.anchoredPosition);
        }

        private void ApplySettingsLayout(Vector2 size)
        {
            if (_settingsUI == null) return;

            Rect settings = SettingsRect(size.x, size.y);
            _settingsUI.ApplyLayout(settings.x, settings.y, settings.width, settings.height);
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
        
    }
}
