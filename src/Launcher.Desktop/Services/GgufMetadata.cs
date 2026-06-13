using System;
using System.IO;
using System.Text;

namespace Launcher.Desktop.Services;

/// <summary>Извлечённые из GGUF сведения о модели (best-effort).</summary>
public sealed record GgufInfo(int? ContextLength, string? ChatTemplate, string? Architecture);

/// <summary>
/// Минимальный читатель метаданных GGUF: «родной» контекст, шаблон чата (для определения
/// поддержки инструментов) и архитектура. Best-effort: при любой ошибке поля = null.
/// </summary>
public static class GgufMetadata
{
    private const long MaxScanBytes = 96L * 1024 * 1024; // не читаем больше ~96 МБ заголовка

    public static int? ReadContextLength(string path) => ReadInfo(path).ContextLength;

    public static GgufInfo ReadInfo(string path)
    {
        int? context = null;
        string? chatTemplate = null;
        string? architecture = null;

        try
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var magic = br.ReadBytes(4);
            if (magic.Length < 4 || magic[0] != (byte)'G' || magic[1] != (byte)'G' || magic[2] != (byte)'U' || magic[3] != (byte)'F')
            {
                return new GgufInfo(null, null, null);
            }

            br.ReadUInt32();                  // version
            br.ReadUInt64();                  // tensor_count
            var kvCount = br.ReadUInt64();

            for (ulong i = 0; i < kvCount; i++)
            {
                if (fs.Position > MaxScanBytes)
                {
                    break;
                }

                var key = ReadString(br);
                var type = br.ReadUInt32();
                var value = ReadValue(br, type);

                if (value is long l && l > 0 && key.EndsWith(".context_length", StringComparison.OrdinalIgnoreCase))
                {
                    context = (int)Math.Min(l, int.MaxValue);
                }
                else if (value is string s)
                {
                    if (key.Equals("tokenizer.chat_template", StringComparison.OrdinalIgnoreCase))
                    {
                        chatTemplate = s;
                    }
                    else if (key.Equals("general.architecture", StringComparison.OrdinalIgnoreCase))
                    {
                        architecture = s;
                    }
                }

                if (context is not null && chatTemplate is not null)
                {
                    break; // главное уже есть
                }
            }
        }
        catch
        {
            // повреждённый/нестандартный файл
        }

        return new GgufInfo(context, chatTemplate, architecture);
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

    /// <summary>Читает значение по типу; целые → long, строки → string, остальное → null (но пропускает).</summary>
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
            case 8: return ReadString(br);             // string
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
