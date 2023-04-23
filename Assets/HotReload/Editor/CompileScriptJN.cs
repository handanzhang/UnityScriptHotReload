
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEngine;
using static ScriptHotReload.CompileScript;
using Debug = UnityEngine.Debug;

namespace ScriptHotReload
{

    static class ReflectionEx
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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

        private static List<MetadataReference> s_AllRefs = new List<MetadataReference>();

        public static void OnAssemblyCompilationStarted(String name)
        {
            
            string assemblyFilename = Path.GetFileName(name);

            if (HotReloadConfig.hotReloadAssemblies.Contains(assemblyFilename) == false)
            {
                return;
            }

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
            FieldInfo fiProcess = tScriptCompilerBase.GetField("process", BindingFlags.NonPublic | BindingFlags.Instance);

            var tmp = ReflectionEx.Get(compiler, "process", fiProcess);
            ProcessStartInfo psi = ReflectionEx.Call(tmp, "GetProcessStartInfo") as ProcessStartInfo;

            string responseFile = Regex.Replace(psi.Arguments, "^.*@(.+)$", "$1");
            
            
            var text = File.ReadAllText(responseFile);

            if (text.Contains("/unsafe") == false)
            {
                text += "/unsafe";
            }
            File.WriteAllText(responseFile, text);
        }
        
        
        public static void ManuCompilation(Dictionary<string, string>dll2AsmPath)
        {
            var sw = new Stopwatch();

            foreach (var hotreloadAsmFileName in HotReloadConfig.hotReloadAssemblies)
            {
                var workingDir = Directory.GetParent(Application.dataPath).FullName;
                
                List<MetadataReference> refAsmblyList = new List<MetadataReference>();
                List<SyntaxTree> syntaxTreeList = new List<SyntaxTree>();

                var files = new List<string>();
                var refs = new List<string>();
                var symbols = new List<string>();
                
                sw.Restart();
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(Path.Combine(workingDir, $"{Path.GetFileNameWithoutExtension(hotreloadAsmFileName)}.csproj"));
                    var xmlNs = new XmlNamespaceManager(xmlDoc.NameTable);
                    xmlNs.AddNamespace("ns", xmlDoc.DocumentElement.NamespaceURI);

                    var docNode = xmlDoc.SelectSingleNode("/ns:Project", xmlNs);

                    var propertyGroupList = xmlDoc.SelectNodes(".//ns:PropertyGroup", xmlNs);
                    if (propertyGroupList != null)
                    {
                        for (int i = 0; i < propertyGroupList.Count; ++i)
                        {
                            var propertyNode = propertyGroupList[i];
                            var defineNode = propertyNode.SelectSingleNode(".//ns:DefineConstants", xmlNs);
                            if (defineNode == null)
                            {
                                continue;
                            }

                            symbols = defineNode.InnerText.Split(',', ';').ToList();
                            break;
                        }
                    }

                    var itemGroupList = docNode?.SelectNodes("//ns:ItemGroup", xmlNs);
                    if (itemGroupList != null)
                    {
                        for (int i = 0; i < itemGroupList.Count; ++i)
                        {
                            var groupNode = itemGroupList[i];
                            var compileList = groupNode.SelectNodes(".//ns:Compile", xmlNs);
                            if (compileList != null)
                            {
                                for (int j = 0; j < compileList.Count; ++j)
                                {
                                    var compileNode = compileList[j];
                                    var relativePath = (compileNode.Attributes?["Include"]?.InnerText);
                                    if (relativePath != null && File.Exists(relativePath))
                                    {
                                        files.Add(Path.GetFullPath(relativePath));
                                    }
                                }
                            }

                            var refNodeList = groupNode.SelectNodes(".//ns:Reference", xmlNs);
                            if (refNodeList != null)
                            {
                                for (int k = 0; k < refNodeList.Count; ++k)
                                {
                                    var refNode = refNodeList[k];
                                    var refPath = refNode.SelectSingleNode(".//ns:HintPath", xmlNs)?.InnerText;
                                    if (refPath != null && File.Exists(refPath))
                                    {
                                        refs.Add(refPath);
                                    }
                                }
                            }
                            
                            var projectNodeList = groupNode.SelectNodes(".//ns:ProjectReference", xmlNs);
                            if (projectNodeList != null)
                            {
                                for(int l=0; l < projectNodeList.Count; ++l)
                                {
                                    var projectRefNode = projectNodeList[l];
                                    var refName = projectRefNode.SelectSingleNode(".//ns:Name", xmlNs)?.InnerText;
                                    refs.Add(Path.Combine(workingDir, HotReloadConfig.kTempCompileToDir, refName+".dll"));
                                }
                            }
                        }
                    }
                }
                sw.Stop();
                HookAssemblies.Log($"parse xml cost:  {sw.ElapsedMilliseconds}");

                sw.Restart();
                {
                    var csharpParseOptions = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: symbols);
                    syntaxTreeList = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), csharpParseOptions, f, Encoding.UTF8)).ToList();
                }
                sw.Stop();
                HookAssemblies.Log($"parse c# file cost:  {sw.ElapsedMilliseconds}");
                
                sw.Restart();
                {
                    refAsmblyList = refs.Select(dllPath => MetadataReference.CreateFromFile(dllPath) as MetadataReference).ToList();
                }
                sw.Stop();
                HookAssemblies.Log($"create dll refs cost:  {sw.ElapsedMilliseconds}");
                
                var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic:true);
                
                var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(hotreloadAsmFileName), syntaxTreeList, refAsmblyList, compilationOptions);
                
                var dllName = hotreloadAsmFileName;
                var pdbName = Path.GetFileNameWithoutExtension(hotreloadAsmFileName) + ".pdb";
                
                var emitOptions = new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.PortablePdb,
                    pdbFilePath: pdbName);
                
                sw.Restart();
                var dllPath = Path.Combine(HotReloadConfig.kTempCompileToDir, dllName);
                var pdbPath = Path.Combine(HotReloadConfig.kTempCompileToDir, pdbName);
                using (var dll = new FileStream(dllPath, FileMode.Create))
                {
                    using(var pdb = new FileStream(pdbPath, FileMode.Create))
                    {
                        var result = compilation.Emit(dll, pdb, options:emitOptions);
                        if (!result.Success)
                        {
                            foreach (var diagnos in result.Diagnostics)
                            {
                                if (diagnos.Severity == DiagnosticSeverity.Error)
                                {
                                    Debug.LogError($"compile error: {diagnos.GetMessage()}");
                                }
                            }
                        }
                    }
                }
                sw.Stop();
                HookAssemblies.Log($"generate target dll cost: {sw.ElapsedMilliseconds}");
            }
        }
    }
}