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
            Debug.Log($"[hot reload] <color=#00ECE5>{s}</color>");
        }

        public static void LogWarning(string s)
        {
            Debug.Log($"[hot reload] <color=yellow>{s}</color>");
        }

        public static void DoHook(Dictionary<string, List<HookMethodInfo>> methodsToHook)
        {
            var prefix = Application.dataPath.Replace(@"\", "/");

            var hookSucceed = new Dictionary<string, int>();
            var unityPath2FullPath = new Dictionary<string, string>();

            const int DEFAULT_VALUE = -1;

            foreach (var fullPath in AutoCheckFileModify.s_CacheFilePath)
            {
                var key = "Assets" + fullPath.Replace(@"\", "/").Replace(prefix, "");
                hookSucceed[key] = DEFAULT_VALUE;
                unityPath2FullPath[key] = fullPath;
            }
            
            foreach (var kv in methodsToHook)
            {
                var assName = kv.Key;
                var hookTag = string.Format(kHotReloadHookTag_Fmt, assName);
                HookPool.UninstallByTag(hookTag);

                var patchAssPath = string.Format(kPatchDllPathFormat, Path.GetFileNameWithoutExtension(assName), GenPatchAssemblies.patchNo);
                var patchAssembly = Assembly.LoadFrom(patchAssPath);
                
                foreach (var hookMethod in kv.Value)
                {
                    var miTarget = hookMethod.methodBase;
                    if (miTarget.ContainsGenericParameters) // 泛型暂时不处理
                        continue;

                    var miReplace = GetMethodFromAssembly(miTarget, patchAssembly);
                    if (miReplace == null)
                    {
                        Debug.LogError($"can not find method `{miTarget}` in [{assName}]");
                        continue;
                    }

                    Log($"hook method, {miTarget.Name}");
                    var key = hookMethod.document.Replace(@"\", "/");
                    try
                    {
                        new MethodHook(miTarget, miReplace, null, hookTag).Install();
                        if (hookSucceed.TryGetValue(key, out var result))
                        {
                            if (result == DEFAULT_VALUE)
                            {
                                hookSucceed[key] = 0;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        hookSucceed[key] = 1;
                        Debug.LogError(exception);
                    }
                }
            }

            var tmp = new List<string>();
            foreach (var pair in hookSucceed)
            {
                if (pair.Value != 0)
                {
                    continue;
                }
                Log("hot reload succeed file: " + unityPath2FullPath[pair.Key]);
                tmp.Add(unityPath2FullPath[pair.Key]);
            }

            AutoCheckFileModify.OnHotReloadSucceedFile(tmp);
        }
    }
}