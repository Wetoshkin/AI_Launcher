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
            AgentKind.Crush => await WriteCrushAsync(request, cancellationToken),
            AgentKind.Pi => await WritePiAsync(request, cancellationToken),
            _ => new AgentProjectConfigResult(false, null, $"Для {request.Agent} проектный конфиг пока не требуется.")
        };
    }

    private static async Task<AgentProjectConfigResult> WritePiAsync(
        AgentLaunchRequest request,
        CancellationToken cancellationToken)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        var dir = Path.Combine(request.ProjectPath, ".pi", "extensions");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "al-launcher.ts");

        var modelId = StripLocalProviderPrefix(request.ProviderModel);
        var baseUrl = request.BaseUrl.Replace("\"", "\\\"");
        var safeModel = modelId.Replace("\"", "\\\"");

        var extension =
            "import type { ExtensionAPI } from \"@earendil-works/pi-coding-agent\";\n\n" +
            "// Сгенерировано AI Launcher Studio: локальный OpenAI-совместимый провайдер.\n" +
            "export default function (pi: ExtensionAPI) {\n" +
            "  pi.registerProvider(\"local\", {\n" +
            $"    baseUrl: \"{baseUrl}\",\n" +
            "    apiKey: \"local\",\n" +
            "    api: \"openai-completions\",\n" +
            "    models: [\n" +
            "      {\n" +
            $"        id: \"{safeModel}\",\n" +
            $"        name: \"{safeModel}\",\n" +
            "        reasoning: false,\n" +
            "        input: [\"text\"],\n" +
            "        cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },\n" +
            "        contextWindow: 128000,\n" +
            "        maxTokens: 4096\n" +
            "      }\n" +
            "    ]\n" +
            "  });\n" +
            "}\n";

        await File.WriteAllTextAsync(path, extension, cancellationToken);
        return new AgentProjectConfigResult(true, path, ".pi/extensions/al-launcher.ts создан (провайдер «local»).");
    }

    private static async Task<AgentProjectConfigResult> WriteCrushAsync(
        AgentLaunchRequest request,
        CancellationToken cancellationToken)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        var path = Path.Combine(request.ProjectPath, "crush.json");
        var modelId = StripLocalProviderPrefix(request.ProviderModel);
        var root = new JsonObject
        {
            ["$schema"] = "https://charm.land/crush.json",
            ["providers"] = new JsonObject
            {
                ["local"] = new JsonObject
                {
                    ["type"] = "openai",
                    ["base_url"] = request.BaseUrl,
                    ["api_key"] = "local",
                    ["models"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = modelId,
                            ["name"] = modelId
                        }
                    }
                }
            }
        };
        await File.WriteAllTextAsync(path, root.ToJsonString(JsonOptions), cancellationToken);
        return new AgentProjectConfigResult(true, path, "crush.json обновлён.");
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
        var localModelId = StripLocalProviderPrefix(request.ProviderModel);
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
                        [localModelId] = new JsonObject
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

    private static string StripLocalProviderPrefix(string providerModel)
    {
        const string prefix = "local/";
        return providerModel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? providerModel[prefix.Length..]
            : providerModel;
    }
}
