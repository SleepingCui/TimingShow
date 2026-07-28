using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TimingShow
{
    public class DebugInfoDrawer : MonoBehaviour
    {
        private struct FpsSample
        {
            public float Time;
            public float Fps;

            public FpsSample(float time, float fps)
            {
                Time = time;
                Fps = fps;
            }
        }

        private const float FPS_WINDOW_SECONDS = 30f;
        private const float PADDING_RIGHT = 15f;
        private const float PADDING_TOP = 10f;
        private const float AREA_WIDTH = 500f;
        private const float AREA_HEIGHT = 400f;

        private static GameObject _drawerObject;
        private GUIStyle _rightAlignedStyle;
        private readonly StringBuilder _stringBuilder = new StringBuilder(512);

        private float _lastUpdateTime = -999f;
        private string _cachedDebugText = string.Empty;

        private float _currentFps;
        private float _minFps;
        private float _maxFps;
        private float _avgFps;
        private float _fpsSum;
        private readonly Queue<FpsSample> _fpsHistory = new Queue<FpsSample>(2000);

        private long _lastFrameGcMemory;
        private long _lastFrameAllocatedBytes;


        public static void Init()
        {
            if (_drawerObject != null) return;

            _drawerObject = new GameObject("TimingShow_DebugDrawer");
            UnityEngine.Object.DontDestroyOnLoad(_drawerObject);
            _drawerObject.AddComponent<DebugInfoDrawer>();
        }

        private void OnEnable()
        {
            ResetTracker();
        }

        private void ResetTracker()
        {
            _lastUpdateTime = -999f;
            _fpsHistory.Clear();
            _fpsSum = 0f;
            _minFps = 0f;
            _maxFps = 0f;
            _avgFps = 0f;
            _currentFps = 0f;
            _lastFrameGcMemory = 0;
            _lastFrameAllocatedBytes = 0;
        }

        private void InitStyle()
        {
            if (_rightAlignedStyle != null) return;

            _rightAlignedStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperRight,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            _rightAlignedStyle.normal.textColor = new Color(0.2f, 1.0f, 0.4f, 0.95f);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            UpdateFpsTracker(now);

            if (!Main.Settings.ShowDebugInfo) return;

            float intervalSec = Mathf.Max(0.05f, Main.Settings.DebugUpdateIntervalMs / 1000f);
            if (now - _lastUpdateTime >= intervalSec)
            {
                _lastUpdateTime = now;
                RefreshDebugText();
            }
        }

        private void OnGUI()
        {
            if (!Main.Settings.ShowDebugInfo) return;
            if (Event.current.type != EventType.Repaint) return;

            InitStyle();

            if (string.IsNullOrEmpty(_cachedDebugText))
            {
                RefreshDebugText();
            }

            if (string.IsNullOrEmpty(_cachedDebugText)) return;

            float posX = Screen.width - AREA_WIDTH - PADDING_RIGHT;
            Rect rect = new Rect(posX, PADDING_TOP, AREA_WIDTH, AREA_HEIGHT);

            GUI.Label(rect, _cachedDebugText, _rightAlignedStyle);
        }



        private void UpdateFpsTracker(float now)
        {
            float unscaledDelta = Time.unscaledDeltaTime;
            _currentFps = 1.0f / Mathf.Max(0.0001f, unscaledDelta);

            _fpsHistory.Enqueue(new FpsSample(now, _currentFps));
            _fpsSum += _currentFps;

            while (_fpsHistory.Count > 0 && (now - _fpsHistory.Peek().Time) > FPS_WINDOW_SECONDS)
            {
                _fpsSum -= _fpsHistory.Dequeue().Fps;
            }

            int count = _fpsHistory.Count;
            if (count <= 0) return;
            _avgFps = _fpsSum / count;

            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (var sample in _fpsHistory)
            {
                if (sample.Fps < min) min = sample.Fps;
                if (sample.Fps > max) max = sample.Fps;
            }
            _minFps = min;
            _maxFps = max;
        }

        private void RefreshDebugText()
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine($"Mod Version: {Main.ModVersion}");
            float frameTimeMs = Time.unscaledDeltaTime * 1000f;
            _stringBuilder.AppendLine($"Perf: {_currentFps:F0} FPS ({frameTimeMs:F1}ms) | Avg {_avgFps:F0} / Min {_minFps:F0} / Max {_maxFps:F0}");

            AppendGcMemoryInfo();
            AppendLoggerInfo();
            AppendXPerfectStatus();
            AppendGraphDebugInfo(HUDMan.urGraphInstance);
            AppendGraphDebugInfo(HUDMan.xaccGraphInstance);

            _cachedDebugText = _stringBuilder.ToString();
        }

        private void AppendGcMemoryInfo()
        {
            long currentGcMemory = GC.GetTotalMemory(false);
            int gcCount0 = GC.CollectionCount(0);

            if (currentGcMemory > _lastFrameGcMemory && _lastFrameGcMemory > 0)
            {
                _lastFrameAllocatedBytes = currentGcMemory - _lastFrameGcMemory;
            }
            else if (currentGcMemory < _lastFrameGcMemory)
            {
                _lastFrameAllocatedBytes = 0;
            }
            _lastFrameGcMemory = currentGcMemory;

            float heapMb = currentGcMemory / (1024f * 1024f);
            float allocKb = _lastFrameAllocatedBytes / 1024f;

            _stringBuilder.AppendLine($"GC Heap: {heapMb:F1} MB (GC0: {gcCount0}) | Alloc: {allocKb:F1} KB/f");
        }

        private void AppendLoggerInfo()
        {
            string binFlag = TimingLogger.IsBinarySession ? " (Binary)" : "";
            long currentBytes = TimingLogger.CurrentBufferBytes;
            long totalBytes = TimingLogger.TotalBufferBytes;

            _stringBuilder.AppendLine($"Logger Buffer: {currentBytes} / {totalBytes} bytes{binFlag}");
            _stringBuilder.AppendLine($"Logger Flush: {TimingLogger.FlushCount}");
        }

        private void AppendXPerfectStatus()
        {
            _stringBuilder.Append($"XPerfect State: {XPerfectBridge.CurrentState}");
            if (XPerfectBridge.CurrentState == XPerfectBridge.HookState.Failed && !string.IsNullOrEmpty(XPerfectBridge.LastErrorMessage))
            {
                _stringBuilder.Append($" ({XPerfectBridge.LastErrorMessage})");
            }
            _stringBuilder.AppendLine();
        }

        private void AppendGraphDebugInfo(GraphDrawerBase graph)
        {
            if (graph == null || !graph.enabled || !graph.gameObject.activeInHierarchy) return;

            _stringBuilder.AppendLine($"{graph.GraphName} Graph: {graph.RenderedPoints} pts | Upd: {graph.UpdateTimeMs:F2}ms | Draw: {graph.RenderTimeMs:F2}ms");
        }

    }
}