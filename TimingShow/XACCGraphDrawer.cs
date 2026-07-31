using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimingShow
{
    public class XACCGraphDrawer : GraphDrawerBase
    {
        private readonly List<float> xaccRenderCache = new List<float>();

        protected override bool IsEnabled => Main.IsEnabled && Main.IsPlaying;
        protected override bool ShowGraph => Main.Settings.ShowXACCGraph;
        protected override float Scale => Main.Settings.XACCGraph_Scale;
        protected override float Width => Main.Settings.XACCGraph_Width;
        protected override float Height => Main.Settings.XACCGraph_Height;
        protected override float PosX => Screen.width * Main.Settings.XACCGraph_X;
        protected override float PosY => Screen.height * Main.Settings.XACCGraph_Y;
        protected override Color BgColor => Main.Settings.XACCGraph_BgColor;
        protected override Color GridColor => Main.Settings.XACCGraph_GridColor;
        protected override Color LineColor => Main.Settings.XACCGraph_LineColor;
        protected override Color AxisTextColor => Main.Settings.XACCGraph_AxisTextColor;
        protected override Color ValueTextColor => Main.Settings.XACCGraph_ValueTextColor;

        protected override int MaxPoints => Main.Settings.XACCGraph_MaxPoints > 0 ? Main.Settings.XACCGraph_MaxPoints : 250;
        public override string GraphName => "XACC";

        private float _cachedMinY;
        private float _cachedMaxY;

        protected override void UpdateData()
        {
            if (Main.FullXAccHistory == null || Main.FullXAccHistory.Count == 0)
            {
                xaccRenderCache.Clear();
                return;
            }

            xaccRenderCache.Clear();
            int total = Main.FullXAccHistory.Count;
            int targetPoints = MaxPoints;

            if (Main.IsLevelFinished)
            {
                if (total <= targetPoints)
                {
                    xaccRenderCache.AddRange(Main.FullXAccHistory);
                }
                else
                {
                    float step = (float)(total - 1) / (targetPoints - 1);
                    for (int i = 0; i < targetPoints; i++)
                    {
                        int idx = Mathf.Clamp((int)(i * step), 0, total - 1);
                        xaccRenderCache.Add(Main.FullXAccHistory[idx]);
                    }
                }
            }
            else
            {
                int startIdx = Math.Max(0, total - targetPoints);
                for (int i = startIdx; i < total; i++)
                {
                    xaccRenderCache.Add(Main.FullXAccHistory[i]);
                }
            }

            int count = xaccRenderCache.Count;
            if (count == 0) return;

            if (Main.IsLevelFinished)
            {
                _cachedMaxY = 100f;
                float minVal = 100f;
                for (int i = 0; i < count; i++)
                {
                    if (xaccRenderCache[i] < minVal) minVal = xaccRenderCache[i];
                }
                _cachedMinY = Mathf.Max(0f, Mathf.Floor(minVal - 1f));
                if (100f - minVal < 1.0f)
                {
                    _cachedMinY = Mathf.Max(0f, (float)Math.Floor(minVal * 10f) / 10f);
                }
            }
            else
            {
                float minVal = 100f;
                float maxVal = 0f;
                for (int i = 0; i < count; i++)
                {
                    float val = xaccRenderCache[i];
                    if (val < minVal) minVal = val;
                    if (val > maxVal) maxVal = val;
                }

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

        protected override int GetDataCount() => xaccRenderCache.Count;
        protected override float GetDataValue(int index) => xaccRenderCache[index];
        protected override float GetMinY() => _cachedMinY;
        protected override float GetMaxY() => _cachedMaxY;

        protected override void DrawGridLines(float posX, float posY, float w, float h, float scale)
        {
            GUI.color = GridColor;

            float lineWidth = 1.0f * scale;
            DrawAALine(new Vector2(posX, posY), new Vector2(posX + w, posY), lineWidth);
            DrawAALine(new Vector2(posX, posY + h * 0.5f), new Vector2(posX + w, posY + h * 0.5f), lineWidth);
            DrawAALine(new Vector2(posX, posY + h), new Vector2(posX + w, posY + h), lineWidth);
        }

        protected override void DrawAxisLabels(float posX, float posY, float w, float h, float scale, float minY, float maxY, float rangeY)
        {
            float labelWidth = 55f * scale;
            string fmtY = rangeY < 2.0f ? "F2" : (rangeY < 10.0f ? "F1" : "F0");
            labelStyle.normal.textColor = AxisTextColor;

            GUI.Label(new Rect(posX - labelWidth - 4f, posY - 8f * scale, labelWidth, 16f * scale), $"{maxY.ToString(fmtY)}%", labelStyle);
            GUI.Label(new Rect(posX - labelWidth - 4f, posY + h * 0.5f - 8f * scale, labelWidth, 16f * scale), $"{(minY + rangeY * 0.5f).ToString(fmtY)}%", labelStyle);
            GUI.Label(new Rect(posX - labelWidth - 4f, posY + h - 8f * scale, labelWidth, 16f * scale), $"{minY.ToString(fmtY)}%", labelStyle);
        }

        protected override void DrawInfoText(float posX, float posY, float w, float h, float scale)
        {
            string infoText = GetInfoText();
            if (!string.IsNullOrEmpty(infoText))
            {
                infoStyle.normal.textColor = ValueTextColor;
                GUI.Label(new Rect(posX + 5f, posY - 22f * scale, w, 20f * scale), infoText, infoStyle);
            }
        }

        protected override string GetInfoText()
        {
            if (xaccRenderCache.Count == 0) return string.Empty;
            float curXAcc = xaccRenderCache[xaccRenderCache.Count - 1];
            return $"XACC: {curXAcc}%";
        }
    }
}