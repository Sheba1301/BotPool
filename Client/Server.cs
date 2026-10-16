using System.Net;
using System.Text;
using System.Linq;

public static class ReportServer
{
    public static async Task RunAsync()
    {
        try
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:8123/");
            listener.Start();
            Console.WriteLine("Слушаю http://127.0.0.1:8123/");

            while (true)
            {
                var ctx = await listener.GetContextAsync();
                _ = Task.Run(() => Handle(ctx));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("[ReportServer] Ошибка: " + e.Message);
        }
    }

    static void Handle(HttpListenerContext ctx)
    {
        try
        {
            var request = ctx.Request;
            var response = ctx.Response;

            string nick = request.QueryString["Nick"] ?? "";
            string balance = request.QueryString["balance"] ?? "";
            string rodDamageRaw = request.QueryString["RodDamage"] ?? "";

            int[] rodDamage = rodDamageRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out int v) ? v : -1)
                .Where(v => v >= 0)
                .ToArray();

            Console.WriteLine($"Получено: Nick='{nick}', balance='{balance}', RodDamage=[{string.Join(", ", rodDamage)}]");

            byte[] buffer = Encoding.UTF8.GetBytes("OK");
            response.StatusCode = 200;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("[ReportServer] Ошибка обработки: " + e.Message);
        }
    }
}