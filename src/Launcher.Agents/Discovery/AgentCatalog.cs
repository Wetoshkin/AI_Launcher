using Launcher.Core.Scenarios;

namespace Launcher.Agents.Discovery;

public enum AgentInstallMethod
{
    /// <summary>Ставится глобально через npm (нужен Node.js).</summary>
    Npm,

    /// <summary>Ставится через pip (нужен Python).</summary>
    Pip,

    /// <summary>Автоустановки нет — открываем страницу с инструкцией.</summary>
    Manual
}

/// <summary>Справочные сведения об агенте: бинарник, способ установки, ссылка, примечание.</summary>
public sealed record AgentInfo(
    AgentKind Kind,
    string Display,
    string Executable,
    AgentInstallMethod Install,
    string? Package,
    string DocsUrl,
    string Note,
    string? InstallExtraArgs = null);

/// <summary>Единый каталог поддерживаемых кодинг-агентов.</summary>
public static class AgentCatalog
{
    public static IReadOnlyList<AgentKind> Supported { get; } = new[]
    {
        AgentKind.OpenCode,
        AgentKind.ClaudeCode,
        AgentKind.Crush,
        AgentKind.Aider,
        AgentKind.Goose,
        AgentKind.Kilo,
        AgentKind.Pi,
    };

    public static AgentInfo Get(AgentKind kind) => kind switch
    {
        AgentKind.OpenCode => new(kind, "OpenCode", "opencode",
            AgentInstallMethod.Npm, "opencode-ai", "https://opencode.ai",
            "Терминальный кодинг-агент. Ставится через npm (нужен Node.js)."),

        AgentKind.ClaudeCode => new(kind, "Claude Code", "claude",
            AgentInstallMethod.Npm, "@anthropic-ai/claude-code", "https://docs.anthropic.com/en/docs/claude-code",
            "Официальный CLI от Anthropic. Работает с локальной моделью напрямую через Anthropic Messages API движка llama.cpp (нужен свежий движок из вкладки «Среды»)."),

        AgentKind.Crush => new(kind, "Crush", "crush",
            AgentInstallMethod.Npm, "@charmland/crush", "https://github.com/charmbracelet/crush",
            "TUI-агент от Charm. Ставится через npm (нужен Node.js)."),

        AgentKind.Aider => new(kind, "Aider", "aider",
            AgentInstallMethod.Pip, "aider-chat", "https://aider.chat",
            "Парное программирование в терминале. Ставится через pip (нужен Python)."),

        AgentKind.Goose => new(kind, "Goose", "goose",
            AgentInstallMethod.Manual, null, "https://block.github.io/goose/docs/getting-started/installation",
            "CLI от Block. На Windows ставится отдельно — откройте инструкцию на сайте."),

        AgentKind.Kilo => new(kind, "Kilo Code", "kilo",
            AgentInstallMethod.Manual, null, "https://kilocode.ai",
            "Это расширение для VS Code / JetBrains — работает внутри редактора, не из терминала."),

        AgentKind.Pi => new(kind, "Pi", "pi",
            AgentInstallMethod.Npm, "@earendil-works/pi-coding-agent", "https://pi.dev/docs/latest",
            "Кодинг-агент Pi. Ставится через npm (нужен Node.js). После запуска выберите модель командой /model → local.",
            InstallExtraArgs: "--ignore-scripts"),

        AgentKind.Claw => new(kind, "Claw", "claw",
            AgentInstallMethod.Manual, null, "https://github.com",
            "Экспериментальный агент."),

        _ => new(kind, kind.ToString(), kind.ToString().ToLowerInvariant(),
            AgentInstallMethod.Manual, null, "https://github.com", string.Empty),
    };
}
