﻿/*
 * Author: Misaka Mikoto
 * email: easy66@live.com
 * github: https://github.com/Misaka-Mikoto-Tech/UnityScriptHotReload
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;

namespace ScriptHotReload
{
    public static class HotReloadUtils
    {
        public static bool IsFilesEqual(string lpath, string rpath)
        {
            if (!File.Exists(lpath) || !File.Exists(rpath))
                return false;

            var lLen = new FileInfo(lpath).Length;
            var rLen = new FileInfo(rpath).Length;
            if (lLen != rLen)
                return false;

            return GetMd5OfFile(lpath) == GetMd5OfFile(rpath);
        }

        public static string GetMd5OfFile(string filePath)
        {
            string fileMd5 = null;
            try
            {
                using (var fs = File.OpenRead(filePath))
                {
                    var md5 = MD5.Create();
                    var md5Bytes = md5.ComputeHash(fs);
                    fileMd5 = BitConverter.ToString(md5Bytes).Replace("-", "").ToLower();
                }
            }
            catch
            {
            }

            return fileMd5;
        }

        public static void RemoveAllFiles(string dir)
        {
            if (!Directory.Exists(dir))
                return;

            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
                File.Delete(file);
        }

        public static BindingFlags BuildBindingFlags(MethodBase methodInfo)
        {
            var flags = BindingFlags.Default;
            if (methodInfo.IsPublic)
                flags |= BindingFlags.Public;
            else
                flags |= BindingFlags.NonPublic;

            if (methodInfo.IsStatic)
                flags |= BindingFlags.Static;
            else
                flags |= BindingFlags.Instance;

            return flags;
        }

        /// <summary>
        ///     从指定Assembly中查找特定签名的方法
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static MethodBase GetMethodFromAssembly(MethodBase methodBase, Assembly assembly)
        {
            var typeName = methodBase.DeclaringType.FullName;
            var t = assembly.GetType(typeName);
            if (t == null)
                return null;

            var flags = BuildBindingFlags(methodBase);
            var sig = methodBase.ToString();
            var mis = methodBase is ConstructorInfo ? (MethodBase[]) t.GetConstructors(flags) : t.GetMethods(flags);

            foreach (var mi in mis)
                if (mi.ToString() == sig)
                    return mi;
            return null;
        }

        private static string GetTypeName(Type t)
        {
            if (t.ContainsGenericParameters)
                return t.Name;
            return t.ToString().Replace('+', '/').Replace('[', '<').Replace(']', '>').Replace("<>", "[]"); // 最后一步是还原数组的[]
        }

        public static Dictionary<string, string> GetFallbackAssemblyPaths()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var ret = new Dictionary<string, string>();
            foreach (var ass in assemblies)
            {
                if (ass.IsDynamic)
                    continue;

                if (!string.IsNullOrEmpty(ass.Location))
                {
                    var name = Path.GetFileNameWithoutExtension(ass.Location);
                    if (name.Contains("_patch_"))
                    {
                        continue;
                    }
                    
                    var prefix = Path.Combine(Application.dataPath, "../", HotReloadConfig.kTempCompileToDir);
                    if (HotReloadConfig.hotReloadAssemblies.Contains(name + ".dll"))
                    {
                        ret.TryAdd(name, prefix + "/" + name);
                    }
                    else
                    {
                        ret.TryAdd(name, ass.Location);
                    }
                }
            }

            return ret;
        }

        public static string GetThisFilePath([CallerFilePath] string path = null)
        {
            return path;
        }
    }
}