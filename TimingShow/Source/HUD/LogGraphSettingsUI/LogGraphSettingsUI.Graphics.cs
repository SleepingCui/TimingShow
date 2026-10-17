using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD.LogGraphSettingsUI
{
    public sealed partial class LogGraphSettingsUI
    {
        internal sealed class SettingsPanelGraphic : MaskableGraphic
        {
            private LogGraphSettingsUI _owner;

            protected override void Awake()
            {
                base.Awake();
                color = Color.white;
                raycastTarget = false;     
                useLegacyMeshGeneration = false;
            }

            internal void Bind(LogGraphSettingsUI owner)
            {
                _owner = owner;
            }

            internal void MarkDirty()
            {
                SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                if (_owner == null) return;

                RectTransform rect = rectTransform;
                if (rect == null) return;

                Rect area = rect.rect;
                float w = area.width;
                float h = area.height;
                if (w <= 0f || h <= 0f) return;

                float x0 = area.xMin;
                float y0 = area.yMin;
                float x1 = area.xMax;
                float y1 = area.yMax;

                float scale = UiScale;
                float border = Mathf.Max(1f, 1.5f * scale);

                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x1, y1), PanelColor);
                DrawQuad(vh, new Vector2(x0, y0), new Vector2(x0 + border, y1), BorderColor);
                DrawQuad(vh, new Vector2(x1 - border, y0), new Vector2(x1, y1), BorderColor);

                List<Row> rows = _owner._rows;
                for (int i = 0; i < rows.Count; i++)
                {
                    Row row = rows[i];

                    if (ReferenceEquals(row, _owner._hoverRow) && row.Kind != RowKind.Title)
                        DrawQuad(vh, row.Rect.min, row.Rect.max, RowHoverColor);

                    switch (row.Kind)
                    {
                        case RowKind.Tab:
                            DrawQuad(vh, row.Rect.min, row.Rect.max,
                                _owner._tab == row.TabIndex ? TabActiveColor : TabColor);
                            break;

                        case RowKind.Button:
                            DrawButton(vh, row, scale, _owner._draftDirty);
                            break;

                        case RowKind.Slider:
                            DrawSlider(vh, row, scale);
                            break;

                        case RowKind.ColorPick:
                            DrawColorChip(vh, row, scale, _owner._colorIndex == row.ColorIndex);
                            break;

                        case RowKind.ColorPreview:
                            DrawColorPreview(vh, row, scale);
                            break;

                        case RowKind.Toggle:
                            DrawToggle(vh, row, scale);
                            break;
                    }
                }
            }

            private static void DrawSlider(VertexHelper vh, Row row, float scale)
            {
                if (row.Bars == null || row.Bars.Length == 0 || row.Get == null) return;

                Rect bar = row.Bars[0];
                DrawQuad(vh, bar.min, bar.max, TrackColor);

                float t = Mathf.InverseLerp(row.Min, row.Max, row.Get());
                if (row.LogScale && row.Min > 0f && row.Max > row.Min)
                    t = Mathf.Log(Mathf.Max(row.Min, row.Get()) / row.Min) / Mathf.Log(row.Max / row.Min);

                t = Mathf.Clamp01(t);
                float fillRight = bar.x + bar.width * t;
                if (fillRight > bar.x)
                    DrawQuad(vh, new Vector2(bar.x, bar.y), new Vector2(fillRight, bar.yMax), AccentColor);

                float knobWidth = Mathf.Max(3f, 3f * scale);
                float knobCenter = Mathf.Clamp(fillRight, bar.x + knobWidth * 0.5f, bar.xMax - knobWidth * 0.5f);
                DrawQuad(vh,
                    new Vector2(knobCenter - knobWidth * 0.5f, bar.y - 2f * scale),
                    new Vector2(knobCenter + knobWidth * 0.5f, bar.yMax + 2f * scale),
                    ValueColor);
            }

            private static void DrawColorChip(VertexHelper vh, Row row, float scale, bool selected)
            {
                if (row.GetColor == null) return;

                DrawQuad(vh, row.Rect.min, row.Rect.max,
                    selected ? TabActiveColor : new Color(1f, 1f, 1f, 0.035f));

                float swatch = ColorSwatchSize(row.Rect.height, scale);
                float sx = row.Rect.x + 4f * scale;
                float sy = row.Rect.y + (row.Rect.height - swatch) * 0.5f;
                Color color = row.GetColor();

                DrawQuad(vh, new Vector2(sx, sy), new Vector2(sx + swatch, sy + swatch),
                    new Color(0.5f, 0.5f, 0.5f, 1f));
                DrawQuad(vh, new Vector2(sx, sy), new Vector2(sx + swatch, sy + swatch), color);

                if (row.ResetRect.width > 0f)
                {
                    DrawQuad(vh, row.ResetRect.min, row.ResetRect.max, new Color(1f, 1f, 1f, 0.10f));

                    float bt = Mathf.Max(1f, 1.2f * scale);
                    DrawQuad(vh, row.ResetRect.min, new Vector2(row.ResetRect.xMax, row.ResetRect.yMin + bt), BorderColor);
                    DrawQuad(vh, new Vector2(row.ResetRect.xMin, row.ResetRect.yMax - bt), row.ResetRect.max, BorderColor);
                    DrawQuad(vh, row.ResetRect.min, new Vector2(row.ResetRect.xMin + bt, row.ResetRect.yMax), BorderColor);
                    DrawQuad(vh, new Vector2(row.ResetRect.xMax - bt, row.ResetRect.yMin), row.ResetRect.max, BorderColor);
                }

                float thickness = selected ? Mathf.Max(1.5f, 1.6f * scale) : Mathf.Max(1f, 1.2f * scale);
                Color edge = selected ? AccentColor : BorderColor;
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMax, row.Rect.yMin + thickness), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMin, row.Rect.yMax - thickness), row.Rect.max, edge);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMin + thickness, row.Rect.yMax), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMax - thickness, row.Rect.yMin), row.Rect.max, edge);
            }

            private static void DrawColorPreview(VertexHelper vh, Row row, float scale)
            {
                if (row.GetColor == null) return;

                DrawQuad(vh, row.Rect.min, row.Rect.max, PanelColor);
                DrawQuad(vh, row.Rect.min, row.Rect.max, row.GetColor());

                float t = Mathf.Max(1f, 1.2f * scale);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMax, row.Rect.yMin + t), BorderColor);
                DrawQuad(vh, new Vector2(row.Rect.xMin, row.Rect.yMax - t), row.Rect.max, BorderColor);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMin + t, row.Rect.yMax), BorderColor);
                DrawQuad(vh, new Vector2(row.Rect.xMax - t, row.Rect.yMin), row.Rect.max, BorderColor);
            }
            
            private static void DrawButton(VertexHelper vh, Row row, float scale, bool dirty)
            {
                Color fill = dirty ? new Color(0.20f, 0.52f, 0.88f, 0.95f) : new Color(1f, 1f, 1f, 0.10f);
                Color edge = dirty ? new Color(0.55f, 0.82f, 1f, 0.95f) : BorderColor;

                DrawQuad(vh, row.Rect.min, row.Rect.max, fill);

                float t = Mathf.Max(1f, 1.2f * scale);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMax, row.Rect.yMin + t), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMin, row.Rect.yMax - t), row.Rect.max, edge);
                DrawQuad(vh, row.Rect.min, new Vector2(row.Rect.xMin + t, row.Rect.yMax), edge);
                DrawQuad(vh, new Vector2(row.Rect.xMax - t, row.Rect.yMin), row.Rect.max, edge);
            }

            private static void DrawToggle(VertexHelper vh, Row row, float scale)
            {
                if (row.Bars == null || row.Bars.Length == 0 || row.GetToggle == null) return;

                Rect box = row.Bars[0];
                bool on = row.GetToggle();

                DrawQuad(vh, box.min, box.max, on ? AccentColor : TrackColor);
                DrawQuad(vh,
                    new Vector2(box.x, box.y),
                    new Vector2(box.xMax, box.yMax),
                    new Color(0f, 0f, 0f, on ? 0f : 0.25f));

                if (on)
                {
                    float inset = Mathf.Max(1f, 3f * scale);
                    DrawQuad(vh,
                        new Vector2(box.x + inset, box.y + inset),
                        new Vector2(box.xMax - inset, box.yMax - inset),
                        new Color(0.05f, 0.06f, 0.09f, 1f));
                }
            }

            private static void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
            {
                if (max.x <= min.x || max.y <= min.y) return;

                int baseIndex = vh.currentVertCount;
                vh.AddVert(new Vector3(min.x, min.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(min.x, max.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(max.x, max.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(max.x, min.y, 0f), color, Vector2.zero);
                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            }
        }
    }
}
