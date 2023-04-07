/*
 * Author: Misaka Mikoto
 * email: easy66@live.com
 * github: https://github.com/Misaka-Mikoto-Tech/UnityScriptHotReload
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MonoHook;
using UnityEngine;
using static ScriptHotReload.HotReloadConfig;
using static ScriptHotReload.HotReloadUtils;

namespace ScriptHotReload
{
    public static class HookAssemblies
    {
        private const string kHotReloadHookTag_Fmt = "kScriptHotReload_{0}";


        public static void Log(string s)
        {
            Debug.Log($"[hot reload]<color=#00ECE5>{s}</color>");
        }

        public static void LogWarning(string s)
        {
            Debug.Log($"[hot reload]<color=yellow>{s}</color>");
        }

        public static void DoHook(Dictionary<string, List<MethodBase>> methodsToHook)
        {
            foreach (var kv in methodsToHook)
            {
                var assName = kv.Key;
                var hookTag = string.Format(kHotReloadHookTag_Fmt, assName);
                HookPool.UninstallByTag(hookTag);

                var patchAssPath = string.Format(kPatchDllPathFormat, Path.GetFileNameWithoutExtension(assName), GenPatchAssemblies.patchNo);
                var patchAssembly = Assembly.LoadFrom(patchAssPath);
                if (patchAssembly == null)
                {
                    Debug.LogError($"Dll Load Fail:{patchAssPath}");
                    continue;
                }

                foreach (var method in kv.Value)
                {
                    var miTarget = method;
                    if (miTarget.ContainsGenericParameters) // 泛型暂时不处理
                        continue;

                    var miReplace = GetMethodFromAssembly(miTarget, patchAssembly);
                    if (miReplace == null)
                    {
                        Debug.LogError($"can not find method `{miTarget}` in [{assName}]");
                        continue;
                    }

                    Debug.Log($"hook method, {miTarget.Name}");
                    try
                    {
                        new MethodHook(miTarget, miReplace, null, hookTag).Install();
                    }
                    catch (BadImageFormatException exception)
                    {
                        Debug.LogError(exception);
                    }
                }
            }
        }
    }
}