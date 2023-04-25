using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace ScriptHotReload
{
    public class HotReloadMainWindow : OdinEditorWindow
    {
        private const string HOT_RELOAD_MACRO = "ENABLE_JN_HOT_RELOAD"; 
        
        [MenuItem("Tools/ScriptHotReload/window #%&R")]
        public static void Open()
        {
            var window = GetWindowWithRect(typeof(HotReloadMainWindow), new Rect(400, 200, 400, 400), false, "JNHotReload");
            window.Show();
        }

        
#if !ENABLE_JN_HOT_RELOAD
        [InfoBox("热重载功能尚未启用：\n  启用后会配置ENABLE_JN_HOT_RELOAD宏。\n  并且触发一轮全量编译.")]
        [Button(ButtonHeight = 30, Name = "点击启用热重载功能")]
        public void EnableHotReload()
        {
            var customMacro = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(',', ';');
            var resultMacro = ""; 
            if (customMacro.Last() == ";" || customMacro.Last() == ",")
            {
                resultMacro = customMacro + HOT_RELOAD_MACRO;
            }
            else
            {
                resultMacro = customMacro + ";" + HOT_RELOAD_MACRO;
            }
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, resultMacro);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#else
        protected override void OnEnable()
        {
            AutoCheckFileModify.FileChanged -= OnHotReloadFileModified;
            AutoCheckFileModify.FileChanged += OnHotReloadFileModified;
            OnHotReloadFileModified();
        }

        private void OnDisable()
        {
            AutoCheckFileModify.FileChanged -= OnHotReloadFileModified;
            AutoCheckFileModify.FileChanged += OnHotReloadFileModified;
        }

        protected override void OnDestroy()
        {
            AutoCheckFileModify.FileChanged -= this.OnHotReloadFileModified;
            base.OnDestroy();
        }

        public void OnHotReloadFileModified()
        {
            hotReloadModifyNumber = AutoCheckFileModify.s_CacheFilePath.Count;
            fullReloadModifyNumber = AutoCheckFileModify.s_AllCacheFilePath.Count;
        }
        
        [VerticalGroup("hotreload")]
        [PropertySpace(spaceBefore:20)]
        [HorizontalGroup("hotreload/display")]
        [LabelText("热重载后修改的文件数")]
        [ReadOnly]
        public int hotReloadModifyNumber;
        
        [VerticalGroup("hotreload")]
        [PropertySpace(spaceBefore:20)]
        [HorizontalGroup("hotreload/display", width:150)]
        [Button("查看文件列表")]
        public void OpenHotReloadModifyFile()
        {
            ModifyFileListWindow.Open(true);
        }

        [VerticalGroup("hotreload")]
        [PropertySpace(spaceBefore:10)]
        [Button(ButtonHeight = 30, Name = "进行热重载（Shift+R)")]
        [GUIColor("GetHotReloadColor")]
        public void HotReload()
        {
            if (Application.isPlaying == false)
            {
                EditorUtility.DisplayDialog("提示", "运行时才能用", "确认");
                return;
            }
            AutoCheckFileModify.AutoPatch();
        }
        
        [VerticalGroup("fullreload")]
        [PropertySpace(spaceBefore:20)]
        [HorizontalGroup("fullreload/display")]
        [LabelText("全量编译后后修改的文件数")]
        [ReadOnly]
        public int fullReloadModifyNumber;
        
        [VerticalGroup("fullreload")]
        [PropertySpace(spaceBefore:20)]
        [HorizontalGroup("fullreload/display", width:150)]
        [Button("查看文件列表")]
        public void OpenReloadModifyFile()
        {
            ModifyFileListWindow.Open(false);
        }
        
        [VerticalGroup("fullreload")]
        [Button(ButtonHeight = 30, Name = "进行全量编译（Ctrl+R)")]
        [PropertySpace(spaceBefore:10)]
        [GUIColor("GetFullReloadColor")]
        public void FullReload()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static Color GetHotReloadColor()
        {
            if (!Application.isPlaying)
            {
                return Color.white;
            }
            return AutoCheckFileModify.s_CacheFilePath.Count == 0 ? Color.white : Color.cyan;
        }

        private static Color GetFullReloadColor()
        {
            if (!Application.isPlaying)
            {
                return Color.white;
            }
            return AutoCheckFileModify.s_AllCacheFilePath.Count == 0 ? Color.white : Color.yellow;
        }
        
        [PropertySpace(spaceBefore:20)]
        [Button(ButtonHeight = 20, Name = "关闭热重载")]
        [GUIColor(1f, 0, 0)]
        public void CloseHotReload()
        {
            if (EditorUtility.DisplayDialog("提示", "是否关闭热重载功能, 关闭后会触发全量编译", "确认", "取消"))
            {
                var customMacro = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(',', ';').ToList();
                if (customMacro.Remove(HOT_RELOAD_MACRO))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", customMacro.ToArray()));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);    
                }
            }
        }
#endif
    }
}