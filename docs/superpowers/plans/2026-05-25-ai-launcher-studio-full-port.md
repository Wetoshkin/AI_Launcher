# AI Launcher Studio Full Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build AI Launcher Studio as a native Avalonia/C# app that fully ports the Python AI Launcher runtime logic into a modular, tested GUI product.

**Architecture:** Keep the existing Avalonia launcher buildable while introducing a new modular solution shape: `Launcher.Core`, `Launcher.Runtimes`, `Launcher.Models`, `Launcher.Agents`, `Launcher.Desktop`, and tests. The UI must request launch plans from domain services instead of building commands directly.

**Tech Stack:** .NET 8, C# 12, Avalonia 12, xUnit, System.Text.Json, HttpClient, PowerShell/Windows process APIs, Hugging Face Hub REST API, existing llama.cpp/Ollama command-line tools.

---

## Current Baseline

- Repository: `D:\AI\LlamaServerLauncherAvalonia`
- Solution: `llama-server-launcher-avalonia.sln`
- Current app project: `LlamaServerLauncher.csproj`
- Baseline build command: `dotnet build .\llama-server-launcher-avalonia.sln --no-restore`
- Baseline status after localization fix: build passes with 5 existing CS1998 warnings.

## Target File Structure

Create these projects and keep each responsibility focused:

- `src/Launcher.Core/Launcher.Core.csproj`: launch scenarios, profiles, settings contracts, wizard routes, parameter help, command plan models.
- `src/Launcher.Runtimes/Launcher.Runtimes.csproj`: port inspection, process management, Ollama client, llama.cpp client, runtime capability detection, VRAM/GPU probing.
- `src/Launcher.Models/Launcher.Models.csproj`: GGUF file catalog, model name parsing, Hugging Face search/download metadata, model filters and sorting.
- `src/Launcher.Agents/Launcher.Agents.csproj`: OpenCode/Kilo/Claw/Aider/PI command builders, CLI discovery, agent install metadata.
- `src/Launcher.Desktop/Launcher.Desktop.csproj`: future Avalonia app shell. During migration it coexists with current root app.
- `tests/Launcher.Core.Tests/Launcher.Core.Tests.csproj`
- `tests/Launcher.Runtimes.Tests/Launcher.Runtimes.Tests.csproj`
- `tests/Launcher.Models.Tests/Launcher.Models.Tests.csproj`
- `tests/Launcher.Agents.Tests/Launcher.Agents.Tests.csproj`

Keep the current root app compiling until the new desktop shell can replace it. Do not delete `MainWindow.axaml` or `MainViewModel.cs` until an equivalent screen exists in `Launcher.Desktop`.

---

### Task 1: Create Solution Skeleton and Test Projects

**Files:**
- Modify: `llama-server-launcher-avalonia.sln`
- Create: `src/Launcher.Core/Launcher.Core.csproj`
- Create: `src/Launcher.Runtimes/Launcher.Runtimes.csproj`
- Create: `src/Launcher.Models/Launcher.Models.csproj`
- Create: `src/Launcher.Agents/Launcher.Agents.csproj`
- Create: `tests/Launcher.Core.Tests/Launcher.Core.Tests.csproj`
- Create: `tests/Launcher.Runtimes.Tests/Launcher.Runtimes.Tests.csproj`
- Create: `tests/Launcher.Models.Tests/Launcher.Models.Tests.csproj`
- Create: `tests/Launcher.Agents.Tests/Launcher.Agents.Tests.csproj`

- [ ] **Step 1: Create class library projects**

Run:

```powershell
dotnet new classlib -n Launcher.Core -o src\Launcher.Core --framework net8.0
dotnet new classlib -n Launcher.Runtimes -o src\Launcher.Runtimes --framework net8.0
dotnet new classlib -n Launcher.Models -o src\Launcher.Models --framework net8.0
dotnet new classlib -n Launcher.Agents -o src\Launcher.Agents --framework net8.0
```

Expected: each command prints `The template "Class Library" was created successfully.`

- [ ] **Step 2: Create xUnit projects**

Run:

```powershell
dotnet new xunit -n Launcher.Core.Tests -o tests\Launcher.Core.Tests --framework net8.0
dotnet new xunit -n Launcher.Runtimes.Tests -o tests\Launcher.Runtimes.Tests --framework net8.0
dotnet new xunit -n Launcher.Models.Tests -o tests\Launcher.Models.Tests --framework net8.0
dotnet new xunit -n Launcher.Agents.Tests -o tests\Launcher.Agents.Tests --framework net8.0
```

Expected: each command prints `The template "xUnit Test Project" was created successfully.`

- [ ] **Step 3: Wire project references**

Run:

