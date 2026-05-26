using System.Threading.Tasks;

namespace Launcher.Desktop.Services;

public interface IFolderPicker
{
    Task<string?> PickFolderAsync(string title);
}
