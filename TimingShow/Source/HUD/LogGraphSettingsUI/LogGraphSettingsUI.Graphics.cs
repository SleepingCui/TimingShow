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

                Rect panelRect = new Rect(x0, y0, w, h);
                float panelCorner = Mathf.Min(LogRoundedRect.PanelRadius * scale, Mathf.Min(w, h) * 0.5f);
                LogRoundedRect.DrawFilled(vh, panelRect, panelCorner, PanelColor, 8);
                LogRoundedRect.DrawBorder(vh, panelRect, panelCorner, border, BorderColor, 8);

                List<Row> rows = _owner._rows;
                for (int i = 0; i < rows.Count; i++)
                {
                    Row row = rows[i];

                    if (ReferenceEquals(row, _owner._hoverRow) && row.Kind != RowKind.Title)
                    {
                        LogRoundedRect.DrawFilled(vh, row.Rect,
                            Mathf.Min(LogRoundedRect.ControlRadius * scale, row.Rect.height * 0.3f), RowHoverColor, 6);
                    }

                    switch (row.Kind)
                    {
                        case RowKind.Tab:
                            LogRoundedRect.DrawFilled(vh, row.Rect,
                                Mathf.Min(LogRoundedRect.ControlRadius * scale, row.Rect.height * 0.3f),
                                _owner._tab == row.TabIndex ? TabActiveColor : TabColor, 6);
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
                float trackRadius = bar.height * LogRoundedRect.CapsuleRatio;
                LogRoundedRect.DrawFilled(vh, bar, trackRadius, TrackColor, 8);

                float t = Mathf.InverseLerp(row.Min, row.Max, row.Get());
                if (row.LogScale && row.Min > 0f && row.Max > row.Min)
                    t = Mathf.Log(Mathf.Max(row.Min, row.Get()) / row.Min) / Mathf.Log(row.Max / row.Min);

                t = Mathf.Clamp01(t);
                float fillRight = bar.x + bar.width * t;
                if (fillRight > bar.x)
                {
                    Rect fill = new Rect(bar.x, bar.y, fillRight - bar.x, bar.height);
                    LogRoundedRect.DrawFilled(vh, fill, trackRadius, AccentColor, 8);
                }

                float knobWidth = Mathf.Max(3f, 3f * scale);
                float knobCenter = Mathf.Clamp(fillRight, bar.x + knobWidth * 0.5f, bar.xMax - knobWidth * 0.5f);
                Rect knob = new Rect(knobCenter - knobWidth * 0.5f, bar.y - 2f * scale,
                    knobWidth, bar.height + 4f * scale);
                LogRoundedRect.DrawFilled(vh, knob, knobWidth * LogRoundedRect.CapsuleRatio, ValueColor, 6);
            }

            private static void DrawColorChip(VertexHelper vh, Row row, float scale, bool selected)
            {
                if (row.GetColor == null) return;

                float rowCorner = Mathf.Min(LogRoundedRect.ControlRadius * scale, row.Rect.height * 0.3f);
                LogRoundedRect.DrawFilled(vh, row.Rect, rowCorner,
                    selected ? TabActiveColor : new Color(1f, 1f, 1f, 0.035f), 6);

                float swatch = ColorSwatchSize(row.Rect.height, scale);
                float sx = row.Rect.x + 4f * scale;
                float sy = row.Rect.y + (row.Rect.height - swatch) * 0.5f;
                Color color = row.GetColor();
                Rect swatchRect = new Rect(sx, sy, swatch, swatch);
                float swatchCorner = Mathf.Min(LogRoundedRect.SwatchRadius * scale, swatch * 0.5f);
                LogRoundedRect.DrawFilled(vh, swatchRect, swatchCorner,
                    new Color(0.5f, 0.5f, 0.5f, 1f), 4);
                LogRoundedRect.DrawFilled(vh, swatchRect, swatchCorner, color, 4);

                if (row.ResetRect.width > 0f)
                {
                    float resetCorner = Mathf.Min(LogRoundedRect.SmallRadius * scale, row.ResetRect.height * 0.35f);
                    LogRoundedRect.DrawFilled(vh, row.ResetRect, resetCorner,
                        new Color(1f, 1f, 1f, 0.10f), 6);
                    LogRoundedRect.DrawBorder(vh, row.ResetRect, resetCorner,
                        Mathf.Max(1f, 1.2f * scale), BorderColor, 6);
                }

                float thickness = selected ? Mathf.Max(1.5f, 1.6f * scale) : Mathf.Max(1f, 1.2f * scale);
                Color edge = selected ? AccentColor : BorderColor;
                LogRoundedRect.DrawBorder(vh, row.Rect, rowCorner, thickness, edge, 6);
            }

            private static void DrawColorPreview(VertexHelper vh, Row row, float scale)
            {
                if (row.GetColor == null) return;

                float corner = Mathf.Min(LogRoundedRect.SmallRadius * scale, row.Rect.height * 0.35f);
                LogRoundedRect.DrawFilled(vh, row.Rect, corner, PanelColor, 6);
                LogRoundedRect.DrawFilled(vh, row.Rect, corner, row.GetColor(), 6);
                LogRoundedRect.DrawBorder(vh, row.Rect, corner,
                    Mathf.Max(1f, 1.2f * scale), BorderColor, 6);
            }
            
            private static void DrawButton(VertexHelper vh, Row row, float scale, bool dirty)
            {
                Color fill = dirty ? new Color(0.20f, 0.52f, 0.88f, 0.95f) : new Color(1f, 1f, 1f, 0.10f);
                Color edge = dirty ? new Color(0.55f, 0.82f, 1f, 0.95f) : BorderColor;

                float corner = Mathf.Min(LogRoundedRect.ControlRadius * scale, row.Rect.height * 0.35f);
                LogRoundedRect.DrawFilled(vh, row.Rect, corner, fill, 6);
                LogRoundedRect.DrawBorder(vh, row.Rect, corner,
                    Mathf.Max(1f, 1.2f * scale), edge, 6);
            }

            private static void DrawToggle(VertexHelper vh, Row row, float scale)
            {
                if (row.Bars == null || row.Bars.Length == 0 || row.GetToggle == null) return;

                Rect box = row.Bars[0];
                bool on = row.GetToggle();

                float corner = Mathf.Min(LogRoundedRect.SmallRadius * scale,
                    Mathf.Min(box.width, box.height) * LogRoundedRect.CapsuleRatio);
                LogRoundedRect.DrawFilled(vh, box, corner, on ? AccentColor : TrackColor, 6);
                LogRoundedRect.DrawFilled(vh, box, corner,
                    new Color(0f, 0f, 0f, on ? 0f : 0.25f), 6);

                if (on)
                {
                    float inset = Mathf.Max(1f, 3f * scale);
                    Rect inner = new Rect(box.x + inset, box.y + inset,
                        Mathf.Max(0f, box.width - inset * 2f), Mathf.Max(0f, box.height - inset * 2f));
                    LogRoundedRect.DrawFilled(vh, inner, Mathf.Max(0f, corner - inset),
                        new Color(0.05f, 0.06f, 0.09f, 1f), 6);
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
