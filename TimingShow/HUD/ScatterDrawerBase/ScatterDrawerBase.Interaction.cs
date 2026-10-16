using UnityEngine;
using UnityEngine.EventSystems;

namespace TimingShow.HUD.ScatterDrawerBase
{
    public abstract partial class ScatterDrawerBase
    {
        public bool InteractionSuspended { get; set; }
        public bool IsGestureActive => _gestureActive;
        public bool IsGesturePanning => _gesturePanning;

        public void BeginPan(Vector2 screenPosition)
        {
            if (!DetailInteractive) return;
            if (!IsPointerInsideRect(screenPosition)) return;

            BeginGesture(screenPosition);
        }

        public void MovePan(Vector2 screenPosition)
        {
            if (!_gestureActive) return;
            if (!DetailInteractive)
            {
                EndPan();
                return;
            }

            if (!_gesturePanning)
            {
                Vector2 delta = screenPosition - _pointerDownScreen;
                if (delta.sqrMagnitude < GestureDragThreshold * GestureDragThreshold) return;

                _gesturePanning = true;
                _hintHideTime = 0f;
                _hoverSourceIndex = -1;
                HideDetailPanel();
                ApplyPan(_pointerDownScreen, screenPosition);
                _lastPointerScreen = screenPosition;
                return;
            }

            ApplyPan(_lastPointerScreen, screenPosition);
            _lastPointerScreen = screenPosition;
        }

        public void EndPan()
        {
            EndGesture();
        }

        public bool ZoomAt(float scrollDelta, Vector2 screenPosition)
        {
            if (Mathf.Approximately(scrollDelta, 0f)) return false;
            if (!DetailInteractive) return false;
            if (!IsPointerInsideRect(screenPosition)) return false;

            ApplyZoom(scrollDelta, screenPosition);
            return true;
        }

        private const float MinTimeAxisRangeMs = 1f;
        private const float GestureDragThreshold = 5f;
        private const float WheelZoomSpeed = 0.08f;
        private const float ViewMarginRatio = 0.05f;
        private const float MinViewSpanRatio = 0.001f;
        private const float MinYViewSpanMs = 0.05f;
        private const float MaxYViewSpanRatio = 5f;
        private const float YPanMarginRatio = 2f;
        private float _autoXMin;
        private float _autoXMax;
        private float _autoYMin;
        private float _autoYMax;
        private float _dataXMin;
        private float _dataXMax;
        private float _viewXMin;
        private float _viewXMax;
        private bool _manualViewActive;
        private int _viewAxisMode = -1;
        private bool _gestureActive;
        private bool _gesturePanning;
        private Vector2 _pointerDownScreen;
        private Vector2 _lastPointerScreen;
        private int _scrollEventFrame = -1;
        private float _lastViewRectWidth = -1f;
        private float _lastViewRectHeight = -1f;
        private int _lastViewScreenWidth = -1;
        private int _lastViewScreenHeight = -1;
        private bool _interactiveLastFrame;
        private bool DetailInteractive
        {
            get
            {
                if (!IsEnabled || !ShowGraph) return false;
                if (InteractionSuspended) return false;
                if (LevelFinished) return true;
                return GamePaused;
            }
        }

        private void UpdateInteraction()
        {
            if (_detailDataVersion != ScatterVersion)
            {
                _detailDataVersion = ScatterVersion;
                ClearDetailInteraction();
                if (ScatterSamples.Count == 0)
                {
                    ResetManualView();
                    ResetPlayerFilter();
                }
            }

            bool interactive = DetailInteractive;
            raycastTarget = interactive;

            if (interactive && !_interactiveLastFrame)
            {
                _hintHideTime = Time.unscaledTime + HintShowSeconds;
            }
            _interactiveLastFrame = interactive;

            UpdateGesture();

            if (!interactive)
            {
                ClearDetailInteraction();
                ResetManualView();
                UpdateHintText();
                return;
            }

            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            if (w <= 0f || h <= 0f || _renderPoints.Count == 0)
            {
                _hoverSourceIndex = -1;
                RefreshDetailPanel();
                UpdateHintText();
                UpdateScrollFallback(false);
                return;
            }

            bool insideRect = false;
            Vector2 localMouse = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 converted))
            {
                localMouse = converted;
                insideRect = localMouse.x >= 0f && localMouse.x <= w && localMouse.y >= 0f && localMouse.y <= h;
            }

            if (_gesturePanning)
            {
                _hoverSourceIndex = -1;
                HideDetailPanel();
            }
            else
            {
                _hoverSourceIndex = insideRect ? FindNearestRenderPoint(localMouse, w, h) : -1;
                RefreshDetailPanel();
            }

