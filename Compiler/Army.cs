//MCCScript 1.0
MCC.LoadBot(new realmsmc());
//MCCScript Extensions
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;

// Класс для хранения данных из файла
public class MixedRecord
{
    public string Text { get; set; }
    public string StringValue { get; set; }
    public sbyte ByteValue { get; set; }
}

class realmsmc : ChatBot
{
    static readonly string PathIni = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.Length - 8) + @"Client\config.ini"; // D:\C# project\BotMinecraft\Compiler
    private bool _waitingForMenu = false;
    private int  _menuId         = -1;
    private bool _wasAtSpawn     = false;
    private bool  _autoAttack    = false;
    private bool _isFalling      = false;

    private static readonly HashSet<string> MOB_WHITELIST = new HashSet<string>
    {
        "zombie", "skeleton", "creeper", "spider", "witch",
        "pillager", "vindicator", "ravager", "phantom",
        "drowned", "husk", "stray", "enderman", "blaze",
        "zombie_villager", "cave_spider", "silverfish"
    };

    private void CheckPositionAndExecute()
    {
        Location pos = GetCurrentLocation();
        if (pos == null) return;

        bool isAtTarget = Math.Abs(pos.X - 0.5) < 0.1 &&
                          Math.Abs(pos.Y - 65.0) < 0.1 &&
                          Math.Abs(pos.Z - 0.5) < 0.1;

        if (isAtTarget && !_wasAtSpawn)
        {
            LogToConsole("[Spawn] Достиг 0.5 65.0 0.5! Открываю меню...");
            OpenCompassMenu();
            _wasAtSpawn = true;
        }
        else if (!isAtTarget)
        {
            _wasAtSpawn = false;
        }
    }

    public override void Update()
    {
        CheckPositionAndExecute();
    }

    public override void AfterGameJoined()
    {
        LogToConsole("[Bot] Бот запущен.");
        
        // Пример использования ReadMixedData (опционально)
        // string path = @"D:\C# project\BotMinecraft\Client\config.ini";
        // var data = ReadMixedData(path);
        // if (data != null) LogToConsole($"Загружено записей: {data.Count}");
    }

    public override void GetText(string text, string nick)
    {
        string message  = "";
        string verbatim = GetVerbatim(text);

        if (verbatim.IndexOf("/reg", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/reg 130331 130331");
        else if (verbatim.IndexOf("/register", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/register 130331 130331");
        else if (verbatim.IndexOf("/login", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Thread.Sleep(5000);
            var records = ReadMixedData(PathIni);
            var grief = GetGrief(GetUsername(), records);
                SendText("/login " + Convert.ToSByte(grief.Value));
        }
        else if (verbatim.IndexOf("просит телепортироваться к Вам", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/tpaccept");
        else if (verbatim.IndexOf("tpa228", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/tpa " + nick);
        else if (verbatim.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0)
{
    
    _autoAttack = !_autoAttack;
    LogToConsole(_autoAttack
        ? "[Attack] ✓ Авто-атака включена"
        : "[Attack] ✗ Авто-атака выключена");
}
        // На сервер заходит большой поток игроков.
        else if (verbatim.IndexOf("На сервер заходит большой поток игроков.", StringComparison.OrdinalIgnoreCase) >= 0){
        var records = ReadMixedData(PathIni);
var grief = GetGrief(GetUsername(), records);
if (grief.HasValue)
    ClickSlot(Convert.ToSByte(grief.Value));
        }
    }
private void TryAutoAttack()
{
    if (!_autoAttack) return;

    var entities = GetEntities();
    if (entities == null || entities.Count == 0) return;

    Location myPos = GetCurrentLocation();
    if (myPos == null) return;

    foreach (var entity in entities.Values)
    {
        
        if (entity.Type == EntityType.Player) continue;

        
        string entityName = entity.Type.ToString().ToLower();
        if (!MOB_WHITELIST.Contains(entityName)) continue;

        
        double dist = Math.Sqrt(
            Math.Pow(entity.Location.X - myPos.X, 2) +
            Math.Pow(entity.Location.Y - myPos.Y, 2) +
            Math.Pow(entity.Location.Z - myPos.Z, 2)
        );

      
        if (dist <= 4.0)
        {
            LogToConsole($"[Attack] Атакую {entityName} (дист: {dist:F1})");
            InteractEntity(entity.ID, InteractType.Attack);
            break;
        }
    }
}
    private void OpenCompassMenu()
    {
        LogToConsole("[Compass] Использую предмет в руке...");
        UseItemInHand();
        _waitingForMenu = true;
        LogToConsole("OpenCompassMenu() SUCCESFULL");
    }

    public override void OnInventoryOpen(int inventoryId)
    {
        if (!_waitingForMenu) return;
        _menuId = inventoryId;
        LogToConsole($"[Menu] Меню открылось (ID: {inventoryId})");

        if (inventoryId == 1)
            ClickSlot(21);
        else if (inventoryId == 2)
        {
            var records = ReadMixedData(PathIni);
            var grief = GetGrief(GetUsername(), records);
            if (grief.HasValue)
                SwitchClickSlots(Convert.ToSByte(grief.Value));
        }
    }

    private void ClickSlot(int slot)
    {
        if (_menuId == -1)
        {
            LogToConsole("[Menu] Ошибка: меню не открыто.");
            _waitingForMenu = false;
            return;
        }
        LogToConsole($"[Menu] Кликаю ЛКМ по слоту {slot}...");
        bool ok = WindowAction(_menuId, slot, WindowActionType.LeftClick);
        LogToConsole(ok ? "[Menu] ✓ Клик выполнен." : "[Menu] ✗ Ошибка клика.");
    }
    public static sbyte? GetGrief(string nick, List<MixedRecord> records)
{
    foreach (var data in records)
        if (data.Text == nick)
            return data.ByteValue;
    return null;
}

public static string GetPassword(string nick, List<MixedRecord> records)
{
    foreach (var data in records)
        if (data.Text == nick)
            return data.StringValue;
    return null;
}
    public override void OnInventoryClose(int inventoryId)
    {
        if (inventoryId == _menuId)
        {
            _menuId = -1;
            _waitingForMenu = false;
        }
    }

    // ─── Чтение данных из файла (нестатический метод) ─────────────────────
    private List<MixedRecord> ReadMixedData(string filePath)
    {
        if (!File.Exists(filePath))
        {
            LogToConsole($"Ошибка: файл не найден: {filePath}");
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
                LogToConsole($"Ошибка: строка должна содержать 3 элемента. Найдено {parts.Length}. Строка: {line}");
                return null;
            }

            string text = parts[0];
            string stringValue = parts[1];
            if (!sbyte.TryParse(parts[2], out sbyte byteValue))
            {
                LogToConsole($"Ошибка: не удалось преобразовать третий элемент в sbyte. Строка: {line}");
                return null;
            }

            records.Add(new MixedRecord { Text = text, StringValue = stringValue, ByteValue = byteValue });
        }

        if (records.Count == 0)
        {
            LogToConsole("Ошибка: файл не содержит данных.");
            return null;
        }

        return records;
    }

    private void SwitchClickSlots(sbyte n)
{
    if (n < 1 || n > 32)
    {
        LogToConsole($"[Switch] Ошибка: неверный номер грифа {n}");
        return;
    }

    int slot;
    if (n <= 4)
        slot = n - 1;           // 1-4 → 0-3
    else if (n <= 12)
        slot = n;               // 5-12 → 5-12 (пропуск слота 4)
    else if (n <= 20)
        slot = n + 1;           // 13-20 → 14-21 (пропуск слота 13)
    else if (n <= 28)
        slot = n + 2;           // 21-28 → 23-30 (пропуск слота 22)
    else
        slot = n + 3;           // 29-32 → 32-35 (пропуск слота 31)

    ClickSlot(slot);
}
}