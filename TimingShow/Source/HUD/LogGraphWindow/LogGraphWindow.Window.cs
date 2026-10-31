using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private static Vector2 ClampSize(Vector2 size, float screenWidth, float screenHeight)
        {
            float maxWidth = Mathf.Max(MinWindowWidth, Mathf.Min(MaxWindowWidth, screenWidth));
            float maxHeight = Mathf.Max(MinWindowHeight, Mathf.Min(MaxWindowHeight, screenHeight));

            size.x = Mathf.Clamp(size.x, MinWindowWidth, maxWidth);
            size.y = Mathf.Clamp(size.y, MinWindowHeight, maxHeight);
            return size;
        }

        private static Vector2 ClampPosition(Vector2 position, Vector2 size, float screenWidth, float screenHeight)
        {
            float maxX = Mathf.Max(0f, screenWidth - size.x);
            float maxY = Mathf.Max(0f, screenHeight - size.y);

            position.x = Mathf.Clamp(position.x, 0f, maxX);
            position.y = Mathf.Clamp(position.y, 0f, maxY);
            return position;
        }



        private static LogGraphWindow _dragProxy;
        private static ILogGraphPanelInteraction _hoverProxy;
        private static IPointerExitHandler _exitProxy;

        private static void ApplyProxy()
        {
            LogGraphWindowPanel panel = _instance != null ? _instance._panel : null;
            if (panel == null) return;

            panel.SetDragProxy(_dragProxy);
            panel.SetHoverProxy(_hoverProxy, _exitProxy);
        }

        private void ToggleSettingsPanel()
        {
            if (_settingsUI == null) return;

            CloseMenus();
            _settingsUI.Toggle();
            if (_windowRect != null) ApplyLayout(_windowRect.sizeDelta, _windowRect.anchoredPosition);

            SyncSettingsInteraction();
            RefreshTitle();

            Trace(_settingsUI.IsOpen ? "settings page opened" : "settings page closed");
        }
        
        private void SetGraphPageVisible(bool visible)
        {
            RectTransform target = _graphContainerRect != null ? _graphContainerRect : _graphRect;
            if (target == null) return;
            if (target.gameObject.activeSelf != visible) target.gameObject.SetActive(visible);
        }
        
        private void SetStatsSidebarVisible(bool visible)
        {
            if (_statsTitleText != null && _statsTitleText.gameObject.activeSelf != visible)
                _statsTitleText.gameObject.SetActive(visible);

            for (int i = 0; i < StatsRowCount; i++)
            {
                if (_statLabels[i] != null && _statLabels[i].gameObject.activeSelf != visible)
                    _statLabels[i].gameObject.SetActive(visible);

                if (_statValues[i] != null && _statValues[i].gameObject.activeSelf != visible)
                    _statValues[i].gameObject.SetActive(visible);
            }
        }
        
        private void SyncSettingsInteraction()
        {
            bool suspended = (_settingsUI != null && _settingsUI.IsOpen) || _view != GraphView.Scatter;
            if (_drawer != null) _drawer.InteractionSuspended = suspended;

            if (suspended && _graphPanActive)
            {
                _graphPanActive = false;
                if (_drawer != null) _drawer.EndPan();
            }
        }
        
        public void OnLogGraphSettingsChanged()
        {
            if (_windowRect == null) return;

            LogRoundedRect.SyncFromSettings();

            ApplyLayout(_windowRect.sizeDelta, _windowRect.anchoredPosition);
            RefreshStats();

            if (_drawer != null) _drawer.ReloadAppearance();

            if (_distributionDrawer != null)
            {
                _distributionDrawer.SetFont(_drawer != null ? _drawer.GraphFontAsset : null);
                _distributionDrawer.ReloadAppearance();
            }

            if (_xaccDrawer != null)
            {
                _xaccDrawer.SetFont(_drawer != null ? _drawer.GraphFontAsset : null);
                _xaccDrawer.ReloadAppearance();
            }

            TMP_FontAsset font = _drawer != null ? _drawer.GraphFontAsset : null;
            if (_deltaDrawer != null)
            {
                _deltaDrawer.SetFont(font);
                _deltaDrawer.ReloadAppearance();
            }
            if (_rollingDrawer != null)
            {
                _rollingDrawer.SetFont(font);
                _rollingDrawer.ReloadAppearance();
            }
            if (_returnDrawer != null)
            {
                _returnDrawer.SetFont(font);
                _returnDrawer.ReloadAppearance();
            }
            if (_comboDrawer != null)
            {
                _comboDrawer.SetFont(font);
                _comboDrawer.ReloadAppearance();
            }
            if (_acfDrawer != null)
            {
                _acfDrawer.SetFont(font);
                _acfDrawer.ReloadAppearance();
            }
            if (_overviewDrawer != null)
            {
                _overviewDrawer.SetFont(font);
                _overviewDrawer.ReloadAppearance();
            }
            if (_chartButton != null)
            {
                _chartButton.SetFont(font);
                _chartButton.SetVerticesDirty();
            }
            if (_playerButton != null)
            {
                _playerButton.SetFont(font);
                _playerButton.SetVerticesDirty();
            }
            if (_chartMenu != null)
            {
                _chartMenu.SetFont(font);
                _chartMenu.SetVerticesDirty();
            }
            if (_playerMenu != null)
            {
                _playerMenu.SetFont(font);
                _playerMenu.SetVerticesDirty();
            }

            ApplyRangeFilterFont(font);

            if (_panel != null) _panel.MarkDirty();
            if (_settingsUI != null) _settingsUI.SetFont(_drawer != null ? _drawer.GraphFontAsset : null);
        }
        
        private void ApplyDrag(Vector2 screen)
        {
            if (_windowRect == null) { EndDrag(); return; }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (_resizing)
            {
                Vector2 size = new Vector2(_sizeAtDragStart.x + (screen.x - _dragStartScreen.x), _sizeAtDragStart.y - (screen.y - _dragStartScreen.y));
                size = ClampSize(size, screenWidth, screenHeight);
                Vector2 position = new Vector2(_windowAtDragStart.x + _sizeAtDragStart.x - size.x, _windowAtDragStart.y + _sizeAtDragStart.y - size.y);
                ApplyLayout(size, ClampPosition(position, size, screenWidth, screenHeight));
                return;
            }

            Vector2 moved = new Vector2(_windowAtDragStart.x + (screen.x - _dragStartScreen.x), _windowAtDragStart.y + (screen.y - _dragStartScreen.y));
            ApplyLayout(_windowRect.sizeDelta, ClampPosition(moved, _windowRect.sizeDelta, screenWidth, screenHeight));
        }

        private void EndDrag()
        {
            bool hasWindow = _windowRect != null;
            bool wasResizing = _resizing;
            Vector2 size = hasWindow ? _windowRect.sizeDelta : Vector2.zero;
            _dragging = false;
            _resizing = false;
            _updateFallbackDrag = false;

            if (wasResizing && hasWindow && size.x > 0f && size.y > 0f) SaveWindowSize(size);
        }
        
        private static Vector2 SavedWindowSize()
        {
            Settings settings = ModContext.Settings;
            if (settings == null) return new Vector2(DefaultWindowWidth, DefaultWindowHeight);

            float width = settings.LogGraph_WindowWidth;
            float height = settings.LogGraph_WindowHeight;

            if (!(width > 0f)) width = DefaultWindowWidth;    
            if (!(height > 0f)) height = DefaultWindowHeight;

            return new Vector2(width, height);
        }
        
        private void SaveWindowSize(Vector2 size)
        {
            Settings settings = ModContext.Settings;
            if (settings == null || string.IsNullOrEmpty(ModContext.ModPath)) return;

            if (Mathf.Approximately(settings.LogGraph_WindowWidth, size.x) &&
                Mathf.Approximately(settings.LogGraph_WindowHeight, size.y))
            {
                return;
            }

            settings.LogGraph_WindowWidth = size.x;
            settings.LogGraph_WindowHeight = size.y;
            settings.Save(ModContext.ModPath);   
        }

        private bool ScreenToLocal(Vector2 screenPosition, out Vector2 local)
        {
            if (_windowRect == null)
            {
                local = Vector2.zero;
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRect, screenPosition, null, out local);
        }

        private bool HitCloseButton(Vector2 local, Vector2 size)
        {
            float maxX = size.x - CloseButtonInset;
            float maxY = size.y - CloseButtonInset;
            float minX = maxX - CloseButtonSize;
            float minY = maxY - CloseButtonSize;

            return local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
        }
        
        private bool HitResizeGrip(Vector2 local, Vector2 size)
        {
            float maxX = size.x - GripInset;
            float minX = maxX - GripSize;
            float minY = GripInset;
            float maxY = minY + GripSize;

            return local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
        }
        
        private bool HitSettingsButton(Vector2 local, Vector2 size)
        {
            float maxX = size.x - CloseButtonInset - CloseButtonSize - ButtonGap;
            float maxY = size.y - CloseButtonInset;
            float minX = maxX - SettingsButtonSize;
            float minY = maxY - SettingsButtonSize;

            return local.x >= minX && local.x <= maxX && local.y >= minY && local.y <= maxY;
        }
        
        private void RefreshButtonHover(Vector2 screenPosition)
        {
            if (_panel == null || _windowRect == null)
            {
                if (_panel != null)
                {
                    _panel.SetHover(false);
                    _panel.SetSettingsHover(false);
                }
                return;
            }

            Vector2 local;
            if (!ScreenToLocal(screenPosition, out local))
            {
                _panel.SetHover(false);
                _panel.SetSettingsHover(false);
                return;
            }

            Vector2 size = _windowRect.sizeDelta;
            bool onClose = HitCloseButton(local, size);
            _panel.SetHover(onClose);
            _panel.SetSettingsHover(!onClose && HitSettingsButton(local, size));
        }

        private static Vector2 WorldToScreen(Vector3 world)
        {
            if (_root != null)
            {
                Canvas canvas = _root.GetComponent<Canvas>();
                if (canvas != null)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return new Vector2(world.x, world.y);
                    return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, world);
                }
            }

            return new Vector2(world.x, world.y);
        }

    }
}
