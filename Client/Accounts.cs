using System;
using System.Collections.Generic;
using System.IO;

// Класс для хранения данных одного аккаунта
public class AccountInfo
{
    public string Nickname { get; set; }
    public string Password { get; set; }
    public int ServerNumber { get; set; }
}

// Статический менеджер для загрузки и доступа к аккаунтам
public static class AccountManager
{
    private static List<AccountInfo> _accounts;

    // Загрузить аккаунты из файла (вызовите один раз в начале)
    public static void LoadFromFile(string filePath)
    {
        _accounts = new List<AccountInfo>();
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Файл {filePath} не найден.");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                Console.WriteLine($"Пропущена строка (ожидалось 3 элемента): {line}");
                continue;
            }

            string nick = parts[0];
            string pass = parts[1];
            if (int.TryParse(parts[2], out int serverNum))
            {
                _accounts.Add(new AccountInfo 
                { 
                    Nickname = nick, 
                    Password = pass, 
                    ServerNumber = serverNum 
                });
            }
            else
            {
                Console.WriteLine($"Ошибка номера сервера в строке: {line}");
            }
        }
        Console.WriteLine($"Загружено аккаунтов: {_accounts.Count}");
    }

    // Получить аккаунт по его порядковому номеру (нумерация с 1)
    public static AccountInfo GetAccountInfo(int number)
    {
        if (_accounts == null || number < 1 || number > _accounts.Count)
        {
            Console.WriteLine($"Аккаунт #{number} не найден.");
            return null;
        }
        return _accounts[number - 1];
    }

    // Общее количество аккаунтов
    public static int Count => _accounts?.Count ?? 0;
}