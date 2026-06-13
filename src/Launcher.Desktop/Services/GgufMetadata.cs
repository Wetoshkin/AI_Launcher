using System;
using System.IO;
using System.Text;

namespace Launcher.Desktop.Services;

/// <summary>
/// Минимальный читатель метаданных GGUF: достаёт «родной» размер контекста модели
/// (ключ *.context_length). Best-effort: при любой ошибке возвращает null.
/// </summary>
public static class GgufMetadata
{
    private const long MaxScanBytes = 64L * 1024 * 1024; // не читаем больше 64 МБ заголовка

    public static int? ReadContextLength(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var magic = br.ReadBytes(4);
            if (magic.Length < 4 || magic[0] != (byte)'G' || magic[1] != (byte)'G' || magic[2] != (byte)'U' || magic[3] != (byte)'F')
            {
                return null;
            }

            br.ReadUInt32();                  // version
            br.ReadUInt64();                  // tensor_count
            var kvCount = br.ReadUInt64();

            for (ulong i = 0; i < kvCount; i++)
            {
                if (fs.Position > MaxScanBytes)
                {
                    return null;
                }

                var key = ReadString(br);
                var type = br.ReadUInt32();
                var value = ReadValue(br, type);

                if (key.EndsWith(".context_length", StringComparison.OrdinalIgnoreCase) && value is long l && l > 0)
                {
                    return (int)Math.Min(l, int.MaxValue);
                }
            }
        }
        catch
        {
            // повреждённый/нестандартный файл — просто нет данных
        }

        return null;
    }

    private static string ReadString(BinaryReader br)
    {
        var len = br.ReadUInt64();
        if (len > 16 * 1024 * 1024)
        {
            throw new InvalidDataException("string too long");
        }

        var bytes = br.ReadBytes((int)len);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Читает значение по типу; для целочисленных возвращает long, иначе null (но всё равно пропускает).</summary>
    private static object? ReadValue(BinaryReader br, uint type)
    {
        switch (type)
        {
            case 0: return (long)br.ReadByte();        // uint8
            case 1: return (long)br.ReadSByte();       // int8
            case 2: return (long)br.ReadUInt16();      // uint16
            case 3: return (long)br.ReadInt16();       // int16
            case 4: return (long)br.ReadUInt32();      // uint32
            case 5: return (long)br.ReadInt32();       // int32
            case 6: br.ReadSingle(); return null;      // float32
            case 7: return (long)br.ReadByte();        // bool
            case 8: ReadString(br); return null;       // string
            case 10: return (long)br.ReadUInt64();     // uint64
            case 11: return br.ReadInt64();            // int64
            case 12: br.ReadDouble(); return null;     // float64
            case 9:                                     // array
                var elemType = br.ReadUInt32();
                var count = br.ReadUInt64();
                for (ulong i = 0; i < count; i++)
                {
                    ReadValue(br, elemType);
                }

                return null;
            default:
                throw new InvalidDataException($"unknown gguf type {type}");
        }
    }
}