```powershell
dotnet sln .\llama-server-launcher-avalonia.sln add src\Launcher.Core\Launcher.Core.csproj
dotnet sln .\llama-server-launcher-avalonia.sln add src\Launcher.Runtimes\Launcher.Runtimes.csproj
dotnet sln .\llama-server-launcher-avalonia.sln add src\Launcher.Models\Launcher.Models.csproj
dotnet sln .\llama-server-launcher-avalonia.sln add src\Launcher.Agents\Launcher.Agents.csproj
dotnet sln .\llama-server-launcher-avalonia.sln add tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj
dotnet sln .\llama-server-launcher-avalonia.sln add tests\Launcher.Runtimes.Tests\Launcher.Runtimes.Tests.csproj
dotnet sln .\llama-server-launcher-avalonia.sln add tests\Launcher.Models.Tests\Launcher.Models.Tests.csproj
dotnet sln .\llama-server-launcher-avalonia.sln add tests\Launcher.Agents.Tests\Launcher.Agents.Tests.csproj
dotnet add src\Launcher.Runtimes\Launcher.Runtimes.csproj reference src\Launcher.Core\Launcher.Core.csproj
dotnet add src\Launcher.Models\Launcher.Models.csproj reference src\Launcher.Core\Launcher.Core.csproj
dotnet add src\Launcher.Agents\Launcher.Agents.csproj reference src\Launcher.Core\Launcher.Core.csproj
dotnet add tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj reference src\Launcher.Core\Launcher.Core.csproj
dotnet add tests\Launcher.Runtimes.Tests\Launcher.Runtimes.Tests.csproj reference src\Launcher.Runtimes\Launcher.Runtimes.csproj
dotnet add tests\Launcher.Models.Tests\Launcher.Models.Tests.csproj reference src\Launcher.Models\Launcher.Models.csproj
dotnet add tests\Launcher.Agents.Tests\Launcher.Agents.Tests.csproj reference src\Launcher.Agents\Launcher.Agents.csproj
```

Expected: each reference command reports the reference was added.

- [ ] **Step 4: Remove template placeholder classes**

Delete:

```text
src\Launcher.Core\Class1.cs
src\Launcher.Runtimes\Class1.cs
src\Launcher.Models\Class1.cs
src\Launcher.Agents\Class1.cs
tests\Launcher.Core.Tests\UnitTest1.cs
tests\Launcher.Runtimes.Tests\UnitTest1.cs
tests\Launcher.Models.Tests\UnitTest1.cs
tests\Launcher.Agents.Tests\UnitTest1.cs
```

- [ ] **Step 5: Verify solution builds**

Run:

```powershell
dotnet restore .\llama-server-launcher-avalonia.sln
dotnet build .\llama-server-launcher-avalonia.sln --no-restore
dotnet test .\llama-server-launcher-avalonia.sln --no-build
```

Expected: build succeeds. Test run succeeds with zero or discovered placeholder-free tests.

- [ ] **Step 6: Commit**

Run:

```powershell
git add .\llama-server-launcher-avalonia.sln .\src .\tests
git commit -m "chore: add modular launcher solution skeleton"
```

---

### Task 2: Core Launch Scenarios and Wizard Routes

**Files:**
- Create: `src/Launcher.Core/Scenarios/LaunchMode.cs`
- Create: `src/Launcher.Core/Scenarios/AgentKind.cs`
- Create: `src/Launcher.Core/Scenarios/RuntimeKind.cs`
- Create: `src/Launcher.Core/Scenarios/WizardStep.cs`
- Create: `src/Launcher.Core/Scenarios/WizardRouteService.cs`
- Test: `tests/Launcher.Core.Tests/WizardRouteServiceTests.cs`

- [ ] **Step 1: Write failing route tests**

Create `tests/Launcher.Core.Tests/WizardRouteServiceTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter WizardRouteServiceTests
```

Expected: FAIL because `Launcher.Core.Scenarios` types do not exist.

- [ ] **Step 3: Implement route types**

Create `src/Launcher.Core/Scenarios/LaunchMode.cs`:

```csharp
namespace Launcher.Core.Scenarios;

public enum LaunchMode
{
    Agent,
    Endpoint
}
```

Create `src/Launcher.Core/Scenarios/AgentKind.cs`:

```csharp
namespace Launcher.Core.Scenarios;

public enum AgentKind
{
    None,
    OpenCode,
    Kilo,
    Claw,
    Aider,
    Pi
}
```

Create `src/Launcher.Core/Scenarios/RuntimeKind.cs`:

```csharp
namespace Launcher.Core.Scenarios;

public enum RuntimeKind
{
    Ollama,
    LlamaCpp,
    LlamaCppTurboQuant,
    LlamaCppMtp
}
```

Create `src/Launcher.Core/Scenarios/WizardStep.cs`:

```csharp
namespace Launcher.Core.Scenarios;

public enum WizardStep
{
    Mode,
    Project,
    Agent,
    Model,
    Runtime,
    Port,
    KvMtpContext,
    AgentOptions,
    Review,
    Launch
}
```

Create `src/Launcher.Core/Scenarios/WizardRouteService.cs`:

```csharp
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
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter WizardRouteServiceTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src\Launcher.Core tests\Launcher.Core.Tests
git commit -m "feat(core): add launch scenarios and wizard routes"
```

---

### Task 3: Profiles and Settings Contracts

**Files:**
- Create: `src/Launcher.Core/Profiles/LauncherSettings.cs`
- Create: `src/Launcher.Core/Profiles/LaunchProfile.cs`
- Create: `src/Launcher.Core/Profiles/ProfileSerializer.cs`
- Test: `tests/Launcher.Core.Tests/ProfileSerializerTests.cs`

- [ ] **Step 1: Write serialization tests**

