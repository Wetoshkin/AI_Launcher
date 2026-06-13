using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class ChatMessageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    private string _reasoning = string.Empty;

    [ObservableProperty]
    private string _display = string.Empty;

    public string Role { get; }
    public bool IsUser { get; }

    public bool HasReasoning => !string.IsNullOrWhiteSpace(Reasoning);

    public ChatMessageViewModel(string role, string content, bool isUser)
    {
        Role = role;
        IsUser = isUser;
        _content = content;
        Split(content);
    }

    public void Append(string text) => Content += text;

    partial void OnContentChanged(string value) => Split(value);

    /// <summary>Отделяет блок размышлений &lt;think&gt;…&lt;/think&gt; от финального ответа (для reasoning-моделей).</summary>
    private void Split(string content)
    {
        const string open = "<think>";
        const string close = "</think>";

        var start = content.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            Reasoning = string.Empty;
            Display = content;
            OnPropertyChanged(nameof(HasReasoning));
            return;
        }

        var afterOpen = start + open.Length;
        var end = content.IndexOf(close, afterOpen, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            // Блок размышлений ещё стримится.
            Reasoning = content[afterOpen..].Trim();
            Display = content[..start].Trim();
        }
        else
        {
            Reasoning = content[afterOpen..end].Trim();
            Display = (content[..start] + content[(end + close.Length)..]).Trim();
        }

        OnPropertyChanged(nameof(HasReasoning));
    }
}
