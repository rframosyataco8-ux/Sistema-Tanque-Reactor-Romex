using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SistemaTanqueReactor.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Pulso neón en barras laterales de KPI (equivalente a animación CSS)
        StartPulse(Glow1, 0.0);
        StartPulse(Glow2, 0.25);
        StartPulse(Glow3, 0.5);
        StartPulse(Glow4, 0.75);
        StartPulse(StatusDot, 0.1);
    }

    private static void StartPulse(UIElement? element, double beginSeconds)
    {
        if (element == null) return;

        var anim = new DoubleAnimation
        {
            From = 0.4,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(1.1),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(beginSeconds),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        element.BeginAnimation(UIElement.OpacityProperty, anim);
    }
}
