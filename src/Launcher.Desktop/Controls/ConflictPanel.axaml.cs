using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Launcher.Runtimes.Compatibility;

namespace Launcher.Desktop.Controls;

/// <summary>
/// Показывает находки <see cref="SettingsConflictEngine"/>: цвет по severity, заголовок,
/// объяснение и предлагаемое исправление. Пусто — значит всё в порядке.
/// </summary>
public partial class ConflictPanel : UserControl
{
    private static readonly IBrush OkBrush = new SolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IBrush InkSoftBrush = new SolidColorBrush(Color.Parse("#76614C"));

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

    private void Rebuild()
    {
        Items.Children.Clear();
        var findings = Findings;

        if (findings is null || findings.Count == 0)
        {
            Items.Children.Add(new TextBlock
            {
                Text = "✓ Конфликтов настроек нет — можно запускать.",
                FontWeight = FontWeight.SemiBold,
                Foreground = OkBrush,
            });
            return;
        }

        foreach (var finding in findings)
        {
            Items.Children.Add(BuildCard(finding));
        }
    }

    private static Control BuildCard(ConflictFinding finding)
    {
        var (accent, background, icon) = finding.Severity switch
        {
            ConflictSeverity.Error => (Color.Parse("#D64B3F"), Color.Parse("#FCEBE9"), "⛔"),
            ConflictSeverity.Warning => (Color.Parse("#B26A00"), Color.Parse("#FFF3DD"), "⚠"),
            _ => (Color.Parse("#2E7D32"), Color.Parse("#EAF5EC"), "ℹ"),
        };

        var content = new StackPanel { Spacing = 3 };
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
            Foreground = InkSoftBrush,
        });

        return new Border
        {
            Background = new SolidColorBrush(background),
            BorderBrush = new SolidColorBrush(accent),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10),
            Child = content,
        };
    }
}
