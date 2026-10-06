using System;
using System.Collections.Generic;
using System.IO;

// ===================== ДАННЫЕ АККАУНТА =====================
public class AccountInfo
{
    public string Nickname     { get; set; } = string.Empty;
    public string Password     { get; set; } = string.Empty;
    public int    ServerNumber { get; set; }
    public bool   Use          { get; set; } = true;
}

// ================== МЕНЕДЖЕР АККАУНТОВ ==================
public static class AccountManager
{
    private static List<AccountInfo> _accounts = new();

    public static int Count => _accounts.Count;

    // ---------- ЗАГРУЗКА ИЗ ФАЙЛА ----------
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

            string[] parts = line.Split(new char[] { ' ', '\t', ',' },
                                        StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
            {
                Console.WriteLine($"Пропущена строка (ожидалось 4 элемента): {line}");
                continue;
            }

            if (!int.TryParse(parts[2], out int serverNum))
            {
                Console.WriteLine($"Ошибка номера сервера в строке: {line}");
                continue;
            }

            if (!bool.TryParse(parts[3], out bool use))
            {
                Console.WriteLine($"Ошибка флага Use в строке: {line}");
                continue;
            }

            _accounts.Add(new AccountInfo
            {
                Nickname     = parts[0],
                Password     = parts[1],
                ServerNumber = serverNum,
                Use          = use
            });
        }

        Console.WriteLine($"Загружено аккаунтов: {_accounts.Count}");
    }

    // ---------- ПОЛУЧЕНИЕ АККАУНТА ПО НОМЕРУ (с 1) ----------
    public static AccountInfo? GetAccountInfo(int number)
    {
        if (number < 1 || number > _accounts.Count)
            return null;
        return _accounts[number - 1];
    }

    // ---------- ПОИСК ПО НИКУ (для проверки дубликатов) ----------
    public static bool ExistsByNickname(string nickname)
    {
        foreach (var acc in _accounts)
            if (acc.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // ---------- ДОБАВИТЬ ----------
    public static void Add(AccountInfo account) => _accounts.Add(account);

    // ---------- УДАЛИТЬ ПО НОМЕРУ ----------
    public static bool RemoveAt(int number)
    {
        if (number < 1 || number > _accounts.Count) return false;
        _accounts.RemoveAt(number - 1);
        return true;
    }

    // ---------- СОХРАНИТЬ В ФАЙЛ ----------
    public static bool SaveToFile(string filePath)
    {
        try
        {
            var lines = new List<string>(_accounts.Count);
            foreach (var acc in _accounts)
            {
                // формат: ник пароль гриф use
                lines.Add($"{acc.Nickname} {acc.Password} {acc.ServerNumber} {acc.Use.ToString().ToLower()}");
            }
            File.WriteAllLines(filePath, lines);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка записи: {ex.Message}");
            return false;
        }
    }
}