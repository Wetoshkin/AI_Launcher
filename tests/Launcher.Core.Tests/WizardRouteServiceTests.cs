using Launcher.Core.Scenarios;

namespace Launcher.Core.Tests;

public sealed class WizardRouteServiceTests
{
    [Fact]
    public void AgentRouteIncludesProjectAgentModelRuntimeTuningReviewLaunch()
    {
        var route = WizardRouteService.Build(new LaunchScenario(
            LaunchMode.Agent,
            AgentKind.Kilo,
            RuntimeKind.LlamaCppTurboQuant));

        Assert.Equal(new[]
        {
            WizardStep.Mode,
            WizardStep.Project,
            WizardStep.Agent,
            WizardStep.Model,
            WizardStep.Runtime,
            WizardStep.KvMtpContext,
            WizardStep.AgentOptions,
            WizardStep.Review,
            WizardStep.Launch
        }, route);
    }

    [Fact]
    public void EndpointRouteDoesNotIncludeProjectOrAgentOptions()
    {
        var route = WizardRouteService.Build(new LaunchScenario(
            LaunchMode.Endpoint,
            AgentKind.None,
            RuntimeKind.LlamaCppMtp));

        Assert.Equal(new[]
        {
            WizardStep.Mode,
            WizardStep.Model,
            WizardStep.Runtime,
            WizardStep.Port,
            WizardStep.KvMtpContext,
            WizardStep.Review,
            WizardStep.Launch
        }, route);
    }
}