Create `tests/Launcher.Core.Tests/ProfileSerializerTests.cs`:

```csharp
using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Core.Tests;

public sealed class ProfileSerializerTests
{
    [Fact]
    public void RoundTripsProfileWithRussianNameAndRuntimeFields()
    {
        var profile = new LaunchProfile(
            Id: "kilo-qwen",
            Name: "Kilo через Qwen",
            Mode: LaunchMode.Agent,
            Agent: AgentKind.Kilo,
            Runtime: RuntimeKind.LlamaCppTurboQuant,
            ProjectPath: @"D:\AI\Projects\Test",
            ModelPath: @"D:\AI\Models\qwen.gguf",
            ContextTokens: 65536,
            Port: 8080,
            AntiLoopPresetId: "code-stable");

        var json = ProfileSerializer.Serialize(profile);
        var restored = ProfileSerializer.DeserializeProfile(json);

        Assert.Equal(profile, restored);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter ProfileSerializerTests
```

Expected: FAIL because profile types do not exist.

- [ ] **Step 3: Implement contracts**

Create `src/Launcher.Core/Profiles/LaunchProfile.cs`:

```csharp
using Launcher.Core.Scenarios;

namespace Launcher.Core.Profiles;

public sealed record LaunchProfile(
    string Id,
    string Name,
    LaunchMode Mode,
    AgentKind Agent,
    RuntimeKind Runtime,
    string? ProjectPath,
    string ModelPath,
    int ContextTokens,
    int Port,
    string AntiLoopPresetId);
```

Create `src/Launcher.Core/Profiles/LauncherSettings.cs`:

```csharp
namespace Launcher.Core.Profiles;

public sealed record LauncherSettings(
    string ModelsRoot,
    string? ProjectsRoot,
    string RuntimeRoot,
    string DownloadsRoot,
    int DefaultPort,
    string Language,
    string HelpMode,
    IReadOnlyList<LaunchProfile> Profiles);
```

Create `src/Launcher.Core/Profiles/ProfileSerializer.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher.Core.Profiles;

public static class ProfileSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(LaunchProfile profile)
    {
        return JsonSerializer.Serialize(profile, Options);
    }

    public static LaunchProfile DeserializeProfile(string json)
    {
        return JsonSerializer.Deserialize<LaunchProfile>(json, Options)
            ?? throw new InvalidOperationException("Profile JSON is empty.");
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter ProfileSerializerTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src\Launcher.Core tests\Launcher.Core.Tests
git commit -m "feat(core): add profile contracts"
```

---

### Task 4: Parameter Help Catalog

**Files:**
- Create: `src/Launcher.Core/Parameters/ParameterRiskLevel.cs`
- Create: `src/Launcher.Core/Parameters/ParameterHelp.cs`
- Create: `src/Launcher.Core/Parameters/ParameterHelpCatalog.cs`
- Test: `tests/Launcher.Core.Tests/ParameterHelpCatalogTests.cs`

- [ ] **Step 1: Write catalog tests**

Create `tests/Launcher.Core.Tests/ParameterHelpCatalogTests.cs`:

```csharp
using Launcher.Core.Parameters;

namespace Launcher.Core.Tests;

public sealed class ParameterHelpCatalogTests
{
    [Theory]
    [InlineData("context", "Контекст")]
    [InlineData("mtp", "MTP")]
    [InlineData("ignore-eos", "--ignore-eos")]
    public void ProvidesRussianHelpForRequiredParameters(string id, string expectedName)
    {
        var help = ParameterHelpCatalog.Get(id);

        Assert.Equal(expectedName, help.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(help.ShortText));
        Assert.False(string.IsNullOrWhiteSpace(help.Details));
    }

    [Fact]
    public void MarksIgnoreEosAsDangerous()
    {
        Assert.Equal(ParameterRiskLevel.Danger, ParameterHelpCatalog.Get("ignore-eos").Risk);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter ParameterHelpCatalogTests
```

Expected: FAIL because parameter types do not exist.

- [ ] **Step 3: Implement catalog**

Create `src/Launcher.Core/Parameters/ParameterRiskLevel.cs`:

```csharp
namespace Launcher.Core.Parameters;

public enum ParameterRiskLevel
{
    Normal,
    Warning,
    Danger
}
```

Create `src/Launcher.Core/Parameters/ParameterHelp.cs`:

```csharp
namespace Launcher.Core.Parameters;

public sealed record ParameterHelp(
    string Id,
    string DisplayName,
    string ShortText,
    string Details,
    ParameterRiskLevel Risk);
```

Create `src/Launcher.Core/Parameters/ParameterHelpCatalog.cs`:

