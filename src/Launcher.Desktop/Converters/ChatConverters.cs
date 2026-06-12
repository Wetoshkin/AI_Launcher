using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;

namespace Launcher.Desktop.Converters;

/// <summary>Конвертеры для оформления пузырей чата по признаку «сообщение пользователя».</summary>
public static class ChatConverters
{
    public static readonly FuncValueConverter<bool, HorizontalAlignment> Alignment =
        new(isUser => isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left);

    public static readonly FuncValueConverter<bool, IBrush> Bubble =
        new(isUser => new SolidColorBrush(Color.Parse(isUser ? "#FFE0BD" : "#FFFFFF")));
}
