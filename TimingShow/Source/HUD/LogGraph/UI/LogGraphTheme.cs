using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    internal static class LogGraphTheme
    {
        internal static readonly Color PanelTop = Rgb(30, 33, 48, 0.985f);
        internal static readonly Color PanelBottom = Rgb(17, 19, 29, 0.99f);
        internal static readonly Color HeaderTop = Rgb(39, 43, 62);
        internal static readonly Color HeaderBottom = Rgb(30, 33, 48);
        internal static readonly Color Surface = Rgb(52, 57, 80, 0.92f);
        internal static readonly Color SurfaceHover = Rgb(64, 70, 96, 0.96f);
        internal static readonly Color SurfaceRaised = Rgb(66, 72, 99);
        internal static readonly Color Sunken = new Color(0f, 0f, 0f, 0.30f);
        internal static readonly Color Accent = Rgb(132, 168, 255);
        internal static readonly Color AccentSoft = Rgb(132, 168, 255, 0.16f);
        internal static readonly Color AccentDisabled = Rgb(132, 168, 255, 0.28f);
        internal static readonly Color TextMain = Rgb(234, 238, 250);
        internal static readonly Color TextMuted = Rgb(163, 171, 199);
        internal static readonly Color TextFaint = Rgb(118, 126, 156);
        internal static readonly Color TextDisabled = Rgb(96, 102, 128);
        internal static readonly Color Hair = new Color(1f, 1f, 1f, 0.06f);
        internal static readonly Color HairStrong = new Color(1f, 1f, 1f, 0.10f);
        internal static readonly Color Shadow = new Color(0f, 0f, 0f, 0.50f);
        internal static readonly Color DangerHover = Rgb(255, 120, 120, 0.22f);

        internal static Color Rgb(int r, int g, int b, float a = 1f)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        internal static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        internal static Color Mix(Color from, Color to, float t)
        {
            return Color.Lerp(from, to, Mathf.Clamp01(t));
        }

        internal static void DrawLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color color,
            bool roundCaps = true)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            float half = Mathf.Max(0.01f, thickness * 0.5f);
            if (length < 0.0001f)
            {
                DrawCircle(vh, from, half, color, 10);
                return;
            }

            Vector2 normal = new Vector2(-delta.y, delta.x) / length * half;
            AddQuad(vh, from + normal, to + normal, to - normal, from - normal, color);

            if (!roundCaps) return;

            DrawCircle(vh, from, half, color, 10);
            DrawCircle(vh, to, half, color, 10);
        }

        internal static void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color color, int segments = 16)
        {
            if (radius <= 0f) return;

            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(center.x, center.y, 0f), color, Vector2.zero);

            int count = Mathf.Max(6, segments);
            for (int i = 0; i <= count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(new Vector3(point.x, point.y, 0f), color, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 2 + i);
            }
        }

        internal static void DrawRing(VertexHelper vh, Vector2 center, float radius, float thickness, Color color,
            int segments = 20)
        {
            float outer = radius + thickness * 0.5f;
            float inner = Mathf.Max(0.01f, radius - thickness * 0.5f);
            int count = Mathf.Max(8, segments);

            int start = vh.currentVertCount;
            for (int i = 0; i <= count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(new Vector3(center.x + direction.x * outer, center.y + direction.y * outer, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(center.x + direction.x * inner, center.y + direction.y * inner, 0f), color, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                int index = start + i * 2;
                vh.AddTriangle(index, index + 1, index + 3);
                vh.AddTriangle(index, index + 3, index + 2);
            }
        }

        internal static void DrawChevronDown(VertexHelper vh, Vector2 center, float width, float thickness, Color color)
        {
            float half = width * 0.5f;
            Vector2 left = new Vector2(center.x - half, center.y + half * 0.42f);
            Vector2 middle = new Vector2(center.x, center.y - half * 0.52f);
            Vector2 right = new Vector2(center.x + half, center.y + half * 0.42f);
            DrawLine(vh, left, middle, thickness, color);
            DrawLine(vh, middle, right, thickness, color);
        }

        internal static void DrawClose(VertexHelper vh, Vector2 center, float size, float thickness, Color color)
        {
            float half = size * 0.5f;
            DrawLine(vh, new Vector2(center.x - half, center.y - half), new Vector2(center.x + half, center.y + half),
                thickness, color);
            DrawLine(vh, new Vector2(center.x - half, center.y + half), new Vector2(center.x + half, center.y - half),
                thickness, color);
        }

        internal static void DrawSliders(VertexHelper vh, Vector2 center, float width, float height, float thickness,
            Color color)
        {
            float halfWidth = width * 0.5f;
            float knob = thickness * 1.15f;
            float[] rows = { 0.5f, 0f, -0.5f };
            float[] knobs = { -0.30f, 0.24f, -0.02f };

            for (int i = 0; i < rows.Length; i++)
            {
                float y = center.y + height * rows[i];
                DrawLine(vh, new Vector2(center.x - halfWidth, y), new Vector2(center.x + halfWidth, y), thickness, color);
                DrawCircle(vh, new Vector2(center.x + width * knobs[i], y), knob, color, 12);
            }
        }

        internal static void DrawPulse(VertexHelper vh, Rect rect, float thickness, Color color)
        {
            Vector2[] points =
            {
                new Vector2(0f, 0.50f),
                new Vector2(0.24f, 0.50f),
                new Vector2(0.40f, 0.94f),
                new Vector2(0.60f, 0.06f),
                new Vector2(0.76f, 0.50f),
                new Vector2(1f, 0.50f)
            };

            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLine(vh, MapPoint(rect, points[i]), MapPoint(rect, points[i + 1]), thickness, color);
            }
        }

        internal static Vector2 MapPoint(Rect rect, Vector2 normalized)
        {
            return new Vector2(rect.x + rect.width * normalized.x, rect.y + rect.height * normalized.y);
        }

        internal static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(a.x, a.y, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(b.x, b.y, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x, c.y, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(d.x, d.y, 0f), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
