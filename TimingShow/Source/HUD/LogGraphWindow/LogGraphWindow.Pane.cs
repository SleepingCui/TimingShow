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
                float maxY = rect.yMax - CloseButtonInset;
                float minX = maxX - size;
                float minY = maxY - size;

                bool onClose = local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
                SetHover(onClose);

                float setMaxX = rect.xMax - CloseButtonInset - CloseButtonSize - ButtonGap;
                float setMaxY = rect.yMax - CloseButtonInset;
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
                float border = Mathf.Min(BorderThickness, Mathf.Min(w, h) * 0.25f);
                float corner = Mathf.Min(LogRoundedRect.WindowRadius * UiScale, Mathf.Min(w, h) * 0.5f);
                float titleTop = y1 - border;
                float titleBottom = Mathf.Max(y0 + border, titleTop - HeaderHeight);

                Rect windowRect = new Rect(x0, y0, w, h);
                LogRoundedRect.DrawFilled(vh, windowRect, corner, PanelColor, 8);
                LogRoundedRect.DrawBorder(vh, windowRect, corner, border, BorderColor, 8);

                if (titleTop > titleBottom && w > border * 2f)
                {
                    float titleCorner = Mathf.Max(0f, corner - border);
                    Rect titleRect = new Rect(x0 + border, titleBottom, w - border * 2f, titleTop - titleBottom);
                    LogRoundedRect.DrawFilled(vh, titleRect, titleCorner, titleCorner, 0f, 0f, TitleBarColor, 8);

                    float headerEdge = Mathf.Max(1f, border * 0.5f);
                    Color headerEdgeColor = BorderColor;
                    headerEdgeColor.a *= 0.45f;
                    DrawQuad(vh, new Vector2(x0 + border, titleBottom), new Vector2(x1 - border, titleBottom + headerEdge), headerEdgeColor);
                }
                
                
                float closeMaxX = x1 - CloseButtonInset;
                float closeMaxY = y1 - CloseButtonInset;
                float closeMinX = closeMaxX - CloseButtonSize;
                float closeMinY = closeMaxY - CloseButtonSize;
                if (closeMinX > x0 && closeMinY > y0)
                {
                    LogRoundedRect.DrawFilled(vh,
                        new Rect(closeMinX, closeMinY, CloseButtonSize, CloseButtonSize),
                        LogRoundedRect.ControlRadius * UiScale, _hovered ? CloseHoverColor : CloseNormalColor, 6);

                    float glyphThickness = Mathf.Max(1f, CloseGlyphThickness);
                    float glyphInset = CloseGlyphInset;
                    Vector2 gMin = new Vector2(closeMinX + glyphInset, closeMinY + glyphInset);
                    Vector2 gMax = new Vector2(closeMaxX - glyphInset, closeMaxY - glyphInset);
                    if (gMax.x > gMin.x && gMax.y > gMin.y)
                    {
                        DrawThickLine(vh, new Vector2(gMin.x, gMin.y), new Vector2(gMax.x, gMax.y), glyphThickness, Color.white);
                        DrawThickLine(vh, new Vector2(gMin.x, gMax.y), new Vector2(gMax.x, gMin.y), glyphThickness, Color.white);
                    }
                }
                
                float setMaxX = closeMinX - ButtonGap;
                float setMaxY = y1 - CloseButtonInset;
                float setMinX = setMaxX - SettingsButtonSize;
                float setMinY = setMaxY - SettingsButtonSize;
                if (setMinX > x0 && setMinY > y0)
                {
                    Color setColor = _settingsActive ? SettingsButtonActiveColor : (_settingsHovered ? SettingsButtonHoverColor : CloseNormalColor);
                    LogRoundedRect.DrawFilled(vh,
                        new Rect(setMinX, setMinY, SettingsButtonSize, SettingsButtonSize),
                        LogRoundedRect.ControlRadius * UiScale, setColor, 6);

                    float barThickness = Mathf.Max(1f, CloseGlyphThickness);
                    float barLeft = setMinX + CloseGlyphInset * 0.75f;
                    float barRight = setMaxX - CloseGlyphInset * 0.75f;
                    float barCenter = (setMinY + setMaxY) * 0.5f;
                    float barSpacing = Mathf.Max(2f, (setMaxY - setMinY) * 0.22f);
                    if (barRight > barLeft)
                    {
                        DrawThickLine(vh, new Vector2(barLeft, barCenter - barSpacing), new Vector2(barRight, barCenter - barSpacing), barThickness, Color.white);
                        DrawThickLine(vh, new Vector2(barLeft, barCenter), new Vector2(barLeft + (barRight - barLeft) * 0.75f, barCenter), barThickness, Color.white);
                        DrawThickLine(vh, new Vector2(barLeft, barCenter + barSpacing), new Vector2(barRight, barCenter + barSpacing), barThickness, Color.white);
                    }
                }
                
                if (!_settingsActive)
                {
                    Rect graph = GraphContainerRect(w, h);
                    if (graph.width > 2f && graph.height > 2f)
                    {
                        LogRoundedRect.DrawFilled(vh, graph, LogRoundedRect.InnerPanelRadius * UiScale, GraphContainerColor, 6);
                    }
                }
                
                Rect sidebar = SidebarRect(w, h);
                if (_settingsActive && sidebar.width > 0f && sidebar.height > 0f)
                {
                    LogRoundedRect.DrawFilled(vh, sidebar, LogRoundedRect.InnerPanelRadius * UiScale, StatsPanelColor, 6);

                    float dividerThickness = Mathf.Max(1f, border * 0.5f);
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
