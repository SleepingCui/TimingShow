using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    internal static class LogRoundedRect
    {
        internal static float GlobalScale = 0.4f;

        internal static float WindowRadius { get { return 26f * GlobalScale; } }
        internal static float InnerPanelRadius { get { return 16f * GlobalScale; } }
        internal static float PanelRadius { get { return 18f * GlobalScale; } }
        internal static float ControlRadius { get { return 10f * GlobalScale; } }
        internal static float SmallRadius { get { return 8f * GlobalScale; } }
        internal static float SwatchRadius { get { return 6f * GlobalScale; } }
        internal static float MenuRadius { get { return 18f * GlobalScale; } }
        internal static float MenuItemRadius { get { return 9f * GlobalScale; } }
        internal static float CapsuleRatio { get { return Mathf.Clamp01(0.35f + 0.5f * GlobalScale); } }

        internal static void SyncFromSettings()
        {
            Settings settings = ModContext.Settings;
            GlobalScale = settings != null ? Mathf.Clamp(settings.LogGraph_CornerScale, 0f, 2f) : 0.4f;
        }

        internal static void DrawFilled(VertexHelper vh, Rect rect, float radius, Color color, int segments = 6)
        {
            DrawFilled(vh, rect, radius, radius, radius, radius, color, color, segments, 0f);
        }

        internal static void DrawFilled(VertexHelper vh, Rect rect, float radius, Color top, Color bottom,
            int segments = 8)
        {
            DrawFilled(vh, rect, radius, radius, radius, radius, top, bottom, segments, 0f);
        }

        internal static void DrawFilled(VertexHelper vh, Rect rect, float radius, Color top, Color bottom,
            int segments, float feather)
        {
            DrawFilled(vh, rect, radius, radius, radius, radius, top, bottom, segments, feather);
        }

        internal static void DrawFilled(VertexHelper vh, Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, Color color, int segments = 6)
        {
            DrawFilled(vh, rect, topLeft, topRight, bottomRight, bottomLeft, color, color, segments, 0f);
        }

        internal static void DrawFilled(VertexHelper vh, Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, Color top, Color bottom, int segments, float feather)
        {
            if (!(rect.width > 0f) || !(rect.height > 0f)) return;
            if (segments < 2) segments = 2;

            List<Vector2> points = BuildPerimeter(rect, topLeft, topRight, bottomRight, bottomLeft, segments);

            if (feather > 0f)
            {
                Rect halo = Expand(rect, feather);
                List<Vector2> haloPoints = BuildPerimeter(halo, topLeft + feather, topRight + feather,
                    bottomRight + feather, bottomLeft + feather, segments);
                AddBand(vh, haloPoints, points, top, bottom, rect, 0f, 1f);
            }

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(rect.center.x, rect.center.y), Shade(top, bottom, rect, rect.center.y), Vector2.zero);
            for (int i = 0; i < points.Count; i++)
                vh.AddVert(new Vector3(points[i].x, points[i].y), Shade(top, bottom, rect, points[i].y), Vector2.zero);

            for (int i = 0; i < points.Count; i++)
            {
                int a = start + 1 + i;
                int b = start + 1 + ((i + 1) % points.Count);
                vh.AddTriangle(start, a, b);
            }
        }

        internal static void DrawBorder(VertexHelper vh, Rect rect, float radius, float thickness,
            Color color, int segments = 6)
        {
            DrawBorder(vh, rect, radius, radius, radius, radius, thickness, color, color, segments, 0f);
        }

        internal static void DrawBorder(VertexHelper vh, Rect rect, float radius, float thickness,
            Color top, Color bottom, int segments, float feather)
        {
            DrawBorder(vh, rect, radius, radius, radius, radius, thickness, top, bottom, segments, feather);
        }

        internal static void DrawBorder(VertexHelper vh, Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, float thickness, Color color, int segments = 6)
        {
            DrawBorder(vh, rect, topLeft, topRight, bottomRight, bottomLeft, thickness, color, color, segments, 0f);
        }

        internal static void DrawBorder(VertexHelper vh, Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, float thickness, Color top, Color bottom,
            int segments = 8, float feather = 0f)
        {
            if (!(rect.width > 0f) || !(rect.height > 0f)) return;
            if (!(thickness > 0f)) return;
            if (segments < 2) segments = 2;

            if (thickness * 2f >= rect.width || thickness * 2f >= rect.height)
            {
                DrawFilled(vh, rect, topLeft, topRight, bottomRight, bottomLeft, top, bottom, segments, feather);
                return;
            }

            Rect inner = new Rect(rect.xMin + thickness, rect.yMin + thickness,
                rect.width - thickness * 2f, rect.height - thickness * 2f);

            List<Vector2> outerPoints = BuildPerimeter(rect, topLeft, topRight, bottomRight, bottomLeft, segments);
            List<Vector2> innerPoints = BuildPerimeter(inner,
                Mathf.Max(0f, topLeft - thickness),
                Mathf.Max(0f, topRight - thickness),
                Mathf.Max(0f, bottomRight - thickness),
                Mathf.Max(0f, bottomLeft - thickness),
                segments);

            if (feather > 0f)
            {
                Rect halo = Expand(rect, feather);
                AddBand(vh, BuildPerimeter(halo, topLeft + feather, topRight + feather,
                    bottomRight + feather, bottomLeft + feather, segments), outerPoints, top, bottom, rect, 0f, 1f);

                Rect core = Inset(rect, thickness + feather);
                if (core.width > 0f && core.height > 0f)
                {
                    AddBand(vh, innerPoints, BuildPerimeter(core,
                        Mathf.Max(0f, topLeft - thickness - feather),
                        Mathf.Max(0f, topRight - thickness - feather),
                        Mathf.Max(0f, bottomRight - thickness - feather),
                        Mathf.Max(0f, bottomLeft - thickness - feather), segments), top, bottom, rect, 1f, 0f);
                }
            }

            AddBand(vh, outerPoints, innerPoints, top, bottom, rect, 1f, 1f);
        }

        internal static void DrawShadow(VertexHelper vh, Rect rect, float radius, float spread,
            Vector2 offset, Color color, int steps = 6)
        {
            if (!(rect.width > 0f) || !(rect.height > 0f)) return;
            if (!(spread > 0f) || color.a <= 0f) return;
            if (steps < 2) steps = 2;

            Rect baseRect = new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height);
            Rect previous = baseRect;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float distance = spread * t;
                Rect band = Expand(baseRect, distance);
                AddBand(vh,
                    BuildPerimeter(band, radius + distance, radius + distance, radius + distance, radius + distance, 8),
                    BuildPerimeter(previous, radius + spread * (i - 1) / steps, radius + spread * (i - 1) / steps,
                        radius + spread * (i - 1) / steps, radius + spread * (i - 1) / steps, 8),
                    color, color, baseRect, Falloff(t), Falloff((i - 1) / (float)steps));
                previous = band;
            }
        }

        // Quadratic falloff reads as a blurred edge: darkest at the panel, transparent at the spread limit.
        private static float Falloff(float t)
        {
            float remaining = 1f - Mathf.Clamp01(t);
            return remaining * remaining;
        }

        // Vertical gradient is affine in y, so per-vertex colours interpolate it exactly.
        private static Color Shade(Color top, Color bottom, Rect rect, float y)
        {
            float t = rect.height > 0f ? Mathf.Clamp01((y - rect.yMin) / rect.height) : 1f;
            return Color.Lerp(bottom, top, t);
        }

        private static void AddBand(VertexHelper vh, List<Vector2> outer, List<Vector2> inner,
            Color top, Color bottom, Rect rect, float outerAlpha, float innerAlpha)
        {
            int count = Mathf.Min(outer.Count, inner.Count);
            if (count < 3) return;

            int start = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                Color color = Shade(top, bottom, rect, outer[i].y);
                color.a *= outerAlpha;
                vh.AddVert(new Vector3(outer[i].x, outer[i].y), color, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                Color color = Shade(top, bottom, rect, inner[i].y);
                color.a *= innerAlpha;
                vh.AddVert(new Vector3(inner[i].x, inner[i].y), color, Vector2.zero);
            }

            int innerStart = start + count;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                vh.AddTriangle(start + i, start + next, innerStart + next);
                vh.AddTriangle(start + i, innerStart + next, innerStart + i);
            }
        }

        private static Rect Expand(Rect rect, float amount)
        {
            return new Rect(rect.xMin - amount, rect.yMin - amount,
                rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static Rect Inset(Rect rect, float amount)
        {
            return new Rect(rect.xMin + amount, rect.yMin + amount,
                Mathf.Max(0f, rect.width - amount * 2f), Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static List<Vector2> BuildPerimeter(Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, int segments)
        {
            List<Vector2> points = new List<Vector2>((segments + 1) * 4);

            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            topLeft = Mathf.Clamp(topLeft, 0f, maxRadius);
            topRight = Mathf.Clamp(topRight, 0f, maxRadius);
            bottomRight = Mathf.Clamp(bottomRight, 0f, maxRadius);
            bottomLeft = Mathf.Clamp(bottomLeft, 0f, maxRadius);

            AddCorner(points, rect.xMin + topLeft, rect.yMax - topLeft, topLeft, 180f, 90f, segments);
            AddCorner(points, rect.xMax - topRight, rect.yMax - topRight, topRight, 90f, 0f, segments);
            AddCorner(points, rect.xMax - bottomRight, rect.yMin + bottomRight, bottomRight, 0f, -90f, segments);
            AddCorner(points, rect.xMin + bottomLeft, rect.yMin + bottomLeft, bottomLeft, -90f, -180f, segments);
            return points;
        }

        private static void AddCorner(List<Vector2> points, float cx, float cy, float radius,
            float startDegrees, float endDegrees, int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                points.Add(new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius));
            }
        }
    }
}
