namespace Launcher.Core.LaunchPlans;

public sealed record LaunchPlan(
    string Executable,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Environment);
