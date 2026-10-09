//MCCScript 1.0
MCC.LoadBot(new realmsmc());
//MCCScript Extensions
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Reflection;
//using System.Linq;

public class MixedRecord
{
    public string Text        { get; set; }
    public string StringValue { get; set; }
    public sbyte  ByteValue   { get; set; }
    public bool   BoolValue   { get; set; }   // НОВОЕ: 4-й столбец
}

class realmsmc : ChatBot
{
    static readonly string PathIni = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.Length - 8) + @"Client\config.ini";

    private bool _waitingForMenu = false;
    private int  _menuId         = -1;
    private bool _wasAtSpawn     = false;
    private bool _autoAttack     = true;
    private int  _menuStep       = 0;
    private int  _targetPage     = 0;
    private int  _targetSlot     = 0;
    private int  _currentPage    = 0;

    private const float RotationStep = 0.15f;

    

    private string _myNick      = "";
private bool   _isMain      = false;
private string _mainNick    = "";   // ник главного (для второстепенного)
private string _ownerNick   = "";   // кому в итоге уходят деньги (для главного)
private readonly List<string> _secondaries = new(); // список второстепенных (для главного)
// ==========================================================
private long _balance = 0;
private void ParseLaunchInfo()
{
    // ВАЖНО: Environment.GetCommandLineArgs() возвращает:
    // [0] = путь к BotPool.exe
    // [1] = ник
    // [2] = "-"
    // [3] = "-"
    // [4] = "true"/"false"
    // [5..] = хвост
    string[] args = Environment.GetCommandLineArgs();

    if (args.Length < 5)
    {
        // Запуск без аргументов — работаем как одиночка
        _myNick = GetUsername();
        LogToConsole("[Args] Аргументов нет — обычный запуск.");
        return;
    }

    _myNick = args[1].Trim('"');
    _isMain = bool.TryParse(args[4], out bool m) && m;

    if (_isMain)
    {
        // args[5] = ownerNick
        if (args.Length >= 6)
            _ownerNick = args[5].Trim('"');

        // args[6..] = ники второстепенных
        for (int i = 6; i < args.Length; i++)
        {
            string nick = args[i].Trim('"');
            if (!string.IsNullOrWhiteSpace(nick))
                _secondaries.Add(nick);
        }
    }
    else
    {
        // args[5] = mainNick
        if (args.Length >= 6)
            _mainNick = args[5].Trim('"');
    }

    LogToConsole($"[Args] Nick         = {_myNick}");
    LogToConsole($"[Args] IsMain       = {_isMain}");
    LogToConsole($"[Args] MainNick     = {_mainNick}");
    LogToConsole($"[Args] OwnerNick    = {_ownerNick}");
    LogToConsole($"[Args] Secondaries  = [{string.Join(", ", _secondaries)}]");
}
    

    private (int page, int slot) GetGriefPageAndSlot(int griefNumber)
    {
        int page;
        int localIndex;

        if (griefNumber <= 32) { page = 0; localIndex = griefNumber - 1; }
        else if (griefNumber <= 64) { page = 1; localIndex = griefNumber - 33; }
        else { page = 2; localIndex = griefNumber - 65; }

        int slot;
        if (localIndex < 4) slot = localIndex;
        else if (localIndex < 12) slot = localIndex + 1;
        else if (localIndex < 20) slot = localIndex + 2;
        else if (localIndex < 28) slot = localIndex + 3;
        else slot = localIndex + 4;

        return (page, slot);
    }

    public override void Update()
    {
        Location pos = GetCurrentLocation();
        if (_wasAtSpawn == false && pos.X == 0.5 && pos.Y == 65.0 && pos.Z == 0.5)
        {
            UseItemInHand();
            _wasAtSpawn = true;
        }

        
    }

    public void Paying() {
        foreach(string nick in _secondaries)
        {
            SendText("/msg" + nick + "paying");
        }

    }

    public override void AfterGameJoined()
    {
        ParseLaunchInfo();
        
        LogToConsole("[Bot] Бот запущен.");
        _wasAtSpawn = false;
        _menuId     = -1;
        _menuStep   = 0;
    }

    public override void OnRespawn()
    {
        
    }

   public override void GetText(string text)
{
    string verbatim = GetVerbatim(text);
    
    // ---------- ПАРСИНГ БАЛАНСА ----------
    var balMatch = Regex.Match(verbatim, @"(-?\d+)\s*₪");
    if (balMatch.Success && long.TryParse(balMatch.Groups[1].Value, out long parsedBalance))
    {
        _balance = parsedBalance;
        LogToConsole($"[Money] Баланс обновлён: {_balance}");
    }
    // ------------------------------------

    if (verbatim.IndexOf("/reg", StringComparison.OrdinalIgnoreCase) >= 0)
        SendText("/reg 130331 130331");
    else if (verbatim.IndexOf("/register", StringComparison.OrdinalIgnoreCase) >= 0)
        SendText("/register 130331 130331");
    else if (verbatim.IndexOf("/login", StringComparison.OrdinalIgnoreCase) >= 0)
    {
        Thread.Sleep(5000);
        var records = ReadMixedData(PathIni);
        if (records == null) return;
        var grief = GetPassword(GetUsername(), records);
        if (string.IsNullOrEmpty(grief)) return;
        SendText("/login " + grief);
    }
    else if (verbatim.IndexOf("просит телепортироваться к Вам", StringComparison.OrdinalIgnoreCase) >= 0)
        SendText("/tpaccept");
    else if (verbatim.IndexOf("tpa228", StringComparison.OrdinalIgnoreCase) >= 0)
        SendText("/tpa " + _ownerNick);
    else if (verbatim.IndexOf("На сервер заходит большой поток игроков.", StringComparison.OrdinalIgnoreCase) >= 0)
    {
        Thread.Sleep(5000);
        UseItemInHand();
    }
    else if (verbatim.IndexOf("Подождите несколько секунд перед повторым подключением!", StringComparison.OrdinalIgnoreCase) >= 0)
    {
        Thread.Sleep(5000);
        UseItemInHand();
    }
    else if (verbatim.IndexOf("paying", StringComparison.OrdinalIgnoreCase) >= 0)
    {
        if (_isMain){
            foreach(string nick in _secondaries){
                SendText($"/msg {nick} paying");
                Thread.Sleep(2000);
            }
            Thread.Sleep(500);

        SendText("/sellfish");
        Thread.Sleep(500);
        SendText("/balance");
        Thread.Sleep(500);
        SendText($"/pay {_ownerNick} {_balance}");
        }
        else {

        
        SendText("/sellfish");
        Thread.Sleep(500);
        SendText("/balance");
        Thread.Sleep(500);
        SendText($"/pay {_mainNick} {_balance}");
        }
    }
    
}

    public override void OnInventoryOpen(int inventoryId)
    {
        _menuId = inventoryId;
        LogToConsole($"[Menu] Меню открылось (ID: {inventoryId})");

        if (_menuStep == 0)
        {
            _menuStep = 1;
            LogToConsole("[Menu] Шаг 1: Выбор сервера");
            Thread.Sleep(500);
            ClickSlot(inventoryId, 21);
        }
        else if (_menuStep == 1)
        {
            _menuStep = 2;
            _currentPage = 0;
            LogToConsole("[Menu] Шаг 2: Выбор мира");

            var records = ReadMixedData(PathIni);
            var grief = GetGrief(GetUsername(), records);

            if (!grief.HasValue)
            {
                LogToConsole("[Menu] Ошибка: не найден гриф для " + GetUsername());
                _menuStep = 0;
                return;
            }

            (_targetPage, _targetSlot) = GetGriefPageAndSlot(grief.Value);
            LogToConsole($"[Menu] Гриф #{grief.Value} → страница {_targetPage}, слот {_targetSlot}");

            Thread.Sleep(500);

            if (_currentPage < _targetPage)
            {
                LogToConsole($"[Menu] Листаю на страницу {_currentPage + 1}...");
                ClickSlot(inventoryId, 44);
            }
            else
            {
                LogToConsole($"[Menu] Кликаю слот {_targetSlot}");
                ClickSlot(inventoryId, _targetSlot);
                _wasAtSpawn = false;
                _menuStep = 0;
                _currentPage = 0;
            }
        }
        else if (_menuStep == 2)
        {
            _currentPage++;
            LogToConsole($"[Menu] Теперь на странице {_currentPage}");
            Thread.Sleep(500);

            if (_currentPage < _targetPage)
            {
                LogToConsole($"[Menu] Листаю на страницу {_currentPage + 1}...");
                ClickSlot(inventoryId, 44);
            }
            else
            {
                LogToConsole($"[Menu] Кликаю слот {_targetSlot}");
                ClickSlot(inventoryId, _targetSlot);
                _wasAtSpawn = false;
                _menuStep = 0;
                _currentPage = 0;
            }
        }
        else
        {
            LogToConsole($"[Menu] Неизвестный шаг {_menuStep}, сброс.");
            _menuStep = 0;
            _currentPage = 0;
        }
    }

    private void ClickSlot(int menuId, int slot)
    {
        LogToConsole($"[Menu] Кликаю ЛКМ по слоту {slot} в меню {menuId}...");
        bool ok = WindowAction(menuId, slot, WindowActionType.LeftClick);
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

    private List<MixedRecord> ReadMixedData(string filePath)
{
    if (!File.Exists(filePath))
    {
        LogToConsole($"Ошибка: файл не найден: {filePath}");
        return null;
    }

    var records = new List<MixedRecord>();
    foreach (string line in File.ReadAllLines(filePath))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;

        string[] parts = line.Split(new char[] { ' ', '\t', ',' },
                                    StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4)
        {
            LogToConsole($"Ошибка: строка должна содержать 4 элемента. Найдено {parts.Length}. Строка: {line}");
            return null;
        }

        if (!sbyte.TryParse(parts[2], out sbyte byteValue))
        {
            LogToConsole($"Ошибка: 3-й элемент не sbyte. Строка: {line}");
            return null;
        }

        if (!bool.TryParse(parts[3], out bool boolValue))
        {
            LogToConsole($"Ошибка: 4-й элемент не bool. Строка: {line}");
            return null;
        }

        records.Add(new MixedRecord
        {
            Text        = parts[0],
            StringValue = parts[1],
            ByteValue   = byteValue,
            BoolValue   = boolValue
        });
    }

    if (records.Count == 0)
    {
        LogToConsole("Ошибка: файл не содержит данных.");
        return null;
    }

    return records;
}
}
