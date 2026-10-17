using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;

public static class PipeProtocol
{
    private const int MaxPayloadBytes = 100 * 1024 * 1024; // 100 МБ sanity-check


    public static async Task WriteStringArrayAsync(
        Stream stream, string[] values, CancellationToken ct = default)
    {
        // 1. Сериализуем массив в UTF-8 JSON. GetBytes → byte[]
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(values);

        // 2. Пишем 4-байтовую длину (little-endian)
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header, ct);

        // 3. Пишем сам JSON
        await stream.WriteAsync(payload, ct);

        // 4. Сбрасываем буфер — иначе данные могут «зависнуть»
        await stream.FlushAsync(ct);
    }

    public static async Task<string[]?> ReadStringArrayAsync(
        Stream stream, CancellationToken ct = default)
    {
        // 1. Читаем 4 байта длины. null → соединение закрыто
        var header = new byte[4];
        if (!await ReadExactlyAsync(stream, header, ct))
            return null;

        int length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length < 0 || length > MaxPayloadBytes)
            throw new InvalidDataException($"Некорректная длина пакета: {length}");

        // 2. Читаем ровно length байт
        var payload = new byte[length];
        if (!await ReadExactlyAsync(stream, payload, ct))
            return null;

        // 3. Десериализуем обратно в string[]
        return JsonSerializer.Deserialize<string[]>(payload);
    }

    // Вспомогательный метод — гарантированно заполняет буфер целиком
    private static async Task<bool> ReadExactlyAsync(
        Stream stream, byte[] buffer, CancellationToken ct)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset), ct);
            if (read == 0) return false; // конец потока = соединение закрыто
            offset += read;
        }
        return true;
    }
}