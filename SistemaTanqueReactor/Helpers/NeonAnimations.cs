using System.Windows;
using System.Windows.Media.Animation;

namespace SistemaTanqueReactor.Helpers;

/// <summary>
/// Animaciones tipo CSS neón para WPF (pulso, fade-in, glow).
/// </summary>
public static class NeonAnimations
{
    public static void Pulse(UIElement element, double from = 0.4, double to = 1.0, double seconds = 1.1, double delay = 0)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromSeconds(seconds),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(delay),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        element.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public static void FadeIn(UIElement element, double seconds = 0.35)
    {
        element.Opacity = 0;
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(seconds))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public static void Flash(UIElement element, double seconds = 0.25)
    {
        var anim = new DoubleAnimation(0.3, 1, TimeSpan.FromSeconds(seconds))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(UIElement.OpacityProperty, anim);
    }
}