```csharp
namespace Launcher.Core.Parameters;

public static class ParameterHelpCatalog
{
    private static readonly IReadOnlyDictionary<string, ParameterHelp> Items =
        new Dictionary<string, ParameterHelp>(StringComparer.OrdinalIgnoreCase)
        {
            ["context"] = new("context", "Контекст",
                "Сколько токенов модель держит в памяти.",
                "Больше контекст дает больше рабочей памяти для проекта, но увеличивает расход VRAM/RAM.",
                ParameterRiskLevel.Normal),
            ["mtp"] = new("mtp", "MTP",
                "Ускорение через предсказание нескольких токенов вперед.",
                "MTP может ускорить генерацию на совместимых моделях и runtime-ах, но при агрессивных настройках повышает риск повторов.",
                ParameterRiskLevel.Warning),
            ["ignore-eos"] = new("ignore-eos", "--ignore-eos",
                "Опасно: модель может не остановиться сама.",
                "Используйте только для диагностики. В agent workflow этот флаг может усиливать зацикливание.",
                ParameterRiskLevel.Danger)
        };

    public static ParameterHelp Get(string id)
    {
        if (Items.TryGetValue(id, out var help)) return help;
        throw new KeyNotFoundException($"Unknown parameter help id: {id}");
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter ParameterHelpCatalogTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src\Launcher.Core tests\Launcher.Core.Tests
git commit -m "feat(core): add Russian parameter help catalog"
```

---

### Task 5: Port Inspection and Port Conflict Actions

**Files:**
- Create: `src/Launcher.Runtimes/Ports/PortOwnerInfo.cs`
- Create: `src/Launcher.Runtimes/Ports/IPortInspector.cs`
- Create: `src/Launcher.Runtimes/Ports/WindowsPortInspector.cs`
- Test: `tests/Launcher.Runtimes.Tests/PortOwnerInfoTests.cs`

- [ ] **Step 1: Write value object tests**

Create `tests/Launcher.Runtimes.Tests/PortOwnerInfoTests.cs`:

```csharp
using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Tests;

public sealed class PortOwnerInfoTests
{
    [Fact]
    public void ClassifiesOwnLlamaServerByExecutableName()
    {
        var info = new PortOwnerInfo(8080, 1234, "llama-server.exe", @"D:\AI\runtimes\llama-server.exe", true, "qwen");

        Assert.True(info.IsLikelyLlamaServer);
        Assert.True(info.EndpointResponds);
        Assert.Equal("qwen", info.LoadedModelId);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Launcher.Runtimes.Tests\Launcher.Runtimes.Tests.csproj --filter PortOwnerInfoTests
```

Expected: FAIL because port types do not exist.

- [ ] **Step 3: Implement port contracts**

Create `src/Launcher.Runtimes/Ports/PortOwnerInfo.cs`:

```csharp
namespace Launcher.Runtimes.Ports;

public sealed record PortOwnerInfo(
    int Port,
    int ProcessId,
    string ProcessName,
    string? ExecutablePath,
    bool EndpointResponds,
    string? LoadedModelId)
{
    public bool IsLikelyLlamaServer =>
        ProcessName.Contains("llama-server", StringComparison.OrdinalIgnoreCase);
}
```

Create `src/Launcher.Runtimes/Ports/IPortInspector.cs`:

```csharp
namespace Launcher.Runtimes.Ports;

public interface IPortInspector
{
    Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken);
}
```

Create `src/Launcher.Runtimes/Ports/WindowsPortInspector.cs`:

```csharp
using System.Diagnostics;

namespace Launcher.Runtimes.Ports;

public sealed class WindowsPortInspector : IPortInspector
{
    public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken)
    {
        // Full netstat/Get-NetTCPConnection implementation comes after the value object is wired into UI.
        return Task.FromResult<PortOwnerInfo?>(null);
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Runtimes.Tests\Launcher.Runtimes.Tests.csproj --filter PortOwnerInfoTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src\Launcher.Runtimes tests\Launcher.Runtimes.Tests
git commit -m "feat(runtimes): add port ownership model"
```

---

### Task 6: GGUF Model Name Parser and Filters

**Files:**
- Create: `src/Launcher.Models/Catalog/LocalModelFile.cs`
- Create: `src/Launcher.Models/Catalog/GgufNameParser.cs`
- Create: `src/Launcher.Models/Catalog/ModelFilter.cs`
- Create: `src/Launcher.Models/Catalog/ModelFilterService.cs`
- Test: `tests/Launcher.Models.Tests/GgufNameParserTests.cs`
- Test: `tests/Launcher.Models.Tests/ModelFilterServiceTests.cs`

- [ ] **Step 1: Write parser tests**

Create `tests/Launcher.Models.Tests/GgufNameParserTests.cs`:

```csharp
using Launcher.Models.Catalog;

namespace Launcher.Models.Tests;

public sealed class GgufNameParserTests
{
    [Fact]
    public void ParsesFamilySizeAndQuantFromCommonGgufName()
    {
        var model = GgufNameParser.Parse(@"D:\AI\Models\Qwen3-Coder-30B-A3B-Instruct-Q4_K_M.gguf");

        Assert.Equal("Qwen", model.Family);
        Assert.Equal("30B", model.SizeLabel);
        Assert.Equal("Q4_K_M", model.Quant);
    }
}
```

- [ ] **Step 2: Write filter tests**

Create `tests/Launcher.Models.Tests/ModelFilterServiceTests.cs`:

