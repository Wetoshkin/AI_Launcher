using Launcher.Core.LaunchPlans;

namespace Launcher.Runtimes.Processes;

public sealed class LaunchPlanProcessService(IProcessStarter processStarter)
{
    public Task<ProcessStartResult> StartAsync(
        LaunchPlan plan,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        return processStarter.StartAsync(
            new ProcessStartRequest(plan.Executable, plan.Arguments, plan.Environment, workingDirectory),
            cancellationToken);
    }
}
