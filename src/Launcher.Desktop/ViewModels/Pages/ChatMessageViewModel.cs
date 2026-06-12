using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class ChatMessageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _content;

    public string Role { get; }
    public bool IsUser { get; }

    public ChatMessageViewModel(string role, string content, bool isUser)
    {
        Role = role;
        _content = content;
        IsUser = isUser;
    }

    public void Append(string text) => Content += text;
}
