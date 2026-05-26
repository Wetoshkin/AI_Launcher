namespace Launcher.Runtimes.Ollama;

public sealed record RuntimeCheck(string Name, bool Success, string Message);
