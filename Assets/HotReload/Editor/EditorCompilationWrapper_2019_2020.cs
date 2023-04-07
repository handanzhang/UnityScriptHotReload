/*
 * Author: Misaka Mikoto
 * email: easy66@live.com
 * github: https://github.com/Misaka-Mikoto-Tech/UnityScriptHotReload
 */

#if !UNITY_2021_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace ScriptHotReload
{
    public enum CompileStatus
    {
        Idle,
        Compiling,
        CompilationStarted,
        CompilationFailed,
        CompilationComplete
    }

    [Flags]
    public enum EditorScriptCompilationOptions
    {
        BuildingEmpty = 0,
        BuildingDevelopmentBuild = 1 << 0,
        BuildingForEditor = 1 << 1,
        BuildingEditorOnlyAssembly = 1 << 2,
        BuildingForIl2Cpp = 1 << 3,
        BuildingWithAsserts = 1 << 4,
        BuildingIncludingTestAssemblies = 1 << 5,
        BuildingPredefinedAssembliesAllowUnsafeCode = 1 << 6,
        BuildingForHeadlessPlayer = 1 << 7,
        BuildingUseDeterministicCompilation = 1 << 9,
        BuildingWithRoslynAnalysis = 1 << 10,
        BuildingWithoutScriptUpdater = 1 << 11
    }

    public class CompileSetttings
    {
        public EditorScriptCompilationOptions options;
        public BuildTarget platform;
        public BuildTargetGroup platformGroup;
    }

    /// <summary>
    ///     封装反射调用的 UnityEditor.Scripting.ScriptCompilation 及相关命名空间内的类型和函数
    /// </summary>
    public static class EditorCompilationWrapper
    {
        public static MethodInfo miSetCompileScriptsOutputDirectory;

        static EditorCompilationWrapper()
        {
            tEditorCompilationInterface = typeof(AssetDatabase).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
            tEditorCompilation = typeof(AssetDatabase).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilation");
            tScriptAssemblySettings = typeof(AssetDatabase).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.ScriptAssemblySettings");
            tCompilationPipeline = typeof(AssetDatabase).Assembly.GetType("UnityEditor.Compilation.CompilationPipeline");

            miTickCompilationPipeline = tEditorCompilationInterface.GetMethod("TickCompilationPipeline", BindingFlags.Static | BindingFlags.Public);

            foreach (var mi in tEditorCompilation.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (mi.Name == "CreateScriptAssemblySettings" && mi.GetParameters().Length == 3)
                    miCreateScriptAssemblySettings = mi;
                else if (mi.Name == "CompileScripts" && mi.GetParameters().Length >= 5) // 2019:6, 2020:5
                    miCompileScripts = mi;
                else if (mi.Name == "SetCompileScriptsOutputDirectory")
                    miSetCompileScriptsOutputDirectory = mi;
            miScriptSettings_SetOutputDirectory = tScriptAssemblySettings.GetProperty("OutputDirectory", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();


            miRequestScriptCompilation = tCompilationPipeline.GetMethod("RequestScriptCompilation", BindingFlags.Public | BindingFlags.Static);
            miDirtyAllScripts = tEditorCompilationInterface.GetMethod("DirtyAllScripts", BindingFlags.Public | BindingFlags.Static);

            EditorCompilation_Instance = tEditorCompilationInterface.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public).GetGetMethod().Invoke(null, null);
        }

        public static Type tEditorCompilationInterface { get; }
        public static Type tEditorCompilation { get; }
        public static Type tScriptAssemblySettings { get; }
        public static Type tCompilationPipeline { get; }

        public static MethodInfo miTickCompilationPipeline { get; }
        public static MethodInfo miBeeDriver_Tick { get; private set; }
        public static MethodInfo miCreateScriptAssemblySettings { get; }
        public static MethodInfo miScriptSettings_SetOutputDirectory { get; }
        public static MethodInfo miCompileScripts { get; }
        public static MethodInfo miRequestScriptCompilation { get; }
        public static MethodInfo miDirtyAllScripts { get; }

        public static object EditorCompilation_Instance { get; }

        public static CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platfromGroup, BuildTarget platform, int subtarget, string[] extraScriptingDefines, bool allowBlocking)
        {
#if UNITY_2020_1_OR_NEWER
            var ret = (CompileStatus) miTickCompilationPipeline.Invoke(null, new object[]
            {
                (int) options, platfromGroup, platform, extraScriptingDefines
            });

#else
            CompileStatus ret = (CompileStatus)miTickCompilationPipeline.Invoke(null, new object[]
            {
                (int)options, platfromGroup, platform
            });
#endif
            return ret;
        }

        public static object CreateScriptAssemblySettings(BuildTargetGroup platfromGroup, BuildTarget platform, EditorScriptCompilationOptions options, string[] extraScriptingDefines, string outputDir)
        {
            var ret = miCreateScriptAssemblySettings.Invoke(EditorCompilation_Instance, new object[] {platfromGroup, platform, (int) options});
            SetScriptAssemblyOutputDir(ret, outputDir);
            return ret;
        }

        public static void SetScriptAssemblyOutputDir(object scriptAssemblySettings, string buildDir)
        {
            miScriptSettings_SetOutputDirectory.Invoke(scriptAssemblySettings, new object[] {buildDir});
        }

        public static CompileStatus CompileScriptsWithSettings(object scriptAssemblySettings)
        {
#if UNITY_2020_1_OR_NEWER
            var param = CompileScript.editorBuildParams;

            miSetCompileScriptsOutputDirectory.Invoke(EditorCompilation_Instance, new object[] {param.outputDir});

            var ret = (CompileStatus) miCompileScripts.Invoke(EditorCompilation_Instance, new object[]
            {
                (int) param.options, param.platformGroup, param.platform, param.extraScriptingDefines, /*StopOnFirstError*/1
            });
#else
            var param = CompileScript.editorBuildParams;
            CompileStatus ret = (CompileStatus)miCompileScripts.Invoke(EditorCompilation_Instance, new object[] 
            {
                scriptAssemblySettings, param.outputDir, (int)param.options, /*StopOnFirstError*/1, null, null
            });
#endif
            return ret;
        }

        public static void RequestScriptCompilation(string reason)
        {
            miRequestScriptCompilation.Invoke(null, new object[] { });
        }

        public static void DirtyAllScripts()
        {
            miDirtyAllScripts.Invoke(null, new object[] { });
            
        }

        public static void DirtyOndemand(Dictionary<string, string> path2Dll)
        {
            var method = tEditorCompilation.GetMethod("DirtyScript", BindingFlags.Public | BindingFlags.Instance);

            foreach (var k in path2Dll)
            {
                method.Invoke(EditorCompilation_Instance, new[]
                {
                    k.Key,
                    k.Value
                });    
            }
        }
    }
}
#endif