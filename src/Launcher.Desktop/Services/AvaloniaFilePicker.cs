using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Launcher.Desktop.Services;

public sealed class AvaloniaFilePicker(Window owner) : IFilePicker
{
    public async Task<string?> PickFileAsync(string title, IReadOnlyList<string> extensions)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Поддерживаемые файлы")
                {
                    Patterns = extensions.Select(extension => $"*{extension}").ToArray()
                }
            ]
        });

        var file = files.FirstOrDefault();
        return file?.TryGetLocalPath() ?? file?.Path.LocalPath;
    }
}
