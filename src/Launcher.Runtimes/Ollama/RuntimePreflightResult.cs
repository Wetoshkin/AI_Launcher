namespace Launcher.Runtimes.Ollama;

public sealed record RuntimePreflightResult(IReadOnlyList<RuntimeCheck> Checks)
{
    public bool Success => Checks.All(check => check.Success);

    public IReadOnlyList<string> Errors => Checks
        .Where(check => !check.Success)
        .Select(check => check.Message)
        .ToArray();
}
