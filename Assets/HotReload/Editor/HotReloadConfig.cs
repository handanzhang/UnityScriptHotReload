﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptHotReload
{
    public class HotReloadConfig
    {
        /// <summary>
        /// 需要执行 HotReload 的程序集名称
        /// </summary>
        public static List<string> hotReloadAssemblies = new List<string>()
        {
            "Assembly-CSharp.dll"
        };

        public const string kTempScriptDir = "Temp/ScriptHotReload";
        public const string kTempCompileToDir = "Temp/ScriptHotReload/tmp";
        public const string kBuiltinAssembliesDir = "Library/ScriptAssemblies";
        public const string kLastDllPathFormat = kTempScriptDir + "/{0}__last.dll"; // {0}:assNameNoExt
        public const string kPatchDllPathFormat = kTempScriptDir + "/{0}_{1}.dll"; // {0}:assNameNoExt, {1}:PatchNo

        public const string kEditorScriptBuildParamsKey = "kEditorScriptBuildParamsKey";
        /// <summary>
        /// 类型内 lambda 表达式的封装类名称，不同版本Unity编译器生成的结果可能不同，待确认
        /// </summary>
        /// <remarks>注意：只修改lambda表达式，但不修改调用处代码，将不会被检测到改变</remarks>
        public const string kLambdaWrapperBackend = "/<>c";

    }

}
