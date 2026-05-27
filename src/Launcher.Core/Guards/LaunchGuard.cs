using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Core.Guards;

public static class LaunchGuard
{
    public static LaunchGuardResult Validate(LaunchProfile profile)
    {
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.ModelPath)
            || profile.ModelPath.Equals("модель не выбрана", StringComparison.OrdinalIgnoreCase))
        {
            messages.Add("Выберите модель перед запуском.");
        }

        if (profile.Mode == LaunchMode.Agent && string.IsNullOrWhiteSpace(profile.ProjectPath))
        {
            messages.Add("Для проектного режима нужна папка проекта.");
        }

        if (profile.Port <= 0 || profile.Port > 65535)
        {
            messages.Add("Порт должен быть в диапазоне 1-65535.");
        }

        return new LaunchGuardResult(messages.Count == 0, messages);
    }
}
