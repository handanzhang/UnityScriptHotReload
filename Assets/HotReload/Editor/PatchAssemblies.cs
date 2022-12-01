﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static UnityEngine.GraphicsBuffer;
using UnityEditor.Build.Player;
using System.IO;
using UnityEditor.Callbacks;
using System.Reflection;
using MonoHook;
using System.Runtime.CompilerServices;
using System;
using System.Reflection.Emit;
using Mono;
using Mono.Cecil;
using System.Linq;
using System.Text;

using static ScriptHotReload.HotReloadUtils;
using static ScriptHotReload.HotReloadConfig;

namespace ScriptHotReload
{
    public class GenPatchAssemblies
    {
        public static bool codeHasChanged => methodsToHook.Count > 0;

        public static Dictionary<string, List<MethodData>> methodsToHook { get; private set; } = new Dictionary<string, List<MethodData>>();

        public static int patchNo { get; private set; } = 0;

        private static Dictionary<string, AssemblyData> _assemblyDatas = new Dictionary<string, AssemblyData>();

        [MenuItem("ScriptHotReload/PatchAssemblies")]
        public static void DoGenPatchAssemblies()
        {
            if (!Application.isPlaying)
                return;

            CompileScript.OnCompileSuccess = OnScriptCompileSuccess;
            CompileScript.CompileScriptToDir(kTempCompileToDir);
        }

        public static void OnScriptCompileSuccess(CompileStatus status)
        {
            if (status != CompileStatus.Idle)
                return;

            foreach(string assName in hotReloadAssemblies)
            {
                string assNameNoExt = Path.GetFileNameWithoutExtension(assName);

                string baseDll = $"{kBuiltinAssembliesDir}/{assName}";
                string lastDll = string.Format(kLastDllPathFormat, assNameNoExt);
                string newDll = $"{kTempCompileToDir}/{assName}";

                if (IsFilesEqual(newDll, lastDll))
                    continue;

                using var baseAssDef = AssemblyDefinition.ReadAssembly(baseDll);
                using var newAssDef = AssemblyDefinition.ReadAssembly(newDll);

                var assBuilder = new AssemblyDataBuilder(baseAssDef, newAssDef);
                if (!assBuilder.DoBuild(patchNo))
                {
                    Debug.LogError($"[{assName}]不符合热重载条件，停止重载");
                    return;
                }

                if (!File.Exists(lastDll))
                    File.Copy(baseDll, lastDll);

                if (assBuilder.assemblyData.methodModified.Count == 0)
                    continue;

                string patchDll = string.Format(kPatchDllPathFormat, assNameNoExt, patchNo);
                newAssDef.Write(patchDll);

                methodsToHook.Add(assName, assBuilder.assemblyData.methodModified);
            }

            if(methodsToHook.Count == 0)
            {
                Debug.Log("代码没有发生改变，不执行热重载");
                return;
            }

            HookAssemblies.DoHook(methodsToHook);
            if (methodsToHook.Count > 0)
                patchNo++;
        }
    }

}
