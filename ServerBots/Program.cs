var server = new PipeServer("MyApp.Ipc");

server.ClientConnected += id => Console.WriteLine($"Подключился клиент {id}");
server.ClientDisconnected += id => Console.WriteLine($"Отключился клиент {id}");

// Что делать, когда пришло сообщение
server.MessageReceived += (clientId, message) =>
{
    CollectionData(message);
    return Task.CompletedTask;
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// Запускаем приём клиентов в фоне
var serverTask = server.RunAsync(cts.Token);

// Console.WriteLine("Сервер запущен. Вводите текст, он уйдёт всем клиентам (слова через пробел = элементы массива).");

/* while (!cts.IsCancellationRequested)
{
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;

    string[] data = line.Split(' ');       // "привет мир" -> ["привет", "мир"]
    await server.BroadcastAsync(data);     // отправить всем подключённым
}
*/
public void static Menu()
{
Console.WriteLine("Выберите нужный вам пункт:");
Console.WriteLine("1) Мониторинг деятельности ботов");
Console.WriteLine("2) Перевести весь заработок(/pay ник)");
sbyte Idmenu = Console.ReadLine();
Console.clear();
switch(Idmenu)
{
    case 1: 
        Monitoring();
        break;
    case 2: 
        Paying();
        break;
}
}

public void static Monitoring()
{

}

public bool static Paying()
{

}
// string[] messages = {nick, balance, SalaryPerHalfHour, 2SalaryPerHalfHour(Hour), status(banned or unbanned)}
public string[] CollectionData(string[] messages)
{
    List<string> DataBots = new List<string> {};
    DataBots.Add(messages);
    
}
await serverTask;