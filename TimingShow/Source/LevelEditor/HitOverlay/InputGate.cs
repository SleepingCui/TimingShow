using System;
using System.Diagnostics;
using UnityEngine;

namespace TimingShow.LevelEditor
{
    internal static class InputGate
    {
        private const float StaleSeconds = 0.75f;

        private static int _bypass;
        private static bool _panelActive;
        private static Rect _panelArea;
        private static bool _panelCaptureKeys;
        private static float _panelUpdatedAt;
        private static bool _windowActive;
        private static Rect _windowArea;
        private static bool _windowCaptureKeys;
        private static float _windowUpdatedAt;
        private static bool _uiDispatching;
        private static int _uiDispatchFrame = int.MinValue;

        internal static void SetArea(bool active, Rect area, bool captureKeys)
        {
            _panelActive = active;
            _panelArea = area;
            _panelCaptureKeys = captureKeys;
            _panelUpdatedAt = Time.realtimeSinceStartup;
        }

        internal static void Clear()
        {
            _panelActive = false;
            _panelCaptureKeys = false;
            _panelUpdatedAt = 0f;
        }

        internal static void SetWindowArea(Rect area, bool captureKeys)
        {
            _windowActive = true;
            _windowArea = area;
            _windowCaptureKeys = captureKeys;
            _windowUpdatedAt = Time.realtimeSinceStartup;
        }

        internal static void ClearWindowArea()
        {
            _windowActive = false;
            _windowCaptureKeys = false;
            _windowUpdatedAt = 0f;
        }

        internal static bool BlocksPointer
        {
            get
            {
                if (_bypass > 0) return false;
                if (PanelEngaged && ContainsPointer(_panelArea)) return true;
                return WindowEngaged && ContainsPointer(_windowArea) && !ComesFromUi();
            }
        }

        internal static bool BlocksKeys
        {
            get
            {
                if (_bypass > 0) return false;
                if (PanelEngaged && (_panelCaptureKeys || ContainsPointer(_panelArea))) return true;
                if (!WindowEngaged) return false;
                if (!_windowCaptureKeys && !ContainsPointer(_windowArea)) return false;
                return !ComesFromUi();
            }
        }

        internal static void BeginBypass()
        {
            _bypass++;
        }

        internal static void EndBypass()
        {
            if (_bypass > 0) _bypass--;
        }

        internal static void BeginUiDispatch()
        {
            _uiDispatching = true;
            _uiDispatchFrame = Time.frameCount;
        }

        internal static void EndUiDispatch()
        {
            _uiDispatching = false;
        }

        private static bool PanelEngaged
        {
            get { return _panelActive && Fresh(_panelUpdatedAt); }
        }

        private static bool WindowEngaged
        {
            get { return _windowActive && Fresh(_windowUpdatedAt); }
        }

        private static bool Fresh(float updatedAt)
        {
            return Time.realtimeSinceStartup - updatedAt <= StaleSeconds;
        }

        private static bool ContainsPointer(Rect area)
        {
            return area.width > 0f && area.height > 0f && area.Contains(Input.mousePosition);
        }

        // uGUI and this mod's own views read Input directly, so the game only loses
        // input over the log graph window when the caller is outside those assemblies.
        // EventSystem.Update is the entry point of every uGUI read and is flagged by
        // InputPatches; the stack walk remains for a missing hook or a disabled event system.
        private static bool ComesFromUi()
        {
            int frame = Time.frameCount;
            if (_uiDispatching && _uiDispatchFrame >= frame - 1) return true;
            if (_uiDispatchFrame >= frame - 2) return false;
            return ComesFromStackTrace();
        }

        private static bool ComesFromStackTrace()
        {
            try
            {
                StackTrace trace = new StackTrace(1, false);
                for (int i = 0; i < trace.FrameCount; i++)
                {
                    Type type = trace.GetFrame(i).GetMethod()?.DeclaringType;
                    if (type == null || IsBridge(type)) continue;

                    string assembly = type.Assembly.GetName().Name;
                    return assembly.StartsWith("UnityEngine.", StringComparison.Ordinal)
                        || assembly.StartsWith("Unity.", StringComparison.Ordinal)
                        || assembly.StartsWith("TimingShow", StringComparison.Ordinal);
                }
            }
            catch (Exception)
            {
            }

            return true;
        }

        private static bool IsBridge(Type type)
        {
            if (type == typeof(Input) || type == typeof(InputGate) || type == typeof(TimingShow.Patches.InputPatches)) return true;

            string assembly = type.Assembly.GetName().Name;
            return assembly.StartsWith("0Harmony", StringComparison.Ordinal)
                || assembly.StartsWith("HarmonyLib", StringComparison.Ordinal)
                || assembly.StartsWith("MonoMod", StringComparison.Ordinal);
        }
    }
}
