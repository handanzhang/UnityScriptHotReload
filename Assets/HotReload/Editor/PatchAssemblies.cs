/*
 * Author: Misaka Mikoto
 * email: easy66@live.com
 * github: https://github.com/Misaka-Mikoto-Tech/UnityScriptHotReload
 */

//#define PATCHER_DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static ScriptHotReload.HotReloadUtils;
using static ScriptHotReload.HotReloadConfig;
using Debug = UnityEngine.Debug;

namespace ScriptHotReload
{

    public class HookMethodInfo
    {
        public MethodBase methodBase;
        public string document;
    }
    
    public class GenPatchAssemblies
    {
        public static bool codeHasChanged => methodsToHook.Count > 0;

        public static Dictionary<string, List<HookMethodInfo>> methodsToHook { get; } = new Dictionary<string, List<HookMethodInfo>>();

        public static int patchNo { get; private set; }

        public static Dictionary<string, HashSet<string>> hookDll2Doc = new Dictionary<string, HashSet<string>>();

        [InitializeOnLoadMethod]
        private static void Init()
        {
            patchNo = 0;
            methodsToHook.Clear();
        }

        public static void DoGenPatchAssemblies(bool dirtyAll, Dictionary<string, HashSet<string>> dll2Files)
        {
            if (!Application.isPlaying)
                return;

            CompileScript.OnCompileSuccess = OnScriptCompileSuccess;
            RemoveAllFiles(kTempCompileToDir);

            if (dirtyAll)
            {
                EditorCompilationWrapper.DirtyAllScripts();
            }
            else
            {
                var dest = Path.Combine(Application.dataPath, "../Temp/ScriptHotReload/tmp");
                if (Directory.Exists(dest)) Directory.Delete(dest, true);
                var dInfo = new DirectoryInfo(dest);
                if (dInfo.Parent?.Exists == false)
                {
                    dInfo.Parent.Create();
                }
                FileUtil.CopyFileOrDirectory("Library/ScriptAssemblies", dInfo.FullName);
                EditorCompilationWrapper.DirtyOndemand(dll2Files);
            }
            CompileScript.CompileScriptToDir(kTempCompileToDir);
        }

        public static void OnScriptCompileSuccess(CompileStatus status)
        {
            if (status != CompileStatus.Idle)
                return;
            methodsToHook.Clear();
            GenPatcherInputArgsFile();
            if (RunAssemblyPatchProcess() != 0)
                return;

            ParseOutputReport();
            if (methodsToHook.Count == 0)
            {
                Debug.Log("代码没有发生改变，不执行热重载");
                return;
            }

            HookAssemblies.DoHook(methodsToHook);
            if (methodsToHook.Count > 0)
                patchNo++;

            Debug.Log($"<color=yellow>热重载完成 patch no: {patchNo}</color>");
        }

        private static int RunAssemblyPatchProcess()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.GetDirectoryName(GetThisFilePath()) + "/AssemblyPatcher~/AssemblyPatcher.exe";
#if PATCHER_DEBUG
            startInfo.Arguments = $"{kAssemblyPatcherInput} {kAssemblyPatcherOutput} debug";
            startInfo.CreateNoWindow = false;
#else
            startInfo.Arguments = $"{kAssemblyPatcherInput} {kAssemblyPatcherOutput}";
            startInfo.CreateNoWindow = true;
#endif
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            //startInfo.StandardInputEncoding = System.Text.UTF8Encoding.UTF8;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
            var procPathcer = new Process();
            procPathcer.StartInfo = startInfo;
            procPathcer.Start();

            Action<StreamReader> outputProcMsgs = sr =>
            {
                var line = sr.ReadLine();
                while (line != null)
                {
                    line = line.Replace("<br/>", "\r\n");
                    if (line.StartsWith("[Info]"))
                        Debug.Log($"<color=lime>[Patcher] {line.Substring("[Info]".Length)}</color>");
                    else if (line.StartsWith("[Warning]"))
                        Debug.LogWarning($"<color=orange>[Patcher] {line.Substring("[Warning]".Length)}</color>");
                    else if (line.StartsWith("[Error]"))
                        Debug.LogError($"[Patcher] {line.Substring("[Error]".Length)}");

#if PATCHER_DEBUG || true
                    else if (line.StartsWith("[Debug]"))
                        Debug.Log($"<color=yellow>[Patcher] {line.Substring("[Debug]".Length)}</color>");
#endif
                    else
                        Debug.Log($"<color=white>[Patcher] {line}</color>");

                    line = sr.ReadLine();
                }
            };

