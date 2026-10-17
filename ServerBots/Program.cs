var server = new PipeServer("MyApp.Ipc");

server.ClientConnected += id => Console.WriteLine($"Подключился клиент {id}");
server.ClientDisconnected += id => Console.WriteLine($"Отключился клиент {id}");

// Что делать, когда пришло сообщение
server.MessageReceived += (clientId, message) =>
{
    Console.WriteLine($"От клиента: {string.Join(" | ", message)}");
    return Task.CompletedTask;
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// Запускаем приём клиентов в фоне
var serverTask = server.RunAsync(cts.Token);

Console.WriteLine("Сервер запущен. Вводите текст, он уйдёт всем клиентам (слова через пробел = элементы массива).");

while (!cts.IsCancellationRequested)
{
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;

    string[] data = line.Split(' ');       // "привет мир" -> ["привет", "мир"]
    await server.BroadcastAsync(data);     // отправить всем подключённым
}

await serverTask;