using Launcher.Core.LaunchPlans;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;

namespace Launcher.Runtimes.Startup;

public sealed class RuntimeStartCoordinator(
    IPortInspector portInspector,
    IPortReleaser portReleaser,
    IProcessStarter processStarter)
{
    public async Task<RuntimeStartResult> StartAsync(
        LaunchPlan plan,
        int port,
        string? workingDirectory,
        CancellationToken cancellationToken)
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
            new ProcessStartRequest(plan.Executable, plan.Arguments, plan.Environment, workingDirectory),
            cancellationToken);
        messages.Add($"Процесс запущен. PID: {result.ProcessId}.");

        return new RuntimeStartResult(true, result.ProcessId, messages);
    }
}
