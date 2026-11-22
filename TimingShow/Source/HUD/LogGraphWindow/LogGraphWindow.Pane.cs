using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        public interface ILogGraphPanelInteraction
        {
            void OnPanelPointerDown(PointerEventData eventData);
            void OnPanelDrag(PointerEventData eventData);
            void OnPanelPointerUp(PointerEventData eventData);
            void OnPanelPointerClick(PointerEventData eventData);
            void OnPanelPointerMove(PointerEventData eventData);
        }
        
        internal class LogGraphWindowPanel : MaskableGraphic,
            IPointerDownHandler, IDragHandler, IPointerUpHandler,
            IPointerClickHandler, IPointerMoveHandler,
            IPointerExitHandler
        {
            private ILogGraphPanelInteraction _dragProxy;
            private ILogGraphPanelInteraction _hoverProxy;
            private IPointerExitHandler _exitProxy;
            private bool _hovered;
            private bool _settingsHovered;
            private bool _settingsActive;

            protected override void Awake()
            {
                base.Awake();

                color = Color.white;       
                raycastTarget = true;     
                useLegacyMeshGeneration = false;
            }

            internal void SetDragProxy(ILogGraphPanelInteraction proxy)
            {
                _dragProxy = proxy;
            }

            internal void SetHoverProxy(ILogGraphPanelInteraction proxy, IPointerExitHandler exitProxy)
            {
                _hoverProxy = proxy;
                _exitProxy = exitProxy;
            }

            internal void SetHover(bool hovered)
            {
                if (_hovered == hovered) return;

                _hovered = hovered;
                SetVerticesDirty();
            }

            internal void SetSettingsHover(bool hovered)
            {
                if (_settingsHovered == hovered) return;

                _settingsHovered = hovered;
                SetVerticesDirty();
            }
            
            internal void SetSettingsActive(bool active)
            {
                if (_settingsActive == active) return;

                _settingsActive = active;
                SetVerticesDirty();
            }
            
            internal void MarkDirty()
            {
                SetVerticesDirty();
            }


            public void OnPointerDown(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelPointerDown(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelDrag(eventData);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelPointerUp(eventData);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
                if (_dragProxy != null) _dragProxy.OnPanelPointerClick(eventData);
            }

            public void OnPointerMove(PointerEventData eventData)
            {
                if (eventData == null) return;

                if (_hoverProxy != null) _hoverProxy.OnPanelPointerMove(eventData);
                else RefreshHover(eventData.position);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (_exitProxy != null) _exitProxy.OnPointerExit(eventData);
                else
                {
                    SetHover(false);
                    SetSettingsHover(false);
                }
            }

            private void RefreshHover(Vector2 screenPosition)
            {
                RectTransform rt = rectTransform;
                if (rt == null) return;

                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPosition, null, out local))
                {
                    SetHover(false);
                    SetSettingsHover(false);
                    return;
                }

                Rect rect = rt.rect;
                float size = Mathf.Min(CloseButtonSize, Mathf.Min(rect.width, rect.height));
                float maxX = rect.xMax - CloseButtonInset;
                float maxY = rect.yMax - ButtonInsetY;
                float minX = maxX - size;
                float minY = maxY - size;

                bool onClose = local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
                SetHover(onClose);

                float setMaxX = rect.xMax - CloseButtonInset - CloseButtonSize - ButtonGap;
                float setMaxY = rect.yMax - ButtonInsetY;
                float setMinX = setMaxX - SettingsButtonSize;
                float setMinY = setMaxY - SettingsButtonSize;
                SetSettingsHover(!onClose && local.x >= setMinX && local.x <= setMaxX
                    && local.y >= setMinY && local.y <= setMaxY);
            }
            
            protected override void OnRectTransformDimensionsChange()
            {
                base.OnRectTransformDimensionsChange();
                SetVerticesDirty();
            }
            
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                Rect rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 0f, 0f);
                float w = rect.width;
                float h = rect.height;
                if (w <= 0f || h <= 0f) return;

                float x0 = rect.xMin;
                float y0 = rect.yMin;
                float x1 = rect.xMax;
                float y1 = rect.yMax;
                float corner = Mathf.Min(LogRoundedRect.WindowRadius * UiScale, Mathf.Min(w, h) * 0.5f);
                float titleBottom = Mathf.Max(y0, y1 - HeaderHeight);

                Rect windowRect = new Rect(x0, y0, w, h);
                LogRoundedRect.DrawShadow(vh, windowRect, corner, ShadowSpread * UiScale,
                    new Vector2(0f, -ShadowOffset * UiScale), ShadowColor, 8);
                LogRoundedRect.DrawFilled(vh, windowRect, corner, PanelTopColor, PanelBottomColor, 10, 1f);

                if (y1 > titleBottom && w > 1f)
                {
                    Rect titleRect = new Rect(x0, titleBottom, w, y1 - titleBottom);
                    LogRoundedRect.DrawFilled(vh, titleRect, corner, corner, 0f, 0f,
                        TitleBarTopColor, TitleBarBottomColor, 8, 1f);

                    float edge = Mathf.Max(1f, UiScale);
                    DrawQuad(vh, new Vector2(x0, titleBottom - edge), new Vector2(x1, titleBottom), HeaderEdgeColor);

                    float fade = 12f * UiScale;
                    LogRoundedRect.DrawFilled(vh, new Rect(x0, titleBottom - fade, w, fade), 0f,
                        new Color(0f, 0f, 0f, 0.18f), new Color(0f, 0f, 0f, 0f), 2, 0f);
                }

                Rect highlightRect = new Rect(x0 + 0.5f, y0 + 0.5f, w - 1f, h - 1f);
                if (highlightRect.width > 0f && highlightRect.height > 0f)
                {
                    LogRoundedRect.DrawBorder(vh, highlightRect, Mathf.Max(0f, corner - 0.5f), 1f,
                        WindowHighlightColor, WindowHighlightColor, 10, 0f);
                }

                DrawAppMark(vh, x0, y1);
                
                
                float closeMaxX = x1 - CloseButtonInset;
                float closeMaxY = y1 - ButtonInsetY;
                float closeMinX = closeMaxX - CloseButtonSize;
                float closeMinY = closeMaxY - CloseButtonSize;
                if (closeMinX > x0 && closeMinY > y0)
                {
                    Color closeColor = _hovered ? CloseHoverColor : CloseNormalColor;
                    LogRoundedRect.DrawFilled(vh,
                        new Rect(closeMinX, closeMinY, CloseButtonSize, CloseButtonSize),
                        ButtonCorner(CloseButtonSize), closeColor, closeColor, 8, 1f);

                    float iconSize = Mathf.Max(4f, CloseButtonSize - CloseGlyphInset * 2f);
                    Vector2 iconCenter = new Vector2((closeMinX + closeMaxX) * 0.5f, (closeMinY + closeMaxY) * 0.5f);
                    Color iconColor = _hovered ? LogGraphTheme.Rgb(255, 196, 196) : IconColor;
                    LogGraphTheme.DrawClose(vh, iconCenter, iconSize, Mathf.Max(1f, CloseGlyphThickness), iconColor);
                }
                
                float setMaxX = closeMinX - ButtonGap;
                float setMaxY = y1 - ButtonInsetY;
                float setMinX = setMaxX - SettingsButtonSize;
                float setMinY = setMaxY - SettingsButtonSize;
                if (setMinX > x0 && setMinY > y0)
                {
                    Color setColor = _settingsActive ? SettingsButtonActiveColor : (_settingsHovered ? SettingsButtonHoverColor : CloseNormalColor);
                    LogRoundedRect.DrawFilled(vh,
                        new Rect(setMinX, setMinY, SettingsButtonSize, SettingsButtonSize),
                        ButtonCorner(SettingsButtonSize), setColor, setColor, 8, 1f);

                    float iconWidth = Mathf.Max(4f, SettingsButtonSize - CloseGlyphInset * 2f);
                    Vector2 iconCenter = new Vector2((setMinX + setMaxX) * 0.5f, (setMinY + setMaxY) * 0.5f);
                    Color iconColor = _settingsActive ? LogGraphTheme.Accent : IconColor;
                    LogGraphTheme.DrawSliders(vh, iconCenter, iconWidth, iconWidth * 0.66f,
                        Mathf.Max(1f, CloseGlyphThickness), iconColor);
                }
                
                Rect sidebar = SidebarRect(w, h);
                if (_settingsActive && sidebar.width > 0f && sidebar.height > 0f)
                {
                    float sidebarCorner = Mathf.Min(LogRoundedRect.InnerPanelRadius * UiScale,
                        Mathf.Min(sidebar.width, sidebar.height) * 0.5f);
                    LogRoundedRect.DrawFilled(vh, sidebar, sidebarCorner,
                        StatsPanelTopColor, StatsPanelBottomColor, 8, 1f);
                    LogRoundedRect.DrawBorder(vh, sidebar, sidebarCorner, 1f,
                        StatsPanelBorderColor, StatsPanelBorderColor, 8, 0f);

                    float dividerThickness = Mathf.Max(1f, UiScale);
                    float dividerX = sidebar.xMin - StatsPanelGap * 0.5f;
                    DrawQuad(vh, new Vector2(dividerX, sidebar.yMin), new Vector2(dividerX + dividerThickness, sidebar.yMax), StatsDividerColor);

                    float rowHeaderHeight, rowsOffset, rowHeight, rowLabelHeight, rowValueHeight;
                    StatsRowMetrics(sidebar.height, out rowHeaderHeight, out rowsOffset, out rowHeight, out rowLabelHeight, out rowValueHeight);
                    
                    float headerLineY = sidebar.yMax - rowHeaderHeight;
                    if (headerLineY > sidebar.yMin + dividerThickness * 2f)
                    {
                        DrawQuad(vh, new Vector2(sidebar.xMin, headerLineY), new Vector2(sidebar.xMax, headerLineY + dividerThickness), StatsDividerColor);
                    }
                    
                    for (int i = 1; i < StatsRowCount; i++)
                    {
                        float lineY = headerLineY - i * rowHeight;
                        if (lineY <= sidebar.yMin + dividerThickness * 2f) break;
                        DrawQuad(vh, new Vector2(sidebar.xMin, lineY), new Vector2(sidebar.xMax, lineY + dividerThickness), StatsDividerColor);
                    }
                }
                
                float gripThickness = Mathf.Max(1f, GripLineThickness);
                float gripMax = GripSize;
                Color grip = GripColor;
                DrawThickLine(vh, new Vector2(x1 - GripInset - gripMax * 0.375f, y0 + GripInset), new Vector2(x1 - GripInset, y0 + GripInset + gripMax * 0.375f), gripThickness, grip);
                DrawThickLine(vh, new Vector2(x1 - GripInset - gripMax * 0.6875f, y0 + GripInset), new Vector2(x1 - GripInset, y0 + GripInset + gripMax * 0.6875f), gripThickness, grip);
                DrawThickLine(vh, new Vector2(x1 - GripInset - gripMax, y0 + GripInset), new Vector2(x1 - GripInset, y0 + GripInset + gripMax), gripThickness, grip);
            }

            private static Color IconColor
            {
                get { return LogGraphTheme.WithAlpha(LogGraphTheme.TextMain, 0.78f); }
            }

            private static float ButtonCorner(float size)
            {
                return Mathf.Min(12f * UiScale, size * 0.40f);
            }

            private static void DrawAppMark(VertexHelper vh, float x0, float y1)
            {
                float size = AppMarkSize;
                float left = x0 + HeaderInset;
                float bottom = y1 - (HeaderHeight - size) * 0.5f - size;
                Rect mark = new Rect(left, bottom, size, size);
                LogRoundedRect.DrawFilled(vh, mark, Mathf.Min(13f * UiScale, size * 0.5f), LogGraphTheme.AccentSoft,
                    LogGraphTheme.AccentSoft, 8, 1f);

                Rect glyph = new Rect(mark.x + size * 0.24f, mark.y + size * 0.28f, size * 0.52f, size * 0.44f);
                LogGraphTheme.DrawPulse(vh, glyph, Mathf.Max(1.4f, 2f * UiScale), LogGraphTheme.Accent);
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

            private static void DrawThickLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color color)
            {
                float dx = to.x - from.x;
                float dy = to.y - from.y;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                if (length <= 0.0001f) return;

                Vector2 direction = new Vector2(dx / length, dy / length);
                Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

                int baseIndex = vh.currentVertCount;
                vh.AddVert(new Vector3(from.x - normal.x, from.y - normal.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(from.x + normal.x, from.y + normal.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(to.x + normal.x, to.y + normal.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(to.x - normal.x, to.y - normal.y, 0f), color, Vector2.zero);
                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            }
        }
    }
}
