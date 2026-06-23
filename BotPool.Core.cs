using System;
using System.IO;

namespace BotPool.Core
{
    public static class Something
    {
        public  static List<MixedRecord> ReadMixedData(string filePath)
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
    string state;
    foreach (var rec in records)
    {
        
        Console.WriteLine($"| {i,2} | {rec.Text,-16} | {rec.StringValue,-15} | {rec.ByteValue,13} |");
        i++;
    }
}
    }
}