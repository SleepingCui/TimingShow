using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD.ScatterDrawerBase
{
    public abstract partial class ScatterDrawerBase
    {
        protected virtual void DrawBands(VertexHelper vh, float w, float h, Settings settings, float minY, float maxY)
        {
        }

        protected virtual void RefreshBandWindow()
        {
        }

        protected const int MinRenderPoints = 20;
        protected const int MaxRenderPoints = 8000;
        private const int DefaultRenderPoints = 800;
        private const int FixedVertexBudget = 128;
        private const int MaxMeshVertexCount = 65535;
        private const int MinCircleSegments = 6;
        private const int MaxCircleSegments = 16;
        protected static readonly int MaxSafePointCount = (MaxMeshVertexCount - FixedVertexBudget) / (MinCircleSegments + 4);

        private static readonly Color DefaultPointColor = new Color(0.30f, 0.76f, 1f, 1f);
        private const float DefaultPointSize = 3f;
        private static readonly Color DefaultZeroLineColor = new Color(1f, 1f, 1f, 0.65f);
        private static readonly Color DefaultAvgLineColor = new Color(1f, 0.85f, 0.20f, 0.95f);
        private static readonly Color DefaultAxisTextColor = new Color(0.80f, 0.80f, 0.80f, 1f);

        private struct RenderPoint
        {
            public int SourceIndex;
            public float TimeMs;
            public float OffsetMs;
            public HitMan Judge;
            public bool IsXPerfect;
            public int PlayerId;
        }

        private readonly List<int> _visibleIndices = new List<int>(1024);
        private readonly List<RenderPoint> _renderPoints = new List<RenderPoint>(1024);
        private readonly List<float> _outlierScratch = new List<float>(1024);

        private RenderPoint[] _bucketMinPoints = new RenderPoint[64];
        private RenderPoint[] _bucketMaxPoints = new RenderPoint[64];
        private bool[] _bucketFilled = new bool[64];

        protected override void DrawReferenceLines(VertexHelper vh, float w, float h)
        {
            Settings settings = ModContext.Settings;
            if (settings == null) return;

            DrawBands(vh, w, h, settings, _minY, _maxY);

            if (!ScatterShowZeroLine) return;
            if (_minY > 0f || _maxY < 0f) return;

            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            float y = Mathf.Clamp01((0f - _minY) / rangeY) * h;
            float halfWidth = Mathf.Max(0.5f, 1.5f * Mathf.Max(0.01f, Scale) * 0.5f);
            DrawSegment(vh, new Vector2(0f, y), new Vector2(w, y), halfWidth, ScatterZeroLineColor);
        }

        protected override void DrawSeries(VertexHelper vh, float w, float h)
        {
            int count = _renderPoints.Count;

            Settings settings = ModContext.Settings;
            bool judgeColor = ScatterUseJudgeColor;
            Color uniformColor = ScatterPointColor;
            float scale = Mathf.Max(0.01f, Scale);
            float radius = Mathf.Max(0.5f, (ScatterPointSize) * scale * 0.5f);
            float rangeY = Mathf.Max(0.01f, _maxY - _minY);

            if (DrawPlotFrame) DrawPlotFrameLines(vh, w, h);

            DrawAverageLine(vh, w, h, settings, scale, rangeY);

            bool colorByPlayer = ColorPointsByPlayer;
            int circleSegments = CircleSegmentsFor(count, radius);
            
            HitMan runJudge = HitMan.Unknown;
            bool runXp = false;
            bool runValid = false;
            Color runColor = uniformColor;

            for (int i = 0; i < count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (!TryGetPointLocalPosition(point, w, h, out Vector2 center)) continue;

                Color color;
                if (colorByPlayer)
                {
                    color = Coop.PlayerColor(point.PlayerId);
                }
                else if (!judgeColor)
                {
                    color = uniformColor;
                }
                else if (runValid && point.Judge == runJudge && point.IsXPerfect == runXp)
                {
                    color = runColor;
                }
                else
                {
                    runColor = JColors.GetColor(point.Judge, point.IsXPerfect, true);
                    runJudge = point.Judge;
                    runXp = point.IsXPerfect;
                    runValid = true;
                    color = runColor;
                }

                DrawCircle(vh, center, radius, circleSegments, color);
            }

            DrawPlayerLegendSwatches(vh, w, h);

            if (ShowOutOfRangeIndicators) DrawOutOfRangeMarkers(vh, w, h);

            DrawDetailPanel(vh, scale);
        }

        protected void DrawBand(VertexHelper vh, float w, float h, float rangeY, float minY, float fromMs, float toMs, Color color)
        {
            float y0 = Mathf.Clamp01((fromMs - minY) / rangeY) * h;
            float y1 = Mathf.Clamp01((toMs - minY) / rangeY) * h;
            if (y1 - y0 < 0.5f) return;

            DrawQuad(vh, new Vector2(0f, y0), new Vector2(w, y1), color);
        }

        private void DrawPlotFrameLines(VertexHelper vh, float w, float h)
        {
            Color color = ScatterAxisTextColor;
            float scale = Mathf.Max(0.01f, Scale);
            float thickness = Mathf.Max(0.5f, 1f * scale * 0.5f);

            Color axisColor = color;
            axisColor.a *= 0.30f;
            DrawSegment(vh, new Vector2(0f, 0f), new Vector2(w, 0f), thickness, axisColor);
            DrawSegment(vh, new Vector2(0f, 0f), new Vector2(0f, h), thickness, axisColor);

            Color frameColor = color;
            frameColor.a *= 0.22f;
            DrawSegment(vh, new Vector2(0f, h), new Vector2(w, h), thickness, frameColor);
            DrawSegment(vh, new Vector2(w, 0f), new Vector2(w, h), thickness, frameColor);

            Color tickColor = color;
            tickColor.a *= 0.35f;
            float tick = Mathf.Max(2f, 4f * scale);
            float tickX = Mathf.Min(tick, Mathf.Max(0f, w * 0.25f));
            float tickY = Mathf.Min(tick, Mathf.Max(0f, h * 0.25f));
            DrawSegment(vh, new Vector2(0f, h), new Vector2(tickX, h), thickness, tickColor);
            DrawSegment(vh, new Vector2(0f, h * 0.5f), new Vector2(tickX, h * 0.5f), thickness, tickColor);
            DrawSegment(vh, new Vector2(0f, 0f), new Vector2(tickX, 0f), thickness, tickColor);
            DrawSegment(vh, new Vector2(0f, 0f), new Vector2(0f, tickY), thickness, tickColor);
            DrawSegment(vh, new Vector2(w, 0f), new Vector2(w, tickY), thickness, tickColor);
        }

        protected override void DrawGridLines(VertexHelper vh, float w, float h)
        {
            if (!ShowGrid)
            {
                base.DrawGridLines(vh, w, h);
                return;
            }

            float scale = Mathf.Max(0.01f, Scale);
            float halfWidth = Mathf.Max(0.25f, 1f * scale * 0.5f);

            Color color = GridColor;
            color.a = Mathf.Clamp(color.a * Mathf.Clamp01(GridAlphaScale), 0.10f, 0.65f);

            float[] fractions = GridLineFractions(GridDivisions);

            for (int i = 0; i < fractions.Length; i++)
            {
                float x = w * fractions[i];
                float y = h * fractions[i];
                DrawSegment(vh, new Vector2(x, 0f), new Vector2(x, h), halfWidth, color);
                DrawSegment(vh, new Vector2(0f, y), new Vector2(w, y), halfWidth, color);
            }
        }

        internal static float[] GridLineFractions(int divisions)
        {
            int n = Mathf.Clamp(divisions, 2, 20);
            float[] fractions = new float[n - 1];

            for (int i = 1; i < n; i++) fractions[i - 1] = (float)i / n;

            return fractions;
        }

        private void DrawOutOfRangeMarkers(VertexHelper vh, float w, float h)
        {
            int count = _renderPoints.Count;
            if (count <= 0) return;

            float scale = Mathf.Max(0.01f, Scale);
            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            bool judgeColor = ScatterUseJudgeColor;
            Color uniformColor = ScatterPointColor;
            float halfWidth = Mathf.Max(1.5f, 3.2f * scale);
            float height = Mathf.Max(2.5f, 5f * scale);
            float inset = Mathf.Max(1f, 1.5f * scale);
            int drawn = 0;

            for (int i = 0; i < count; i++)
            {
                RenderPoint point = _renderPoints[i];
                float ny = (point.OffsetMs - _minY) / rangeY;
                if (ny >= 0f && ny <= 1f) continue;

                float nx = Mathf.Clamp01(NormalizedX(point));
                float x = Mathf.Clamp(nx * w, halfWidth, Mathf.Max(halfWidth, w - halfWidth));
                bool above = ny > 1f;

                Color color = judgeColor ? JColors.GetColor(point.Judge, point.IsXPerfect, true) : uniformColor;
                color.a *= 0.85f;

                float baseY = above ? h - inset - height : inset + height;
                float tipY = above ? h - inset : inset;
                DrawTriangle(vh, new Vector2(x, baseY), new Vector2(x, tipY), halfWidth, color);

                drawn++;
                if (drawn >= MaxOutOfRangeIndicators) break;
            }
        }

        private void DrawTriangle(VertexHelper vh, Vector2 baseCenter, Vector2 tip, float halfWidth, Color color)
        {
            int baseIndex = vh.currentVertCount;
            vh.AddVert(new Vector3(baseCenter.x - halfWidth, baseCenter.y, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(baseCenter.x + halfWidth, baseCenter.y, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(tip.x, tip.y, 0f), color, Vector2.zero);
            vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
        }

        private const float MaxCircleSagittaPx = 0.35f;

        private static int CircleSegmentsFor(int pointCount, float radius)
        {
            int budgetPerPoint = (MaxMeshVertexCount - FixedVertexBudget) / Mathf.Max(1, pointCount);
            int byBudget = budgetPerPoint - 4;

            int byRoundness = MaxCircleSegments;
            if (radius > MaxCircleSagittaPx)
            {
                float cosHalf = Mathf.Clamp(1f - MaxCircleSagittaPx / radius, -1f, 1f);
                float step = 2f * Mathf.Acos(cosHalf);
                if (step > 1e-4f) byRoundness = Mathf.CeilToInt((Mathf.PI * 2f) / step);
            }

            return Mathf.Clamp(Mathf.Min(byBudget, byRoundness), MinCircleSegments, MaxCircleSegments);
        }

        private static readonly Dictionary<int, Vector2[]> CircleUnitCache = new Dictionary<int, Vector2[]>();

        private static Vector2[] GetUnitCircle(int segments)
        {
            if (CircleUnitCache.TryGetValue(segments, out Vector2[] unit)) return unit;

            unit = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                unit[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            CircleUnitCache[segments] = unit;
            return unit;
        }

        private void DrawCircle(VertexHelper vh, Vector2 center, float radius, int segments, Color color)
        {
            if (segments < 3) segments = 3;

            Vector2[] unit = GetUnitCircle(segments);
            int baseIndex = vh.currentVertCount;
            for (int i = 0; i < segments; i++)
            {
                Vector2 p = unit[i];
                vh.AddVert(new Vector3(center.x + p.x * radius, center.y + p.y * radius, 0f), color, Vector2.zero);
            }

            for (int i = 1; i < segments - 1; i++)
            {
                vh.AddTriangle(baseIndex, baseIndex + i, baseIndex + i + 1);
            }
        }

        private void DrawAverageLine(VertexHelper vh, float w, float h, Settings settings, float scale, float rangeY)
        {
            if (!_avgAvailable || !ScatterShowAvgLine) return;
            if (_avgValues.Count != _visibleIndices.Count || _avgValues.Count < 2) return;

            int count = _renderPoints.Count;
            float halfWidth = Mathf.Max(0.5f, 1.6f * scale * 0.5f);
            Color color = ScatterAvgLineColor;

            int cursor = 0;
            bool hasPrev = false;
            bool hasFirst = false;
            Vector2 prev = Vector2.zero;
            Vector2 first = Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                RenderPoint point = _renderPoints[i];
                while (cursor < _avgValues.Count - 1 && _visibleIndices[cursor] < point.SourceIndex) cursor++;
                float avg = i == count - 1 && _avgSmoothValid ? _avgSmoothValue : _avgValues[cursor];
                float nx = NormalizedX(point);
                float ny = Mathf.Clamp01((avg - _minY) / rangeY);
                Vector2 cur = new Vector2(nx * w, ny * h);

                if (!hasFirst)
                {
                    first = cur;
                    hasFirst = true;
                }

                if (hasPrev) DrawClippedSegment(vh, prev, cur, w, halfWidth, color);
                prev = cur;
                hasPrev = true;
            }
            
            if (hasFirst && first.x > 0.01f)
                DrawClippedSegment(vh, new Vector2(0f, first.y), first, w, halfWidth, color);

            float currentAvg = _avgSmoothValid ? _avgSmoothValue : _avgCurrent;
            float tailNy = Mathf.Clamp01((currentAvg - _minY) / rangeY);
            if (hasPrev)
            {
                Vector2 tail = new Vector2(w, tailNy * h);
                DrawClippedSegment(vh, prev, tail, w, halfWidth, color);
            }
            else
            {
                DrawSegment(vh, new Vector2(0f, tailNy * h), new Vector2(w, tailNy * h), halfWidth, color);
            }

            if (AvgLabelStub)
            {
                Color stub = color;
                stub.a *= 0.55f;
                float stubHalf = Mathf.Max(0.5f, halfWidth * 0.8f);
                DrawSegment(vh, new Vector2(w - 1f * scale, tailNy * h), new Vector2(w + 4f * scale, tailNy * h), stubHalf, stub);
            }
        }

        private void DrawClippedSegment(VertexHelper vh, Vector2 a, Vector2 b, float w, float halfWidth, Color color)
        {
            if (a.x > b.x) (a, b) = (b, a);
            if (b.x <= 0f || a.x >= w) return;

            float dx = b.x - a.x;
            if (a.x < 0f && dx > 0.0001f) a = new Vector2(0f, Mathf.Lerp(a.y, b.y, (0f - a.x) / dx));
            if (b.x > w && dx > 0.0001f) b = new Vector2(w, Mathf.Lerp(a.y, b.y, (w - a.x) / dx));

            if (b.x - a.x < 0.01f) return;

            DrawSegment(vh, a, b, halfWidth, color);
        }

        private bool TryGetPointLocalPosition(RenderPoint point, float w, float h, out Vector2 local)
        {
            float rangeY = _maxY - _minY;
            if (!(rangeY > 0f))
            {
                local = Vector2.zero;
                return false;
            }

            float nx = NormalizedX(point);
            float ny = (point.OffsetMs - _minY) / rangeY;
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
            {
                local = Vector2.zero;
                return false;
            }

            local = new Vector2(nx * w, ny * h);
            return true;
        }

    }
}