```csharp
using Launcher.Models.Catalog;

namespace Launcher.Models.Tests;

public sealed class ModelFilterServiceTests
{
    [Fact]
    public void FiltersByQuantAndFamily()
    {
        var models = new[]
        {
            new LocalModelFile("a.gguf", "Qwen", "30B", "Q4_K_M", 18),
            new LocalModelFile("b.gguf", "Gemma", "27B", "Q8_0", 30)
        };

        var result = ModelFilterService.Apply(models, new ModelFilter(Family: "Qwen", Quant: "Q4", MaxSizeGb: null));

        Assert.Single(result);
        Assert.Equal("a.gguf", result[0].Path);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run:

```powershell
dotnet test tests\Launcher.Models.Tests\Launcher.Models.Tests.csproj --filter "GgufNameParserTests|ModelFilterServiceTests"
```

Expected: FAIL because catalog types do not exist.

- [ ] **Step 4: Implement parser and filters**

Create `src/Launcher.Models/Catalog/LocalModelFile.cs`:

```csharp
namespace Launcher.Models.Catalog;

public sealed record LocalModelFile(
    string Path,
    string Family,
    string? SizeLabel,
    string? Quant,
    double SizeGb);
```

Create `src/Launcher.Models/Catalog/GgufNameParser.cs`:

```csharp
using System.Text.RegularExpressions;

namespace Launcher.Models.Catalog;

public static partial class GgufNameParser
{
    public static LocalModelFile Parse(string path)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var family = fileName.StartsWith("Qwen", StringComparison.OrdinalIgnoreCase) ? "Qwen"
            : fileName.StartsWith("Gemma", StringComparison.OrdinalIgnoreCase) ? "Gemma"
            : fileName.StartsWith("DeepSeek", StringComparison.OrdinalIgnoreCase) ? "DeepSeek"
            : "Other";
        var size = SizeRegex().Match(fileName).Value;
        var quant = QuantRegex().Match(fileName).Value;
        var sizeGb = File.Exists(path) ? new FileInfo(path).Length / 1024d / 1024d / 1024d : 0;
        return new LocalModelFile(path, family, string.IsNullOrWhiteSpace(size) ? null : size, string.IsNullOrWhiteSpace(quant) ? null : quant, sizeGb);
    }

    [GeneratedRegex(@"\b\d+(?:\.\d+)?B\b", RegexOptions.IgnoreCase)]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"\b(?:Q[2-8](?:_[A-Z0-9]+)*|IQ[0-9A-Z_]+|F16|BF16)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuantRegex();
}
```

Create `src/Launcher.Models/Catalog/ModelFilter.cs`:

```csharp
namespace Launcher.Models.Catalog;

public sealed record ModelFilter(string? Family, string? Quant, double? MaxSizeGb);
```

Create `src/Launcher.Models/Catalog/ModelFilterService.cs`:

```csharp
namespace Launcher.Models.Catalog;

public static class ModelFilterService
{
    public static IReadOnlyList<LocalModelFile> Apply(IEnumerable<LocalModelFile> models, ModelFilter filter)
    {
        var query = models;
        if (!string.IsNullOrWhiteSpace(filter.Family))
            query = query.Where(m => string.Equals(m.Family, filter.Family, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Quant))
            query = query.Where(m => m.Quant?.Contains(filter.Quant, StringComparison.OrdinalIgnoreCase) == true);
        if (filter.MaxSizeGb is { } max)
            query = query.Where(m => m.SizeGb <= max);
        return query.ToList();
    }
}
```

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Models.Tests\Launcher.Models.Tests.csproj --filter "GgufNameParserTests|ModelFilterServiceTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

Run:

```powershell
git add src\Launcher.Models tests\Launcher.Models.Tests
git commit -m "feat(models): add GGUF parser and filters"
```

---

### Task 7: Hugging Face Search DTOs, Sorting, and Scoring

**Files:**
- Create: `src/Launcher.Models/HuggingFace/HuggingFaceModelSummary.cs`
- Create: `src/Launcher.Models/HuggingFace/HuggingFaceSort.cs`
- Create: `src/Launcher.Models/HuggingFace/ModelChoiceScore.cs`
- Create: `src/Launcher.Models/HuggingFace/ModelChoiceScorer.cs`
- Test: `tests/Launcher.Models.Tests/ModelChoiceScorerTests.cs`

- [ ] **Step 1: Write scoring tests**

Create `tests/Launcher.Models.Tests/ModelChoiceScorerTests.cs`:

```csharp
using Launcher.Models.HuggingFace;

namespace Launcher.Models.Tests;

