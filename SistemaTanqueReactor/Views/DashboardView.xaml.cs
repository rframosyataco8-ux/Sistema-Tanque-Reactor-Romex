using System.Windows;
using System.Windows.Controls;
using SistemaTanqueReactor.Helpers;

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
        NeonAnimations.FadeIn(this, 0.4);
        NeonAnimations.Pulse(Glow1, delay: 0.0);
        NeonAnimations.Pulse(Glow2, delay: 0.25);
        NeonAnimations.Pulse(Glow3, delay: 0.5);
        NeonAnimations.Pulse(Glow4, delay: 0.75);
        NeonAnimations.Pulse(StatusDot, delay: 0.1);
    }
}
