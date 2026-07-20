using System;
using System.Reflection;

namespace TimingShow
{
    public static class XPerfectBridge
    {
        private static bool isInitialized = false;
        private static bool isAvailable = false;
        private static Func<int> getLastJudgeDelegate;
        private const int XPERFECT_ENUM_VALUE = 1;

        public enum HookState
        {
            Disabled,
            Success,
            Failed
        }

        private static HookState internalState = HookState.Disabled;
        private static string internalErrorMsg = string.Empty;

        public static HookState CurrentState
        {
            get
            {
                if (!Main.Settings.UseHookMode) return HookState.Disabled;
                if (!isInitialized) TryInit();
                return internalState;
            }
        }

        public static string LastErrorMessage
        {
            get
            {
                if (!Main.Settings.UseHookMode) return string.Empty;
                return internalErrorMsg;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                if (!Main.Settings.UseHookMode) return false;
                if (!isInitialized) TryInit();
                return isAvailable;
            }
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
                Type accuracyStateType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = asm.GetType("XPerfect.AccuracyState");
                    if (type != null)
                    {
                        accuracyStateType = type;
                        break;
                    }
                }

                if (accuracyStateType == null)
                {
                    SetFailedState(LangMan.T("Err_AssemblyNotFound"));
                    Main.Logger.Log("XPerfect not installed or not loaded");
                    return;
                }

                PropertyInfo prop = accuracyStateType.GetProperty("LastJudgeForText", BindingFlags.Public | BindingFlags.Static) ?? accuracyStateType.GetProperty("LastJudge", BindingFlags.Public | BindingFlags.Static);

                if (prop == null)
                {
                    SetFailedState(LangMan.T("Err_PropertyNotFound"));
                    return;
                }

                MethodInfo getter = prop.GetGetMethod();
                if (getter == null)
                {
                    SetFailedState(LangMan.T("Err_GetterNotFound"));
                    return;
                }

                getLastJudgeDelegate = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), getter);
                isAvailable = (getLastJudgeDelegate != null);

                if (isAvailable)
                {
                    internalState = HookState.Success;
                    internalErrorMsg = string.Empty;
                    Main.Logger.Log("Successfully hooked into XPerfect mod");
                }
                else
                {
                    SetFailedState(LangMan.T("Err_DelegateFailed"));
                }
            }
            catch (Exception e)
            {
                SetFailedState($"{LangMan.T("Err_UnhandledException")}{e.Message}");
                Main.Logger.Error($"Failed to hook XPerfect: {e.Message}");
            }
        }

        public static void UnloadHook()
        {
            isInitialized = false;
            isAvailable = false;
            getLastJudgeDelegate = null;
            internalState = HookState.Disabled;
            internalErrorMsg = string.Empty;
        }

        private static void SetFailedState(string errorMsg)
        {
            isAvailable = false;
            getLastJudgeDelegate = null;
            internalState = HookState.Failed;
            internalErrorMsg = errorMsg;
        }

        public static bool IsXPerfect()
        {
            if (!Main.Settings.UseHookMode || !IsAvailable || getLastJudgeDelegate == null)
            {
                return false;
            }

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