using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TimingShow.LevelEditor;
using UnityEngine;

namespace TimingShow.Patches
{
    internal static class InputPatches
    {
        private sealed class HookTarget
        {
            public string Name;
            public Type[] Arguments;
            public string Prefix;
            public object Probe;
        }

        private static readonly HookTarget[] Targets =
        {
            new HookTarget { Name = "GetKey", Arguments = new[] { typeof(KeyCode) }, Prefix = "GetKeyCode", Probe = KeyCode.None },
            new HookTarget { Name = "GetKey", Arguments = new[] { typeof(string) }, Prefix = "GetKeyName", Probe = "space" },
            new HookTarget { Name = "GetKeyDown", Arguments = new[] { typeof(KeyCode) }, Prefix = "GetKeyDownCode", Probe = KeyCode.None },
            new HookTarget { Name = "GetKeyDown", Arguments = new[] { typeof(string) }, Prefix = "GetKeyDownName", Probe = "space" },
            new HookTarget { Name = "GetKeyUp", Arguments = new[] { typeof(KeyCode) }, Prefix = "GetKeyUpCode", Probe = KeyCode.None },
            new HookTarget { Name = "GetKeyUp", Arguments = new[] { typeof(string) }, Prefix = "GetKeyUpName", Probe = "space" },
            new HookTarget { Name = "GetButton", Arguments = new[] { typeof(string) }, Prefix = "GetButton", Probe = "Jump" },
            new HookTarget { Name = "GetButtonDown", Arguments = new[] { typeof(string) }, Prefix = "GetButtonDown", Probe = "Jump" },
            new HookTarget { Name = "GetButtonUp", Arguments = new[] { typeof(string) }, Prefix = "GetButtonUp", Probe = "Jump" },
            new HookTarget { Name = "GetMouseButton", Arguments = new[] { typeof(int) }, Prefix = "GetMouseButton", Probe = 0 },
            new HookTarget { Name = "GetMouseButtonDown", Arguments = new[] { typeof(int) }, Prefix = "GetMouseButtonDown", Probe = 0 },
            new HookTarget { Name = "GetMouseButtonUp", Arguments = new[] { typeof(int) }, Prefix = "GetMouseButtonUp", Probe = 0 },
            new HookTarget { Name = "GetAxis", Arguments = new[] { typeof(string) }, Prefix = "GetAxis", Probe = "Horizontal" },
            new HookTarget { Name = "GetAxisRaw", Arguments = new[] { typeof(string) }, Prefix = "GetAxisRaw", Probe = "Horizontal" },
        };

        internal static void Install(Harmony harmony)
        {
            if (harmony == null) return;
            int installed = 0;
            List<string> native = new List<string>();
            List<string> failed = new List<string>();
            foreach (HookTarget target in Targets)
            {
                MethodInfo original = AccessTools.Method(typeof(Input), target.Name, target.Arguments);
                if (original == null)
                {
                    failed.Add(target.Name);
                    continue;
                }
                if (!HasBody(original))
                {
                    native.Add(target.Name);
                    continue;
                }
                try
                {
                    MethodInfo prefix = typeof(InputPatches).GetMethod(target.Prefix, BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(original, prefix: new HarmonyMethod(prefix));
                    if (!Probe(original, target.Probe))
                    {
                        harmony.Unpatch(original, HarmonyPatchType.All, harmony.Id);
                        failed.Add(target.Name + ":broken");
                        continue;
                    }
                    installed++;
                }
                catch (Exception e)
                {
                    failed.Add(target.Name + ":" + e.GetType().Name);
                }
            }
            string summary = "Input hooks " + installed + "/" + Targets.Length;
            if (native.Count > 0) summary += ", no body: " + string.Join(", ", native.ToArray());
            if (failed.Count > 0) summary += ", failed: " + string.Join(", ", failed.ToArray());
            if (ModContext.Logger != null) ModContext.Logger.Log(summary);
        }

        private static bool HasBody(MethodInfo method)
        {
            try
            {
                return method.GetMethodBody() != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool Probe(MethodInfo method, object argument)
        {
            try
            {
                method.Invoke(null, new[] { argument });
                return true;
            }
            catch (TargetInvocationException e)
            {
                return !(e.InnerException is InvalidProgramException);
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool GetKeyCode(KeyCode key, ref bool __result)
        {
            return BlockKeys(key, ref __result);
        }

        private static bool GetKeyName(ref bool __result)
        {
            return BlockKeys(ref __result);
        }

        private static bool GetKeyDownCode(KeyCode key, ref bool __result)
        {
            return BlockKeys(key, ref __result);
        }

        private static bool GetKeyDownName(ref bool __result)
        {
            return BlockKeys(ref __result);
        }

        private static bool GetKeyUpCode(KeyCode key, ref bool __result)
        {
            return BlockKeys(key, ref __result);
        }

        private static bool GetKeyUpName(ref bool __result)
        {
            return BlockKeys(ref __result);
        }

        private static bool GetButton(ref bool __result)
        {
            return BlockKeys(ref __result);
        }

        private static bool GetButtonDown(ref bool __result)
        {
            return BlockKeys(ref __result);
        }

        private static bool GetButtonUp(ref bool __result)
        {
            return BlockKeys(ref __result);
        }

        private static bool GetMouseButton(ref bool __result)
        {
            return BlockPointer(ref __result);
        }

        private static bool GetMouseButtonDown(ref bool __result)
        {
            return BlockPointer(ref __result);
        }

        private static bool GetMouseButtonUp(ref bool __result)
        {
            return BlockPointer(ref __result);
        }

        private static bool GetAxis(ref float __result)
        {
            if (!InputGate.BlocksPointer) return true;
            __result = 0f;
            return false;
        }

        private static bool GetAxisRaw(ref float __result)
        {
            if (!InputGate.BlocksPointer) return true;
            __result = 0f;
            return false;
        }

        private static bool BlockKeys(KeyCode key, ref bool __result)
        {
            if (key == KeyCode.F9 || key == KeyCode.F10) return true;
            if (!InputGate.BlocksKeys) return true;
            __result = false;
            return false;
        }

        private static bool BlockKeys(ref bool __result)
        {
            if (!InputGate.BlocksKeys) return true;
            __result = false;
            return false;
        }

        private static bool BlockPointer(ref bool __result)
        {
            if (!InputGate.BlocksPointer) return true;
            __result = false;
            return false;
        }
    }
}
