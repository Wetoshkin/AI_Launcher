namespace Launcher.Core.LaunchPlans;

public sealed record LaunchPlanPreview(
    string CommandLine,
    IReadOnlyList<string> EnvironmentLines);
