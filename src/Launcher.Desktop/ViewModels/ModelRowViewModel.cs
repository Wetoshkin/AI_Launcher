using System.IO;
using Launcher.Models.Catalog;

namespace Launcher.Desktop.ViewModels;

public sealed class ModelRowViewModel(LocalModelFile model)
{
    public LocalModelFile Model => model;

    public string Name => System.IO.Path.GetFileName(model.Path);

    public string Family => model.Family;

    public string Quant => model.Quant ?? "неизвестно";

    public string Size => $"{model.SizeGb:0.0} ГБ";

    public string Path => model.Path;
}
