using System;

namespace BotMinecraft
{
    class PathHelper
    {
        public static string GetCompilerPath(sbyte a)
        {
            // AppContext.BaseDirectory всегда указывает на папку, где находится .exe
            string baseDir = AppContext.BaseDirectory;

            if (a == 0)
                return baseDir.Substring(0, baseDir.Length - 7) + @"Compiler\BotPool.exe";
            else if (a == 1)
                return baseDir; // корневая папка (где лежат папки Compiler и Client)
            else if (a == 2)
                return baseDir.Substring(0, baseDir.Length - 7) + @"Compiler";
            else
                return string.Empty;
        }

        public static string DirecDir() => AppContext.BaseDirectory;
    }
}