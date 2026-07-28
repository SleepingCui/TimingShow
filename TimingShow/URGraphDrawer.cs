using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimingShow
{
    public class URGraphDrawer : GraphDrawerBase
    {
        private readonly Queue<float> urQueue = new Queue<float>();
        private readonly List<float> urCache = new List<float>();

        private float currentMinUR = 0f;
        private float currentMaxUR = 100f;

        private int lastProcessedCount = 0;

        protected override bool IsEnabled => Main.IsEnabled && Main.IsPlaying;
        protected override bool ShowGraph => Main.Settings.ShowURGraph;
        protected override float Scale => Main.Settings.URGraph_Scale;
        protected override float Width => Main.Settings.URGraph_Width;
        protected override float Height => Main.Settings.URGraph_Height;
        protected override float PosX => Screen.width * Main.Settings.URGraph_X;
        protected override float PosY => Screen.height * Main.Settings.URGraph_Y;
        protected override Color BgColor => Main.Settings.URGraph_BgColor;
        protected override Color GridColor => Main.Settings.URGraph_GridColor;
        protected override Color LineColor => Main.Settings.URGraph_LineColor;
        protected override Color AxisTextColor => Main.Settings.URGraph_TextColor;
        protected override Color ValueTextColor => Main.Settings.URGraph_TextColor;
        private float GridAlpha => Mathf.Clamp01(Main.Settings.URGraph_GridAlpha);
        private float AxisTextAlpha => Mathf.Clamp01(Main.Settings.URGraph_AxisTextAlpha);
        private float InfoTextAlpha => Mathf.Clamp01(Main.Settings.URGraph_InfoTextAlpha);

        protected override int MaxPoints => Main.Settings.URGraph_MaxPoints > 0 ? Main.Settings.URGraph_MaxPoints : 100;
        public override string GraphName => "UR";

        protected override void UpdateData()
        {
            if (Main.SessionOffsets == null || Main.SessionOffsets.Count == 0)
            {
                urQueue.Clear();
                urCache.Clear();
                lastProcessedCount = 0;
                currentMinUR = 0f;
                currentMaxUR = 100f;
                return;
            }

            int total = Main.SessionOffsets.Count;
            int maxCapacity = MaxPoints;

            while (lastProcessedCount < total)
            {
                int currIdx = lastProcessedCount;
                int windowSize = Main.Settings.URGraph_WindowSize;
                int startIdx = Math.Max(0, currIdx - windowSize + 1);
                int length = currIdx - startIdx + 1;

                List<double> subList = Main.SessionOffsets.GetRange(startIdx, length);
                double currentUR = CalcUR.calc(subList);

                if (urQueue.Count >= maxCapacity)
                {
                    urQueue.Dequeue();
                }
                urQueue.Enqueue((float)currentUR);

                lastProcessedCount++;
            }

            urCache.Clear();
            urCache.AddRange(urQueue);

            if (urCache.Count > 0)
            {
                float minVal = float.MaxValue;
                float maxVal = float.MinValue;

                for (int i = 0; i < urCache.Count; i++)
                {
                    if (urCache[i] < minVal) minVal = urCache[i];
                    if (urCache[i] > maxVal) maxVal = urCache[i];
                }

                float padding = (maxVal - minVal) * 0.15f;
                if (padding < 2f) padding = 2f;

                float targetMin = Mathf.Max(0f, minVal - padding);
                float targetMax = maxVal + padding;

                currentMinUR = Mathf.Lerp(currentMinUR, targetMin, Time.deltaTime * 5f);
                currentMaxUR = Mathf.Lerp(currentMaxUR, targetMax, Time.deltaTime * 5f);
            }
        }

        protected override int GetDataCount() => urCache.Count;
        protected override float GetDataValue(int index) => urCache[index];

        protected override float GetMinY() => currentMinUR;
        protected override float GetMaxY() => currentMaxUR;

        protected override void DrawGridLines(float posX, float posY, float w, float h, float scale)
        {
            Color c = GridColor;
            c.a *= GridAlpha;
            GUI.color = c;

            float lineWidth = 1.0f * scale;
            DrawAALine(new Vector2(posX, posY), new Vector2(posX + w, posY), lineWidth);
            DrawAALine(new Vector2(posX, posY + h * 0.5f), new Vector2(posX + w, posY + h * 0.5f), lineWidth);
            DrawAALine(new Vector2(posX, posY + h), new Vector2(posX + w, posY + h), lineWidth);
        }

        protected override void DrawAxisLabels(float posX, float posY, float w, float h, float scale, float minY, float maxY, float rangeY)
        {
            float labelWidth = 45f * scale;
            string fmt = rangeY < 5.0f ? "F1" : "F0";

            Color textColor = AxisTextColor;
            textColor.a *= AxisTextAlpha;
            labelStyle.normal.textColor = textColor;

            GUI.Label(new Rect(posX - labelWidth - 4f, posY - 8f * scale, labelWidth, 16f * scale), maxY.ToString(fmt), labelStyle);
            GUI.Label(new Rect(posX - labelWidth - 4f, posY + h * 0.5f - 8f * scale, labelWidth, 16f * scale), (minY + rangeY * 0.5f).ToString(fmt), labelStyle);
            GUI.Label(new Rect(posX - labelWidth - 4f, posY + h - 8f * scale, labelWidth, 16f * scale), minY.ToString(fmt), labelStyle);
        }

        protected override void DrawInfoText(float posX, float posY, float w, float h, float scale)
        {
            string infoText = GetInfoText();
            if (!string.IsNullOrEmpty(infoText))
            {
                Color textColor = ValueTextColor;
                textColor.a *= InfoTextAlpha;
                infoStyle.normal.textColor = textColor;

                GUI.Label(new Rect(posX + 5f, posY - 22f * scale, w, 20f * scale), infoText, infoStyle);
            }
        }

        protected override string GetInfoText()
        {
            if (urCache.Count == 0) return string.Empty;

            float curUR = urCache[urCache.Count - 1];
            double globalUR = CalcUR.calc(Main.SessionOffsets);
            string fmt = "F" + Math.Max(0, Main.Settings.Perc4);

            return $"UR: {globalUR.ToString(fmt)}  (Last {MaxPoints}: {curUR.ToString(fmt)})";
        }

        protected override void DrawDataCurve(float posX, float posY, float w, float h, float scale, int count, float minY, float rangeY)
        {
            int maxCapacity = MaxPoints;
            float stepX = w / Math.Max(1, maxCapacity - 1);
            float lineWidth = 2.0f * scale;

            for (int i = 0; i < count - 1; i++)
            {
                float norm1 = Mathf.Clamp01((urCache[i] - minY) / rangeY);
                float norm2 = Mathf.Clamp01((urCache[i + 1] - minY) / rangeY);

                Vector2 p1 = new Vector2(posX + i * stepX, posY + h - norm1 * h);
                Vector2 p2 = new Vector2(posX + (i + 1) * stepX, posY + h - norm2 * h);

                DrawAALine(p1, p2, lineWidth);
            }
        }
    }
}