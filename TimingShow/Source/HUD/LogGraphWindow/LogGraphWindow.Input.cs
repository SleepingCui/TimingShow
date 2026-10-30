using UnityEngine;
using UnityEngine.EventSystems;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        public void OnPanelPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_windowRect == null) return;

            Vector2 screen = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            if (_ignoreNextClick)
            {
                _ignoreNextClick = false;
                return;
            }

            Vector2 local;
            if (!ScreenToLocal(screen, out local)) return;

            Vector2 size = _windowRect.sizeDelta;
            bool onClose = HitCloseButton(local, size);
            bool onSettings = !onClose && HitSettingsButton(local, size);

            if (_chartMenuOpen || _playerMenuOpen)
            {
                _ignoreNextClick = true;
                CloseMenus();
                return;
            }

            bool onGrip = !onClose && !onSettings && HitResizeGrip(local, size);
            bool onTitleBar = !onClose && !onSettings && !onGrip && local.y >= size.y - HeaderHeight;
            
            if (onClose || onSettings) return;
            if (IsPointerInsideGraphArea(screen)) return;
            if (!onGrip && !onTitleBar) return;

            _resizing = onGrip;
            _updateFallbackDrag = false;
            _dragStartScreen = screen;
            _windowAtDragStart = _windowRect.anchoredPosition;
            _sizeAtDragStart = size;
            _dragging = true;
        }

        public void OnPanelDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_windowRect == null) return;

            Vector2 screen = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            ApplyDrag(screen);
        }

        public void OnPanelPointerUp(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            EndDrag();
        }

        public void OnPanelPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_windowRect == null) return;
        }

        public void OnPanelPointerMove(PointerEventData eventData)
        {
            if (eventData == null) return;
            RefreshButtonHover(eventData.position);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            OnPanelPointerExit(eventData);
        }

        internal void OnPanelPointerExit(PointerEventData eventData)
        {
            if (_panel != null)
            {
                _panel.SetHover(false);
                _panel.SetSettingsHover(false);
            }
        }
        
        private void UpdateInputFallback()
        {
            if (_ignoreNextClick && Input.GetMouseButtonUp(0)) _ignoreNextClick = false;

            Vector2 screen = Input.mousePosition;
            if (!Application.isFocused)
            {
                if (_dragging) EndDrag();
                _pendingButton = PendingButton.None;
                if (_graphPanActive)
                {
                    _graphPanActive = false;
                    if (_graphPanRunning) Trace("Unfocused");
                    _graphPanRunning = false;
                    if (_drawer != null) _drawer.EndPan();
                }
                return;
            }

            if (Input.GetMouseButtonDown(0)) EndRangeEditingOnClick(screen);

            if (_graphPanActive)
            {
                if (Input.GetMouseButton(0))
                {
                    if (_drawer != null)
                    {
                        _drawer.MovePan(screen);
                        if (!_graphPanRunning && _drawer.IsGesturePanning)
                        {
                            _graphPanRunning = true;
                            Trace("Graph pan in progress");
                        }
                    }
                }
                else
                {
                    if (_graphPanRunning) Trace("Graph gesture ended");;
                    _graphPanRunning = false;
                    _graphPanActive = false;
                    if (_drawer != null) _drawer.EndPan();
                }
                return;
            }
            
            if (_dragging)
            {
                if (!Input.GetMouseButton(0))
                {
                    EndDrag();
                    return;
                }

                if (_windowRect == null)
                {
                    EndDrag();
                    return;
                }

                if (!_updateFallbackDrag) return;  
                ApplyDrag(screen);
                return;
            }

            if (_windowRect == null) return;
            
            float wheel = Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(wheel, 0f)
                && _view == GraphView.Scatter
                && !_chartMenuOpen && !_playerMenuOpen
                && (_settingsUI == null || !_settingsUI.IsOpen)
                && !IsPointerInsideRangeFilter(screen)
                && IsPointerInsideGraphArea(screen))
            {
                if (_drawer != null && _drawer.ZoomAt(wheel, screen))
                {
                    return;
                }
            }
            
            if (Input.GetMouseButtonUp(0) && _pendingButton != PendingButton.None)
            {
                PendingButton pending = _pendingButton;
                _pendingButton = PendingButton.None;

                Vector2 upLocal;
                if (ScreenToLocal(screen, out upLocal) && HitRect(upLocal, _windowRect.sizeDelta))
                {
                    if (pending == PendingButton.Close && HitCloseButton(upLocal, _windowRect.sizeDelta))
                    {
                        Close();
                        return;
                    }

                    if (pending == PendingButton.Settings && HitSettingsButton(upLocal, _windowRect.sizeDelta))
                    {
                        ToggleSettingsPanel();
                        return;
                    }
                }
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;

            if (_ignoreNextClick)
            {
                _ignoreNextClick = false;
                return;
            }

            if (_chartMenuOpen || _playerMenuOpen)
            {
                if (IsPointerInsideOpenMenu(screen) || IsPointerInsideSelectorButton(screen)) return;
                _ignoreNextClick = true;
                CloseMenus();
                return;
            }

            Vector2 local;
            if (!ScreenToLocal(screen, out local)) return;

            Vector2 size = _windowRect.sizeDelta;
            if (!HitRect(local, size)) return;

            if (HitCloseButton(local, size))
            {
                _pendingButton = PendingButton.Close;
                return;
            }

            if (HitSettingsButton(local, size))
            {
                _pendingButton = PendingButton.Settings;
                return;
            }

            if (HitResizeGrip(local, size))
            {
                _resizing = true;
                _updateFallbackDrag = true;
                _dragStartScreen = screen;
                _windowAtDragStart = _windowRect.anchoredPosition;
                _sizeAtDragStart = size;
                _dragging = true;
                return;
            }
            
            if (_settingsUI != null && _settingsUI.IsOpen && _settingsUI.ContainsScreenPoint(screen))
            {
                return;
            }
            
            if (local.y >= size.y - HeaderHeight)
            {
                Trace("press -> header");
                _resizing = false;
                _updateFallbackDrag = true;
                _dragStartScreen = screen;
                _windowAtDragStart = _windowRect.anchoredPosition;
                _sizeAtDragStart = size;
                _dragging = true;
                return;
            }
            
            if (_drawer != null && _view == GraphView.Scatter && IsPointerInsideGraphArea(screen)
                && !IsPointerInsideRangeFilter(screen))
            {
                if (_drawer.TryPlayerLegendClick(screen)) return;
                _graphPanActive = true;
                _drawer.BeginPan(screen);
                return;
            }
            
        }


        private bool IsPointerInsideOpenMenu(Vector2 screenPosition)
        {
            if (_chartMenuOpen && _chartMenu != null
                && RectTransformUtility.RectangleContainsScreenPoint(_chartMenu.rectTransform, screenPosition, null))
                return true;

            if (_playerMenuOpen && _playerMenu != null
                && RectTransformUtility.RectangleContainsScreenPoint(_playerMenu.rectTransform, screenPosition, null))
                return true;

            return false;
        }

        private bool IsPointerInsideSelectorButton(Vector2 screenPosition)
        {
            if (_chartButton != null && _chartButton.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(_chartButton.rectTransform, screenPosition, null))
                return true;

            if (_playerButton != null && _playerButton.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(_playerButton.rectTransform, screenPosition, null))
                return true;

            return false;
        }

        private bool IsPointerInsideGraphArea(Vector2 screenPosition)
        {
            if (_windowRect == null) return false;

            Vector2 local;
            if (!ScreenToLocal(screenPosition, out local)) return false;

            Vector2 size = _windowRect.sizeDelta;
            Rect graph = ResolvedPlotRect();
            return local.x >= graph.xMin && local.x <= graph.xMax && local.y >= graph.yMin && local.y <= graph.yMax;
        }

        private static bool HitRect(Vector2 local, Vector2 size)
        {
            return local.x >= 0f && local.x <= size.x && local.y >= 0f && local.y <= size.y;
        }


        
    }
}
