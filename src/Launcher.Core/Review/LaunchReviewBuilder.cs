using System.Globalization;
using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Core.Review;

public static class LaunchReviewBuilder
{
    public static LaunchReview Build(LaunchProfile profile)
    {
        var lines = new List<string>
        {
            $"Профиль: {profile.Name}",
            $"Режим: {ModeLabel(profile.Mode)}",
            $"Агент: {AgentLabel(profile.Agent)}",
            $"Runtime: {RuntimeLabel(profile.Runtime)}",
            $"Модель: {profile.ModelPath}",
            string.Create(CultureInfo.InvariantCulture, $"Контекст: {profile.ContextTokens:N0} токенов").Replace(",", " "),
            $"Порт: {profile.Port}",
            $"Anti-loop preset: {profile.AntiLoopPresetId}"
        };

        if (!string.IsNullOrWhiteSpace(profile.ProjectPath))
        {
            lines.Insert(2, $"Проект: {profile.ProjectPath}");
        }

        if (profile.Mtp is { Enabled: true } mtp)
        {
            var specType = string.IsNullOrWhiteSpace(mtp.SpeculativeType)
                ? "draft-mtp"
                : mtp.SpeculativeType;
            var draftMin = mtp.DraftMinTokens?.ToString(CultureInfo.InvariantCulture) ?? "не задан";
            var draftMax = mtp.DraftTokens?.ToString(CultureInfo.InvariantCulture) ?? "не задан";
            lines.Add($"Speculative decoding: {specType}, draft min/max: {draftMin}/{draftMax}");
        }

        return new LaunchReview(lines);
    }

    private static string ModeLabel(LaunchMode mode) => mode switch
    {
        LaunchMode.Agent => "проект",
        LaunchMode.Endpoint => "сервер",
        _ => mode.ToString()
    };

    private static string AgentLabel(AgentKind agent) => agent switch
    {
        AgentKind.None => "нет",
        AgentKind.OpenCode => "OpenCode",
        AgentKind.Kilo => "Kilo",
        AgentKind.Claw => "ClawCode",
        AgentKind.Aider => "Aider",
        AgentKind.Pi => "PI",
        _ => agent.ToString()
    };

    private static string RuntimeLabel(RuntimeKind runtime) => runtime switch
    {
        RuntimeKind.Ollama => "Ollama",
        RuntimeKind.LlamaCpp => "llama.cpp",
        RuntimeKind.LlamaCppTurboQuant => "llama.cpp TurboQuant",
        RuntimeKind.LlamaCppMtp => "llama.cpp MTP",
        _ => runtime.ToString()
    };
}
