using System;
using System.Linq;
using System.Reflection;

namespace TimingShow
{
    public static class XPerfectBridge
    {
        private const int XPERFECT_ENUM_VALUE = 1;
        private static bool isInitialized;
        private static Func<int> getLastJudgeDelegate;


        public enum HookState { Disabled, Success, Failed }

        public static HookState CurrentState { get; private set; } = HookState.Disabled;
        public static string LastErrorMessage { get; private set; } = string.Empty;

        public static bool IsAvailable => ProtInitialize() && CurrentState == HookState.Success && getLastJudgeDelegate != null;
        private static bool ProtInitialize()
        {
            if (!Main.Settings.UseHookMode)
            {
                if (isInitialized) UnloadHook();
                return false;
            }

            if (!isInitialized) TryInit();
            return true;
        }

        public static void TryInit(bool force = false)
        {
            if (!Main.Settings.UseHookMode)
            {
                UnloadHook();
                return;
            }

            if (isInitialized && !force) return;
            isInitialized = true;

            try
            {
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType("XPerfect.AccuracyState")).FirstOrDefault(t => t != null);
                if (type == null)
                {
                    Main.Logger.Log("XPerfect not installed or not loaded");
                    SetState(HookState.Failed, LangMan.T("Err_AssemblyNotFound"));
                    return;
                }

                var prop = type.GetProperty("LastJudgeForText", BindingFlags.Public | BindingFlags.Static) ?? type.GetProperty("LastJudge", BindingFlags.Public | BindingFlags.Static);
                var getter = prop?.GetGetMethod();
                if (getter == null)
                {
                    SetState(HookState.Failed, LangMan.T(prop == null ? "Err_PropertyNotFound" : "Err_GetterNotFound"));
                    return;
                }

                getLastJudgeDelegate = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), getter);
                if (getLastJudgeDelegate != null)
                {
                    SetState(HookState.Success);
                    Main.Logger.Log("Successfully hooked into XPerfect mod");
                }
                else
                    SetState(HookState.Failed, LangMan.T("Err_DelegateFailed"));
            }
            catch (Exception e)
            {
                SetState(HookState.Failed, $"{LangMan.T("Err_UnhandledException")}{e.Message}");
                Main.Logger.Error($"Failed to hook XPerfect: {e.Message}");
            }
        }

        public static void UnloadHook()
        {
            isInitialized = false;
            getLastJudgeDelegate = null;
            SetState(HookState.Disabled);
        }

        private static void SetState(HookState state, string errorMsg = "")
        {
            CurrentState = state;
            LastErrorMessage = errorMsg;
            if (state != HookState.Success) getLastJudgeDelegate = null;
        }

        public static bool IsXPerfect()
        {
            if (!IsAvailable) return false;
            try
            {
                return getLastJudgeDelegate() == XPERFECT_ENUM_VALUE;
            }
            catch
            {
                return false;
            }
        }
    }
}