namespace Launcher.Runtimes.Ports;

public sealed class WindowsPortInspector : IPortInspector
{
    private readonly ICommandRunner _commandRunner;

    public WindowsPortInspector()
        : this(new ProcessCommandRunner())
    {
    }

    public WindowsPortInspector(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken)
    {
        var script =
            "$c = Get-NetTCPConnection -LocalPort " + port + " -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1; " +
            "if ($null -eq $c) { '' } else { " +
            "$p = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue; " +
            "[pscustomobject]@{ LocalPort = $c.LocalPort; OwningProcess = $c.OwningProcess; ProcessName = $p.ProcessName; Path = $p.Path } | ConvertTo-Json -Compress }";
        var arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"";
        var json = await _commandRunner.RunAsync("powershell.exe", arguments, cancellationToken);
        return PortInspectorParser.ParsePowerShellJson(json, endpointResponds: false, loadedModelId: null);
    }
}
