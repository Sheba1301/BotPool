//MCCScript 1.0
MCC.LoadBot(new realmsmc());
//MCCScript Extensions
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.IO.Pipes;
//using System.Reflection;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Text;
//using System.Text.RegularExpressions;
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
    private bool _IsSell = false;
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
        if (!_IsSell)
        {
            _IsSell = true;
            _ = SellingFishAsync();
        }
        Location pos = GetCurrentLocation();
        if (_wasAtSpawn == false && pos.X == 0.5 && pos.Y == 65.0 && pos.Z == 0.5)
        {
            LogToConsole("Terrarian&Movements отключен");
            SetTerrainEnabled(false);
            UseItemInHand();
            _wasAtSpawn = true;
        }
    }

    public async Task SellingFishAsync()
    {
        while (true)
        {
            SendText("/sellfish");
            await Task.Delay(10000);
        }
    }

    public int Balance()
    {
        return (1);
    }

    int[] GetRodDurabilities()
    {
        const int RodMaxDurability = 64;
        var result = new System.Collections.Generic.List<int>();

        Container inv = GetPlayerInventory();
        if (inv == null) return result.ToArray();

        for (int slot = 36; slot <= 44; slot++) // хотбар
        {
            if (!inv.Items.TryGetValue(slot, out Item item) || item == null) continue;
            if (item.Type != ItemType.FishingRod) continue;

            int damage = 0;
            if (item.NBT != null && item.NBT.TryGetValue("Damage", out object d) && d != null)
                int.TryParse(d.ToString(), out damage);

            result.Add(RodMaxDurability - damage); // остаток прочности
        }

        return result.ToArray();
    }

    public override void AfterGameJoined()
    {
        Sneak(true);
        ParseLaunchInfo();
        _ = ConnectPipeAsync();

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
            PipeSend("balance", _myNick, _balance.ToString());
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
            SendText("/tpa " + _mainNick);
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
            if (_isMain)
            {
                foreach (string nick in _secondaries)
                {
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
            else
            {
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

    // ==========================================================
    //                    СВЯЗЬ С ЦЕНТРОМ (PIPE)
    // ==========================================================
    private PipeClient _pipe;
    private bool _pipeConnecting = false;

    private async Task ConnectPipeAsync()
    {
        if (_pipe != null || _pipeConnecting) return;
        _pipeConnecting = true;
        try
        {
            var client = new PipeClient("MyApp.Ipc");
            client.MessageReceived += OnPipeMessage;

            LogToConsole("[Pipe] Подключаюсь...");
            await client.ConnectAsync();
            _pipe = client;
            LogToConsole("[Pipe] ЛОКАЛЬНЫЙ СЕРВЕР ПОДКЛЮЧЕН");

            // Представляемся центральному приложению
            await _pipe.SendAsync(new[] { "hello", _myNick });
        }
        catch (Exception ex)
        {
            LogToConsole("[Pipe] Ошибка подключения: " + ex.Message);
        }
        finally
        {
            _pipeConnecting = false;
        }
    }

    // Сообщение от центрального приложения
    private Task OnPipeMessage(string[] message)
    {
        LogToConsole("[Pipe] От сервера: " + string.Join(" | ", message));

        switch (message[0])
        {
            case "say":                       // ["say", "текст"]
                if (message.Length > 1) SendText(message[1]);
                break;
            case "paying":                    // ["paying"]
                SendText("/sellfish");
                break;
        }
        return Task.CompletedTask;
    }

    // Отправка в центральное приложение: PipeSend("balance", _myNick, _balance.ToString());
    private async void PipeSend(params string[] data)
    {
        if (_pipe == null) return;
        try
        {
            await _pipe.SendAsync(data);
        }
        catch (Exception ex)
        {
            LogToConsole("[Pipe] Связь потеряна: " + ex.Message);
            _pipe = null;
        }
    }
}

// ==============================================================
//        КЛАССЫ PIPE (вне класса бота, в конце файла)
// ==============================================================
public sealed class PipeMessageChannel
{
    private const int MaxMessageSize = 16 * 1024 * 1024;

    private readonly PipeStream _pipe;
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

    public PipeMessageChannel(PipeStream pipe)
    {
        _pipe = pipe;
    }

    public bool IsConnected { get { return _pipe.IsConnected; } }

    public async Task SendAsync(string[] data, CancellationToken ct = default(CancellationToken))
    {
        byte[] payload;
        using (var ms = new MemoryStream())
        {
            using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
            {
                writer.Write(data.Length);
                foreach (var s in data)
                    writer.Write(s ?? string.Empty);
            }
            payload = ms.ToArray();
        }

        var header = BitConverter.GetBytes(payload.Length);
        if (!BitConverter.IsLittleEndian) Array.Reverse(header);

        await _writeLock.WaitAsync(ct);
        try
        {
            await _pipe.WriteAsync(header, 0, header.Length, ct);
            await _pipe.WriteAsync(payload, 0, payload.Length, ct);
            await _pipe.FlushAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    // Возвращает null, если собеседник закрыл соединение
    public async Task<string[]> ReceiveAsync(CancellationToken ct = default(CancellationToken))
    {
        var header = new byte[4];
        if (!await ReadExactAsync(header, ct)) return null;

        if (!BitConverter.IsLittleEndian) Array.Reverse(header);
        int length = BitConverter.ToInt32(header, 0);
        if (length < 0 || length > MaxMessageSize)
            throw new InvalidDataException("Недопустимый размер сообщения: " + length);

        var buffer = new byte[length];
        if (!await ReadExactAsync(buffer, ct)) return null;

        using (var reader = new BinaryReader(new MemoryStream(buffer), Encoding.UTF8))
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > length)
                throw new InvalidDataException("Некорректное число элементов");

            var result = new string[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadString();
            return result;
        }
    }

    public async Task RunReceiveLoopAsync(Func<string[], Task> onMessage, CancellationToken ct = default(CancellationToken))
    {
        while (!ct.IsCancellationRequested && _pipe.IsConnected)
        {
            var message = await ReceiveAsync(ct);
            if (message == null) break;
            await onMessage(message);
        }
    }

    private async Task<bool> ReadExactAsync(byte[] buffer, CancellationToken ct)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await _pipe.ReadAsync(buffer, offset, buffer.Length - offset, ct);
            if (read == 0) return false;
            offset += read;
        }
        return true;
    }

    public void Dispose()
    {
        _pipe.Dispose();
        _writeLock.Dispose();
    }
}

public sealed class PipeClient
{
    private readonly string _pipeName;
    private NamedPipeClientStream _pipe;
    private PipeMessageChannel _channel;

    public event Func<string[], Task> MessageReceived;

    public PipeClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public async Task ConnectAsync(int timeoutMs = 5000, CancellationToken ct = default(CancellationToken))
    {
        _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        await _pipe.ConnectAsync(timeoutMs, ct);
        _channel = new PipeMessageChannel(_pipe);

        var ignored = Task.Run(async () =>
        {
            try
            {
                await _channel.RunReceiveLoopAsync(async msg =>
                {
                    var handler = MessageReceived;
                    if (handler != null)
                        await handler(msg);
                }, ct);
            }
            catch (Exception) { /* связь закрыта */ }
        });
    }

    public Task SendAsync(string[] data, CancellationToken ct = default(CancellationToken))
    {
        if (_channel == null)
            throw new InvalidOperationException("Нет подключения");
        return _channel.SendAsync(data, ct);
    }

    public void Dispose()
    {
        if (_channel != null) _channel.Dispose();
    }
}