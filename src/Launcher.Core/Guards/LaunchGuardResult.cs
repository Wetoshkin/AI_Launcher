namespace Launcher.Core.Guards;

public sealed record LaunchGuardResult(bool CanLaunch, IReadOnlyList<string> Messages);
