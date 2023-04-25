using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace ScriptHotReload
{
    public class ModifyFileListWindow : OdinEditorWindow
    {
        public static void Open(bool hotreload)
        {
            ModifyFileListWindow window = GetWindowWithRect(typeof(ModifyFileListWindow), new Rect(400, 200, 400, 400), false, "ListFiles") as ModifyFileListWindow;
            if (window == null)
            {
                return;
            }
            window.showHotReload = hotreload;

            if (window.opened)
            {
                window.UpdateList();
            }
            else
            {
                window.maxSize = new Vector2(1920, 1080); 
                EditorCoroutineUtility.StartCoroutineOwnerless(StartDockWindows(window));    
            }
        }

        public static IEnumerator StartDockWindows(ModifyFileListWindow w)
        {
            yield return new WaitForSeconds(0.2f);
            GetWindow<HotReloadMainWindow>().DockWindow(w, Docker.DockPosition.Right);
            w.opened = true;
            w.UpdateList();
            yield return null;
        }
        
        protected override void OnEnable()
        {
            AutoCheckFileModify.FileChanged -= UpdateList;
            AutoCheckFileModify.FileChanged += UpdateList;
            UpdateList();
        }
        
        private void OnDisable()
        {
            AutoCheckFileModify.FileChanged -= UpdateList;
            AutoCheckFileModify.FileChanged += UpdateList;
        }

        protected override void OnDestroy()
        {
            AutoCheckFileModify.FileChanged -= UpdateList;
            base.OnDestroy();
        }

        [HideInInspector] public bool opened = false;
        
        [HideInInspector]
        public bool showHotReload;

        [ListDrawerSettings(Expanded = true, IsReadOnly = true)]
        [ReadOnly]
        [HideLabel]
        public List<string> modifyFiles;

        void UpdateList()
        {
            var workingDir = Directory.GetParent(Application.dataPath)?.FullName + "\\";
            if (showHotReload)
            {
                modifyFiles = AutoCheckFileModify.s_CacheFilePath.Select(f => f.Replace("/", "\\").Replace(workingDir, "")).ToList();
            }
            else
            {
                modifyFiles = AutoCheckFileModify.s_AllCacheFilePath.Select(f => f.Replace("/", "\\").Replace(workingDir, "")).ToList();
            }
        }
    }
}