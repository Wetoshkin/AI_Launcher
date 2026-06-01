using Launcher.Core.LaunchPlans;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;

namespace Launcher.Runtimes.Startup;

public sealed class RuntimeStartCoordinator(
    IPortInspector portInspector,
    IPortReleaser portReleaser,
    IProcessStarter processStarter,
    IEndpointHealthClient? endpointHealthClient = null)
{
    public async Task<RuntimeStartResult> StartAsync(
        LaunchPlan plan,
        int port,
        string? workingDirectory,
        CancellationToken cancellationToken,
        Action<string>? outputReceived = null)
    {
        var messages = new List<string>();
        var portOwner = await portInspector.InspectAsync(port, cancellationToken);
        if (portOwner is not null)
        {
            if (!portOwner.IsLikelyLlamaServer)
            {
                return new RuntimeStartResult(
                    Started: false,
                    ProcessId: null,
                    Messages: [$"Порт {port} занят процессом {portOwner.ProcessName}. Запуск остановлен."]);
            }

            var release = await portReleaser.ReleaseIfSafeAsync(portOwner, cancellationToken);
            messages.Add(release.Message);
            if (!release.Released)
            {
                return new RuntimeStartResult(false, null, messages);
            }
        }

        var result = await processStarter.StartAsync(
            new ProcessStartRequest(plan.Executable, plan.Arguments, plan.Environment, workingDirectory, outputReceived),
            cancellationToken);
        messages.Add($"Процесс запущен. PID: {result.ProcessId}.");
        if (endpointHealthClient is not null && ShouldWaitForEndpoint(plan))
        {
            var health = await endpointHealthClient
                .WaitUntilReadyAsync($"http://127.0.0.1:{port}/v1", Attempts: 30, Delay: TimeSpan.FromSeconds(1), cancellationToken);
            messages.Add(health.Message);
            if (!health.IsReady)
            {
                return new RuntimeStartResult(false, result.ProcessId, messages);
            }
        }

        return new RuntimeStartResult(true, result.ProcessId, messages);
    }

    private static bool ShouldWaitForEndpoint(LaunchPlan plan) =>
        Path.GetFileNameWithoutExtension(plan.Executable)
            .Equals("llama-server", StringComparison.OrdinalIgnoreCase);
}
