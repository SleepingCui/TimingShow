using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD.LogGraphSettingsUI
{
    public sealed partial class LogGraphSettingsUI
    {
        internal sealed class SettingsPanelGraphic : MaskableGraphic
        {
            private const float Feather = 1f;

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
                if (area.width <= 0f || area.height <= 0f) return;

                float scale = UiScale;
                Rect panelRect = Snap(area);
                float w = panelRect.width;
                float h = panelRect.height;

                float border = Mathf.Max(1f, Mathf.Round(1.5f * scale));
                float borderFeather = border > 1f ? Feather : 0f;
                float panelCorner = Mathf.Min(LogRoundedRect.PanelRadius * scale, Mathf.Min(w, h) * 0.5f);
                int panelSegments = LogRoundedRect.SegmentsFor(panelCorner);

                LogRoundedRect.DrawShadow(vh, panelRect, panelCorner, SheetShadowSpread * scale,
                    new Vector2(0f, -SheetShadowOffset * scale), SheetShadowColor, 8);
                LogRoundedRect.DrawFilled(vh, panelRect, panelCorner, PanelTopColor, PanelBottomColor,
                    panelSegments, Feather);
                LogRoundedRect.DrawBorder(vh, panelRect, panelCorner, border,
                    SheetBorderColor, SheetBorderColor, panelSegments, borderFeather);

                Rect highlightRect = new Rect(panelRect.x + border, panelRect.y + border,
                    w - border * 2f, h - border * 2f);
                if (highlightRect.width > 0f && highlightRect.height > 0f)
                {
                    LogRoundedRect.DrawBorder(vh, highlightRect, Mathf.Max(0f, panelCorner - border), 1f,
                        SheetHighlightColor, SheetHighlightColor, panelSegments, 0f);
                }

                List<Row> rows = _owner._rows;
                for (int i = 0; i < rows.Count; i++)
                {
                    Row row = rows[i];
                    Rect rowRect = Snap(row.Rect);
                    float rowCorner = Mathf.Min(LogRoundedRect.ControlRadius * scale, rowRect.height * 0.3f);

                    if (ReferenceEquals(row, _owner._hoverRow) && row.Kind != RowKind.Title)
                    {
                        LogRoundedRect.DrawFilled(vh, rowRect, rowCorner,
                            RowHoverColor, RowHoverColor, LogRoundedRect.SegmentsFor(rowCorner), Feather);
                    }

                    switch (row.Kind)
                    {
                        case RowKind.Tab:
                            Color tabColor = _owner._tab == row.TabIndex ? TabActiveColor : TabColor;
                            LogRoundedRect.DrawFilled(vh, rowRect, rowCorner, tabColor, tabColor,
                                LogRoundedRect.SegmentsFor(rowCorner), Feather);
                            break;

                        case RowKind.Button:
                            DrawButton(vh, row, rowRect, scale, _owner._draftDirty && row.HighlightDirty);
                            break;

                        case RowKind.Slider:
                            DrawSlider(vh, row, scale);
                            break;

                        case RowKind.ColorPick:
                            DrawColorChip(vh, row, scale, _owner._colorIndex == row.ColorIndex);
                            break;

                        case RowKind.ColorPreview:
                            DrawColorPreview(vh, row, rowRect, scale);
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

                Rect bar = Snap(row.Bars[0]);
                float trackRadius = bar.height * LogRoundedRect.CapsuleRatio;
                int trackSegments = LogRoundedRect.SegmentsFor(trackRadius);
                LogRoundedRect.DrawFilled(vh, bar, trackRadius, TrackColor, TrackColor, trackSegments, Feather);

                float t = Mathf.InverseLerp(row.Min, row.Max, row.Get());
                if (row.LogScale && row.Min > 0f && row.Max > row.Min)
                    t = Mathf.Log(Mathf.Max(row.Min, row.Get()) / row.Min) / Mathf.Log(row.Max / row.Min);

                t = Mathf.Clamp01(t);
                float fillRight = bar.x + bar.width * t;
                if (fillRight > bar.x)
                {
                    Rect fill = new Rect(bar.x, bar.y, fillRight - bar.x, bar.height);
                    LogRoundedRect.DrawFilled(vh, fill, trackRadius, AccentColor, AccentColor, trackSegments, Feather);
                }

                float knobWidth = Mathf.Max(3f, 3f * scale);
                float knobCenter = Mathf.Clamp(fillRight, bar.x + knobWidth * 0.5f, bar.xMax - knobWidth * 0.5f);
                Rect knob = new Rect(knobCenter - knobWidth * 0.5f, bar.y - 2f * scale,
                    knobWidth, bar.height + 4f * scale);
                float knobCorner = knobWidth * LogRoundedRect.CapsuleRatio;
                LogRoundedRect.DrawFilled(vh, knob, knobCorner, ValueColor, ValueColor,
                    LogRoundedRect.SegmentsFor(knobCorner), Feather);
            }

            private static void DrawColorChip(VertexHelper vh, Row row, float scale, bool selected)
            {
                if (row.GetColor == null) return;

                Rect rowRect = Snap(row.Rect);
                float rowCorner = Mathf.Min(LogRoundedRect.ControlRadius * scale, rowRect.height * 0.3f);
                int rowSegments = LogRoundedRect.SegmentsFor(rowCorner);
                Color fill = selected ? TabActiveColor : TabColor;
                LogRoundedRect.DrawFilled(vh, rowRect, rowCorner, fill, fill, rowSegments, Feather);

                float swatch = ColorSwatchSize(rowRect.height, scale);
                float sx = rowRect.x + 4f * scale;
                float sy = rowRect.y + (rowRect.height - swatch) * 0.5f;
                Rect swatchRect = new Rect(sx, sy, swatch, swatch);
                float swatchCorner = Mathf.Min(LogRoundedRect.SwatchRadius * scale, swatch * 0.5f);
                int swatchSegments = LogRoundedRect.SegmentsFor(swatchCorner);
                LogRoundedRect.DrawFilled(vh, swatchRect, swatchCorner,
                    new Color(0.5f, 0.5f, 0.5f, 1f), new Color(0.5f, 0.5f, 0.5f, 1f), swatchSegments, Feather);
                LogRoundedRect.DrawFilled(vh, swatchRect, swatchCorner, row.GetColor(), row.GetColor(),
                    swatchSegments, Feather);

                if (row.ResetRect.width > 0f)
                {
                    Rect resetRect = Snap(row.ResetRect);
                    float resetCorner = Mathf.Min(LogRoundedRect.SmallRadius * scale, resetRect.height * 0.35f);
                    int resetSegments = LogRoundedRect.SegmentsFor(resetCorner);
                    Color resetFill = new Color(1f, 1f, 1f, 0.10f);
                    LogRoundedRect.DrawFilled(vh, resetRect, resetCorner, resetFill, resetFill, resetSegments, Feather);
                    LogRoundedRect.DrawBorder(vh, resetRect, resetCorner,
                        Mathf.Max(1f, Mathf.Round(1.2f * scale)), BorderColor, BorderColor, resetSegments, 0f);
                }

                float thickness = selected
                    ? Mathf.Max(1.5f, Mathf.Round(1.6f * scale))
                    : Mathf.Max(1f, Mathf.Round(1.2f * scale));
                Color edge = selected ? AccentColor : BorderColor;
                LogRoundedRect.DrawBorder(vh, rowRect, rowCorner, thickness, edge, edge, rowSegments,
                    thickness > 1f ? Feather : 0f);
            }

            private static void DrawColorPreview(VertexHelper vh, Row row, Rect rowRect, float scale)
            {
                if (row.GetColor == null) return;

                float corner = Mathf.Min(LogRoundedRect.SmallRadius * scale, rowRect.height * 0.35f);
                int segments = LogRoundedRect.SegmentsFor(corner);
                LogRoundedRect.DrawFilled(vh, rowRect, corner, PanelColor, PanelColor, segments, Feather);
                LogRoundedRect.DrawFilled(vh, rowRect, corner, row.GetColor(), row.GetColor(), segments, Feather);
                float thickness = Mathf.Max(1f, Mathf.Round(1.2f * scale));
                LogRoundedRect.DrawBorder(vh, rowRect, corner, thickness, BorderColor, BorderColor, segments,
                    thickness > 1f ? Feather : 0f);
            }

            private static void DrawButton(VertexHelper vh, Row row, Rect rowRect, float scale, bool dirty)
            {
                bool enabled = row.IsEnabled == null || row.IsEnabled();
                Color fill = dirty ? LogGraphTheme.WithAlpha(AccentColor, 0.90f) : RowHoverColor;
                Color edge = dirty ? LogGraphTheme.WithAlpha(AccentColor, 0.95f) : BorderColor;

                if (!enabled)
                {
                    fill.a *= 0.35f;
                    edge.a *= 0.4f;
                }

                float corner = Mathf.Min(LogRoundedRect.ControlRadius * scale, rowRect.height * 0.35f);
                int segments = LogRoundedRect.SegmentsFor(corner);
                LogRoundedRect.DrawFilled(vh, rowRect, corner, fill, fill, segments, Feather);
                float thickness = Mathf.Max(1f, Mathf.Round(1.2f * scale));
                LogRoundedRect.DrawBorder(vh, rowRect, corner, thickness, edge, edge, segments,
                    thickness > 1f ? Feather : 0f);
            }

            private static void DrawToggle(VertexHelper vh, Row row, float scale)
            {
                if (row.Bars == null || row.Bars.Length == 0 || row.GetToggle == null) return;

                Rect box = Snap(row.Bars[0]);
                bool on = row.GetToggle();

                float corner = box.height * 0.5f;
                int segments = LogRoundedRect.SegmentsFor(corner);
                Color track = on ? AccentColor : TrackColor;
                LogRoundedRect.DrawFilled(vh, box, corner, track, track, segments, Feather);

                if (!on)
                {
                    float thickness = Mathf.Max(1f, Mathf.Round(1f * scale));
                    LogRoundedRect.DrawBorder(vh, box, corner, thickness, BorderColor, BorderColor, segments,
                        thickness > 1f ? Feather : 0f);
                }

                float inset = Mathf.Max(1.5f * scale, box.height * 0.14f);
                float knobRadius = Mathf.Max(1f, box.height * 0.5f - inset);
                float knobX = on
                    ? box.xMax - inset - knobRadius
                    : box.x + inset + knobRadius;
                LogGraphTheme.DrawCircle(vh,
                    new Vector2(knobX, box.center.y), knobRadius,
                    on ? ValueColor : LogGraphTheme.TextMuted, 18, Feather);
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

            // Whole-pixel edges keep one-pixel strokes and borders at a constant weight.
            private static Rect Snap(Rect rect)
            {
                return new Rect(Mathf.Round(rect.x), Mathf.Round(rect.y),
                    Mathf.Max(1f, Mathf.Round(rect.width)), Mathf.Max(1f, Mathf.Round(rect.height)));
            }
        }
    }
}
