using System;
using System.Linq;
using Launcher.Agents.Commands;
using Launcher.Core.Decoding;
using Launcher.Core.LaunchPlans;
using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.ViewModels;

public static class LaunchPlanComposer
{
    public static LaunchPlan BuildServerPlan(
        LaunchProfile profile,
        DecodingPreset decodingPreset,
        LlamaRuntimeInfo? runtime)
    {
        var plan = LlamaServerCommandBuilder.Build(profile, decodingPreset);
        return runtime is null
            ? plan
            : plan with { Executable = runtime.ExecutablePath };
    }

    public static LaunchPlanPreview BuildAgentScenarioPreview(
        LaunchProfile profile,
        DecodingPreset decodingPreset,
        LlamaRuntimeInfo? runtime)
    {
        var serverProfile = profile with { Mode = LaunchMode.Endpoint, Agent = AgentKind.None };
        var serverPreview = LaunchPlanFormatter.Format(BuildServerPlan(serverProfile, decodingPreset, runtime));
        var agentPreview = LaunchPlanFormatter.Format(BuildAgentPlan(profile));
        var commandLine = string.Join(
            Environment.NewLine,
            $"SERVER: {serverPreview.CommandLine}",
            $"AGENT: {agentPreview.CommandLine}");
        var environmentLines = serverPreview.EnvironmentLines.Select(line => $"SERVER: {line}")
            .Concat(agentPreview.EnvironmentLines.Select(line => $"AGENT: {line}"))
            .ToArray();

        return new LaunchPlanPreview(commandLine, environmentLines);
    }

    public static AgentLaunchRequest BuildAgentRequest(LaunchProfile profile) => new(
        profile.Agent,
        profile.ProjectPath ?? "",
        LaunchProfileModelIds.ProviderModelId(profile),
        $"http://127.0.0.1:{profile.Port}/v1");

    public static LaunchPlan BuildAgentPlan(LaunchProfile profile) =>
        BuildAgentPlan(BuildAgentRequest(profile));

    public static LaunchPlan BuildAgentPlan(AgentLaunchRequest request)
    {
        IAgentCommandBuilder builder = request.Agent switch
        {
            AgentKind.OpenCode => new OpenCodeCommandBuilder(),
            AgentKind.Claw => new ClawCommandBuilder(),
            AgentKind.Aider => new AiderCommandBuilder(),
            _ => new KiloCommandBuilder()
        };

        return builder.Build(request);
    }
}
