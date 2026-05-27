namespace Launcher.Core.LaunchPlans;

public static class LaunchPlanFormatter
{
    public static LaunchPlanPreview Format(LaunchPlan plan)
    {
        var commandLine = string.Join(" ", new[] { Quote(plan.Executable) }.Concat(plan.Arguments.Select(Quote)));
        var environmentLines = plan.Environment
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => $"{item.Key}={item.Value}")
            .ToArray();

        return new LaunchPlanPreview(commandLine, environmentLines);
    }

    private static string Quote(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        return value.Any(char.IsWhiteSpace)
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;
    }
}
