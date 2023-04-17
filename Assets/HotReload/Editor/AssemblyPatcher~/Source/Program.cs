using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AssemblyPatcher
{

internal class Program
{
    static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        if(args.Length < 2)
        {
            Debug.Log("usage: AssemblyPatcher <InputFile.json> <OutputFile.json> [debug]");
            return -1;
        }

        if (!File.Exists(args[0]))
        {
            Debug.Log($"file `{args[0]}` does not exists");
            return -1;
        }

        if (args.Length > 2 && args[2] == "debug")
        {
            Debug.Log("waitting for debugger attach");
            System.Diagnostics.Debugger.Launch();
        }

        Stopwatch sw = new Stopwatch();
        sw.Start();
        bool success = false;
        try
        {
            var patcher = new Patcher(args[0], args[1]);
            success = patcher.DoPatch();
        }
        catch(Exception ex)
        {
            Debug.LogError($"Patch Fail:{ex.Message}, stack:{ex.StackTrace}");
        }
        sw.Stop();
        Debug.Log($"执行Patch耗时 {sw.ElapsedMilliseconds} ms");
        if (!success)
            return -1;

        Debug.Log("Patch成功");
        return 0;
    }
}
}