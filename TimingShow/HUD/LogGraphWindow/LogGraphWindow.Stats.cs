using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private static readonly string[] StatLabelKeys =
        {
            "LogGraph_StatTotalHits",
            "LogGraph_StatUR",
            "LogGraph_StatRatio",
            "LogGraph_StatXAcc"
        };

        private static readonly string[] DistributionStatLabelKeys =
        {
            "LogGraph_DistMean",
            "LogGraph_DistStdDev",
            "LogGraph_DistSkewness",
            "LogGraph_DistKurtosis"
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

            PushDistributionData();

            if (_distributionView) UpdateDistributionStatTexts();
            else UpdateStatTexts();
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

            _distributionStats = DistributionStats.Compute(samples, DistributionStats.DefaultBinCount);
            _statsComputed = true;
        }
        
        private void UpdateStatTexts()
        {
            RestoreScatterStatLabels();

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

        private void RestoreScatterStatLabels()
        {
            for (int i = 0; i < StatsRowCount; i++)
            {
                if (_statLabels[i] == null) continue;
                string label = (i18n.T(StatLabelKeys[i]) ?? "").ToUpperInvariant();
                if (_statLabels[i].text != label) _statLabels[i].text = label;
            }

            if (_statsTitleText != null)
            {
                string title = i18n.T("LogGraph_StatsTitle");
                if (_statsTitleText.text != title) _statsTitleText.text = title;
            }
        }

        private void UpdateDistributionStatTexts()
        {
            DistributionStats stats = _distributionStats;
            bool hasData = stats != null && stats.HasData;
            string unit = _data != null && _data.IsAngle ? "\u00b0" : "ms";

            for (int i = 0; i < StatsRowCount; i++)
            {
                if (_statLabels[i] == null) continue;
                string label = (i18n.T(DistributionStatLabelKeys[i]) ?? "").ToUpperInvariant();
                if (_statLabels[i].text != label) _statLabels[i].text = label;
            }

            string[] values = new string[StatsRowCount];
            values[0] = hasData ? FormatStat(stats.Mean, 2) + unit : "-";
            values[1] = hasData ? FormatStat(stats.StdDev, 2) + unit : "-";
            values[2] = hasData ? FormatStat(stats.Skewness, 3) : "-";
            values[3] = hasData ? FormatStat(stats.ExcessKurtosis, 3) : "-";

            for (int i = 0; i < StatsRowCount; i++)
            {
                if (_statValues[i] != null && _statValues[i].text != values[i]) _statValues[i].text = values[i];
            }

            if (_statsTitleText != null)
            {
                int valid = hasData ? stats.ValidCount : 0;
                int clipped = hasData ? stats.ClippedCount : 0;
                string title = string.Format(i18n.T("LogGraph_DistSampleCount"), valid);
                if (clipped > 0) title = title + "  |  " + string.Format(i18n.T("LogGraph_DistClipped"), clipped);
                if (_statsTitleText.text != title) _statsTitleText.text = title;
            }
        }

        private static string FormatStat(double value, int digits)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "-";
            return value.ToString("F" + digits, System.Globalization.CultureInfo.InvariantCulture);
        }


    }
}
