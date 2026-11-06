using UnityEngine;

namespace TimingShow.LevelEditor
{
    internal static class InputGate
    {
        private const float StaleSeconds = 0.75f;

        private static int _bypass;
        private static bool _active;
        private static Rect _area;
        private static bool _captureKeys;
        private static float _updatedAt;

        internal static void SetArea(bool active, Rect area, bool captureKeys)
        {
            _active = active;
            _area = area;
            _captureKeys = captureKeys;
            _updatedAt = Time.realtimeSinceStartup;
        }

        internal static void Clear()
        {
            _active = false;
            _captureKeys = false;
            _updatedAt = 0f;
        }

        internal static bool BlocksPointer
        {
            get { return Engaged && _area.width > 0f && _area.height > 0f && _area.Contains(Input.mousePosition); }
        }

        internal static bool BlocksKeys
        {
            get { return Engaged && (_captureKeys || BlocksPointer); }
        }

        private static bool Engaged
        {
            get { return _bypass <= 0 && _active && Time.realtimeSinceStartup - _updatedAt <= StaleSeconds; }
        }

        internal static void BeginBypass()
        {
            _bypass++;
        }

        internal static void EndBypass()
        {
            if (_bypass > 0) _bypass--;
        }
    }
}
