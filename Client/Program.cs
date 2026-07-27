namespace BotMinecraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Client
{
    
    public static List<string> actived = new List<string>();

    public static sbyte grief = 0;
    public static string password     = "-";
    public static string ServerIP     = "50.114.4.207";   // IP сервера
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
       // D:\C# Project\BotMinecraft\Client\bin\Debug\net10.0\sdaf.cs
       // D:\C# Project\BotMinecraft\Compiler\BotPool.exe
        
AccountManager.LoadFromFile(PathHelper.GetCompilerPath(1) + @"config.ini");

       while (true){
        
        if (menustage == 0)
            {
                MainMenu(); 
            }
            else if (menustage == 1)
            {
               PrintRecords(ReadMixedData(PathHelper.GetCompilerPath(1) + @"config.ini")); 
               Console.WriteLine("------------------------------------------------------------");
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
                Console.WriteLine($"Гриф: {acc.ServerNumber}");
                Console.WriteLine("1) Запустить бота");
                Console.WriteLine("Введите нужный пункт меню: ");
                accountnumber = Convert.ToSByte(Console.ReadLine());
                actived.Add(acc.Nickname);
                grief = Convert.ToSByte(acc.ServerNumber);
                password = acc.Password;
                Process mcc = LaunchMCC(acc.Nickname,PathHelper.GetCompilerPath(0));
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
                return "Telegram Channel: t.me/BotPoolApp";
                
            }
        }

    return "";
}

public static void MainMenu()
    {
        Console.WriteLine(PathHelper.DirecDir());
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










   static Process LaunchMCC(string username, string exePath)
{
    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Никнейм не может быть пустым.", nameof(username));

    if (!File.Exists(exePath))
        throw new FileNotFoundException("MCC.exe не найден по указанному пути.", exePath);

    var startInfo = new ProcessStartInfo
    {
        FileName         = exePath,
        Arguments        = $"\"{username}\" - -",
        WorkingDirectory = Path.GetDirectoryName(exePath),
        UseShellExecute  = true,  // <- запуск через оболочку ОС
        CreateNoWindow   = false, // <- MCC открывает своё окно
    };

    return Process.Start(startInfo)
        ?? throw new InvalidOperationException("Не удалось запустить процесс MCC.");
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
    Console.WriteLine("| № |     Никнейм        |     Пароль    |  Номер грифа  |");
    Console.WriteLine("|---|--------------------|---------------|---------------|");
    int i = 1;
    
    foreach (var rec in records)
    {
        
        Console.WriteLine($"| {i,2} | {rec.Text,-16} | {rec.StringValue,-15} | {rec.ByteValue,13} |");
        i++;
    }
}
    
}

