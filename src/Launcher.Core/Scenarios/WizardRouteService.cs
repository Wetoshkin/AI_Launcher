namespace Launcher.Core.Scenarios;

public sealed record LaunchScenario(
    LaunchMode Mode,
    AgentKind Agent,
    RuntimeKind Runtime);

public static class WizardRouteService
{
    public static IReadOnlyList<WizardStep> Build(LaunchScenario scenario)
    {
        var steps = new List<WizardStep> { WizardStep.Mode };

        if (scenario.Mode == LaunchMode.Agent)
        {
            steps.Add(WizardStep.Project);
            steps.Add(WizardStep.Agent);
            steps.Add(WizardStep.Model);
            steps.Add(WizardStep.Runtime);
            steps.Add(WizardStep.KvMtpContext);
            steps.Add(WizardStep.AgentOptions);
        }
        else
        {
            steps.Add(WizardStep.Model);
            steps.Add(WizardStep.Runtime);
            steps.Add(WizardStep.Port);
            steps.Add(WizardStep.KvMtpContext);
        }

        steps.Add(WizardStep.Review);
        steps.Add(WizardStep.Launch);
        return steps;
    }
}
