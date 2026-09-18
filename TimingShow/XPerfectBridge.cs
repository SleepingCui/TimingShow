using System;
using System.Linq;
using System.Reflection;

namespace TimingShow
{
    public static class XPerfectBridge
    {
        private const int XpEnum = 1;
        private static bool _isInitialized;
        private static Func<int> _getLastJudgeDelegate;

        public enum HookState
        {
            Disabled,
            Success,
            Failed,
            NotApplicable
        }

        public static HookState CurrentState { get; private set; } = HookState.Disabled;
        public static string LastErrorMessage { get; private set; } = string.Empty;
        
        public static bool IsSupported => !HitMarginCompat.IsGame34;

        public static bool IsAvailable => IsSupported && ProtInitialize() && CurrentState == HookState.Success && _getLastJudgeDelegate != null;

        private static bool ProtInitialize()
        {
            if (!IsSupported) return false;

            if (!ModContext.Settings.UseHookMode)
            {
                if (_isInitialized) UnloadHook();
                return false;
            }

            if (!_isInitialized) TryInit();
            return true;
        }

        public static void TryInit(bool force = false)
        {
            if (!IsSupported)
            {
                _getLastJudgeDelegate = null;
                SetState(HookState.NotApplicable);
                return;
            }

            if (!ModContext.Settings.UseHookMode)
            {
                UnloadHook();
                return;
            }

            if (_isInitialized && !force) return;
            _isInitialized = true;

            try
            {
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType("XPerfect.AccuracyState")).FirstOrDefault(t => t != null);
                if (type == null)
                {
                    ModContext.Logger.Log("XPerfect not installed or not loaded");
                    SetState(HookState.Failed, i18n.T("Err_AssemblyNotFound"));
                    return;
                }

                var prop = type.GetProperty("LastJudgeForText", BindingFlags.Public | BindingFlags.Static) ?? type.GetProperty("LastJudge", BindingFlags.Public | BindingFlags.Static);
                var getter = prop?.GetGetMethod();
                if (getter == null)
                {
                    SetState(HookState.Failed, i18n.T(prop == null ? "Err_PropertyNotFound" : "Err_GetterNotFound"));
                    return;
                }

                _getLastJudgeDelegate = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), getter);
                if (_getLastJudgeDelegate != null)
                {
                    SetState(HookState.Success);
                    ModContext.Logger.Log("Successfully hooked into XPerfect mod (legacy compatibility mode)");
                }
                else
                    SetState(HookState.Failed, i18n.T("Err_DelegateFailed"));
            }
            catch (Exception e)
            {
                SetState(HookState.Failed, $"{i18n.T("Err_UnhandledException")}{e.Message}");
                ModContext.Logger.Error($"Failed to hook XPerfect: {e.Message}");
            }
        }

        public static void UnloadHook()
        {
            _isInitialized = false;
            _getLastJudgeDelegate = null;
            SetState(IsSupported ? HookState.Disabled : HookState.NotApplicable);
        }

        private static void SetState(HookState state, string errorMsg = "")
        {
            CurrentState = state;
            LastErrorMessage = errorMsg;
            if (state != HookState.Success) _getLastJudgeDelegate = null;
        }
        
        public static bool IsXPerfect()
        {
            if (!IsAvailable) return false;
            try
            {
                return _getLastJudgeDelegate() == XpEnum;
            }
            catch
            {
                return false;
            }
        }
    }
}
