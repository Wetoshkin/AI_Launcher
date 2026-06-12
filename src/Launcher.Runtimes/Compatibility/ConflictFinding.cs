namespace Launcher.Runtimes.Compatibility;

public enum ConflictSeverity
{
    /// <summary>Полезная информация / совет, не мешает запуску.</summary>
    Info,
    /// <summary>Запустится, но может быть медленно/хуже по качеству.</summary>
    Warning,
    /// <summary>Так запускать нельзя — почти наверняка не заработает.</summary>
    Error
}

/// <summary>
/// Одна находка движка конфликтов: что не так, почему, и как починить — простым языком.
/// </summary>
public sealed record ConflictFinding(
    ConflictSeverity Severity,
    string Title,
    string Explanation,
    string SuggestedFix);
