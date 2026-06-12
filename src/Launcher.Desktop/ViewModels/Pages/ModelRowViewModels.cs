namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>Строка локальной модели в каталоге.</summary>
public sealed record LocalModelRow(string FileName, string Family, string Quant, string Size, string Path);

/// <summary>Строка результата поиска на Hugging Face.</summary>
public sealed record HfModelRow(string Id, string Stats, string Quants, bool HasGguf);
