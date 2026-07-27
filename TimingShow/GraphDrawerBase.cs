using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace TimingShow
{
    public abstract class GraphDrawerBase : MonoBehaviour
    {
        public float RenderTimeMs { get; protected set; } = 0f;
        public float UpdateTimeMs { get; protected set; } = 0f;
        public int RenderedPoints { get; protected set; } = 0;

        protected Stopwatch renderTimer = new Stopwatch();
        protected Stopwatch updateTimer = new Stopwatch();

        protected Texture2D bgTexture;
        protected Texture2D lineTexture;

        protected GUIStyle labelStyle;
        protected GUIStyle infoStyle;

        protected abstract bool IsEnabled { get; }
        protected abstract bool ShowGraph { get; }
        protected abstract float Scale { get; }
        protected abstract float Width { get; }
        protected abstract float Height { get; }
        protected abstract float PosX { get; }
        protected abstract float PosY { get; }
        protected abstract Color BgColor { get; }
        protected abstract Color GridColor { get; }
        protected abstract Color LineColor { get; }
        protected abstract Color AxisTextColor { get; }
        protected abstract Color ValueTextColor { get; }
        protected abstract int MaxPoints { get; }
        public abstract string GraphName { get; }

        protected abstract void UpdateData();
        protected abstract int GetDataCount();
        protected abstract float GetDataValue(int index);
        protected abstract float GetMinY();
        protected abstract float GetMaxY();
        protected abstract string GetInfoText();

        protected virtual void Awake()
        {
            CreateTextures();
        }

        protected virtual void OnDestroy()
        {
            DestroyTextures();
        }

        private void CreateTextures()
        {
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, Color.white);
            bgTexture.Apply();

            lineTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            Color[] colors = new Color[4] { Color.white, Color.white, Color.white, Color.white };
            lineTexture.SetPixels(colors);
            lineTexture.filterMode = FilterMode.Bilinear;
            lineTexture.Apply();
        }

        private void DestroyTextures()
        {
            if (bgTexture != null) Destroy(bgTexture);
            if (lineTexture != null) Destroy(lineTexture);
        }

        protected virtual void InitStyles(float baseLabelSize, float baseInfoSize)
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = Mathf.Max(10, (int)(baseLabelSize * Scale)),
                    fontStyle = FontStyle.Bold
                };
            }

            if (infoStyle == null)
            {
                infoStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = Mathf.Max(11, (int)(baseInfoSize * Scale)),
                    fontStyle = FontStyle.Bold
                };
            }
        }

        protected virtual void OnGUI()
        {
            if (!IsEnabled || !ShowGraph)
            {
                RenderedPoints = 0;
                RenderTimeMs = 0f;
                UpdateTimeMs = 0f;
                return;
            }
            if (Event.current.type != EventType.Repaint) return;

            renderTimer.Restart();

            InitStyles(11f, 12f);

            updateTimer.Restart();
            UpdateData();
            updateTimer.Stop();
            UpdateTimeMs = (float)updateTimer.Elapsed.TotalMilliseconds;

            int count = GetDataCount();
            RenderedPoints = count;
            if (count < 2)
            {
                renderTimer.Stop();
                RenderTimeMs = (float)renderTimer.Elapsed.TotalMilliseconds;
                return;
            }

            float scale = Scale;
            float w = Width * scale;
            float h = Height * scale;
            float posX = PosX;
            float posY = PosY;

            Rect graphRect = new Rect(posX, posY, w, h);
            Color oldColor = GUI.color;

            GUI.color = BgColor;
            GUI.DrawTexture(graphRect, bgTexture);

            float minY = GetMinY();
            float maxY = GetMaxY();
            float rangeY = Mathf.Max(0.01f, maxY - minY);

            GUI.color = GridColor;
            DrawGridLines(posX, posY, w, h, scale);

            labelStyle.normal.textColor = AxisTextColor;
            DrawAxisLabels(posX, posY, w, h, scale, minY, maxY, rangeY);

            infoStyle.normal.textColor = ValueTextColor;
            DrawInfoText(posX, posY, w, h, scale);

            GUI.color = LineColor;
            DrawDataCurve(posX, posY, w, h, scale, count, minY, rangeY);

            GUI.color = oldColor;

            renderTimer.Stop();
            RenderTimeMs = (float)renderTimer.Elapsed.TotalMilliseconds;
        }

        protected virtual void DrawGridLines(float posX, float posY, float w, float h, float scale)
        {
            float lineWidth = 1.0f * scale;
            DrawAALine(new Vector2(posX, posY), new Vector2(posX + w, posY), lineWidth);
            DrawAALine(new Vector2(posX, posY + h * 0.5f), new Vector2(posX + w, posY + h * 0.5f), lineWidth);
            DrawAALine(new Vector2(posX, posY + h), new Vector2(posX + w, posY + h), lineWidth);
        }

        protected virtual void DrawAxisLabels(float posX, float posY, float w, float h, float scale, float minY, float maxY, float rangeY)
        {
            float labelWidth = 55f * scale;
            string fmtY = rangeY < 2.0f ? "F2" : (rangeY < 10.0f ? "F1" : "F0");

            GUI.Label(new Rect(posX - labelWidth - 4f, posY - 8f * scale, labelWidth, 16f * scale),
                $"{maxY.ToString(fmtY)}%", labelStyle);
            GUI.Label(new Rect(posX - labelWidth - 4f, posY + h * 0.5f - 8f * scale, labelWidth, 16f * scale),
                $"{(minY + rangeY * 0.5f).ToString(fmtY)}%", labelStyle);
            GUI.Label(new Rect(posX - labelWidth - 4f, posY + h - 8f * scale, labelWidth, 16f * scale),
                $"{minY.ToString(fmtY)}%", labelStyle);
        }

        protected virtual void DrawInfoText(float posX, float posY, float w, float h, float scale)
        {
            string infoText = GetInfoText();
            if (!string.IsNullOrEmpty(infoText))
            {
                GUI.Label(new Rect(posX + 5f, posY - 22f * scale, w, 20f * scale), infoText, infoStyle);
            }
        }

        protected virtual void DrawDataCurve(float posX, float posY, float w, float h, float scale, int count, float minY, float rangeY)
        {
            int maxCapacity = MaxPoints > 0 ? MaxPoints : 250;
            float stepX = w / Math.Max(1, maxCapacity - 1);
            float lineWidth = 2.0f * scale;

            for (int i = 0; i < count - 1; i++)
            {
                float norm1 = Mathf.Clamp01((GetDataValue(i) - minY) / rangeY);
                float norm2 = Mathf.Clamp01((GetDataValue(i + 1) - minY) / rangeY);

                Vector2 p1 = new Vector2(posX + i * stepX, posY + h - norm1 * h);
                Vector2 p2 = new Vector2(posX + (i + 1) * stepX, posY + h - norm2 * h);

                DrawAALine(p1, p2, lineWidth);
            }
        }

        protected void DrawAALine(Vector2 pointA, Vector2 pointB, float width)
        {
            Vector2 d = pointB - pointA;
            float magnitude = d.magnitude;
            if (magnitude < 0.01f) return;

            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pointA);

            Rect lineRect = new Rect(pointA.x, pointA.y - width * 0.5f, magnitude, width);
            GUI.DrawTexture(lineRect, lineTexture, ScaleMode.StretchToFill, true);

            GUI.matrix = matrixBackup;
        }
    }
}