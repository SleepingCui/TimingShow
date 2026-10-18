using System;
using UnityEngine;

namespace TimingShow.HUD.LogGraphSettingsUI
{
    public sealed partial class LogGraphSettingsUI
    {
        private sealed class Draft
        {
            public float UiScale = 1.25f;
            public float CornerScale = 0.4f;
            public float PointSize = 3f;
            public int MaxRenderPoints = 800;
            public bool IgnoreOutliers;
            public bool UseHitAxis;
            public bool UseJudgeColor = true;
            public bool DistinguishPlayers = true;
            public bool ShowZeroLine;
            public bool ShowAvgLine;
            public bool ShowGrid = true;
            public bool ShowJudgeBands = true;
            public bool ShowXpBand = true;
            public float BandThresholdBpm = 310f;
            public bool UseCustomFont;
            public Color BgColor = Color.black;
            public Color GridColor = Color.white;
            public Color PointColor = Color.white;
            public Color ZeroLineColor = Color.white;
            public Color AvgLineColor = Color.yellow;
            public Color AxisTextColor = Color.gray;
            public Color BandPerfectColor = Settings.LogGraph_DefaultBandPerfectColor;
            public Color BandElPerfectColor = Settings.LogGraph_DefaultBandElPerfectColor;
            public Color BandEarlyLateColor = Settings.LogGraph_DefaultBandEarlyLateColor;
            public Color BandXpColor = Settings.LogGraph_DefaultBandXpColor;

            public static Draft FromSettings(Settings s)
            {
                Draft d = new Draft();
                if (s == null) return d;

                d.UiScale = s.LogGraph_UIScale;
                d.CornerScale = s.LogGraph_CornerScale;
                d.PointSize = s.LogGraph_PointSize;
                d.MaxRenderPoints = s.LogGraph_MaxRenderPoints;
                d.IgnoreOutliers = s.LogGraph_IgnoreOutliers;
                d.UseHitAxis = s.LogGraph_UseHitAxis;
                d.UseJudgeColor = s.LogGraph_UseJudgeColor;
                d.DistinguishPlayers = s.LogGraph_DistinguishPlayers;
                d.ShowZeroLine = s.LogGraph_ShowZeroLine;
                d.ShowAvgLine = s.LogGraph_ShowAvgLine;
                d.ShowGrid = s.LogGraph_ShowGrid;
                d.ShowJudgeBands = s.LogGraph_ShowJudgeBands;
                d.ShowXpBand = s.LogGraph_ShowXpBand;
                d.BandThresholdBpm = s.LogGraph_BandThresholdBpm;
                d.UseCustomFont = s.LogGraph_UseCustomFont;
                d.BgColor = s.LogGraph_BgColor;
                d.GridColor = s.LogGraph_GridColor;
                d.PointColor = s.LogGraph_PointColor;
                d.ZeroLineColor = s.LogGraph_ZeroLineColor;
                d.AvgLineColor = s.LogGraph_AvgLineColor;
                d.AxisTextColor = s.LogGraph_AxisTextColor;
                d.BandPerfectColor = s.LogGraph_BandPerfectColor;
                d.BandElPerfectColor = s.LogGraph_BandElPerfectColor;
                d.BandEarlyLateColor = s.LogGraph_BandEarlyLateColor;
                d.BandXpColor = s.LogGraph_BandXpColor;
                return d;
            }

            public void ApplyTo(Settings s)
            {
                if (s == null) return;

                s.LogGraph_UIScale = UiScale;
                s.LogGraph_CornerScale = CornerScale;
                s.LogGraph_PointSize = PointSize;
                s.LogGraph_MaxRenderPoints = MaxRenderPoints;
                s.LogGraph_IgnoreOutliers = IgnoreOutliers;
                s.LogGraph_UseHitAxis = UseHitAxis;
                s.LogGraph_UseJudgeColor = UseJudgeColor;
                s.LogGraph_DistinguishPlayers = DistinguishPlayers;
                s.LogGraph_ShowZeroLine = ShowZeroLine;
                s.LogGraph_ShowAvgLine = ShowAvgLine;
                s.LogGraph_ShowGrid = ShowGrid;
                s.LogGraph_ShowJudgeBands = ShowJudgeBands;
                s.LogGraph_ShowXpBand = ShowXpBand;
                s.LogGraph_BandThresholdBpm = BandThresholdBpm;
                s.LogGraph_UseCustomFont = UseCustomFont;
                s.LogGraph_BgColor = BgColor;
                s.LogGraph_GridColor = GridColor;
                s.LogGraph_PointColor = PointColor;
                s.LogGraph_ZeroLineColor = ZeroLineColor;
                s.LogGraph_AvgLineColor = AvgLineColor;
                s.LogGraph_AxisTextColor = AxisTextColor;
                s.LogGraph_BandPerfectColor = BandPerfectColor;
                s.LogGraph_BandElPerfectColor = BandElPerfectColor;
                s.LogGraph_BandEarlyLateColor = BandEarlyLateColor;
                s.LogGraph_BandXpColor = BandXpColor;
            }
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


    }
}
