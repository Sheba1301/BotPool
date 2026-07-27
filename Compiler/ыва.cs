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
    public string Text { get; set; }
    public string StringValue { get; set; }
    public sbyte ByteValue { get; set; }
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

    private const double AttackRange   = 2.8;
    private const string CurrentWeapon = "sword";
    private const double JitterSeconds = 0.05;

    private static readonly Dictionary<string, double> WeaponCooldowns = new Dictionary<string, double>
    {
        { "hand",    0.25  },
        { "sword",   0.625 },
        { "axe",     1.0   },
        { "pickaxe", 0.833 },
        { "shovel",  1.0   },
    };

    private DateTime nextAttackAllowed = DateTime.MinValue;
    private readonly Random rnd = new Random();

    private static readonly HashSet<string> MOB_WHITELIST = new HashSet<string>
    {
        "zombie", "skeleton", "creeper", "spider", "witch",
        "pillager", "vindicator", "ravager", "phantom",
        "drowned", "husk", "stray", "enderman", "blaze",
        "zombie_villager", "cave_spider", "silverfish"
    };

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

        if (_autoAttack)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (DateTime.Now < nextAttackAllowed)
            return;

        Entity target = FindNearestTarget();
        if (target == null)
            return;

        AttackEntity(target);

        double baseCooldown = WeaponCooldowns.TryGetValue(CurrentWeapon, out double cd) ? cd : 0.625;
        double jitter = (rnd.NextDouble() * 2 - 1) * JitterSeconds;
        double delay = Math.Max(0.05, baseCooldown + jitter);

        nextAttackAllowed = DateTime.Now.AddSeconds(delay);
    }

    private Entity FindNearestTarget()
    {
        Location myLoc = GetCurrentLocation();

        var candidates = GetEntities()
            .Select(e => e.Value)
            .Where(e => e.Type != EntityType.Player)
            .Where(e => MOB_WHITELIST.Contains(e.Type.ToString().ToLower()))
            .Where(e => myLoc.Distance(e.Location) <= AttackRange)
            .OrderBy(e => myLoc.Distance(e.Location))
            .ToList();

        return candidates.FirstOrDefault();
    }

    private void AttackEntity(Entity target)
    {
        Location headPos = new Location(target.Location.X, target.Location.Y + 1.1, target.Location.Z);
        LookAtLocation(headPos);

        bool ok = InteractEntity(target.ID, InteractType.Attack);
        SendAnimation(Hand.MainHand);

        LogToConsole(ok
            ? $"[Attack] ✓ {target.Type} dist={GetCurrentLocation().Distance(target.Location):F1}"
            : $"[Attack] ✗ Промах {target.Type}");
    }

    private void RunDiagnostics()
    {
        LogToConsole("=== Значения enum Hand ===");
        foreach (Hand h in Enum.GetValues(typeof(Hand)))
            LogToConsole($"{h} = {(int)h}");

        LogToConsole("=== Значения enum InteractType ===");
        foreach (InteractType t in Enum.GetValues(typeof(InteractType)))
            LogToConsole($"{t} = {(int)t}");

        LogToConsole("=== Поиск InteractEntity/SendAnimation/UseEntity/Attack по всем сборкам ===");
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            foreach (var t in types)
            {
                MethodInfo[] methods;
                try
                {
                    methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                        .Where(m => m.Name == "InteractEntity" || m.Name == "SendAnimation" || m.Name == "SwingArm" || m.Name == "UseEntity")
                        .ToArray();
                }
                catch { continue; }

                foreach (var m in methods)
                {
                    var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    LogToConsole($"{t.FullName}.{m.Name}({parms})");
                }
            }
        }
        LogToConsole("=== Конец диагностики ===");
    }

    public override void AfterGameJoined()
    {
        LogToConsole("[Bot] Бот запущен.");
        _wasAtSpawn = false;
        _menuId     = -1;
        _menuStep   = 0;
    }

    public override void OnRespawn()
    {
        LogToConsole("[Bot] Респаун");
        Thread.Sleep(3000);
        SendText("/home");
        Thread.Sleep(3000);
    }

    public override void GetText(string text)
    {
        string username = "";
        string verbatim = GetVerbatim(text);

        if (verbatim.IndexOf("/reg", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/reg 130331 130331");
        else if (verbatim.IndexOf("/register", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/register 130331 130331");
        else if (verbatim.IndexOf("/login", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Thread.Sleep(5000);
            var records = ReadMixedData(PathIni);
            var grief = GetPassword(GetUsername(), records);
            SendText("/login " + grief);
        }
        else if (verbatim.IndexOf("просит телепортироваться к Вам", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/tpaccept");
        else if (verbatim.IndexOf("tpa228", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/tpa " + username);
        else if (verbatim.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _autoAttack = !_autoAttack;
            LogToConsole(_autoAttack
                ? "[Attack] ✓ Авто-атака включена"
                : "[Attack] ✗ Авто-атака выключена");
        }
        else if (verbatim.IndexOf("diag", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            RunDiagnostics();
        }
        else if (verbatim.IndexOf("handmain", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SendAnimation(Hand.MainHand);
            LogToConsole("[Diag] SendAnimation(Hand.MainHand) отправлен");
        }
        else if (verbatim.IndexOf("handoff", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SendAnimation(Hand.OffHand);
            LogToConsole("[Diag] SendAnimation(Hand.OffHand) отправлен");
        }
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
        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

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
}