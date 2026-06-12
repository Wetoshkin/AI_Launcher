using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Launcher.Core.Parameters;

namespace Launcher.Desktop.Controls;

/// <summary>
/// Иконка «?», которая по наведению показывает понятную подсказку: что это / зачем / на что влияет.
/// Текст берётся из <see cref="ParameterHelpCatalog"/> по <see cref="HelpId"/>,
/// либо задаётся напрямую через <see cref="Title"/>/<see cref="Text"/>.
/// </summary>
public partial class HelpHint : UserControl
{
    private static readonly IBrush InkSoftBrush = new SolidColorBrush(Color.Parse("#76614C"));
    private static readonly IBrush DangerBrush = new SolidColorBrush(Color.Parse("#D64B3F"));
    private static readonly IBrush WarnBrush = new SolidColorBrush(Color.Parse("#B26A00"));

    public static readonly StyledProperty<string?> HelpIdProperty =
        AvaloniaProperty.Register<HelpHint, string?>(nameof(HelpId));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<HelpHint, string?>(nameof(Title));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<HelpHint, string?>(nameof(Text));

    public string? HelpId { get => GetValue(HelpIdProperty); set => SetValue(HelpIdProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public HelpHint()
    {
        InitializeComponent();
        UpdateTip();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HelpIdProperty || change.Property == TitleProperty || change.Property == TextProperty)
        {
            UpdateTip();
        }
    }

    private void UpdateTip()
    {
        var title = Title;
        var body = Text;
        IBrush? riskBrush = null;
        string? riskText = null;

        if (!string.IsNullOrWhiteSpace(HelpId) && ParameterHelpCatalog.TryGet(HelpId!, out var help))
        {
            title ??= help.DisplayName;
            body ??= help.ShortText + "\n\n" + help.Details;
            (riskBrush, riskText) = help.Risk switch
            {
                ParameterRiskLevel.Danger => (DangerBrush, "⚠ Будьте осторожны с этим параметром."),
                ParameterRiskLevel.Warning => (WarnBrush, "Совет: меняйте осознанно."),
                _ => (null, null)
            };
        }

        var panel = new StackPanel { Spacing = 4, MaxWidth = 340 };
        if (!string.IsNullOrWhiteSpace(title))
        {
            panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.Bold });
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            panel.Children.Add(new TextBlock
            {
                Text = body,
                TextWrapping = TextWrapping.Wrap,
                Foreground = InkSoftBrush,
            });
        }

        if (riskText is not null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = riskText,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeight.SemiBold,
                Foreground = riskBrush,
            });
        }

        ToolTip.SetTip(this, panel);
        ToolTip.SetShowDelay(this, 200);
    }
}