public sealed class ModelChoiceScorerTests
{
    [Fact]
    public void RewardsPopularityAndCompatibility()
    {
        var model = new HuggingFaceModelSummary(
            Id: "unsloth/Qwen3-Coder-GGUF",
            Downloads: 3_000_000,
            Likes: 600,
            Tags: new[] { "gguf", "qwen", "text-generation", "imatrix" },
            IsCompatibleWithCurrentGpu: true,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true);

        var score = ModelChoiceScorer.Score(model);

        Assert.True(score.Value > 90);
        Assert.Contains("HF popularity", score.Reasons);
        Assert.Contains("fits current GPU", score.Reasons);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Launcher.Models.Tests\Launcher.Models.Tests.csproj --filter ModelChoiceScorerTests
```

Expected: FAIL because Hugging Face scoring types do not exist.

- [ ] **Step 3: Implement DTOs and scorer**

Create `src/Launcher.Models/HuggingFace/HuggingFaceModelSummary.cs`:

```csharp
namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceModelSummary(
    string Id,
    long Downloads,
    int Likes,
    IReadOnlyList<string> Tags,
    bool IsCompatibleWithCurrentGpu,
    bool HasPreferredQuant,
    bool IsRuntimeCompatible);
```

Create `src/Launcher.Models/HuggingFace/HuggingFaceSort.cs`:

```csharp
namespace Launcher.Models.HuggingFace;

public enum HuggingFaceSort
{
    Trending,
    Downloads,
    Likes,
    CreatedAt,
    LastModified
}
```

Create `src/Launcher.Models/HuggingFace/ModelChoiceScore.cs`:

```csharp
namespace Launcher.Models.HuggingFace;

public sealed record ModelChoiceScore(int Value, IReadOnlyList<string> Reasons);
```

Create `src/Launcher.Models/HuggingFace/ModelChoiceScorer.cs`:

```csharp
namespace Launcher.Models.HuggingFace;

public static class ModelChoiceScorer
{
    public static ModelChoiceScore Score(HuggingFaceModelSummary model)
    {
        var score = 0;
        var reasons = new List<string>();

        if (model.Downloads >= 1_000_000) { score += 35; reasons.Add("HF popularity"); }
        else if (model.Downloads >= 100_000) { score += 20; reasons.Add("moderate downloads"); }

        if (model.Likes >= 500) { score += 20; reasons.Add("many likes"); }
        if (model.IsCompatibleWithCurrentGpu) { score += 20; reasons.Add("fits current GPU"); }
        if (model.HasPreferredQuant) { score += 15; reasons.Add("preferred quant available"); }
        if (model.IsRuntimeCompatible) { score += 15; reasons.Add("runtime compatible"); }

        return new ModelChoiceScore(Math.Min(100, score), reasons);
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Models.Tests\Launcher.Models.Tests.csproj --filter ModelChoiceScorerTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src\Launcher.Models tests\Launcher.Models.Tests
git commit -m "feat(models): add Hugging Face model scoring"
```

---

### Task 8: Agent Command Builders

**Files:**
- Create: `src/Launcher.Core/LaunchPlans/LaunchPlan.cs`
- Create: `src/Launcher.Agents/Commands/AgentLaunchRequest.cs`
- Create: `src/Launcher.Agents/Commands/IAgentCommandBuilder.cs`
- Create: `src/Launcher.Agents/Commands/KiloCommandBuilder.cs`
- Create: `src/Launcher.Agents/Commands/AiderCommandBuilder.cs`
- Test: `tests/Launcher.Agents.Tests/AgentCommandBuilderTests.cs`

- [ ] **Step 1: Write command builder tests**

Create `tests/Launcher.Agents.Tests/AgentCommandBuilderTests.cs`:

```csharp
using Launcher.Agents.Commands;
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Tests;

public sealed class AgentCommandBuilderTests
{
    [Fact]
    public void KiloCommandUsesProjectAndProviderModel()
    {
        var plan = new KiloCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.Kilo,
            @"D:\AI\Projects\App",
            "local/llama.cpp/qwen",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("kilo", plan.Executable);
        Assert.Contains("-m", plan.Arguments);
        Assert.Contains(@"D:\AI\Projects\App", plan.Arguments);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Launcher.Agents.Tests\Launcher.Agents.Tests.csproj --filter AgentCommandBuilderTests
```

Expected: FAIL because command builder types do not exist.

- [ ] **Step 3: Implement command plan and Kilo builder**

Create `src/Launcher.Core/LaunchPlans/LaunchPlan.cs`:

```csharp
namespace Launcher.Core.LaunchPlans;

public sealed record LaunchPlan(
    string Executable,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Environment);
```

Create `src/Launcher.Agents/Commands/AgentLaunchRequest.cs`:

```csharp
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Commands;

public sealed record AgentLaunchRequest(
    AgentKind Agent,
    string ProjectPath,
    string ProviderModel,
    string BaseUrl);
```

Create `src/Launcher.Agents/Commands/IAgentCommandBuilder.cs`:

```csharp
using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public interface IAgentCommandBuilder
{
    LaunchPlan Build(AgentLaunchRequest request);
}
```

Create `src/Launcher.Agents/Commands/KiloCommandBuilder.cs`:

```csharp
using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public sealed class KiloCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        return new LaunchPlan(
            "kilo",
            new[] { "-m", request.ProviderModel, request.ProjectPath },
            new Dictionary<string, string>
            {
                ["OPENAI_BASE_URL"] = request.BaseUrl,
                ["OPENAI_API_KEY"] = "local"
            });
    }
}
```

Create `src/Launcher.Agents/Commands/AiderCommandBuilder.cs`:

```csharp
using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public sealed class AiderCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        return new LaunchPlan(
            "aider",
            new[] { "--model", $"openai/{request.ProviderModel}", request.ProjectPath },
            new Dictionary<string, string>
            {
                ["OPENAI_API_BASE"] = request.BaseUrl,
                ["OPENAI_API_KEY"] = "local"
            });
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Agents.Tests\Launcher.Agents.Tests.csproj --filter AgentCommandBuilderTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src\Launcher.Core src\Launcher.Agents tests\Launcher.Agents.Tests
git commit -m "feat(agents): add initial command builders"
```

---

### Task 9: Desktop Shell First Screen

**Files:**
- Create: `src/Launcher.Desktop/Launcher.Desktop.csproj`
- Create: `src/Launcher.Desktop/App.axaml`
- Create: `src/Launcher.Desktop/App.axaml.cs`
- Create: `src/Launcher.Desktop/Program.cs`
- Create: `src/Launcher.Desktop/ViewModels/HomeViewModel.cs`
- Create: `src/Launcher.Desktop/Views/HomeView.axaml`
- Create: `src/Launcher.Desktop/Resources/Theme.axaml`

- [ ] **Step 1: Create Avalonia MVVM project**

Run:

```powershell
dotnet new avalonia.mvvm -n Launcher.Desktop -o src\Launcher.Desktop --framework net8.0
dotnet sln .\llama-server-launcher-avalonia.sln add src\Launcher.Desktop\Launcher.Desktop.csproj
dotnet add src\Launcher.Desktop\Launcher.Desktop.csproj reference src\Launcher.Core\Launcher.Core.csproj
```

Expected: Avalonia project created and added to solution.

- [ ] **Step 2: Replace home view model**

Create `src/Launcher.Desktop/ViewModels/HomeViewModel.cs`:

```csharp
using System.Collections.ObjectModel;

namespace Launcher.Desktop.ViewModels;

public sealed class HomeViewModel
{
    public string Title => "AI Launcher Studio";
    public string Subtitle => "локальные агенты · сервер моделей · каталог GGUF";
    public ObservableCollection<string> Presets { get; } =
    [
        "Kilo · Qwen3 Coder · TurboQuant · 64k",
        "OpenCode · Gemma · Ollama",
        "Endpoint · Hermes · MTP · 8081"
    ];
}
```

- [ ] **Step 3: Create first screen XAML**

Create `src/Launcher.Desktop/Views/HomeView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Launcher.Desktop.Views.HomeView">
  <Grid ColumnDefinitions="220,*" RowDefinitions="Auto,*" Background="#F6F2EB" Margin="18">
    <Border Grid.RowSpan="2" Background="#FFFAF2" BorderBrush="#EADCC9" BorderThickness="1" CornerRadius="22" Padding="14">
      <StackPanel Spacing="12">
        <TextBlock Text="Пресеты" FontWeight="Bold" FontSize="16"/>
        <ItemsControl ItemsSource="{Binding Presets}">
          <ItemsControl.ItemTemplate>
            <DataTemplate>
              <Border Background="White" BorderBrush="#EADCC9" BorderThickness="1" CornerRadius="14" Padding="10" Margin="0,0,0,8">
                <TextBlock Text="{Binding}" TextTrimming="CharacterEllipsis"/>
              </Border>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </StackPanel>
    </Border>
    <StackPanel Grid.Column="1" Spacing="4" Margin="18,0,0,16">
      <TextBlock Text="{Binding Title}" FontSize="30" FontWeight="Bold" Foreground="#20242C"/>
      <TextBlock Text="{Binding Subtitle}" FontSize="13" Foreground="#6F7785"/>
    </StackPanel>
    <Grid Grid.Column="1" Grid.Row="1" ColumnDefinitions="*,*" RowDefinitions="Auto,Auto,Auto" Margin="18,0,0,0" ColumnSpacing="16" RowSpacing="14">
      <Button Grid.Column="0" MinHeight="220" Background="White" BorderBrush="#FF9B35" BorderThickness="2">
        <StackPanel Margin="18">
          <TextBlock Text="ПРОЕКТ" Foreground="#C15F00" FontWeight="Bold"/>
          <TextBlock Text="Запустить агента" FontSize="30" FontWeight="Bold" Margin="0,42,0,8"/>
          <TextBlock Text="проект · агент · модель · runtime · старт" Foreground="#697386"/>
        </StackPanel>
      </Button>
      <Button Grid.Column="1" MinHeight="220" Background="White" BorderBrush="#70B8C7" BorderThickness="2">
        <StackPanel Margin="18">
          <TextBlock Text="СЕРВЕР" Foreground="#247187" FontWeight="Bold"/>
          <TextBlock Text="Поднять endpoint" FontSize="30" FontWeight="Bold" Margin="0,42,0,8"/>
          <TextBlock Text="модель · контекст · KV · MTP · сервер" Foreground="#697386"/>
        </StackPanel>
      </Button>
      <Button Grid.Row="1" Grid.Column="0" Content="📁  Папка моделей     D:\AI\Models" HorizontalContentAlignment="Left"/>
      <Button Grid.Row="1" Grid.Column="1" Content="📁  Папка проектов    не указана" HorizontalContentAlignment="Left"/>
      <UniformGrid Grid.Row="2" Grid.ColumnSpan="2" Columns="4">
        <Button Content="Модели"/>
        <Button Content="Рантаймы"/>
        <Button Content="Агенты"/>
        <Button Content="Журнал"/>
      </UniformGrid>
    </Grid>
  </Grid>
</UserControl>
```

- [ ] **Step 4: Wire app startup to HomeView**

Update generated `src/Launcher.Desktop/MainWindow.axaml` so its content is:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:views="using:Launcher.Desktop.Views"
        xmlns:vm="using:Launcher.Desktop.ViewModels"
        x:Class="Launcher.Desktop.MainWindow"
        Width="1180"
        Height="760"
        Title="AI Launcher Studio">
  <Window.DataContext>
    <vm:HomeViewModel/>
  </Window.DataContext>
  <views:HomeView/>
</Window>
```

- [ ] **Step 5: Build desktop shell**

Run:

```powershell
dotnet build src\Launcher.Desktop\Launcher.Desktop.csproj
```

Expected: PASS.

- [ ] **Step 6: Commit**

Run:

```powershell
git add src\Launcher.Desktop .\llama-server-launcher-avalonia.sln
git commit -m "feat(desktop): add AI Launcher Studio home shell"
```

---

### Task 10: Migration Adapter from Python Config

**Files:**
- Create: `src/Launcher.Core/Migration/PythonLauncherConfigImporter.cs`
- Test: `tests/Launcher.Core.Tests/PythonLauncherConfigImporterTests.cs`

- [ ] **Step 1: Write importer test**

Create `tests/Launcher.Core.Tests/PythonLauncherConfigImporterTests.cs`:

```csharp
using Launcher.Core.Migration;

namespace Launcher.Core.Tests;

public sealed class PythonLauncherConfigImporterTests
{
    [Fact]
    public void ImportsKnownFoldersWithoutMutatingSourceConfig()
    {
        var json = """
        {
          "models_dir": "D:\\AI\\Models",
          "projects_dir": "D:\\AI\\Projects",
          "llama_server_path": "D:\\AI\\runtimes\\llama-server.exe"
        }
        """;

        var result = PythonLauncherConfigImporter.Import(json);

        Assert.Equal(@"D:\AI\Models", result.ModelsRoot);
        Assert.Equal(@"D:\AI\Projects", result.ProjectsRoot);
        Assert.Equal(@"D:\AI\runtimes", result.RuntimeRoot);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter PythonLauncherConfigImporterTests
```

Expected: FAIL because importer does not exist.

- [ ] **Step 3: Implement importer**

Create `src/Launcher.Core/Migration/PythonLauncherConfigImporter.cs`:

```csharp
using System.Text.Json;
using Launcher.Core.Profiles;

namespace Launcher.Core.Migration;

public static class PythonLauncherConfigImporter
{
    public static LauncherSettings Import(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var modelsRoot = GetString(root, "models_dir") ?? "";
        var projectsRoot = GetString(root, "projects_dir");
        var serverPath = GetString(root, "llama_server_path") ?? "";
        var runtimeRoot = string.IsNullOrWhiteSpace(serverPath)
            ? ""
            : Path.GetDirectoryName(serverPath) ?? "";

        return new LauncherSettings(
            modelsRoot,
            projectsRoot,
            runtimeRoot,
            DownloadsRoot: modelsRoot,
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "on-demand",
            Profiles: Array.Empty<LaunchProfile>());
    }

    private static string? GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter PythonLauncherConfigImporterTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src\Launcher.Core tests\Launcher.Core.Tests
git commit -m "feat(core): import Python launcher settings"
```

---

## Later Product Tasks

After Tasks 1-10 are complete and green, continue with these feature groups using the same TDD pattern:

1. Implement real `WindowsPortInspector` using `Get-NetTCPConnection`/process APIs and endpoint probing.
2. Implement Ollama API client with `/api/tags`, `/v1/models`, and tiny `/api/generate` preflight.
3. Implement llama.cpp/TurboQuant capability parser by porting `--help` parsing from the current Python launcher.
4. Implement VRAM/GPU probing by porting `nvidia-smi` parsing and clean/parallel launch forecasting.
5. Implement Hugging Face REST client with sorting: `trendingScore`, `downloads`, `likes`, `createdAt`, `lastModified`.
6. Implement cancellable model downloads with split-file grouping and local status.
7. Implement full agent command builders for OpenCode, Kilo, Claw, Aider, and PI.
8. Implement review screen that displays `LaunchPlan`, VRAM/RAM forecast, risks, and command preview.
9. Replace the original root Avalonia app with `Launcher.Desktop` after feature parity.
10. Add Avalonia headless UI smoke tests for home screen, wizard navigation, Back/Cancel, and profile launch.

Each later task must start with failing tests, make the smallest implementation, run targeted tests, run the full solution build/test, then commit.

## Verification Commands

Run after every task:

```powershell
dotnet build .\llama-server-launcher-avalonia.sln --no-restore
dotnet test .\llama-server-launcher-avalonia.sln --no-build
```

When touching the original Python launcher project, also run from `D:\AI\AI launcher`:

```powershell
python _build_launcher.py
python -m unittest -v test_ai_launcher.py
```

## Self-Review

- Spec coverage: the plan covers modular architecture, profiles, model catalog foundations, HF scoring, agent/endpoint split, port ownership, Russian parameter help, migration, and desktop home shell. Full runtime/HF/download implementation is scheduled as later product tasks with the same TDD pattern.
- Placeholder scan: this plan avoids unfinished-marker tokens; later work is listed as named feature groups, not ambiguous placeholders.
- Type consistency: `LaunchMode`, `AgentKind`, `RuntimeKind`, `LaunchProfile`, `LaunchPlan`, and DTO names are introduced before later tasks use them.
