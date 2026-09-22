using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class MainWindow : Window
{
    private bool _sidebarOpen = true;
    private const double SidebarOpenW = 250;
    private const double SidebarClosedW = 68;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetService(typeof(MainViewModel));
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await System.Threading.Tasks.Task.Delay(700);
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        anim.Completed += (_, _) => SplashOverlay.Visibility = Visibility.Collapsed;
        SplashOverlay.BeginAnimation(OpacityProperty, anim);
    }

    private void BtnMenu_Click(object sender, RoutedEventArgs e)
    {
        _sidebarOpen = !_sidebarOpen;
        var target = _sidebarOpen ? SidebarOpenW : SidebarClosedW;

        var anim = new GridLengthAnimation
        {
            From = ColSidebar.Width,
            To = new GridLength(target),
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        ColSidebar.BeginAnimation(ColumnDefinition.WidthProperty, anim);

        // Solo textos del logo y labels de sección; los botones mantienen iconos visibles
        var vis = _sidebarOpen ? Visibility.Visible : Visibility.Collapsed;
        TxtLogoTitle.Visibility = vis;
        TxtLogoSub.Visibility = vis;
        LblMenu.Visibility = vis;
        LblOtros.Visibility = vis;
        SepConfig.Visibility = vis;
        FooterBox.Visibility = vis;

        // Textos de nav: ocultar al colapsar (quedan solo iconos)
        TxtDash.Visibility = vis;
        TxtNuevo.Visibility = vis;
        TxtReg.Visibility = vis;
        TxtHist.Visibility = vis;
        TxtOtros.Visibility = vis;
    }

    private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnMax_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}

public class GridLengthAnimation : AnimationTimeline
{
    public override Type TargetPropertyType => typeof(GridLength);

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }
    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

    public IEasingFunction? EasingFunction { get; set; }

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        double fromVal = From.Value;
        double toVal = To.Value;
        double progress = animationClock.CurrentProgress ?? 0;
        if (EasingFunction != null)
            progress = EasingFunction.Ease(progress);
        return new GridLength(fromVal + (toVal - fromVal) * progress);
    }

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
}
