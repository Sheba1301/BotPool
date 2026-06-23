using System.Reflection.Metadata.Ecma335;

namespace BotMinecraft;
using System.IO;
using System.Reflection;

class PathHelper
{
    public static string DirecDir()
    {
        string result = GetCompilerPath(0).Substring(0, GetCompilerPath(0).Length - 12);
        return result;
    }
    public static string GetCompilerPath(sbyte a)
    {
        // Папка, где находится текущий лаунчер (Compiler\)
        string launcherDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        string Direc = launcherDir.Substring(0, launcherDir.Length - 24);
        string Result = Direc + @"Compiler\BotPool.exe";
        string exeDir = AppContext.BaseDirectory;
        string path = exeDir.Substring(0,exeDir.Length - 7);
        path = path + @"Compiler\BotPool.exe";


        if (a == 0)
        {
            return path;
        }
        else if (a == 1)
        {
            return exeDir;
        }
        return Result;
    }

}