            if (!ExternalPointerInput) UpdateScrollFallback(insideRect);
            UpdateHintText();
        }

        private void UpdateGesture()
        {
            if (ExternalPointerInput)
            {
                return;
            }

            if (!DetailInteractive)
            {
                _gestureActive = false;
                _gesturePanning = false;
                return;
            }

            if (!_gestureActive)
            {
                if (EventSystem.current == null && Input.GetMouseButtonDown(0)
                    && IsPointerInsideRect(Input.mousePosition))
                {
                    if (TryPlayerLegendClick(Input.mousePosition)) return;
                    BeginGesture(Input.mousePosition);
                }
                return;
            }
            if (!Input.GetMouseButton(0))
            {
                EndGesture();
                return;
            }

            Vector2 current = Input.mousePosition;
            if (!_gesturePanning)
            {
                if ((current - _pointerDownScreen).sqrMagnitude < GestureDragThreshold * GestureDragThreshold)
                    return;

                _gesturePanning = true;
                _hintHideTime = 0f;
                _hoverSourceIndex = -1;
                HideDetailPanel();
                ApplyPan(_pointerDownScreen, current);
                _lastPointerScreen = current;
                return;
            }

            ApplyPan(_lastPointerScreen, current);
            _lastPointerScreen = current;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ExternalPointerInput) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (!DetailInteractive) return;

            Vector2 screen = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            if (TryPlayerLegendClick(screen)) return;

            BeginGesture(screen);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (ExternalPointerInput) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            EndGesture();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (ExternalPointerInput) return;
            if (eventData == null || !DetailInteractive) return;

            _scrollEventFrame = Time.frameCount;
            Vector2 position = eventData.position;
            if (position == Vector2.zero) position = Input.mousePosition;
            ApplyZoom(eventData.scrollDelta.y, position);
        }

        private void BeginGesture(Vector2 screenPosition)
        {
            if (!IsPointerInsideRect(screenPosition)) return;

            _gestureActive = true;
            _gesturePanning = false;
            _pointerDownScreen = screenPosition;
            _lastPointerScreen = screenPosition;
        }

        private void EndGesture()
        {
            if (!_gestureActive && !_gesturePanning) return;

            _gestureActive = false;
            _gesturePanning = false;
            SetVerticesDirty();
        }

        private bool IsPointerInsideRect(Vector2 screenPosition)
        {
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out Vector2 local)) return false;

            return local.x >= 0f && local.x <= rect.width && local.y >= 0f && local.y <= rect.height;
        }

        private void ApplyPan(Vector2 previousScreen, Vector2 currentScreen)
        {
            Rect rect = rectTransform.rect;
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, previousScreen, null, out Vector2 previousLocal)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, currentScreen, null, out Vector2 currentLocal)) return;

            float dx = currentLocal.x - previousLocal.x;
            float dy = currentLocal.y - previousLocal.y;
            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f)) return;

            float spanX = _viewXMax - _viewXMin;
            float spanY = _maxY - _minY;
            if (!(spanX > 0f) && !(spanY > 0f)) return;

            _manualViewActive = true;
            _hintHideTime = 0f;

            if (spanX > 0f)
            {
                _viewXMin -= dx / w * spanX;
                _viewXMax = _viewXMin + spanX;
            }
            if (spanY > 0f)
            {
                _minY -= dy / h * spanY;
                _maxY = _minY + spanY;
            }

            ClampViewX();
            ClampViewY();

            RefreshViewGeometry();
        }

        private void ApplyZoom(float scrollDelta, Vector2 screenPosition)
        {
            if (Mathf.Approximately(scrollDelta, 0f)) return;
            if (!_hasData || !DetailInteractive) return;

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out Vector2 local)) return;

            float factor = Mathf.Exp(-scrollDelta * WheelZoomSpeed);
            if (float.IsNaN(factor) || float.IsInfinity(factor) || factor <= 0f) return;

            float tx = Mathf.Clamp01(local.x / rect.width);
            float ty = Mathf.Clamp01(local.y / rect.height);

            _manualViewActive = true;
            _hintHideTime = 0f;

            float hardXMin, hardXMax, minXSpan, maxXSpan;
            float hardYMin, hardYMax, minYSpan, maxYSpan;
            GetXViewLimits(out hardXMin, out hardXMax, out minXSpan, out maxXSpan);
            GetYViewLimits(out hardYMin, out hardYMax, out minYSpan, out maxYSpan);

            ZoomAxis(ref _viewXMin, ref _viewXMax, tx, factor, hardXMin, hardXMax, minXSpan, maxXSpan);
            ZoomAxis(ref _minY, ref _maxY, ty, factor, hardYMin, hardYMax, minYSpan, maxYSpan);

            RefreshViewGeometry();
        }

        private void UpdateScrollFallback(bool insideRect)
        {
            if (!insideRect || _scrollEventFrame == Time.frameCount) return;

            float delta = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(delta, 0f)) return;

            ApplyZoom(delta, Input.mousePosition);
        }

        private static void ZoomAxis(ref float min, ref float max, float anchorRatio, float factor,
            float hardMin, float hardMax, float minSpan, float maxSpan)
        {
            float span = max - min;
            if (float.IsNaN(span) || float.IsInfinity(span) || span <= 0f) return;

            float anchor = min + Mathf.Clamp01(anchorRatio) * span;
            float newSpan = Mathf.Clamp(span * factor, minSpan, Mathf.Max(minSpan, maxSpan));

            min = anchor - Mathf.Clamp01(anchorRatio) * newSpan;
            max = min + newSpan;

            ClampViewRange(ref min, ref max, hardMin, hardMax, minSpan, maxSpan);
        }

        private void GetXViewLimits(out float hardMin, out float hardMax, out float minSpan, out float maxSpan)
        {
            float dataMin = Mathf.Min(_dataXMin, _dataXMax);
            float dataMax = Mathf.Max(_dataXMin, _dataXMax);
            float dataSpan = Mathf.Max(1e-4f, dataMax - dataMin);
            float margin = dataSpan * ViewMarginRatio;

            hardMin = dataMin - margin;
            hardMax = dataMax + margin;
            maxSpan = hardMax - hardMin;
            minSpan = _useTimeAxis
                ? Mathf.Max(MinTimeAxisRangeMs, dataSpan * MinViewSpanRatio)
                : Mathf.Max(1f, dataSpan * MinViewSpanRatio);
        }
        
        private void GetYViewLimits(out float hardMin, out float hardMax, out float minSpan, out float maxSpan)
        {
            float autoSpan = Mathf.Max(1e-4f, _autoYMax - _autoYMin);

            float reachMin = _hasData ? Mathf.Min(_autoYMin, _dataYMin) : _autoYMin;
            float reachMax = _hasData ? Mathf.Max(_autoYMax, _dataYMax) : _autoYMax;
            float reachSpan = Mathf.Max(autoSpan, reachMax - reachMin);
            float margin = reachSpan * YPanMarginRatio;

            hardMin = reachMin - margin;
            hardMax = reachMax + margin;
            maxSpan = reachSpan * MaxYViewSpanRatio;
            minSpan = Mathf.Max(MinYViewSpanMs, autoSpan * MinViewSpanRatio);
        }

        private void ClampViewX()
        {
            float hardMin, hardMax, minSpan, maxSpan;
            GetXViewLimits(out hardMin, out hardMax, out minSpan, out maxSpan);
            ClampViewRange(ref _viewXMin, ref _viewXMax, hardMin, hardMax, minSpan, maxSpan);
        }

        private void ClampViewY()
        {
            float hardMin, hardMax, minSpan, maxSpan;
            GetYViewLimits(out hardMin, out hardMax, out minSpan, out maxSpan);
            ClampViewRange(ref _minY, ref _maxY, hardMin, hardMax, minSpan, maxSpan);
        }
        
        private static void ClampViewRange(ref float min, ref float max, float hardMin, float hardMax, float minSpan, float maxSpan)
        {
            if (float.IsNaN(min) || float.IsNaN(max) || float.IsInfinity(min) || float.IsInfinity(max))
            {
                min = hardMin;
                max = hardMin + Mathf.Max(minSpan, Mathf.Min(maxSpan, hardMax - hardMin));
                return;
            }

            if (min > max) (min, max) = (max, min);
            if (!(hardMax > hardMin)) return;

            float span = max - min;
            if (!(span > 0f))
            {
                float center = (min + max) * 0.5f;
                span = Mathf.Max(minSpan, Mathf.Min(maxSpan, hardMax - hardMin));
                min = center - span * 0.5f;
                max = min + span;
            }

            float clampedSpan = Mathf.Clamp(max - min, minSpan, Mathf.Max(minSpan, maxSpan));
            if (clampedSpan != max - min)
            {
                float center = (min + max) * 0.5f;
                min = center - clampedSpan * 0.5f;
                max = min + clampedSpan;
            }

            float finalSpan = max - min;
            if (finalSpan >= hardMax - hardMin)
            {
                min = hardMin;
                max = hardMax;
                return;
            }

            if (min < hardMin)
            {
                min = hardMin;
                max = min + finalSpan;
            }
            else if (max > hardMax)
            {
                max = hardMax;
                min = max - finalSpan;
            }
        }

        private void RefreshViewGeometry()
        {
            if (!_hasData)
            {
                SetVerticesDirty();
                return;
            }

            BuildRenderPoints(ScatterSamples, _visibleIndices.Count);
            UpdateTextLayoutAndValues();
            SetVerticesDirty();
        }

        protected void ResetManualView()
        {
            bool wasManual = _manualViewActive;
            _manualViewActive = false;
            _gestureActive = false;
            _gesturePanning = false;
            _viewXMin = _autoXMin;
            _viewXMax = _autoXMax;
            _minY = _autoYMin;
            _maxY = _autoYMax;
            if (wasManual) SetVerticesDirty();
        }
        private void ResetViewOnLayoutChange()
        {
            Rect rect = rectTransform.rect;
            bool rectChanged = rect.width != _lastViewRectWidth || rect.height != _lastViewRectHeight;
            bool screenChanged = Screen.width != _lastViewScreenWidth || Screen.height != _lastViewScreenHeight;
            if (!rectChanged && !screenChanged) return;

            _lastViewRectWidth = rect.width;
            _lastViewRectHeight = rect.height;
            _lastViewScreenWidth = Screen.width;
            _lastViewScreenHeight = Screen.height;

            if (!screenChanged && !ResetViewOnSizeChange) return;

            ResetManualView();
        }
    }
}
