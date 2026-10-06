namespace BotMinecraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Client
{
    public static List<string> actived = new();

    public static sbyte  grief      = 0;
    public static string password   = "-";
    public static string ServerIP   = "50.114.4.207";
    public static string ScriptName = "Army.cs";

    // ====================== ПУТЬ К КОНФИГУ ======================
    // ВАЖНО: в финале убрать .Substring(0, 36).
    // Всё обращение к config.ini идёт только через эту переменную!
    public static string ConfigPath =>
        PathHelper.GetCompilerPath(1).Substring(0, 36) + @"config.ini";
    // ============================================================

    // ============ ЗАДЕРЖКА МЕЖДУ ЗАПУСКАМИ АККАУНТОВ ============
    // 5 секунд между запусками при "Запустить все"
    private const int LAUNCH_COOLDOWN_MS = 5000;
    // ============================================================

    public static sbyte menustage = 0;

    static void Main()
    {
        AccountManager.LoadFromFile(ConfigPath);

        while (true)
        {
            if (menustage == 0)      MainMenu();
            else if (menustage == 1) AccountListMenu();
        }
    }

    // ======================= ГЛАВНОЕ МЕНЮ =======================
    public static void MainMenu()
    {

        Console.WriteLine(PathHelper.GetCompilerPath(1).Substring(0, 29) + @"Compiler\BotPool.exe");
        Console.WriteLine("Вас приветствует Менеджер Майнкрафт ботов!");
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("1) Список аккаунтов");
        Console.WriteLine("2) Сообщить об ошибке/Предложить идею");
        Console.WriteLine("3) Наши медиа");
        Console.WriteLine("------------------------------------------");
        Console.Write("Введите нужный пункт меню: ");

        string item = Console.ReadLine() ?? string.Empty;
        Console.Clear();
        Console.WriteLine(MenuGuide(menustage, item));

        if (menustage == 0)
        {
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
        Console.Clear();
    }

    public static string MenuGuide(sbyte stage, string item)
    {
        if (stage == 0)
        {
            switch (item)
            {
                case "1": menustage = 1; break;
                case "2": menustage = 0; return "Telegram: @Feykomet12";
                case "3": menustage = 0; return "Telegram Channel: t.me/BotPoolApp";
            }
        }
        return string.Empty;
    }

    // ==================== СПИСОК АККАУНТОВ ====================
    public static void AccountListMenu()
    {
        PrintAccounts();

        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("Введите номер аккаунта для действий");
        Console.WriteLine("A)   Добавить аккаунт");
        Console.WriteLine("D)   Удалить аккаунт");
        Console.WriteLine("ALL) Запустить все аккаунты (Use? = Y)");
        Console.WriteLine("0)   Вернуться в главное меню");
        Console.Write("> ");

        string input = (Console.ReadLine() ?? string.Empty).Trim();

        if (input == "0")
        {
            Console.Clear();
            menustage = 0;
            return;
        }

        if (input.Equals("A", StringComparison.OrdinalIgnoreCase))
        {
            AddAccountMenu();
            return;
        }

        if (input.Equals("D", StringComparison.OrdinalIgnoreCase))
        {
            DeleteAccountMenu();
            return;
        }

        if (input.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            LaunchAllMenu();
            return;
        }

        if (!sbyte.TryParse(input, out sbyte accountnumber))
        {
            Console.Clear();
            Console.WriteLine("Неверный номер/тип данных!");
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        AccountInfo? acc = AccountManager.GetAccountInfo(accountnumber);
        if (acc == null)
        {
            Console.Clear();
            Console.WriteLine("Аккаунт не найден. Нажмите любую клавишу...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        AccountActionsMenu(acc, accountnumber);
    }

    // ---------- ДЕЙСТВИЯ С КОНКРЕТНЫМ АККАУНТОМ ----------
    static void AccountActionsMenu(AccountInfo acc, int number)
    {
        Console.Clear();
        Console.WriteLine("Примечание: удалять бота можно прямо тут (пункт 3)");
        Console.WriteLine($"Ник:   {acc.Nickname}");
        Console.WriteLine($"Пароль:{acc.Password}");
        Console.WriteLine($"Гриф:  {acc.ServerNumber}");
        Console.WriteLine($"Use?:  {(acc.Use ? "Да" : "Нет")}");
        Console.WriteLine("------------------------------");
        Console.WriteLine("1) Запустить бота");
        Console.WriteLine("2) Переключить Use? (вкл/выкл)");
        Console.WriteLine("3) Удалить аккаунт");
        Console.WriteLine("0) Назад");
        Console.Write("> ");

        if (!sbyte.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out sbyte action))
            action = -1;

        switch (action)
        {
            case 0:
                Console.Clear();
                return;

            case 1:
                LaunchSingleAccount(acc);
                Console.Clear();
                return;

            case 2:
                acc.Use = !acc.Use;
                if (AccountManager.SaveToFile(ConfigPath))
                    Console.WriteLine($"\nUse? изменён на: {(acc.Use ? "Да" : "Нет")}");
                else
                    Console.WriteLine("\nНе удалось сохранить изменения.");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey();
                Console.Clear();
                return;

            case 3:
                Console.Write("Удалить аккаунт? (y/n): ");
                string confirm = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
                if (confirm is "y" or "yes" or "д" or "да")
                {
                    if (AccountManager.RemoveAt(number) && AccountManager.SaveToFile(ConfigPath))
                        Console.WriteLine($"\nАккаунт '{acc.Nickname}' удалён.");
                    else
                        Console.WriteLine("\nНе удалось удалить аккаунт.");
                }
                else
                {
                    Console.WriteLine("\nОтменено.");
                }
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey();
                Console.Clear();
                return;

            default:
                Console.Clear();
                Console.WriteLine("Неверный пункт меню.");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey();
                Console.Clear();
                return;
        }
    }

    // ---------- ОДИНОЧНЫЙ ЗАПУСК (с выбором главный/второстепенный) ----------
    // ---------- ОДИНОЧНЫЙ ЗАПУСК (с выбором главный/второстепенный) ----------
static void LaunchSingleAccount(AccountInfo acc)
{
    Console.Write("\nЗапустить как ГЛАВНОГО? (y/n, Enter = y): ");
    string ans = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
    bool isMain = ans is not ("n" or "no" or "н" or "нет");

    if (isMain)
    {
        // Главный — спрашиваем владельца. Список второстепенных пустой,
        // потому что при одиночном запуске нет других ботов рядом.
        Console.Write("Ник владельца (куда переводятся все деньги): ");
        string ownerNick = (Console.ReadLine() ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(ownerNick))
        {
            Console.WriteLine("Ник владельца не указан. Запуск отменён.");
            Console.ReadKey();
            return;
        }

        LaunchOneAccount(acc,
                         isMain: true,
                         mainNick: "",
                         secondaries: new List<string>(),   // пусто
                         ownerNick: ownerNick);

        Console.WriteLine("\nНажмите любую клавишу...");
        Console.ReadKey();
        return;
    }

    // ---- Второстепенный ----
    // Нужен ник главного. Показываем уже запущенные как подсказку.
    Console.WriteLine("\nЗапущенные аккаунты:");
    if (actived.Count == 0)
    {
        Console.WriteLine("  (нет запущенных)");
    }
    else
    {
        for (int i = 0; i < actived.Count; i++)
            Console.WriteLine($"  {i + 1}) {actived[i]}");
    }

    Console.Write("Введите ник главного аккаунта: ");
    string mainNick = (Console.ReadLine() ?? string.Empty).Trim();

    if (string.IsNullOrWhiteSpace(mainNick))
    {
        Console.WriteLine("Ник главного не указан. Запуск отменён.");
        Console.ReadKey();
        return;
    }

    LaunchOneAccount(acc,
                     isMain: false,
                     mainNick: mainNick,
                     secondaries: null,
                     ownerNick: "");

    Console.WriteLine("\nНажмите любую клавишу...");
    Console.ReadKey();
}
    // ---------- ЗАПУСК ВСЕХ АККАУНТОВ С Use = true ----------
    // ---------- ЗАПУСК ВСЕХ АККАУНТОВ С Use = true ----------
static void LaunchAllMenu()
{
    Console.Clear();

    // 1) Собираем список включённых аккаунтов (Use? = Y)
    var enabled = new List<AccountInfo>();
    for (int i = 1; i <= AccountManager.Count; i++)
    {
        AccountInfo? a = AccountManager.GetAccountInfo(i);
        if (a != null && a.Use) enabled.Add(a);
    }

    if (enabled.Count == 0)
    {
        Console.WriteLine("Нет аккаунтов с Use? = Y. Нечего запускать.");
        Console.WriteLine("Нажмите любую клавишу...");
        Console.ReadKey();
        Console.Clear();
        return;
    }

    // 2) Показываем список и просим выбрать главный
    Console.WriteLine("=== ЗАПУСК ВСЕХ АККАУНТОВ ===");
    Console.WriteLine("\nДоступные аккаунты (Use? = Y):");
    for (int i = 0; i < enabled.Count; i++)
        Console.WriteLine($"  {i + 1}) {enabled[i].Nickname}");

    Console.Write("\nВыберите номер ГЛАВНОГО аккаунта (0 - отмена): ");
    if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out int mainIdx)
        || mainIdx == 0
        || mainIdx < 1 || mainIdx > enabled.Count)
    {
        Console.WriteLine("Отменено.");
        Console.ReadKey();
        Console.Clear();
        return;
    }

    AccountInfo mainAcc = enabled[mainIdx - 1];

    // 3) Просим ник владельца — куда в итоге уходят деньги
    Console.Write("\nНик владельца (куда переводятся все деньги): ");
    string ownerNick = (Console.ReadLine() ?? string.Empty).Trim();

    if (string.IsNullOrWhiteSpace(ownerNick))
    {
        Console.WriteLine("Ник владельца не указан. Отмена.");
        Console.ReadKey();
        Console.Clear();
        return;
    }

    // 4) Собираем ники второстепенных (все включённые кроме главного)
    var secondariesNicks = new List<string>();
    foreach (var a in enabled)
    {
        if (ReferenceEquals(a, mainAcc)) continue;
        secondariesNicks.Add(a.Nickname);
    }

    // 5) Подтверждение
    Console.Clear();
    Console.WriteLine($"Главный аккаунт: {mainAcc.Nickname}");
    Console.WriteLine($"Владелец:        {ownerNick}");
    Console.WriteLine($"Второстепенных:  {secondariesNicks.Count} " +
                      $"[{string.Join(", ", secondariesNicks)}]");
    Console.WriteLine($"Всего запускаем: {enabled.Count}");
    Console.WriteLine($"Задержка между запусками: {LAUNCH_COOLDOWN_MS / 1000} сек");
    Console.WriteLine("-----------------------------");
    Console.Write("Начать? (y/n): ");
    string go = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
    if (go is not ("y" or "yes" or "д" or "да"))
    {
        Console.Clear();
        return;
    }

    Console.Clear();

    // 6) Запускаем главного — ему передаём ownerNick и список второстепенных
    Console.WriteLine($"[1/{enabled.Count}] Запуск ГЛАВНОГО: {mainAcc.Nickname}");
    LaunchOneAccount(mainAcc,
                     isMain: true,
                     mainNick: "",
                     secondaries: secondariesNicks,
                     ownerNick: ownerNick);

    // 7) Запускаем всех остальных с задержкой
    int index = 1;
    foreach (var acc in enabled)
    {
        if (ReferenceEquals(acc, mainAcc)) continue;

        index++;
        WaitWithCountdown(LAUNCH_COOLDOWN_MS);

        Console.WriteLine($"[{index}/{enabled.Count}] Запуск второстепенного: {acc.Nickname} " +
                          $"(главный: {mainAcc.Nickname})");

        LaunchOneAccount(acc,
                         isMain: false,
                         mainNick: mainAcc.Nickname,
                         secondaries: null,
                         ownerNick: "");
    }

    Console.WriteLine("\n-----------------------------");
    Console.WriteLine("Все аккаунты запущены.");
    Console.WriteLine("Нажмите любую клавишу...");
    Console.ReadKey();
    Console.Clear();
}

    // ---------- ОБЩИЙ ХЕЛПЕР ЗАПУСКА ----------
   static void LaunchOneAccount(AccountInfo acc, bool isMain, string mainNick,
                             List<string>? secondaries = null,
                             string ownerNick = "")
{
    if (!actived.Contains(acc.Nickname))
        actived.Add(acc.Nickname);

    grief    = Convert.ToSByte(acc.ServerNumber);
    password = acc.Password;

    try
    {
        LaunchMCC(acc.Nickname, PathHelper.GetCompilerPath(1).Substring(0, 29) + @"\Compiler\BotPool.exe",
                  isMain, mainNick, secondaries, ownerNick);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ! Ошибка запуска '{acc.Nickname}': {ex.Message}");
    }
}

    // ---------- ОЖИДАНИЕ С ОБРАТНЫМ ОТСЧЁТОМ ----------
    static void WaitWithCountdown(int milliseconds)
    {
        int seconds = milliseconds / 1000;
        for (int i = seconds; i > 0; i--)
        {
            Console.Write($"\rОжидание {i} сек... ");
            Thread.Sleep(1000);
        }
        Console.Write("\r                     \r");
    }

    // ---------- ДОБАВЛЕНИЕ АККАУНТА ----------
    static void AddAccountMenu()
    {
        Console.Clear();
        Console.WriteLine("=== ДОБАВЛЕНИЕ АККАУНТА ===");

        Console.Write("Ник: ");
        string nick = (Console.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nick))
        {
            Console.WriteLine("Ник не может быть пустым. Нажмите любую клавишу...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        if (AccountManager.ExistsByNickname(nick))
        {
            Console.WriteLine($"Аккаунт с ником '{nick}' уже существует.");
            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        Console.Write("Пароль: ");
        string pass = (Console.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(pass))
        {
            Console.WriteLine("Пароль не может быть пустым. Нажмите любую клавишу...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        Console.Write("Номер грифа: ");
        if (!int.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out int serverNum))
        {
            Console.WriteLine("Номер грифа должен быть числом. Нажмите любую клавишу...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        Console.Write("Use? (y/n, по умолчанию y): ");
        string useInput = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
        bool use = useInput is not ("n" or "no" or "н" or "нет");

        AccountManager.Add(new AccountInfo
        {
            Nickname     = nick,
            Password     = pass,
            ServerNumber = serverNum,
            Use          = use
        });

        if (AccountManager.SaveToFile(ConfigPath))
            Console.WriteLine($"\nАккаунт '{nick}' добавлен.");
        else
            Console.WriteLine("\nНе удалось сохранить конфиг.");

        Console.WriteLine("Нажмите любую клавишу...");
        Console.ReadKey();
        Console.Clear();
    }

    // ---------- УДАЛЕНИЕ АККАУНТА ----------
    static void DeleteAccountMenu()
    {
        Console.Clear();
        PrintAccounts();
        Console.WriteLine("------------------------------------------------------------");
        Console.Write("Введите номер аккаунта для удаления (0 - отмена): ");

        if (!sbyte.TryParse((Console.ReadLine() ?? string.Empty).Trim(), out sbyte number) || number == 0)
        {
            Console.Clear();
            return;
        }

        AccountInfo? acc = AccountManager.GetAccountInfo(number);
        if (acc == null)
        {
            Console.WriteLine("Аккаунт не найден. Нажмите любую клавишу...");
            Console.ReadKey();
            Console.Clear();
            return;
        }

        Console.Write($"Удалить '{acc.Nickname}'? (y/n): ");
        string confirm = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
        if (confirm is "y" or "yes" or "д" or "да")
        {
            if (AccountManager.RemoveAt(number) && AccountManager.SaveToFile(ConfigPath))
                Console.WriteLine($"\nАккаунт '{acc.Nickname}' удалён.");
            else
                Console.WriteLine("\nНе удалось удалить.");
            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }
        Console.Clear();
    }

    // ---------- ПЕЧАТЬ СПИСКА ----------
    static void PrintAccounts()
    {
        Console.Clear();
        Console.WriteLine($"\nАккаунтов: {AccountManager.Count}\n");
        Console.WriteLine("| № |     Никнейм        |     Пароль    |  Номер грифа  |  Use?  |");
        Console.WriteLine("|---|--------------------|---------------|---------------|--------|");

        for (int i = 1; i <= AccountManager.Count; i++)
        {
            AccountInfo? acc = AccountManager.GetAccountInfo(i);
            if (acc == null) continue;
            Console.WriteLine($"| {i,2} | {acc.Nickname,-16} | {acc.Password,-15} | {acc.ServerNumber,13} | {(acc.Use ? "Y " : "N")} |");
        }
    }

    // ---------- ЗАПУСК MCC ----------
    // Формат аргументов:
    //   Главный:        "Nick" - - true
    //   Второстепенный: "Nick" - - false "MainNick"
    static Process? LaunchMCC(string username, string exePath, bool isMain,
                          string mainNick = "",
                          List<string>? secondaries = null,
                          string ownerNick = "")
{
    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Никнейм не может быть пустым.", nameof(username));
    if (!File.Exists(exePath))
        throw new FileNotFoundException("BotPool.exe не найден.", exePath);

    // Собираем хвост аргументов
    var extra = new List<string>();
    if (isMain)
    {
        extra.Add($"\"{ownerNick}\"");
        if (secondaries != null)
            foreach (var nick in secondaries)
                extra.Add($"\"{nick}\"");
    }
    else
    {
        extra.Add($"\"{mainNick}\"");
    }

    string args = $"\"{username}\" - - {isMain.ToString().ToLower()} " +
                  string.Join(" ", extra);

    var psi = new ProcessStartInfo
    {
        FileName         = exePath,
        Arguments        = args,
        WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
        UseShellExecute  = true,
        CreateNoWindow   = false,
    };

    return Process.Start(psi);
}
}