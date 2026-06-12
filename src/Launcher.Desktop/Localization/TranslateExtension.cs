using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Launcher.Desktop.Localization;

/// <summary>Markup-расширение {l:T key} — биндит локализованную строку из <see cref="Loc"/>.</summary>
public sealed class TExtension : MarkupExtension
{
    public TExtension()
    {
    }

    public TExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding($"[{Key}]")
        {
            Source = Loc.Instance,
            Mode = BindingMode.OneWay,
        };
    }
}
