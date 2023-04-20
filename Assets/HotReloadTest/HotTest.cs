using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;


namespace HOT_RELOAD_TEST
{
    public class HotTest
    {
        private static int _ii = 1;
        
        [MenuItem("haitong/test")]
        public static void Test()
        {
            Debug.Log($"Here122 {_ii}");
        }

        public static void TestAsm()
        {
             var asm = AppDomain.CurrentDomain.GetAssemblies();
            
            var metadataReferences = new List<MetadataReference>();
            foreach (var a in asm)
            {
                if (string.IsNullOrEmpty(a.Location))
                {
                    continue;
                }
                metadataReferences.Add(MetadataReference.CreateFromFile(a.Location));
            }
            
            var customMacro = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(',', ';');;

            var builtinMacro = new string[]
            {
                "CSHARP_7_3_OR_NEWER",
                "CSHARP_7_OR_LATER",
                "DEBUG",
                "ENABLE_AR",
                "ENABLE_AUDIO",
                "ENABLE_BURST_AOT",
                "ENABLE_CACHING",
                "ENABLE_CLOTH",
                "ENABLE_CLOUD_LICENSE",
                "ENABLE_CLOUD_SERVICES",
                "ENABLE_CLOUD_SERVICES_ADS",
                "ENABLE_CLOUD_SERVICES_ANALYTICS",
                "ENABLE_CLOUD_SERVICES_BUILD",
                "ENABLE_CLOUD_SERVICES_COLLAB",
                "ENABLE_CLOUD_SERVICES_COLLAB_SOFTLOCKS",
                "ENABLE_CLOUD_SERVICES_CRASH_REPORTING",
                "ENABLE_CLOUD_SERVICES_PURCHASING",
                "ENABLE_CLOUD_SERVICES_UNET",
                "ENABLE_CLOUD_SERVICES_USE_WEBREQUEST",
                "ENABLE_CLUSTERINPUT",
                "ENABLE_CLUSTER_SYNC",
                "ENABLE_CRUNCH_TEXTURE_COMPRESSION",
                "ENABLE_CUSTOM_RENDER_TEXTURE",
                "ENABLE_DIRECTOR",
                "ENABLE_DIRECTOR_AUDIO",
                "ENABLE_DIRECTOR_TEXTURE",
                "ENABLE_EDITOR_HUB_LICENSE",
                "ENABLE_EVENT_QUEUE",
                "ENABLE_LEGACY_INPUT_MANAGER",
                "ENABLE_LOCALIZATION",
                "ENABLE_LZMA",
                "ENABLE_MANAGED_ANIMATION_JOBS",
                "ENABLE_MANAGED_AUDIO_JOBS",
                "ENABLE_MANAGED_JOBS",
                "ENABLE_MANAGED_TRANSFORM_JOBS",
                "ENABLE_MANAGED_UNITYTLS",
                "ENABLE_MICROPHONE",
                "ENABLE_MONO",
                "ENABLE_MONO_BDWGC",
                "ENABLE_MOVIES",
                "ENABLE_MULTIPLE_DISPLAYS",
                "ENABLE_NETWORK",
                "ENABLE_JN_HOT_RELOAD",
                "ENABLE_OUT_OF_PROCESS_CRASH_HANDLER",
                "ENABLE_PHYSICS",
                "ENABLE_PROFILER",
                "ENABLE_RUNTIME_GI",
                "ENABLE_SCRIPTING_GC_WBARRIERS",
                "ENABLE_SPRITES",
                "ENABLE_TERRAIN",
                "ENABLE_TEXTURE_STREAMING",
                "ENABLE_TILEMAP",
                "ENABLE_TIMELINE",
                "ENABLE_UNET",
                "ENABLE_UNITYEVENTS",
                "ENABLE_UNITYWEBREQUEST",
                "ENABLE_UNITY_COLLECTIONS_CHECKS",
                "ENABLE_VIDEO",
                "ENABLE_VIRTUALTEXTURING",
                "ENABLE_VR",
                "ENABLE_WEBCAM",
                "ENABLE_WEBSOCKET_CLIENT",
                "ENABLE_WEBSOCKET_HOST",
                "ENABLE_WWW",
                "GFXDEVICE_WAITFOREVENT_MESSAGEPUMP",
                "INCLUDE_DYNAMIC_GI",
                "NET_4_6",
                "PLATFORM_ARCH_64",
                "PLATFORM_STANDALONE",
                "PLATFORM_STANDALONE_WIN",
                "PLATFORM_SUPPORTS_MONO",
                "PLATFORM_UPDATES_TIME_OUTSIDE_OF_PLAYER_LOOP",
                "RENDER_SOFTWARE_CURSOR",
                "SCENE_TEMPLATE_MODULE",
                "TRACE",
                "UNITY_2017_1_OR_NEWER",
                "UNITY_2017_2_OR_NEWER",
                "UNITY_2017_3_OR_NEWER",
                "UNITY_2017_4_OR_NEWER",
                "UNITY_2018_1_OR_NEWER",
                "UNITY_2018_2_OR_NEWER",
                "UNITY_2018_3_OR_NEWER",
                "UNITY_2018_4_OR_NEWER",
                "UNITY_2019_1_OR_NEWER",
                "UNITY_2019_2_OR_NEWER",
                "UNITY_2019_3_OR_NEWER",
                "UNITY_2019_4_OR_NEWER",
                "UNITY_2020",
                "UNITY_2020_1_OR_NEWER",
                "UNITY_2020_2_OR_NEWER",
                "UNITY_2020_3",
                "UNITY_2020_3_40",
                "UNITY_2020_3_OR_NEWER",
                "UNITY_5_3_OR_NEWER",
                "UNITY_5_4_OR_NEWER",
                "UNITY_5_5_OR_NEWER",
                "UNITY_5_6_OR_NEWER",
                "UNITY_64",
                "UNITY_ASSERTIONS",
                "UNITY_EDITOR",
                "UNITY_EDITOR_64",
                "UNITY_EDITOR_WIN",
                "UNITY_INCLUDE_TESTS",
                "UNITY_STANDALONE",
                "UNITY_STANDALONE_WIN",
                "UNITY_TEAM_LICENSE",
                "USE_SEARCH_ENGINE_API"
            };


            var symbols = new List<string>(builtinMacro);
            symbols.AddRange(customMacro);
            
            
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe:true).WithMetadataImportOptions(MetadataImportOptions.All);
            
