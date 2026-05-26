using Launcher.Core.Scenarios;

namespace Launcher.Core.Tests;

public sealed class LaunchWizardStateTests
{
    [Fact]
    public void StartsAgentScenarioAtModeAndAdvancesThroughRoute()
    {
        var state = LaunchWizardState.ForScenario(new LaunchScenario(
            LaunchMode.Agent,
            AgentKind.Kilo,
            RuntimeKind.LlamaCppTurboQuant));

        Assert.Equal(WizardStep.Mode, state.CurrentStep);

        state = state.Next();

        Assert.Equal(WizardStep.Project, state.CurrentStep);
        Assert.Equal(1, state.CurrentIndex);
    }

    [Fact]
    public void BackDoesNotMoveBeforeFirstStep()
    {
        var state = LaunchWizardState.ForScenario(new LaunchScenario(
            LaunchMode.Endpoint,
            AgentKind.None,
            RuntimeKind.LlamaCppMtp));

        var previous = state.Back();

        Assert.Equal(WizardStep.Mode, previous.CurrentStep);
        Assert.Equal(0, previous.CurrentIndex);
    }
}