            using (var sr = procPathcer.StandardOutput)
            {
                outputProcMsgs(sr);
            }

            using (var sr = procPathcer.StandardError)
            {
                outputProcMsgs(sr);
            }

            var exitCode = -1;
            if (procPathcer.WaitForExit(60 * 1000)) // 最长等待1分钟
                exitCode = procPathcer.ExitCode;
            else
                procPathcer.Kill();

            return exitCode;
        }

        private static void GenPatcherInputArgsFile()
        {
            var inputArgs = new InputArgs();
            inputArgs.patchNo = patchNo;
            inputArgs.workDir = Environment.CurrentDirectory;

            var docList = new List<string>();
            var prefix = Directory.GetParent(Application.dataPath);
            foreach (var dll in hotReloadAssemblies)
            {
                docList.AddRange(hookDll2Doc[dll].Select(filePath =>
                                                         {
                                                             return Path.Combine(prefix.FullName, filePath).Replace("\\", "/");
                                                         }));
            }
            docList.Add("[segmentation]");
            
            inputArgs.assembliesToPatch = hotReloadAssemblies.ToArray();
            inputArgs.hookDocs = docList.ToArray();
            inputArgs.patchAssemblyNameFmt = kPatchAssemblyName;
            inputArgs.tempScriptDir = kTempScriptDir;
            inputArgs.tempCompileToDir = kTempCompileToDir;
            inputArgs.builtinAssembliesDir = kBuiltinAssembliesDir;
            inputArgs.lastDllPathFmt = kLastDllPathFormat;
            inputArgs.patchDllPathFmt = kPatchDllPathFormat;
            inputArgs.lambdaWrapperBackend = kLambdaWrapperBackend;
            inputArgs.fallbackAssemblyPathes = GetFallbackAssemblyPaths().Values.ToArray();

            var jsonStr = JsonUtility.ToJson(inputArgs, true);
            File.WriteAllText(kAssemblyPatcherInput, jsonStr);
        }

        private static void ParseOutputReport()
        {
            methodsToHook.Clear();

            if (!File.Exists(kAssemblyPatcherOutput))
                throw new Exception($"can not find output report file `{kAssemblyPatcherOutput}`");

            var text = File.ReadAllText(kAssemblyPatcherOutput);
            var outputReport = JsonUtility.FromJson<OutputReport>(text);

            foreach (var assName in outputReport.assemblyChangedFromLast) methodsToHook.Add(assName, new List<HookMethodInfo>());

            foreach (var data in outputReport.methodsNeedHook)
            {
                if (data.isGeneric)
                    continue;

                var t = Type.GetType(data.type, true);

                var flags = BindingFlags.Default;
                flags |= data.isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
                flags |= data.isStatic ? BindingFlags.Static : BindingFlags.Instance;

                var paramTypes = new Type[data.paramTypes.Length];
                for (int i = 0, imax = paramTypes.Length; i < imax; i++) paramTypes[i] = Type.GetType(data.paramTypes[i], true);
                MethodBase method;
                if (data.isConstructor)
                    method = t.GetConstructor(flags, null, paramTypes, null);
                else
                    method = t.GetMethod(data.name, flags, null, paramTypes, null);
                
                if (method == null)
                {
                    HookAssemblies.Log($"add new method {data.name}");
                    return;
                }
                    // throw new Exception($"can not find method `{data.name}`");

                if (!methodsToHook.TryGetValue(data.assembly, out var list))
                    throw new Exception($"unexpected assembly name `{data.assembly}`");

                list.Add(new HookMethodInfo()
                {
                    methodBase = method,
                    document   = data.document
                });
            }
        }

        [Serializable]
        public class InputArgs
        {
            public int patchNo;
            public string workDir;
            public string[] assembliesToPatch;
            public string[] hookDocs;
            public string patchAssemblyNameFmt;
            public string tempScriptDir;
            public string tempCompileToDir;
            public string builtinAssembliesDir;
            public string lastDllPathFmt;
            public string patchDllPathFmt;
            public string lambdaWrapperBackend;

            public string[] fallbackAssemblyPathes;
        }

        [Serializable]
        public class OutputReport
        {
            public int patchNo;
            public string[] assemblyChangedFromLast;
            public List<MethodData> methodsNeedHook;

            [Serializable]
            public class MethodData
            {
                public string name;
                public string type;
                public string assembly;
                public bool isConstructor;
                public bool isGeneric;
                public bool isPublic;
                public bool isStatic;
                public bool isLambda;
                public bool ilChanged;
                public string document;
                public string returnType;
                public string[] paramTypes;
            }
        }
    }
}