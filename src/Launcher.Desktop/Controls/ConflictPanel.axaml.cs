using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Launcher.Runtimes.Compatibility;

namespace Launcher.Desktop.Controls;

/// <summary>
/// Показывает находки <see cref="SettingsConflictEngine"/>: цвет по severity, заголовок,
/// объяснение и предлагаемое исправление. Пусто — значит всё в порядке. Тема-зависимая.
/// </summary>
public partial class ConflictPanel : UserControl
{
    private static readonly Color ErrorColor = Color.Parse("#E5484D");
    private static readonly Color WarnColor = Color.Parse("#E08600");
    private static readonly Color OkColor = Color.Parse("#37A463");

    public static readonly StyledProperty<IReadOnlyList<ConflictFinding>?> FindingsProperty =
        AvaloniaProperty.Register<ConflictPanel, IReadOnlyList<ConflictFinding>?>(nameof(Findings));

    public IReadOnlyList<ConflictFinding>? Findings
    {
        get => GetValue(FindingsProperty);
        set => SetValue(FindingsProperty, value);
    }

    public ConflictPanel()
    {
        InitializeComponent();
        ActualThemeVariantChanged += (_, _) => Rebuild();
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FindingsProperty)
        {
            Rebuild();
        }
    }

    private IBrush Res(string key, string fallback)
    {
        if (this.TryFindResource(key, out var v) && v is IBrush b)
        {
            return b;
        }

        return new SolidColorBrush(Color.Parse(fallback));
    }

    private void Rebuild()
    {
        Items.Children.Clear();
        var findings = Findings;
        var inkSoft = Res("InkSoftBrush", "#6E6E73");

        if (findings is null || findings.Count == 0)
        {
            Items.Children.Add(new TextBlock
            {
                Text = "✓ Конфликтов настроек нет — можно запускать.",
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(OkColor),
            });
            return;
        }

        foreach (var finding in findings)
        {
            Items.Children.Add(BuildCard(finding, inkSoft));
        }
    }

    private static Control BuildCard(ConflictFinding finding, IBrush inkSoft)
    {
        var (accent, icon) = finding.Severity switch
        {
            ConflictSeverity.Error => (ErrorColor, "⛔"),
            ConflictSeverity.Warning => (WarnColor, "⚠"),
            _ => (OkColor, "ℹ"),
        };
        var tint = Color.FromArgb(28, accent.R, accent.G, accent.B);

        var content = new StackPanel { Spacing = 4 };
        content.Children.Add(new TextBlock
        {
            Text = $"{icon}  {finding.Title}",
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(accent),
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(new TextBlock
        {
            Text = finding.Explanation,
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(new TextBlock
        {
            Text = "Как исправить: " + finding.SuggestedFix,
            TextWrapping = TextWrapping.Wrap,
            FontStyle = FontStyle.Italic,
            Foreground = inkSoft,
        });

        return new Border
        {
            Background = new SolidColorBrush(tint),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, accent.R, accent.G, accent.B)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12),
            Child = content,
        };
    }
}
