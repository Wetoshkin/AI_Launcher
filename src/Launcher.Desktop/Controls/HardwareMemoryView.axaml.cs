using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Launcher.Runtimes.Memory;

namespace Launcher.Desktop.Controls;

/// <summary>
/// Рисует наглядную диаграмму: как модель ложится в память каждой видеокарты и системной RAM.
/// Серый сегмент — уже занято, акцент — наша модель, красный — перегруз. Тема-зависимая.
/// </summary>
public partial class HardwareMemoryView : UserControl
{
    private static readonly IBrush OverflowBrush = new SolidColorBrush(Color.Parse("#E5484D"));
    private static readonly IBrush OkBrush = new SolidColorBrush(Color.Parse("#37A463"));

    public static readonly StyledProperty<DeviceMemoryPlan?> PlanProperty =
        AvaloniaProperty.Register<HardwareMemoryView, DeviceMemoryPlan?>(nameof(Plan));

    public DeviceMemoryPlan? Plan
    {
        get => GetValue(PlanProperty);
        set => SetValue(PlanProperty, value);
    }

    public HardwareMemoryView()
    {
        InitializeComponent();
        ActualThemeVariantChanged += (_, _) => Rebuild();
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PlanProperty)
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
        Rows.Children.Clear();
        var inkSoft = Res("InkSoftBrush", "#6E6E73");
        var plan = Plan;
        if (plan is null || plan.Devices.Count == 0)
        {
            Verdict.Text = "Выберите модель и runtime, чтобы увидеть раскладку памяти.";
            Verdict.Foreground = inkSoft;
            return;
        }

        var track = Res("SurfaceAltBrush", "#EEEFF3");
        var used = Res("InkSoftBrush", "#9A9AA0");
        var model = Res("AccentBrush", "#F2750A");

        foreach (var device in plan.Devices)
        {
            Rows.Children.Add(BuildDeviceRow(device, track, used, model, inkSoft));
        }

        if (plan.OverflowGb > 0.01)
        {
            Verdict.Text =
                $"⚠ Не помещается в память: {plan.OverflowGb:0.0} ГБ. Будет очень медленно или не запустится — " +
                "уменьшите контекст, возьмите модель меньше или более лёгкий KV-кэш.";
            Verdict.Foreground = OverflowBrush;
        }
        else
        {
            Verdict.Text = $"✓ Модель помещается в память ({plan.TotalModelGb:0.0} ГБ).";
            Verdict.Foreground = OkBrush;
        }
    }

    private Control BuildDeviceRow(DeviceMemoryRow device, IBrush track, IBrush used, IBrush model, IBrush inkSoft)
    {
        var capacity = Math.Max(device.CapacityGb, 0.001);
        var usedFraction = Math.Clamp(device.BaseUsedGb / capacity, 0.0, 1.0);
        var remaining = 1.0 - usedFraction;
        var modelFraction = Math.Clamp(device.ModelGb / capacity, 0.0, remaining);
        var freeFraction = Math.Max(0.0, remaining - modelFraction);

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var icon = device.Kind == MemoryDeviceKind.Gpu ? "🎮" : "🧠";
        var name = new TextBlock
        {
            Text = $"{icon}  {device.Name}",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var detail = new TextBlock
        {
            Text = $"модель {device.ModelGb:0.0} ГБ · {device.BaseUsedGb + device.ModelGb:0.0} / {device.CapacityGb:0.0} ГБ",
            FontSize = 12,
            Foreground = inkSoft,
        };
        Grid.SetColumn(detail, 1);
        header.Children.Add(name);
        header.Children.Add(detail);

        var bar = new Grid
        {
            Height = 26,
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(usedFraction, GridUnitType.Star),
                new ColumnDefinition(modelFraction, GridUnitType.Star),
                new ColumnDefinition(Math.Max(freeFraction, 0.0001), GridUnitType.Star),
            },
        };
        bar.Children.Add(Segment(used, 0));
        bar.Children.Add(Segment(device.IsOverflowing ? OverflowBrush : model, 1));

        var trackBorder = new Border
        {
            Height = 26,
            CornerRadius = new CornerRadius(8),
            Background = track,
            ClipToBounds = true,
            Child = bar,
        };

        return new StackPanel
        {
            Spacing = 6,
            Orientation = Orientation.Vertical,
            Children = { header, trackBorder },
        };
    }

    private static Border Segment(IBrush brush, int column)
    {
        var segment = new Border { Background = brush };
        Grid.SetColumn(segment, column);
        return segment;
    }
}
