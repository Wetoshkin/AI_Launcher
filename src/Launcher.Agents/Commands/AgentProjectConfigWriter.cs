using System.Text.Json;
using System.Text.Json.Nodes;
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Commands;

public sealed class AgentProjectConfigWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<AgentProjectConfigResult> WriteAsync(
        AgentLaunchRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.ProjectPath);
        return request.Agent switch
        {
            AgentKind.Kilo => await WriteKiloAsync(request, cancellationToken),
            AgentKind.OpenCode => await WriteOpenCodeAsync(request, cancellationToken),
            _ => new AgentProjectConfigResult(false, null, $"Для {request.Agent} проектный конфиг пока не требуется.")
        };
    }

    private static async Task<AgentProjectConfigResult> WriteKiloAsync(
        AgentLaunchRequest request,
        CancellationToken cancellationToken)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        var path = Path.Combine(request.ProjectPath, "kilo.jsonc");
        var root = new JsonObject
        {
            ["provider"] = new JsonObject
            {
                ["type"] = "openai-compatible",
                ["baseUrl"] = request.BaseUrl,
                ["apiKey"] = "local",
                ["model"] = request.ProviderModel,
                ["tools"] = false
            }
        };
        await File.WriteAllTextAsync(path, root.ToJsonString(JsonOptions), cancellationToken);
        return new AgentProjectConfigResult(true, path, "kilo.jsonc обновлён.");
    }

    private static async Task<AgentProjectConfigResult> WriteOpenCodeAsync(
        AgentLaunchRequest request,
        CancellationToken cancellationToken)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        var path = Path.Combine(request.ProjectPath, "opencode.json");
        var root = new JsonObject
        {
            ["provider"] = new JsonObject
            {
                ["local"] = new JsonObject
                {
                    ["npm"] = "@ai-sdk/openai-compatible",
                    ["name"] = "local",
                    ["options"] = new JsonObject
                    {
                        ["baseURL"] = request.BaseUrl,
                        ["apiKey"] = "local"
                    },
                    ["models"] = new JsonObject
                    {
                        [request.ProviderModel] = new JsonObject
                        {
                            ["tools"] = new JsonObject
                            {
                                ["disabled"] = true
                            }
                        }
                    }
                }
            },
            ["model"] = request.ProviderModel
        };
        await File.WriteAllTextAsync(path, root.ToJsonString(JsonOptions), cancellationToken);
        return new AgentProjectConfigResult(true, path, "opencode.json обновлён.");
    }
}
