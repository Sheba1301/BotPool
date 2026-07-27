using System;
using System.IO;

namespace BotMinecraft
{
    class PathHelper
    {
        /// <summary>
        /// Возвращает путь к компилятору (BotPool.exe) или корневую папку.
        /// </summary>
        /// <param name="a">0 — путь к BotPool.exe, 1 — корневая папка</param>
        public static string GetCompilerPath(sbyte a)
        {
            // AppContext.BaseDirectory всегда указывает на папку, где находится .exe
            string baseDir = AppContext.BaseDirectory;

            if (a == 0)
                return baseDir.Substring(0,baseDir.Length - 7) + @"Compiler\BotPool.exe";
            else if (a == 1)
                return baseDir; // корневая папка (где лежат папки Compiler и Client)
            else if (a == 2)
                return baseDir.Substring(0,baseDir.Length - 7) + @"Compiler";
                else
                return null;
        }

        /// <summary>
        /// Возвращает рабочую директорию для запуска MCC.
        /// </summary>
        public static string DirecDir()
        {
            return AppContext.BaseDirectory;
        }
    }
}