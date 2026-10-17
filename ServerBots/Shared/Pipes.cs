using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Collections.Concurrent;

public sealed class PipeMessageChannel : IAsyncDisposable
{
    private const int MaxMessageSize = 16 * 1024 * 1024; // защита от мусора в потоке

    private readonly PipeStream _pipe;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public PipeMessageChannel(PipeStream pipe) => _pipe = pipe;

    public bool IsConnected => _pipe.IsConnected;

    public async Task SendAsync(string[] data, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(data.Length);
            foreach (var s in data)
                writer.Write(s ?? string.Empty);
        }

        var payload = ms.GetBuffer().AsMemory(0, (int)ms.Length);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

        // Lock, чтобы параллельные отправки не перемешали байты
        await _writeLock.WaitAsync(ct);
        try
        {
            await _pipe.WriteAsync(header, ct);
            await _pipe.WriteAsync(payload, ct);
            await _pipe.FlushAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>Возвращает null, если собеседник закрыл соединение.</summary>
    public async Task<string[]?> ReceiveAsync(CancellationToken ct = default)
    {
        var header = new byte[4];
        if (!await ReadExactAsync(header, ct)) return null;

        int length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length < 0 || length > MaxMessageSize)
            throw new InvalidDataException($"Недопустимый размер сообщения: {length}");

        var buffer = new byte[length];
        if (!await ReadExactAsync(buffer, ct)) return null;

        using var reader = new BinaryReader(new MemoryStream(buffer), Encoding.UTF8);
        int count = reader.ReadInt32();
        if (count < 0 || count > length) // каждая строка занимает минимум 1 байт
            throw new InvalidDataException("Некорректное число элементов");

        var result = new string[count];
        for (int i = 0; i < count; i++)
            result[i] = reader.ReadString();
        return result;
    }

    /// <summary>Читает сообщения в цикле, пока соединение не закроется.</summary>
    public async Task RunReceiveLoopAsync(Func<string[], Task> onMessage, CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested && _pipe.IsConnected)
        {
            var message = await ReceiveAsync(ct);
            if (message is null) break;
            await onMessage(message);
        }
    }

    private async Task<bool> ReadExactAsync(byte[] buffer, CancellationToken ct)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await _pipe.ReadAsync(buffer.AsMemory(offset), ct);
            if (read == 0) return false; // конец потока
            offset += read;
        }
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        await _pipe.DisposeAsync();
        _writeLock.Dispose();
    }
}



public sealed class PipeServer
{
    private readonly string _pipeName;
    private readonly ConcurrentDictionary<Guid, PipeMessageChannel> _clients = new();

    public event Func<Guid, string[], Task>? MessageReceived;
    public event Action<Guid>? ClientConnected;
    public event Action<Guid>? ClientDisconnected;

    public PipeServer(string pipeName) => _pipeName = pipeName;

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            try
            {
                await pipe.WaitForConnectionAsync(ct);
            }
            catch (OperationCanceledException)
            {
                await pipe.DisposeAsync();
                break;
            }

            _ = HandleClientAsync(pipe, ct); // не ждём, сразу принимаем следующего
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await using var channel = new PipeMessageChannel(pipe);
        _clients[id] = channel;
        ClientConnected?.Invoke(id);

        try
        {
            await channel.RunReceiveLoopAsync(async msg =>
            {
                if (MessageReceived is { } handler)
                    await handler(id, msg);
            }, ct);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or OperationCanceledException)
        {
            // клиент отвалился или прислал мусор — просто закрываем
        }
        finally
        {
            _clients.TryRemove(id, out _);
            ClientDisconnected?.Invoke(id);
        }
    }

    public Task SendAsync(Guid clientId, string[] data, CancellationToken ct = default) =>
        _clients.TryGetValue(clientId, out var ch)
            ? ch.SendAsync(data, ct)
            : Task.CompletedTask;

    public async Task BroadcastAsync(string[] data, CancellationToken ct = default)
    {
        foreach (var ch in _clients.Values)
        {
            try { await ch.SendAsync(data, ct); }
            catch (IOException) { /* клиент уже ушёл */ }
        }
    }
}



public sealed class PipeClient : IAsyncDisposable
{
    private readonly string _pipeName;
    private NamedPipeClientStream? _pipe;
    private PipeMessageChannel? _channel;
    private Task? _receiveTask;

    public event Func<string[], Task>? MessageReceived;

    public PipeClient(string pipeName) => _pipeName = pipeName;

    public async Task ConnectAsync(int timeoutMs = 5000, CancellationToken ct = default)
    {
        _pipe = new NamedPipeClientStream(
            ".", _pipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        await _pipe.ConnectAsync(timeoutMs, ct);
        _channel = new PipeMessageChannel(_pipe);

        _receiveTask = Task.Run(() => _channel.RunReceiveLoopAsync(async msg =>
        {
            if (MessageReceived is { } handler)
                await handler(msg);
        }, ct), ct);
    }

    public Task SendAsync(string[] data, CancellationToken ct = default) =>
        _channel?.SendAsync(data, ct)
        ?? throw new InvalidOperationException("Нет подключения");

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
    }
}