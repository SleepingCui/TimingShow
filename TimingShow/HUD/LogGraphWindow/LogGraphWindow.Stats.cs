using System.Collections.Generic;
using System.Text;
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
            text.richText = true;
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

            _playerCount = _data != null && _data.PlayerCount > 0 ? _data.PlayerCount : 1;
            if (_playerCount > 4) _playerCount = 4;

            for (int p = 0; p < 4; p++)
            {
                _statTotalByPlayer[p] = 0;
                _statNormalPerfectByPlayer[p] = 0;
                _statPerfectFamilyByPlayer[p] = 0;
                _statXPerfectByPlayer[p] = 0;
                _statUrByPlayer[p] = 0.0;
            }

            List<TimingScatterSample> samples = _data != null ? _data.Samples : null;
            if (samples != null)
            {
                List<double> all = new List<double>(samples.Count);
                List<double>[] perPlayer = new List<double>[_playerCount];
                for (int p = 0; p < _playerCount; p++) perPlayer[p] = new List<double>();

                for (int i = 0; i < samples.Count; i++)
                {
                    TimingScatterSample sample = samples[i];
                    if (!sample.HasJudge) continue;

                    int p = sample.PlayerId;
                    if (p < 0 || p >= _playerCount) p = 0;

                    _statTotal++;
                    _statTotalByPlayer[p]++;
                    if (HitMarginCompat.IsPerfectFamily(sample.Judge)) { _statPerfectFamily++; _statPerfectFamilyByPlayer[p]++; }
                    if (HitMarginCompat.IsNormalPerfect(sample.Judge)) { _statNormalPerfect++; _statNormalPerfectByPlayer[p]++; }
                    if (sample.IsXPerfect) { _statXPerfect++; _statXPerfectByPlayer[p]++; }

                    if (!float.IsNaN(sample.OffsetMs) && !float.IsInfinity(sample.OffsetMs))
                    {
                        all.Add(sample.OffsetMs);
                        perPlayer[p].Add(sample.OffsetMs);
                    }
                }

                if (all.Count > 0) _statUr = CalcUR.calc(all);
                for (int p = 0; p < _playerCount; p++)
                {
                    if (perPlayer[p].Count > 0) _statUrByPlayer[p] = CalcUR.calc(perPlayer[p]);
                }
            }

            _distributionStats = DistributionStats.Compute(FilteredSamples(samples), DistributionStats.DefaultBinCount);
            _statsComputed = true;
        }

        private List<TimingScatterSample> FilteredSamples(List<TimingScatterSample> samples)
        {
            int filter = _drawer != null ? _drawer.SelectedPlayerId : -1;
            if (filter < 0 || samples == null) return samples;

            List<TimingScatterSample> filtered = new List<TimingScatterSample>(samples.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].PlayerId == filter) filtered.Add(samples[i]);
            }
            return filtered;
        }

        private void OnPlayerFilterChanged()
        {
            _statsComputed = false;
            RefreshStats();
        }
        
        private void UpdateStatTexts()
        {
            RestoreScatterStatLabels();

            Settings settings = ModContext.Settings;
            int percentDigits = settings != null ? settings.Perc3 : 1;
            int ratioDigits = settings != null ? settings.PercRatioHUD : 1;
            int ratioMode = settings != null ? settings.Ratio_Mode : Settings.RatioMode_NormalPerfect;

            int players = _playerCount > 1 ? _playerCount : 1;

            if (players <= 1)
            {
                _statTexts[0] = _statTotal.ToString();
                _statTexts[1] = _statUr.ToString("F" + percentDigits);
                _statTexts[2] = ComputeRatioText(_statTotal, _statNormalPerfect, _statPerfectFamily, _statXPerfect, ratioMode, ratioDigits);
                _statTexts[3] = ComputeXAccText(_statTotal, _statXPerfect, percentDigits);
            }
            else
            {
                _statTexts[0] = JoinPlayerValues(players, p => _statTotalByPlayer[p].ToString());
                _statTexts[1] = JoinPlayerValues(players, p => _statUrByPlayer[p].ToString("F" + percentDigits));
                _statTexts[2] = JoinPlayerValues(players, p => ComputeRatioText(
                    _statTotalByPlayer[p], _statNormalPerfectByPlayer[p], _statPerfectFamilyByPlayer[p], _statXPerfectByPlayer[p],
                    ratioMode, ratioDigits));
                _statTexts[3] = JoinPlayerValues(players, p => ComputeXAccText(_statTotalByPlayer[p], _statXPerfectByPlayer[p], percentDigits));
            }

            for (int i = 0; i < 4; i++)
            {
                if (_statValues[i] != null) _statValues[i].text = _statTexts[i];
            }
        }

        private static string ComputeRatioText(int total, int normalPerfect, int perfectFamily, int xPerfect, int ratioMode, int ratioDigits)
        {
            int target;
            switch (ratioMode)
            {
                case Settings.RatioMode_PerfectFamily:
                    target = perfectFamily;
                    break;
                case Settings.RatioMode_XPerfect:
                    target = xPerfect;
                    break;
                default:
                    target = normalPerfect;
                    break;
            }

            int other = total - target;
            if (total == 0) return "0:1";
            if (other <= 0) return "infinity";
            return ((double)target / other).ToString("F" + ratioDigits) + ":1";
        }

        private static string ComputeXAccText(int total, int xPerfect, int percentDigits)
        {
            float xAcc = total > 0 ? (float)xPerfect / total * 100f : 0f;
            return xAcc.ToString("F" + percentDigits) + "%";
        }

        private static string JoinPlayerValues(int players, System.Func<int, string> value)
        {
            StringBuilder sb = new StringBuilder(64);
            for (int p = 0; p < players; p++)
            {
                if (p > 0) sb.Append("  ");
                sb.Append("<color=#");
                sb.Append(ColorUtility.ToHtmlStringRGB(Coop.PlayerColor(p)));
                sb.Append('>');
                sb.Append(Coop.Label(p));
                sb.Append(' ');
                sb.Append(value(p));
                sb.Append("</color>");
            }
            return sb.ToString();
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