            var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            topLevelBinderFlagsProperty.SetValue(compilationOptions, (uint)1 << 22);
            
            var code = File.ReadAllText(@"Assets\HotReloadTest\HotTest.cs");
            var code2 = File.ReadAllText(@"Assets\HotReloadTest\HotTest1.cs");

            var csharpParseOptions = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: symbols);

            var treeCode = CSharpSyntaxTree.ParseText(code, csharpParseOptions, path: Path.GetFullPath(@"Assets\HotReloadTest\HotTest.cs"));
            
            var compilation = CSharpCompilation.Create("HotReloadTest", new[] {
                CSharpSyntaxTree.ParseText(code, csharpParseOptions, Path.GetFullPath(@"Assets\HotReloadTest\HotTest.cs"), Encoding.UTF8),
                CSharpSyntaxTree.ParseText(code2, csharpParseOptions, Path.GetFullPath(@"Assets\HotReloadTest\HotTest1.cs"), Encoding.UTF8)
            }, metadataReferences, compilationOptions);
            
            var emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.PortablePdb,
                pdbFilePath: "HotReloadTest.pdb");

            var dllPath = Path.Combine( "../HotReloadTest.dll");
            var pdbPath = Path.Combine( "../HotReloadTest.pdb");
            using (var dll = new FileStream(dllPath, FileMode.Create))
            using(var pdb = new FileStream(pdbPath, FileMode.Create))
            {
                var result = compilation.Emit(dll, pdb, options:emitOptions);
            }
        }
    }
}


