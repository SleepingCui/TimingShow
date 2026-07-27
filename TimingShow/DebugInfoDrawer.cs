using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace TimingShow
{
    public class DebugInfoDrawer : MonoBehaviour
    {
        private static GameObject _drawerObject;
        private float _lastUpdateTime = -999f;
        private string _cachedDebugText = string.Empty;
        private GUIStyle _rightAlignedStyle;
        private float _fps = 0f;
        private long _lastFrameGcMemory = 0;
        private long _lastFrameAllocatedBytes = 0;

        public static void Init()
        {
            if (_drawerObject != null) return;

            _drawerObject = new GameObject("TimingShow_DebugDrawer");
            UnityEngine.Object.DontDestroyOnLoad(_drawerObject);
            _drawerObject.AddComponent<DebugInfoDrawer>();
        }

        private void OnEnable()
        {
            _lastUpdateTime = -999f;
        }

        private void InitStyle()
        {
            if (_rightAlignedStyle == null)
            {
                _rightAlignedStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperRight, 
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };
                _rightAlignedStyle.normal.textColor = new Color(0.2f, 1.0f, 0.4f, 0.95f);
            }
        }

        private void Update()
        {
            if (!Main.Settings.ShowDebugInfo) return;
            float intervalSec = Mathf.Max(0.05f, Main.Settings.DebugUpdateIntervalMs / 1000f);
            if (Time.unscaledTime - _lastUpdateTime >= intervalSec)
            {
                _lastUpdateTime = Time.unscaledTime;
                RefreshDebugText();
            }
        }

        private void RefreshDebugText()
        {
            List<string> debugLines = new List<string>();

            debugLines.Add($"Mod Version: {Main.ModVersion}");

            float unscaledDelta = Time.unscaledDeltaTime;
            _fps = 1.0f / Mathf.Max(0.0001f, unscaledDelta);
            float frameTimeMs = unscaledDelta * 1000f;

            long currentGcMemory = GC.GetTotalMemory(false);
            int gcCount0 = GC.CollectionCount(0);

            if (currentGcMemory > _lastFrameGcMemory && _lastFrameGcMemory > 0)
                _lastFrameAllocatedBytes = currentGcMemory - _lastFrameGcMemory;
            else if (currentGcMemory < _lastFrameGcMemory)
                _lastFrameAllocatedBytes = 0; 
            _lastFrameGcMemory = currentGcMemory;

            float heapMb = currentGcMemory / (1024f * 1024f);
            float allocKb = _lastFrameAllocatedBytes / 1024f;


            debugLines.Add($"Perf: {_fps:F0} FPS ({frameTimeMs:F1}ms)");
            debugLines.Add($"GC Heap: {heapMb:F1} MB (GC0: {gcCount0}) | Alloc: {allocKb:F1} KB/f");

            string binFlag = TimingLogger.IsBinarySession ? " (Binary)" : "";
            long currentBytes = TimingLogger.CurrentBufferBytes;
            long totalBytes = TimingLogger.TotalBufferBytes;

            debugLines.Add($"Logger Buffer: {currentBytes} / {totalBytes} bytes{binFlag}");
            debugLines.Add($"Logger Flush: {TimingLogger.FlushCount}");

            string xpStatus = $"XPerfect State: {XPerfectBridge.CurrentState}"; 
            if (XPerfectBridge.CurrentState == XPerfectBridge.HookState.Failed && !string.IsNullOrEmpty(XPerfectBridge.LastErrorMessage)) 
                xpStatus += $" ({XPerfectBridge.LastErrorMessage})"; 
            debugLines.Add(xpStatus);


            AddGraphDebugInfo(debugLines, HUDMan.urGraphInstance);
            AddGraphDebugInfo(debugLines, HUDMan.xaccGraphInstance);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < debugLines.Count; i++)
            {
                sb.AppendLine(debugLines[i]);
            }
            _cachedDebugText = sb.ToString();
        }

        private void AddGraphDebugInfo(List<string> lines, GraphDrawerBase graph)
        {
            if (graph == null || !graph.enabled || !graph.gameObject.activeInHierarchy) return;
            lines.Add($"{graph.GraphName} Graph: {graph.RenderedPoints} pts | Upd: {graph.UpdateTimeMs:F2}ms | Draw: {graph.RenderTimeMs:F2}ms");
        }

        private void OnGUI()
        {
            if (!Main.Settings.ShowDebugInfo) return;
            if (Event.current.type != EventType.Repaint) return;

            InitStyle();

            if (string.IsNullOrEmpty(_cachedDebugText))
                RefreshDebugText();

            if (string.IsNullOrEmpty(_cachedDebugText)) return;

            float paddingRight = 15f;
            float paddingTop = 10f;
            float areaWidth = 500f; 
            float areaHeight = 400f;

            float posX = Screen.width - areaWidth - paddingRight;
            float posY = paddingTop;

            Rect rect = new Rect(posX, posY, areaWidth, areaHeight);

            GUI.Label(rect, _cachedDebugText, _rightAlignedStyle);
        }
    }
}