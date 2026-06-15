
//MCCScript 1.0
MCC.LoadBot(new realmsmc());
//MCCScript Extensions
class realmsmc : ChatBot
{
    private bool _waitingForMenu = false;
    private int  _menuId         = -1;
    private bool _wasAtSpawn     = false;
    private bool _autoAttack     = false;

    // ─── Список мобов для атаки (не игроки) ──────────────────────────────
    private static readonly HashSet<string> MOB_WHITELIST = new HashSet<string>
    {
        "zombie", "skeleton", "creeper", "spider", "witch",
        "pillager", "vindicator", "ravager", "phantom",
        "drowned", "husk", "stray", "enderman", "blaze",
        "zombie_villager", "cave_spider", "silverfish"
    };

    // ─── Проверка позиции ─────────────────────────────────────────────────
    private void CheckPositionAndExecute()
    {
        Location pos = GetCurrentLocation();

        bool isAtTarget = Math.Abs(pos.X - 0) < 0.5 &&
                          Math.Abs(pos.Y - 1) < 0.5 &&
                          Math.Abs(pos.Z - 0) < 0.5;

        if (isAtTarget && !_wasAtSpawn)
        {
            LogToConsole("[Spawn] Достиг 0 65 0! Открываю меню...");
            OpenCompassMenu();
            _wasAtSpawn = true;
        }
        else if (!isAtTarget)
        {
            _wasAtSpawn = false;
        }
    }

    // ─── Авто атака ───────────────────────────────────────────────────────
    private void TryAutoAttack()
    {
        if (!_autoAttack) return;

        var entities = GetEntities();
        if (entities == null || entities.Count == 0) return;

        Location myPos = GetCurrentLocation();

        foreach (var entity in entities.Values)
        {
            // Пропускаем игроков
            if (entity.Type == EntityType.Player) continue;

            // Проверяем что моб в белом списке
            string entityName = entity.Type.ToString().ToLower();
            if (!MOB_WHITELIST.Contains(entityName)) continue;

            // Проверяем дистанцию (не дальше 4 блоков)
            double dist = Math.Sqrt(
                Math.Pow(entity.Location.X - myPos.X, 2) +
                Math.Pow(entity.Location.Y - myPos.Y, 2) +
                Math.Pow(entity.Location.Z - myPos.Z, 2)
            );

            if (dist <= 4.0)
            {
                LogToConsole($"[Attack] Атакую {entityName} (дист: {dist:F1})");
                InteractEntity(entity.ID, InteractType.Attack);
                break; // атакуем одного за тик
            }
        }
    }

    public override void Update()
    {
        CheckPositionAndExecute();
        TryAutoAttack();
    }

    public override void AfterGameJoined()
    {
        LogToConsole("[Bot] Бот запущен.");
        SendChat("/login 130331");
    }

    // ─── Чат ──────────────────────────────────────────────────────────────
    public override void GetText(string text)
    {
        string message  = "";
        string username = "Feykomet";
        string verbatim = GetVerbatim(text);

        if (verbatim.IndexOf("/reg", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/reg 130331 130331");
        else if (verbatim.IndexOf("/register", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/register 130331 130331");
        else if (verbatim.IndexOf("/login", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/login 130331");
        else if (verbatim.IndexOf("Принять запрос", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/tpaccept");
        else if (verbatim.IndexOf("TPA", StringComparison.OrdinalIgnoreCase) >= 0)
            SendText("/tpa " + username);

        // Активация/деактивация авто атаки
        else if (verbatim.IndexOf("ATTACK", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _autoAttack = !_autoAttack;
            LogToConsole(_autoAttack
                ? "[Attack] ✓ Авто-атака включена"
                : "[Attack] ✗ Авто-атака выключена");
        }
    }

    // ─── Компас ───────────────────────────────────────────────────────────
    private void OpenCompassMenu()
    {
        LogToConsole("[Compass] Использую предмет в руке...");
        UseItemInHand();
        _waitingForMenu = true;
    }

    // ─── Меню открылось ───────────────────────────────────────────────────
    public override void OnInventoryOpen(int inventoryId)
    {
        if (!_waitingForMenu) return;

        _menuId = inventoryId;
        LogToConsole($"[Menu] Меню открылось (ID: {inventoryId})");

        Thread.Sleep(1500);
        ClickSlot(21);
        Thread.Sleep(1500);
        ClickSlot(BotMinecraft.grief);
    }

    // ─── Клик по слоту ────────────────────────────────────────────────────
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

    // ─── Меню закрылось ───────────────────────────────────────────────────
    public override void OnInventoryClose(int inventoryId)
    {
        if (inventoryId == _menuId)
        {
            _menuId = -1;
            _waitingForMenu = false;
        }
    }
}