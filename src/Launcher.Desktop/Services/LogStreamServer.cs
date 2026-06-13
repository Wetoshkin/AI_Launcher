using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Desktop.Services;

/// <summary>
/// Лёгкий локальный сервер логов: отдаёт веб-страницу-вьюер и HTTP API (/api/logs, /api/status).
/// Браузер на http://127.0.0.1:PORT смотрит логи llama-server в реальном времени (через опрос).
/// Реализован на TcpListener — работает без прав администратора.
/// </summary>
public sealed class LogStreamServer
{
    private readonly object _lock = new();
    private readonly List<string> _lines = new();
    private long _baseIndex;            // индекс первой строки в буфере
    private const int MaxLines = 1000;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private string _model = string.Empty;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }

    /// <summary>Случайный токен сессии в пути — знания порта недостаточно для доступа к логам.</summary>
    public string Token { get; private set; } = string.Empty;

    public string Url => $"http://127.0.0.1:{Port}/{Token}/";

    public void SetModel(string model) => _model = model;

    public bool Start(int port)
    {
        Stop();
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Port = port;
            Token = System.Convert.ToHexString(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            IsRunning = true;
            _cts = new CancellationTokenSource();
            _ = AcceptLoopAsync(_listener, _cts.Token);
            return true;
        }
        catch
        {
            IsRunning = false;
            return false;
        }
    }

    public void Stop()
    {
        IsRunning = false;
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _listener = null;
        _cts = null;
    }

    public void Append(string line)
    {
        lock (_lock)
        {
            _lines.Add(line);
            if (_lines.Count > MaxLines)
            {
                var remove = _lines.Count - MaxLines;
                _lines.RemoveRange(0, remove);
                _baseIndex += remove;
            }
        }
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(ct);
            }
            catch
            {
                break;
            }

            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using (client)
            await using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(requestLine))
                {
                    return;
                }

                // читаем заголовки, перехватываем Host
                var host = string.Empty;
                string? header;
                while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync()))
                {
                    if (header.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                    {
                        host = header[5..].Trim();
                    }
                }

                var parts = requestLine.Split(' ');
                var path = parts.Length > 1 ? parts[1] : "/";

                string status;
                string contentType;
                string body;

                if (!IsAllowedHost(host))
                {
                    // защита от DNS-rebinding: принимаем только loopback-хост
                    (status, contentType, body) = ("403 Forbidden", "text/plain", "forbidden");
                }
                else if (!TryStripToken(path, out var subPath))
                {
                    // без верного токена сессии доступа нет
                    (status, contentType, body) = ("404 Not Found", "text/plain", "not found");
                }
                else
                {
                    (contentType, body) = Route(subPath);
                    status = "200 OK";
                }

                var bytes = Encoding.UTF8.GetBytes(body);
                var head = $"HTTP/1.1 {status}\r\n" +
                           $"Content-Type: {contentType}; charset=utf-8\r\n" +
                           $"Content-Length: {bytes.Length}\r\n" +
                           "X-Content-Type-Options: nosniff\r\n" +
                           "Connection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(head));
                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
            }
        }
        catch
        {
            // соединение закрыто/ошибка — игнорируем
        }
    }

    private bool IsAllowedHost(string host) =>
        host.Equals($"127.0.0.1:{Port}", StringComparison.OrdinalIgnoreCase)
        || host.Equals($"localhost:{Port}", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

    private bool TryStripToken(string path, out string subPath)
    {
        subPath = "/";
        var prefix = "/" + Token;
        if (Token.Length == 0)
        {
            return false;
        }

        if (path == prefix)
        {
            return true;
        }

        if (path.StartsWith(prefix + "/", StringComparison.Ordinal))
        {
            subPath = path[prefix.Length..];
            return true;
        }

        return false;
    }

    private (string contentType, string body) Route(string path)
    {
        if (path.StartsWith("/api/logs", StringComparison.OrdinalIgnoreCase))
        {
            var since = 0L;
            var q = path.IndexOf("since=", StringComparison.OrdinalIgnoreCase);
            if (q >= 0)
            {
                long.TryParse(new string(path[(q + 6)..].TakeWhile(char.IsDigit).ToArray()), out since);
            }

            long next;
            string[] slice;
            lock (_lock)
            {
                var from = (int)Math.Max(0, since - _baseIndex);
                slice = from >= _lines.Count ? Array.Empty<string>() : _lines.Skip(from).ToArray();
                next = _baseIndex + _lines.Count;
            }

            var json = "{\"next\":" + next + ",\"lines\":[" +
                       string.Join(",", slice.Select(JsonString)) + "]}";
            return ("application/json", json);
        }

        if (path.StartsWith("/api/status", StringComparison.OrdinalIgnoreCase))
        {
            int count;
            lock (_lock) { count = _lines.Count; }
            return ("application/json",
                "{\"running\":" + (IsRunning ? "true" : "false") +
                ",\"port\":" + Port + ",\"model\":" + JsonString(_model) + ",\"lines\":" + count + "}");
        }

        return ("text/html", Html());
    }

    private static string JsonString(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                    else sb.Append(ch);
                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static string Html() => """
        <!doctype html><html lang="ru"><head><meta charset="utf-8">
        <title>AI Launcher Studio — лог сервера</title>
        <style>
          body{margin:0;background:#1b1b1d;color:#e8e8ea;font:13px/1.5 Consolas,Menlo,monospace}
          header{padding:10px 16px;background:#2a2a2d;border-bottom:1px solid #3a3a3c;display:flex;gap:12px;align-items:center}
          .dot{width:9px;height:9px;border-radius:50%;background:#37a463}
          #log{padding:12px 16px;white-space:pre-wrap;word-break:break-word}
          .muted{color:#9a9aa0}
        </style></head><body>
        <header><span class="dot"></span><b>AI Launcher Studio</b><span class="muted" id="st">лог сервера llama.cpp</span></header>
        <div id="log"></div>
        <script>
          let since=0; const log=document.getElementById('log'); const st=document.getElementById('st');
          async function tick(){
            try{
              const r=await fetch('api/logs?since='+since); const d=await r.json();
              since=d.next;
              if(d.lines.length){ log.textContent+=d.lines.join('\n')+'\n'; window.scrollTo(0,document.body.scrollHeight); }
              st.textContent='подключено · строк: '+since;
            }catch(e){ st.textContent='нет связи с приложением'; }
          }
          setInterval(tick,700); tick();
        </script></body></html>
        """;
}
