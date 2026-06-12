using System.ComponentModel;

namespace Launcher.Desktop.Localization;

/// <summary>
/// Синглтон локализации. Биндинги вида {l:T key} тянут текст через индексатор;
/// при смене языка поднимается уведомление и все строки обновляются.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    private string _language = "ru";

    public string Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        }
    }

    public string this[string key] => LocalizationStrings.Get(_language, key);

    public event PropertyChangedEventHandler? PropertyChanged;
}
