using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    internal static class LogRoundedRect
    {
        internal static float GlobalScale = 0.4f;

        internal static float WindowRadius { get { return 10f * GlobalScale; } }
        internal static float InnerPanelRadius { get { return 6f * GlobalScale; } }
        internal static float PanelRadius { get { return 8f * GlobalScale; } }
        internal static float ControlRadius { get { return 4f * GlobalScale; } }
        internal static float SmallRadius { get { return 3f * GlobalScale; } }
        internal static float SwatchRadius { get { return 2f * GlobalScale; } }
        internal static float MenuRadius { get { return 7f * GlobalScale; } }
        internal static float MenuItemRadius { get { return 4f * GlobalScale; } }
        internal static float CapsuleRatio { get { return Mathf.Clamp01(0.5f * GlobalScale); } }

        internal static void SyncFromSettings()
        {
            Settings settings = ModContext.Settings;
            GlobalScale = settings != null ? Mathf.Clamp(settings.LogGraph_CornerScale, 0f, 2f) : 0.4f;
        }

        internal static void DrawFilled(VertexHelper vh, Rect rect, float radius, Color color, int segments = 6)
        {
            DrawFilled(vh, rect, radius, radius, radius, radius, color, segments);
        }

        internal static void DrawFilled(VertexHelper vh, Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, Color color, int segments = 6)
        {
            if (!(rect.width > 0f) || !(rect.height > 0f)) return;
            if (segments < 2) segments = 2;

            List<Vector2> points = new List<Vector2>((segments + 1) * 4);
            BuildPerimeter(rect, topLeft, topRight, bottomRight, bottomLeft, segments, points);
            if (points.Count < 3) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(rect.center.x, rect.center.y), color, Vector2.zero);
            for (int i = 0; i < points.Count; i++)
                vh.AddVert(new Vector3(points[i].x, points[i].y), color, Vector2.zero);

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
            DrawBorder(vh, rect, radius, radius, radius, radius, thickness, color, segments);
        }

        internal static void DrawBorder(VertexHelper vh, Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, float thickness, Color color, int segments = 6)
        {
            if (!(rect.width > 0f) || !(rect.height > 0f)) return;
            if (!(thickness > 0f)) return;
            if (segments < 2) segments = 2;

            if (thickness * 2f >= rect.width || thickness * 2f >= rect.height)
            {
                DrawFilled(vh, rect, topLeft, topRight, bottomRight, bottomLeft, color, segments);
                return;
            }

            Rect inner = new Rect(rect.xMin + thickness, rect.yMin + thickness,
                rect.width - thickness * 2f, rect.height - thickness * 2f);

            List<Vector2> outerPoints = new List<Vector2>((segments + 1) * 4);
            List<Vector2> innerPoints = new List<Vector2>((segments + 1) * 4);
            BuildPerimeter(rect, topLeft, topRight, bottomRight, bottomLeft, segments, outerPoints);
            BuildPerimeter(inner,
                Mathf.Max(0f, topLeft - thickness),
                Mathf.Max(0f, topRight - thickness),
                Mathf.Max(0f, bottomRight - thickness),
                Mathf.Max(0f, bottomLeft - thickness),
                segments, innerPoints);

            int start = vh.currentVertCount;
            for (int i = 0; i < outerPoints.Count; i++)
                vh.AddVert(new Vector3(outerPoints[i].x, outerPoints[i].y), color, Vector2.zero);
            for (int i = 0; i < innerPoints.Count; i++)
                vh.AddVert(new Vector3(innerPoints[i].x, innerPoints[i].y), color, Vector2.zero);

            int outerStart = start;
            int innerStart = start + outerPoints.Count;
            int count = Mathf.Min(outerPoints.Count, innerPoints.Count);
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int o0 = outerStart + i;
                int o1 = outerStart + next;
                int i0 = innerStart + i;
                int i1 = innerStart + next;
                vh.AddTriangle(o0, o1, i1);
                vh.AddTriangle(o0, i1, i0);
            }
        }

        private static void BuildPerimeter(Rect rect, float topLeft, float topRight,
            float bottomRight, float bottomLeft, int segments, List<Vector2> points)
        {
            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            topLeft = Mathf.Clamp(topLeft, 0f, maxRadius);
            topRight = Mathf.Clamp(topRight, 0f, maxRadius);
            bottomRight = Mathf.Clamp(bottomRight, 0f, maxRadius);
            bottomLeft = Mathf.Clamp(bottomLeft, 0f, maxRadius);

            AddCorner(points, rect.xMin + topLeft, rect.yMax - topLeft, topLeft, 180f, 90f, segments);
            AddCorner(points, rect.xMax - topRight, rect.yMax - topRight, topRight, 90f, 0f, segments);
            AddCorner(points, rect.xMax - bottomRight, rect.yMin + bottomRight, bottomRight, 0f, -90f, segments);
            AddCorner(points, rect.xMin + bottomLeft, rect.yMin + bottomLeft, bottomLeft, -90f, -180f, segments);
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
