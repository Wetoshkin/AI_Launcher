namespace Launcher.Runtimes.Ports;

public sealed class PortReleaseService(ICommandRunner commandRunner) : IPortReleaser
{
    public async Task<PortReleaseResult> ReleaseIfSafeAsync(
        PortOwnerInfo portOwner,
        CancellationToken cancellationToken)
    {
        if (!portOwner.IsLikelyLlamaServer)
        {
            return new PortReleaseResult(
                Released: false,
                Message: $"Порт {portOwner.Port} занят процессом {portOwner.ProcessName}. Автоматически останавливаем только llama-server.");
        }

        var script = $"Stop-Process -Id {portOwner.ProcessId} -Force -ErrorAction Stop";
        var arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"";
        await commandRunner.RunAsync("powershell.exe", arguments, cancellationToken);

        return new PortReleaseResult(
            Released: true,
            Message: $"Остановлен llama-server на порту {portOwner.Port}.");
    }
}
