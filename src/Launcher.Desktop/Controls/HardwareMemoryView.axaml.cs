using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Launcher.Runtimes.Memory;

namespace Launcher.Desktop.Controls;

/// <summary>
/// Рисует наглядную диаграмму: как модель ложится в память каждой видеокарты и системной RAM.
/// Серый сегмент — уже занято, оранжевый — наша модель, красный — перегруз.
/// </summary>
public partial class HardwareMemoryView : UserControl
{
    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.Parse("#EFE3D2"));
    private static readonly IBrush UsedBrush = new SolidColorBrush(Color.Parse("#B9A68C"));
    private static readonly IBrush ModelBrush = new SolidColorBrush(Color.Parse("#E86F16"));
    private static readonly IBrush OverflowBrush = new SolidColorBrush(Color.Parse("#D64B3F"));
    private static readonly IBrush InkSoftBrush = new SolidColorBrush(Color.Parse("#76614C"));
    private static readonly IBrush OkBrush = new SolidColorBrush(Color.Parse("#2E7D32"));

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

    private void Rebuild()
    {
        Rows.Children.Clear();
        var plan = Plan;
        if (plan is null || plan.Devices.Count == 0)
        {
            Verdict.Text = "Выберите модель и runtime, чтобы увидеть раскладку памяти.";
            Verdict.Foreground = InkSoftBrush;
            return;
        }

        foreach (var device in plan.Devices)
        {
            Rows.Children.Add(BuildDeviceRow(device));
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

    private static Control BuildDeviceRow(DeviceMemoryRow device)
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
            Foreground = InkSoftBrush,
        };
        Grid.SetColumn(detail, 1);
        header.Children.Add(name);
        header.Children.Add(detail);

        var bar = new Grid
        {
            Height = 24,
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(usedFraction, GridUnitType.Star),
                new ColumnDefinition(modelFraction, GridUnitType.Star),
                new ColumnDefinition(Math.Max(freeFraction, 0.0001), GridUnitType.Star),
            },
        };
        bar.Children.Add(Segment(UsedBrush, 0));
        bar.Children.Add(Segment(device.IsOverflowing ? OverflowBrush : ModelBrush, 1));

        var track = new Border
        {
            Height = 24,
            CornerRadius = new CornerRadius(6),
            Background = TrackBrush,
            ClipToBounds = true,
            Child = bar,
        };

        return new StackPanel
        {
            Spacing = 4,
            Orientation = Orientation.Vertical,
            Children = { header, track },
        };
    }

    private static Border Segment(IBrush brush, int column)
    {
        var segment = new Border { Background = brush };
        Grid.SetColumn(segment, column);
        return segment;
    }
}
