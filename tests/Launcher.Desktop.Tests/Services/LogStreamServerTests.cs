using System.Net.Http;
using System.Threading.Tasks;
using Launcher.Desktop.Services;

namespace Launcher.Desktop.Tests.Services;

public class LogStreamServerTests
{
    [Fact]
    public async Task Serves_logs_status_and_viewer()
    {
        var server = new LogStreamServer();
        var port = 18793;
        Assert.True(server.Start(port));
        try
        {
            server.SetModel("local/test");
            server.Append("first line");
            server.Append("second line");

            using var http = new HttpClient { BaseAddress = new System.Uri($"http://127.0.0.1:{port}") };
            var t = server.Token;
            Assert.False(string.IsNullOrEmpty(t));

            var logs = await http.GetStringAsync($"/{t}/api/logs?since=0");
            Assert.Contains("first line", logs);
            Assert.Contains("second line", logs);
            Assert.Contains("\"next\":2", logs);

            var status = await http.GetStringAsync($"/{t}/api/status");
            Assert.Contains("\"running\":true", status);
            Assert.Contains("local/test", status);

            var page = await http.GetStringAsync($"/{t}/");
            Assert.Contains("AI Launcher Studio", page);

            // без токена — нет доступа к логам
            var noToken = await http.GetAsync("/api/logs?since=0");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, noToken.StatusCode);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public void Start_returns_false_on_busy_port()
    {
        var a = new LogStreamServer();
        var port = 18794;
        Assert.True(a.Start(port));
        try
        {
            var b = new LogStreamServer();
            Assert.False(b.Start(port));
        }
        finally
        {
            a.Stop();
        }
    }
}
