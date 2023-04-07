#define ENABLE_OPEN_HOT_RELOAD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScriptHotReload;
using UnityEditor;
using UnityEngine;
using static ScriptHotReload.HookAssemblies;

namespace NS_Test
{
#if ENABLE_OPEN_HOT_RELOAD

    public class AutoCheckFileModify
    {
        [Serializable]
        public class AssemblyDefinitionContent
        {
            public string name;
        }
        
        private static FileSystemWatcher s_Watcher;

        private static HashSet<string> s_CacheFilePath;


#if UNITY_EDITOR
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
                LogWarning("no file changed!!!");
                return;
            }

            var file2Dll = new Dictionary<string, string>();

            var prefix = Application.dataPath.Replace(@"\", "/");
            
            foreach (var fullPath in s_CacheFilePath)
            {
                var asmdef = TraverseAsmDef(fullPath);
                if (!string.IsNullOrEmpty(asmdef))
                {
                    var key = "Assets" + fullPath.Replace(@"\", "/").Replace(prefix, "");
                    var content = JsonUtility.FromJson<AssemblyDefinitionContent>(File.ReadAllText(asmdef));
                    var value = content.name + ".dll";
                    file2Dll[key] = value;
                }
            }
            s_CacheFilePath.Clear();

            HotReloadConfig.hotReloadAssemblies = file2Dll.Values.ToList();

            var sb = new StringBuilder();
            foreach (var ss in HotReloadConfig.hotReloadAssemblies) sb.Append(ss);

            LogWarning(sb.ToString());

            GenPatchAssemblies.DoGenPatchAssemblies(false, file2Dll);
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
                s_Watcher = new FileSystemWatcher(Application.dataPath + "/Scripts", "*.cs");
                s_Watcher.NotifyFilter = NotifyFilters.LastWrite;
                s_Watcher.Changed += OnModified;
                s_Watcher.Created += OnModified;
                s_Watcher.Deleted += OnModified;
                s_Watcher.Renamed += OnModified;
                // s_Watcher.Changed += OnChanged;
                // s_Watcher.Created += OnCreated;
                // s_Watcher.Deleted += OnDeleted;
                // s_Watcher.Renamed += OnRenamed;
                s_Watcher.Error += OnError;
                s_Watcher.IncludeSubdirectories = true;
                s_Watcher.EnableRaisingEvents = true;

                Log("<color=yellow>start watching file changed</color>");
            }

            s_CacheFilePath = new HashSet<string>();
        }

        private static void OnModified(object sender, FileSystemEventArgs e)
        {
            Log($"modify state: {e.ChangeType}, {e.FullPath}");
            s_CacheFilePath.Add(e.FullPath);
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed) return;

            s_CacheFilePath.Add(e.FullPath);
            Log("changed: " + e.FullPath);
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            var value = $"Created: {e.FullPath}";
            s_CacheFilePath.Add(e.FullPath);
            Log(value);
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            s_CacheFilePath.Add(e.FullPath);
            Log($"Deleted: {e.FullPath}");
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            s_CacheFilePath.Add(e.FullPath);
            Log($"Renamed: Old: {e.OldFullPath}, New: {e.FullPath}");
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
#endif
}