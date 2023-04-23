using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static ScriptHotReload.HookAssemblies;

namespace ScriptHotReload
{
    public class AutoCheckFileModify
    {
        [Serializable]
        public class AssemblyDefinitionContent
        {
            public string name;
            public bool allowUnsafeCode;
        }

        public static HashSet<string> s_CacheFilePath;

        private static FileSystemWatcher s_Watcher;

#if ENABLE_JN_HOT_RELOAD && UNITY_EDITOR

        [MenuItem("ScriptHotReload/AutoPatchAssembly")]
        public static void AutoPatch()
        {
            if (!Application.isPlaying)
            {
                LogWarning("only runtime can hot reload");
                return;
            }

            if (s_CacheFilePath == null) return;

            if (s_CacheFilePath.Count == 0)
            {
                LogWarning("no file need hot reload!!!");
                return;
            }

            var dll2Files = GenPatchAssemblies.hookDll2Doc;
            dll2Files.Clear();

            var prefix = Application.dataPath.Replace(@"\", "/");

            var dll2AsmPath = new Dictionary<string, string>();
            foreach (var fullPath in s_CacheFilePath.ToList())
            {
                var asmdef = TraverseAsmDef(fullPath);
                if (!string.IsNullOrEmpty(asmdef))
                {
                    var key = "Assets" + fullPath.Replace(@"\", "/").Replace(prefix, "");
                    var content = JsonUtility.FromJson<AssemblyDefinitionContent>(File.ReadAllText(asmdef));
                    var value = content.name + ".dll";
                    if (dll2Files.ContainsKey(value) == false)
                    {
                        dll2Files[value] = new HashSet<string>();
                    }
                    dll2AsmPath[value] = asmdef;

                    dll2Files[value].Add(key);
                }
            }

            if (dll2Files.Count == 0)
            {
                s_CacheFilePath.Clear();
                LogWarning("no dll need hot reload");
                return;
            }

            HotReloadConfig.hotReloadAssemblies = dll2Files.Keys.ToList();

            var sb = new StringBuilder();
            foreach (var ss in HotReloadConfig.hotReloadAssemblies) sb.Append(ss);

            Log("hot reload dll: " + sb.ToString());

            GenPatchAssemblies.DoGenPatchAssemblies(false, dll2Files, dll2AsmPath);
        }

        private static string TraverseAsmDef(string fullPath)
        {
            DirectoryInfo dInfo;
            if (Directory.Exists(fullPath))
                dInfo = new DirectoryInfo(fullPath).Parent;
            else
                dInfo = new FileInfo(fullPath).Directory;
            var result = dInfo.EnumerateFiles("*.asmdef");
            if (result.Count() != 0)
                return result.First().FullName;
            return TraverseAsmDef(dInfo.FullName);
        }


        [InitializeOnLoadMethod]
        public static void CheckModifyListener()
        {
            if (s_Watcher == null)
            {
                var watchPath = Path.Combine(Application.dataPath, "../");
                s_Watcher = new FileSystemWatcher(Path.GetFullPath(watchPath), "*.cs");
                s_Watcher.NotifyFilter = NotifyFilters.LastWrite;
                s_Watcher.Changed += OnModified;
                s_Watcher.Created += OnModified;
                s_Watcher.Deleted += OnModified;
                s_Watcher.Renamed += OnModified;
                s_Watcher.Error += OnError;
                s_Watcher.IncludeSubdirectories = true;
                s_Watcher.EnableRaisingEvents = true;

                Log("<color=yellow>start watching file changed</color>");
            }

            s_CacheFilePath = new HashSet<string>();
        }

        private static void OnModified(object sender, FileSystemEventArgs e)
        {
            if (CheckValidFile(e.FullPath))
            {
                s_CacheFilePath.Add(e.FullPath);
            }
        }

        private static bool CheckValidFile(string fullName)
        {
            var fileName = Path.GetFileName(fullName);
            if (fileName.StartsWith(".") || fileName.EndsWith("~"))
            {
                return false;
            }

            var dInfo = Directory.GetParent(fullName);
            while (dInfo != null)
            {
                if (dInfo.Name == "client")
                {
                    return true;
                }

                if (dInfo.Name.StartsWith(".") || dInfo.Name.EndsWith("~"))
                {
                    return false;
                }

                dInfo = dInfo.Parent;
            }

            return true;
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            PrintException(e.GetException());
        }

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                Log($"Message: {ex.Message}");
                Log("Stacktrace:");
                Log(ex.StackTrace);
                Log("\n");
                PrintException(ex.InnerException);
            }
        }
#endif
    }
}