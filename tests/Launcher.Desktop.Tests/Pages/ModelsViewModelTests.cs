using System.Net;
using System.Text;
using System.Threading.Tasks;
using Launcher.Desktop.ViewModels.Pages;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.Tests.Pages;

public class ModelsViewModelTests
{
    private sealed class StubHandler(string json) : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static ModelsViewModel BuildVm(string json)
    {
        var http = new System.Net.Http.HttpClient(new StubHandler(json))
        {
            BaseAddress = new System.Uri("https://huggingface.co"),
        };
        return new ModelsViewModel(new HuggingFaceModelClient(http));
    }

    [Fact]
    public async Task Search_populates_results_from_hugging_face()
    {
        var json = "[{\"id\":\"Qwen/Qwen2.5-Coder-7B-Instruct-GGUF\",\"downloads\":1234,\"likes\":56," +
                   "\"tags\":[\"gguf\"],\"siblings\":[{\"rfilename\":\"qwen2.5-coder-7b-q4_k_m.gguf\"}]}]";
        var vm = BuildVm(json);

        await vm.SearchHuggingFaceCommand.ExecuteAsync(null);

        Assert.Single(vm.SearchResults);
        Assert.Equal("Qwen/Qwen2.5-Coder-7B-Instruct-GGUF", vm.SearchResults[0].Id);
        Assert.Contains("Q4_K_M", vm.SearchResults[0].Quants);
        Assert.True(vm.SearchResults[0].HasGguf);
    }

    [Fact]
    public void Scan_without_folder_reports_hint()
    {
        var vm = BuildVm("[]");
        vm.ScanCommand.Execute(null);
        Assert.Empty(vm.LocalModels);
        Assert.Contains("выберите папку", vm.LocalStatus);
    }
}
