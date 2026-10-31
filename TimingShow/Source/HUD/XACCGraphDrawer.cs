using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public class XACCGraphDrawer : GraphDrawerBase
    {
        protected override bool IsEnabled => ModContext.IsEnabled && ModContext.IsPlaying;
        protected override bool ShowGraph => ModContext.Settings.ShowXACCGraph;
        protected override float Scale => ModContext.Settings.XACCGraph_Scale;
        protected override float Width => ModContext.Settings.XACCGraph_Width;
        protected override float Height => ModContext.Settings.XACCGraph_Height;
        protected override float PosX => ModContext.Settings.XACCGraph_X;
        protected override float PosY => ModContext.Settings.XACCGraph_Y;

        protected override Color BgColor => ModContext.Settings.XACCGraph_BgColor;
        protected override Color GridColor => ModContext.Settings.XACCGraph_GridColor;
        protected override Color LineColor => ModContext.Settings.XACCGraph_LineColor;

        protected override bool ShowBorder => ModContext.Settings != null && ModContext.Settings.XACCGraph_ShowBorder;
        protected override Color BorderColor => ModContext.Settings != null ? ModContext.Settings.XACCGraph_BorderColor : new Color(1f, 1f, 1f, 0.35f);

        protected override int MaxPoints => ModContext.Settings.XACCGraph_MaxPoints > 0 ? ModContext.Settings.XACCGraph_MaxPoints : 250;
        public override string GraphName => "XACC";

        protected override bool UseCustomFont => ModContext.Settings != null && ModContext.Settings.XACCGraph_UseCustomFont;
        protected override string FontPath => ModContext.Settings != null ? ModContext.Settings.XACCGraph_FontPath : "";

        protected override bool ShowPerfInfo => ModContext.Settings != null && ModContext.Settings.XACCGraph_ShowPerfInfo;

        protected override int PerfDataCount
        {
            get
            {
                int total = 0;
                int count = SeriesCount;
                for (int i = 0; i < count; i++)
                {
                    List<float> history = ModContext.FullXAccHistory[i];
                    if (history != null) total += history.Count;
                }
                return total;
            }
        }

        private const float MinYRange = 0.1f;

        private readonly List<float>[] _seriesCache = CreateSeriesCaches();

        private static List<float>[] CreateSeriesCaches()
        {
            List<float>[] all = new List<float>[Players.MaxPlayers];
            for (int i = 0; i < all.Length; i++)
                all[i] = new List<float>();
            return all;
        }

        private int _seriesCount = 1;
        private float _cachedMinY;
        private float _cachedMaxY;

        private TMP_Text _legendText;
        private readonly StringBuilder _legendBuilder = new StringBuilder(64);

        private int SeriesCount
        {
            get
            {
                int count = Coop.ActiveCount;
                if (count < 1) count = 1;
                if (count > Players.MaxPlayers) count = Players.MaxPlayers;
                return count;
            }
        }

        private bool ShowLegend => SeriesCount > 1;

        protected override void CreateTextComponents()
        {
            base.CreateTextComponents();

            _legendText = CreateText("LegendText", CurrentFontAsset, TextAnchor.UpperRight);
            _legendText.richText = true;
            _legendText.color = Color.white;
        }

        protected override void ApplyFontToTexts(TMP_FontAsset font)
        {
            base.ApplyFontToTexts(font);
            ApplyFontToText(_legendText, font);
        }

        protected override void ToggleTexts(bool active)
        {
            base.ToggleTexts(active);

            if (_legendText == null) return;
            bool legendActive = active && ShowLegend;
            if (_legendText.gameObject.activeSelf != legendActive) _legendText.gameObject.SetActive(legendActive);
        }

        public override void CollectBoundsRectangles(List<KeyValuePair<string, RectTransform>> targets)
        {
            base.CollectBoundsRectangles(targets);
            AddBoundsTarget(targets, "LegendText", _legendText);
        }

        protected override int ComputeSettingsHash()
        {
            unchecked
            {
                return base.ComputeSettingsHash() * 31 + SeriesCount;
            }
        }

        protected override void UpdateData()
        {
            _seriesCount = SeriesCount;
            for (int s = 0; s < _seriesCount; s++) _seriesCache[s].Clear();

            bool hasAny = false;
            for (int s = 0; s < _seriesCount; s++)
            {
                if (BuildSeriesCache(s)) hasAny = true;
            }

            if (!hasAny) return;
            ComputeYRange();
        }

        private bool BuildSeriesCache(int seriesIndex)
        {
            List<float> source = ModContext.FullXAccHistory[seriesIndex];
            List<float> cache = _seriesCache[seriesIndex];
            cache.Clear();
            if (source == null || source.Count == 0) return false;

            int total = source.Count;
            int targetPoints = MaxPoints;

            if (ModContext.IsLevelFinished)
            {
                if (total <= targetPoints)
                {
                    cache.AddRange(source);
                }
                else
                {
                    float step = (float)(total - 1) / (targetPoints - 1);
                    for (int i = 0; i < targetPoints; i++)
                    {
                        int idx = Mathf.Clamp((int)(i * step), 0, total - 1);
                        cache.Add(source[idx]);
                    }
                }
            }
            else
            {
                int startIdx = Math.Max(0, total - targetPoints);
                for (int i = startIdx; i < total; i++)
                {
                    cache.Add(source[i]);
                }
            }

            return cache.Count > 0;
        }

        private void ComputeYRange()
        {
            bool hasAny = false;
            float minVal = 100f;
            float maxVal = 0f;

            for (int s = 0; s < _seriesCount; s++)
            {
                List<float> series = _seriesCache[s];
                for (int i = 0; i < series.Count; i++)
                {
                    float v = series[i];
                    if (v < minVal) minVal = v;
                    if (v > maxVal) maxVal = v;
                    hasAny = true;
                }
            }

            if (!hasAny)
            {
                _cachedMinY = 0f;
                _cachedMaxY = 100f;
                return;
            }

            if (ModContext.IsLevelFinished)
            {
                _cachedMaxY = 100f;
                _cachedMinY = Mathf.Max(0f, Mathf.Floor(minVal - 1f));
                if (100f - minVal < 1.0f)
                {
                    _cachedMinY = Mathf.Max(0f, (float)Math.Floor(minVal * 10f) / 10f);
                }

                if (_cachedMaxY - _cachedMinY < MinYRange)
                {
                    _cachedMinY = Mathf.Max(0f, _cachedMaxY - 1f);
                }
            }
            else
            {
                if (Mathf.Abs(maxVal - minVal) < 0.2f)
                {
                    _cachedMinY = Mathf.Max(0f, minVal - 1.0f);
                    _cachedMaxY = Mathf.Min(100f, maxVal + 1.0f);
                }
                else
                {
                    _cachedMinY = Mathf.Max(0f, minVal - 1.5f);
                    _cachedMaxY = Mathf.Min(100f, maxVal + 1.5f);
                }
            }
        }

        protected override int GetDataCount()
        {
            int total = 0;
            int count = _seriesCount;
            for (int s = 0; s < count; s++) total += _seriesCache[s].Count;
            return total;
        }

        protected override float GetDataValue(int index)
        {
            for (int s = 0; s < _seriesCount; s++)
            {
                List<float> series = _seriesCache[s];
                if (index < series.Count) return series[index];
                index -= series.Count;
            }
            return 0f;
        }

        protected override float GetMinY() => _cachedMinY;
        protected override float GetMaxY() => _cachedMaxY;

        protected override void DrawSeries(VertexHelper vh, float w, float h)
        {
            float minY = GetMinY();
            float maxY = GetMaxY();
            float rangeY = Mathf.Max(0.01f, maxY - minY);
            int maxCapacity = MaxPoints > 0 ? MaxPoints : 250;
            float stepX = w / Mathf.Max(1, maxCapacity - 1);
            float lineWidth = 2.0f * Scale;

            int count = _seriesCount;
            for (int s = 0; s < count; s++)
            {
                List<float> series = _seriesCache[s];
                int points = series.Count;
                if (points < 2) continue;

                Color color = LineColorFor(s);
                Vector2 prevPoint = Vector2.zero;

                for (int i = 0; i < points; i++)
                {
                    float normY = Mathf.Clamp01((series[i] - minY) / rangeY);
                    Vector2 curPoint = new Vector2(i * stepX, normY * h);
                    if (i > 0)
                        DrawSegment(vh, prevPoint, curPoint, lineWidth * 0.5f, color);
                    prevPoint = curPoint;
                }
            }
        }

        private Color LineColorFor(int seriesIndex)
        {
            if (SeriesCount <= 1) return LineColor;
            return Coop.PlayerColor(seriesIndex);
        }

        protected override void UpdateTextLayoutAndValues()
        {
            base.UpdateTextLayoutAndValues();
            UpdateLegend();
        }

        private void UpdateLegend()
        {
            if (_legendText == null || !ShowLegend) return;

            int count = SeriesCount;
            float scale = Mathf.Max(0.01f, Scale);
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;

            _legendBuilder.Length = 0;
            for (int i = 0; i < count; i++)
            {
                if (i > 0) _legendBuilder.Append("  ");
                _legendBuilder.Append("<color=#");
                _legendBuilder.Append(ColorToHex(LineColorFor(i)));
                _legendBuilder.Append('>');
                _legendBuilder.Append(Coop.Label(i));
                _legendBuilder.Append("</color>");
            }

            string content = _legendBuilder.ToString();
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(12f * scale), 8, 32);
            _legendText.fontSize = fontSize;
            if (_legendText.text != content) _legendText.text = content;
            _legendText.color = Color.white;
            _legendText.alignment = TextAlignmentOptions.TopRight;

            RectTransform rt = _legendText.rectTransform;
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f * scale, h - 2f * scale);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, w * 0.8f), Mathf.Max(1f, h * 0.3f));
        }

        private static string ColorToHex(Color color)
        {
            Color32 c = color;
            return c.r.ToString("X2", CultureInfo.InvariantCulture)
                 + c.g.ToString("X2", CultureInfo.InvariantCulture)
                 + c.b.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
