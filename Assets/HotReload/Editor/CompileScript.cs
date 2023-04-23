/*
 * Author: Misaka Mikoto
 * email: easy66@live.com
 * github: https://github.com/Misaka-Mikoto-Tech/UnityScriptHotReload
 */


using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MonoHook;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ScriptHotReload
{
    /// <summary>
    ///     手动编译Editor下的脚本到指定目录
    /// </summary>
    public static class CompileScript
    {
        public static CompileStatus compileStatus { get; private set; }

        public static Action<CompileStatus> OnCompileSuccess;
        public static EditorBuildParams editorBuildParams;

        [Serializable]
        public struct EditorBuildParams
        {
            public EditorScriptCompilationOptions options;
            public int subtarget;
            public string[] extraScriptingDefines;
            public bool allowBlocking;

            public string outputDir;
            public BuildTarget platform;
            public BuildTargetGroup platformGroup;
        }

        private static bool s_CompileRequested;

        public static void CompileScriptToDir(string outputDir)
        {
            if (!IsIdle())
            {
                Debug.LogError($"当前编译状态:{compileStatus}, 不允许执行编译");
                return;
            }

            // 生成编译配置并指定输出目录
            editorBuildParams.outputDir = outputDir;
            var scriptAssemblySettings = EditorCompilationWrapper.CreateScriptAssemblySettings(
                editorBuildParams.platformGroup, editorBuildParams.platform, editorBuildParams.options, editorBuildParams.extraScriptingDefines, outputDir);

            CompilationPipeline.assemblyCompilationStarted -= CompileScriptJN.OnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationStarted += CompileScriptJN.OnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= AsmCompileResult;
            CompilationPipeline.assemblyCompilationFinished += AsmCompileResult;

            EditorApplication.update -= EditorApplication_Update;
            EditorApplication.update += EditorApplication_Update;
            
            s_CompileRequested = true;

            EditorCompilationWrapper.CompileScriptsWithSettings(scriptAssemblySettings);
            Debug.Log($"开始编译dll到目录: {outputDir}");
            

            ManualTickCompilationPipeline();
        }

        public static void AsmCompileResult(string asm, CompilerMessage[] msgs)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"asm name: {asm}");

            var hasError = false;
            
            foreach (var msg in msgs)
            {
                if (msg.type == CompilerMessageType.Error)
                {
                    sb.AppendLine(msg.message);
                    hasError = true;
                }
            }

            if (hasError)
            {
                Debug.LogError($"compile errors: {sb}");
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void Init()
        {
#if ENABLE_JN_HOT_RELOAD
            {
                // install hook
                var miOri = EditorCompilationWrapper.miTickCompilationPipeline;
                var miNew = typeof(CompileScript).GetMethod(nameof(TickCompilationPipeline), BindingFlags.NonPublic | BindingFlags.Static);
                var miReplace = typeof(CompileScript).GetMethod(nameof(TickCompilationPipeline_Proxy), BindingFlags.NonPublic | BindingFlags.Static);
                new MethodHook(miOri, miNew, miReplace).Install();
            }
#endif
        }

        private static void EditorApplication_Update()
        {
            if (s_CompileRequested)
            {
                if (IsIdle())
                {
                    s_CompileRequested = false;

                    if (compileStatus == CompileStatus.Idle)
                        Debug.Log("编译已完成");
                    else
                        Debug.LogError($"编译失败:{compileStatus}");

                    CompilationPipeline.assemblyCompilationFinished -= AsmCompileResult;
                    CompilationPipeline.assemblyCompilationStarted -= CompileScriptJN.OnAssemblyCompilationStarted;
                    EditorApplication.update -= EditorApplication_Update;
                    OnCompileSuccess?.Invoke(compileStatus);
                }
                else if (Application.isPlaying) // PlayMode 下Unity会停止调用 TickCompilationPipeline, 导致编译请求进度无法前进，所以需要我们手动去执行
                {
                    ManualTickCompilationPipeline();
                }
            }
        }

        private static bool IsIdle()
        {
            return compileStatus == CompileStatus.Idle || compileStatus == CompileStatus.CompilationFailed;
        }

        public static void ResetCompileStatus()
        {
            s_CompileRequested = false;
            compileStatus = CompileStatus.Idle;
        }

        private static void ManualTickCompilationPipeline()
        {
            compileStatus = EditorCompilationWrapper.TickCompilationPipeline(
                editorBuildParams.options, editorBuildParams.platformGroup, editorBuildParams.platform,
                editorBuildParams.subtarget, editorBuildParams.extraScriptingDefines, editorBuildParams.allowBlocking);
        }


#if UNITY_2022_2_OR_NEWER
        /// <summary>
        /// 拦截Unity自己的Editor编译函数获取编译参数
        /// </summary>
        /// <param name="options">type:EditorScriptCompilationOptions</param>
        /// <remarks>此函数每帧都会被调用，即使当前无需编译</remarks>
        static CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform, int subtarget, string[] extraScriptingDefines, bool allowBlocking)
        {
            editorBuildParams.options = options;
            editorBuildParams.platformGroup = platfromGroup;
            editorBuildParams.platform = platform;
            editorBuildParams.subtarget = subtarget;
            editorBuildParams.extraScriptingDefines = extraScriptingDefines;
            editorBuildParams.allowBlocking = allowBlocking;

            compileStatus = TickCompilationPipeline_Proxy(options, platfromGroup, platform, subtarget, extraScriptingDefines, allowBlocking);
            //Debug.Log($"TickCompilationPipleline with status:{s_CompileStatus}");
            return compileStatus;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static CompileStatus TickCompilationPipeline_Proxy(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform, int subtarget, string[] extraScriptingDefines, bool allowBlocking)
        {
            Debug.Log($"dummy code " + platfromGroup.GetType());
            return CompileStatus.Idle;
        }
#elif UNITY_2021_2_OR_NEWER
        /// <summary>
        /// 拦截Unity自己的Editor编译函数获取编译参数
        /// </summary>
        /// <param name="options">type:EditorScriptCompilationOptions</param>
        /// <remarks>此函数每帧都会被调用，即使当前无需编译</remarks>
        static CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform, int subtarget, string[] extraScriptingDefines)
        {
            editorBuildParams.options = options;
            editorBuildParams.platformGroup = platfromGroup;
            editorBuildParams.platform = platform;
            editorBuildParams.subtarget = subtarget;
            editorBuildParams.extraScriptingDefines = extraScriptingDefines;

            compileStatus = TickCompilationPipeline_Proxy(options, platfromGroup, platform, subtarget, extraScriptingDefines);
            //Debug.Log($"TickCompilationPipleline with status:{s_CompileStatus}");
            return compileStatus;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static CompileStatus TickCompilationPipeline_Proxy(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform, int subtarget, string[] extraScriptingDefines)
        {
            Debug.Log($"dummy code " + platfromGroup.GetType());
            return CompileStatus.Idle;
        }
#elif UNITY_2020_1_OR_NEWER
        /// <summary>
        ///     拦截Unity自己的Editor编译函数获取编译参数
        /// </summary>
        /// <param name="options">type:EditorScriptCompilationOptions</param>
        /// <remarks>此函数每帧都会被调用，即使当前无需编译</remarks>
        private static CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform, string[] extraScriptingDefines)
        {
            editorBuildParams.options = options;
            editorBuildParams.platformGroup = platfromGroup;
            editorBuildParams.platform = platform;
            editorBuildParams.extraScriptingDefines = extraScriptingDefines;

            compileStatus = TickCompilationPipeline_Proxy(options, platfromGroup, platform, extraScriptingDefines);
            //Debug.Log($"TickCompilationPipleline with status:{s_CompileStatus}");
            return compileStatus;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static CompileStatus TickCompilationPipeline_Proxy(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform, string[] extraScriptingDefines)
        {
            Debug.Log("dummy code " + platfromGroup.GetType());
            return CompileStatus.Idle;
        }
#else
        /// <summary>
        /// 拦截Unity自己的Editor编译函数获取编译参数
        /// </summary>
        /// <param name="options">type:EditorScriptCompilationOptions</param>
        /// <remarks>此函数每帧都会被调用，即使当前无需编译</remarks>
        static CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform)
        {
            editorBuildParams.options = options;
            editorBuildParams.platformGroup = platfromGroup;
            editorBuildParams.platform = platform;

            compileStatus = TickCompilationPipeline_Proxy(options, platfromGroup, platform);
            //Debug.Log($"TickCompilationPipleline with status:{s_CompileStatus}");
            return compileStatus;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static CompileStatus TickCompilationPipeline_Proxy(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform)
        {
            Debug.Log($"dummy code " + platfromGroup.GetType());
            return CompileStatus.Idle;
        }
#endif
    }
}
