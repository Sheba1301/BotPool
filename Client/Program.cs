namespace BotMinecraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Linq;
class Client
{
    
    public static List<string> actived = new List<string>();

    public static sbyte grief = 0;
    public static string password;
    public static string ServerIP     = "mc.reallyworld.ru";   // IP сервера
    public static string ScriptName   = "Army.cs";          // Имя C# скрипта

    
    private const string MCC_DIR        = @"D:\C# project\BotMinecraft";
    private const string MCC_EXE        = MCC_DIR + @"\BotPool.exe";
    public static sbyte menustage = 0;
public class MixedRecord
{
    public string Text { get; set; }      // первый столбец: строка
    public string StringValue { get; set; } // второй столбец: строка
    public sbyte ByteValue { get; set; }    // третий столбец: sbyte
}
    static void Main()
    {
        Console.WriteLine(PathHelper.GetCompilerPath(0));
        Console.WriteLine(PathHelper.GetCompilerPath(1));
AccountManager.LoadFromFile(PathHelper.GetCompilerPath(1) + @"config.ini");

       while (true){
        
        if (menustage == 0)
            {
                MainMenu(); 
            }
            else if (menustage == 1)
            {
               PrintRecords(ReadMixedData(PathHelper.GetCompilerPath(1) + @"config.ini")); 
                Console.WriteLine("Введите номер аккаунта с которым хотите произвести действия: ");
                Console.WriteLine("Или введите 0, что бы вернутся в главное меню");
                
                
                try{
                    
                sbyte accountnumber = Convert.ToSByte(Console.ReadLine());
                if (accountnumber == 0)
                    {
                        Console.Clear();
                        menustage = 0;
                    }
                    else {
                AccountInfo acc = AccountManager.GetAccountInfo(accountnumber);
                Console.Clear();
                Console.WriteLine("Примечание: Удалять бота через конфиг");
                Console.WriteLine($"Ник: {acc.Nickname}");
                Console.WriteLine($"Пароль: {acc.Password}");
                Console.WriteLine($"Сервер: {acc.ServerNumber}");
                Console.WriteLine("1) Запустить бота");
                Console.WriteLine("Введите нужный пункт меню: ");
                accountnumber = Convert.ToSByte(Console.ReadLine());
                actived.Add(acc.Nickname);
                grief = Convert.ToSByte(acc.ServerNumber);
                password = acc.Password;
                StartMCC(acc.Nickname);
                Console.Clear();
                menustage = 1;
                    }
                }
                catch
                {
                    Console.Clear();
                    Console.WriteLine("Вы ввели неверный номер/тип данных!!");
                    Console.WriteLine("Нажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                    menustage = 1;
                }

                
            }
        
        }
        
    }





public static string MenuGuide(sbyte stage, string Item) {
     
     if (stage == 0)
        {
            switch (Item)
            {
                case "1":
                
                menustage = 1;
                break;

                case "2":
                menustage = 0;
                return "Telegram: @Feykomet12";
                
               

                case "3":
                menustage = 0;
                return "Telegram Channel: BotPoolMine";
                
            }
        }

    return "";
}

public static void MainMenu()
    {
        
        Console.WriteLine("Вас приветствует Менеджер Майнкрафт ботов!");
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("1) Cписок аккаунтов");
        Console.WriteLine("2) Сообщить об ошибке/Предложить идею");
        Console.WriteLine("3) Наши медиа");
        Console.WriteLine("------------------------------------------");
        Console.Write("Введите нужный пункт меню: ");
        string menuItem1 = Console.ReadLine();
        Console.Clear();
        Console.WriteLine(MenuGuide(menustage, menuItem1));
        if (menustage == 0){
        Console.WriteLine("Нажмите любую клавишу для продолжения...");
        Console.ReadKey();
        }
        
        Console.Clear();
        
    }










    private static Process StartMCC(string nick)
    {
        if (!File.Exists(PathHelper.GetCompilerPath(0)))
        {
            Console.WriteLine($"[Launcher] {MCC_EXE} не найден.");
            return null;
        }
        try
        {
            // ✅ Аргументы собраны строго по документации: Логин Сервер /script ИмяСкрипта
            string arguments = $"{nick} {ServerIP} /script {ScriptName}";
            Console.WriteLine($"[Launcher] Аргументы: {arguments}");
            var psi = new ProcessStartInfo
            {
                FileName = PathHelper.GetCompilerPath(0),
                Arguments = arguments,
                UseShellExecute = true,
                WorkingDirectory = PathHelper.DirecDir()
            };
            Process proc = Process.Start(psi);
            if (proc != null)
            {
                Console.WriteLine($"[Launcher] ✓ Запущен PID={proc.Id}, ник={nick}");
                return proc;
            }
            else
            {
                Console.WriteLine($"[Launcher] Ошибка: Process.Start вернул null для {nick}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Launcher] Ошибка запуска: {ex.Message}");
            return null;
        }
    }
    

    
                    static List<MixedRecord> ReadMixedData(string filePath)
{
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"Ошибка: файл не найден: {filePath}");
        return null;
    }

    var records = new List<MixedRecord>();
    string[] lines = File.ReadAllLines(filePath);

    foreach (string line in lines)
    {
        if (string.IsNullOrWhiteSpace(line))
            continue;

        string[] parts = line.Split(new char[] { ' ', '\t', ',' }, 
                                    StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3)
        {
            Console.WriteLine($"Ошибка: строка должна содержать 3 элемента. Найдено {parts.Length}. Строка: {line}");
            return null;
        }

        string text = parts[0];
        string stringValue = parts[1];
        if (!sbyte.TryParse(parts[2], out sbyte byteValue))
        {
            Console.WriteLine($"Ошибка: не удалось преобразовать третий элемент в sbyte. Строка: {line}");
            return null;
        }

        records.Add(new MixedRecord { Text = text, StringValue = stringValue, ByteValue = byteValue });
    }

    if (records.Count == 0)
    {
        Console.WriteLine("Ошибка: файл не содержит данных.");
        return null;
    }

    return records;
}
        /// <summary>
        /// Выводит записи в консоль.
        /// </summary>
        static void PrintRecords(List<MixedRecord> records)
{
    Console.WriteLine($"\nАккаунтов: {records.Count}\n");
    Console.WriteLine("| № |     Никнейм        |     Пароль    |  Номер грифа  | Активность |");
    Console.WriteLine("|---|--------------------|---------------|---------------|------------|");
    int i = 1;
    string state;
    foreach (var rec in records)
    {
        if (actived.Contains(rec.Text))
            {
                state = "Активен";
            }
            else
            {
                state = "Неактивен";
            }
        Console.WriteLine($"| {i,2} | {rec.Text,-16} | {rec.StringValue,-15} | {rec.ByteValue,13} | {state,5} |");
        i++;
    }
}
    
}

