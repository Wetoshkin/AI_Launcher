namespace Launcher.Core.Scenarios;

public sealed record LaunchWizardState(
    LaunchScenario Scenario,
    IReadOnlyList<WizardStep> Route,
    int CurrentIndex)
{
    public WizardStep CurrentStep => Route[CurrentIndex];

    public static LaunchWizardState ForScenario(LaunchScenario scenario) =>
        new(scenario, WizardRouteService.Build(scenario), CurrentIndex: 0);

    public LaunchWizardState Next() =>
        this with { CurrentIndex = Math.Min(CurrentIndex + 1, Route.Count - 1) };

    public LaunchWizardState Back() =>
        this with { CurrentIndex = Math.Max(CurrentIndex - 1, 0) };
}
