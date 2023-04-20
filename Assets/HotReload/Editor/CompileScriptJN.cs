
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using static ScriptHotReload.CompileScript;

namespace ScriptHotReload
{

    static class ReflectionEx
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                   BindingFlags.NonPublic;

        static object Inst(object self)
        {
            return (self is Type) ? null : self;
        }
        
        static Type Type(object self)
        {
            return (self as Type) ?? self.GetType();
        }
        
        public static object New(Type self, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return Type(self).GetConstructor(types)
                .Invoke(args);
        }

        public static object Call(object self, string methodName, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return Type(self).GetMethod(methodName, types)
                .Invoke(Inst(self), args);
        }

        public static object Call(object self, Type[] genericTypes, string methodName, params object[] args)
        {
            return Type(self).GetMethod(methodName, FLAGS)
                .MakeGenericMethod(genericTypes)
                .Invoke(Inst(self), args);
        }

        public static object Get(object self, string memberName, MemberInfo mi = null)
        {
            mi = mi ?? Type(self).GetMember(memberName, FLAGS)[0];
            return mi is PropertyInfo
                ? (mi as PropertyInfo).GetValue(Inst(self), new object[0])
                : (mi as FieldInfo).GetValue(Inst(self));
        }
        
        public static void Set(object self, string memberName, object value, MemberInfo mi = null)
        {
            mi = mi ?? Type(self).GetMember(memberName, FLAGS)[0];
            if (mi is PropertyInfo)
                (mi as PropertyInfo).SetValue(Inst(self), value, new object[0]);
            else
                (mi as FieldInfo).SetValue(Inst(self), value);
        }


    }

    public static class CompileScriptJN
    {

        public static List<string> s_CompileMarco = new List<string>();
        
        public static void CompileHotTest()
        {
            var settings = new ScriptCompilationSettings
            {
                target = BuildTarget.StandaloneWindows64,
                group = BuildTargetGroup.Standalone,
                extraScriptingDefines = new string[]{},
            };
            
            EditorCompilationWrapper.DirtyOndemand(new Dictionary<string, HashSet<string>>()
            {
                ["ReloadTest.dll"] = new HashSet<String>()
                {
                    "Assets/HotReload/ReloadTest/ReloadTest.cs"
                }
            });

            if (s_CompileMarco.Count == 0)
            {
                var scriptAssemblySettings = EditorCompilationWrapper.CreateScriptAssemblySettings(
                    editorBuildParams.platformGroup, editorBuildParams.platform, editorBuildParams.options, editorBuildParams.extraScriptingDefines, HotReloadConfig.kTempCompileToDir);

                editorBuildParams.outputDir = HotReloadConfig.kTempScriptDir;
                CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;

                EditorCompilationWrapper.CompileScriptsWithSettings(settings);
                
            }
            
        }

        static void OnAssemblyCompilationStarted(String name)
        {
            
            string assemblyName = Path.GetFileNameWithoutExtension(name);

            if (assemblyName != "ReloadTest")
                return;
            
            CompilationPipeline.assemblyCompilationStarted -= OnAssemblyCompilationStarted;

            string assemblyFilename = assemblyName + ".dll";

            var editorType = typeof(AssetDatabase).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilation");

            object instance = typeof(AssetDatabase).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface").GetProperty("Instance").GetValue(null);

            var field = editorType.GetField("compilationTask", BindingFlags.Instance | BindingFlags.NonPublic);
            var tasks = field.GetValue(instance);


            var taskType = typeof(AssetDatabase).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.CompilationTask");

            var result = taskType.GetField("compilerTasks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tasks);

            var compilerTasks = result as IDictionary;
                
            var scriptAssembly = compilerTasks.Keys.Cast<object>()
                .FirstOrDefault(x => (ReflectionEx.Get(x, "Filename") as string) == assemblyFilename);

            var compiler = compilerTasks[scriptAssembly];
            
            Type tScriptCompilerBase = Type.GetType("UnityEditor.Scripting.Compilers.ScriptCompilerBase, UnityEditor");
            FieldInfo fiProcess =
                tScriptCompilerBase.GetField("process", BindingFlags.NonPublic | BindingFlags.Instance);

            var tmp = ReflectionEx.Get(compiler, "process", fiProcess);
            ProcessStartInfo psi = ReflectionEx.Call(tmp, "GetProcessStartInfo") as ProcessStartInfo;
            ReflectionEx.Call(compiler, "Dispose");

            string responseFile = Regex.Replace(psi.Arguments, "^.*@(.+)$", "$1");
            var text = File.ReadAllText(responseFile);
            text = Regex.Replace(text, "[\r\n]+", "\n");
            text = Regex.Replace(text, "^-", "/", RegexOptions.Multiline);

            // get unity macro
            {
                var defines = Regex.Matches(text, "^/define:(.*)$", RegexOptions.Multiline)
                    .Cast<Match>()
                    .Select(x => x.Groups[1].Value);
                s_CompileMarco = defines.Distinct().ToList();
            }
        }

    }
}