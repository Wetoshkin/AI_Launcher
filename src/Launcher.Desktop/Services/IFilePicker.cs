using System.Collections.Generic;
using System.Threading.Tasks;

namespace Launcher.Desktop.Services;

public interface IFilePicker
{
    Task<string?> PickFileAsync(string title, IReadOnlyList<string> extensions);
}
